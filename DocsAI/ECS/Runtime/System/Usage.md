<!-- migrated-from: Src/ECS/Base/System/Core/README.md -->

> 迁移来源：`Src/ECS/Base/System/Core/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# System Core 使用说明

> 2026-05 更新：系统管理已切到“代码只注册 `SystemId + Factory`，DataOS snapshot records 声明装载、挂载分组、标签和运行条件”的数据驱动模型。旧版代码侧生命周期/形态枚举、Profile 装配表和四维阶段模型已退出正式流程。

> 2026-06-03 更新：System AI-first contract hardening 已进入 SDD-0029。后续修改 System Core 时必须同步 `DocsAI/ECS/Runtime/System/README.md`、本文、`SystemManifest.md` 和 SDD-0029 进度；首切片只补 manifest / preflight / diagnostics / trace / artifact，不做 typed `SystemId` hard cutover。

## 0. 开发者从哪里开始看

如果你是第一次接这套 System Core，建议按下面顺序看：

1. 先看 `DocsAI/ECS/Runtime/System/Usage.md`
2. 再看 `DocsAI/ECS/Runtime/System/Concept.md`（核心概念）
3. 接着回来看本文，建立“怎么接入新系统”的源码级心智模型
4. 最后按目的跳源码：
   - 想看启动链路：`SystemManager.cs`
   - 想看注册约束：`SystemRegistry.cs`
   - 想看代码注册：`SystemDescriptor.cs`
   - 想看系统配置：`Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config`
   - 想看状态门禁：`Lifecycle/SystemRunCondition.cs`
   - 想看启动预设：`Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.preset`
   - 想看系统清单：`DocsAI/ECS/Runtime/System/SystemManifest.md`
   - 想看 preflight：`Src/ECS/Runtime/System/Preflight/`
   - 想看 diagnostics / reason code / trace：`Src/ECS/Runtime/System/Diagnostics/`
   - 想看 AI-first contract 执行计划：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md`

推荐这样看的原因是：

- 总览文档先帮你分清系统治理、项目状态、实体状态、AI 决策四层边界。
- 历史设计文档回答“为什么从旧模型迁移到这个模型”。
- 本文才回答“我现在要新增一个系统，具体怎么写”。

## 1. 启动模型

`SystemManager` 现在是项目唯一的 Godot autoload。

启动顺序固定为：

1. Godot 先实例化 `SystemManager`
2. `SystemManager._EnterTree()` 初始化 `RuntimeMountService`
3. `SystemManager._Ready()` 加载 `system.config` / `system.preset` snapshot records，创建 Host 并启动被配置选中的系统
4. 主场景随后进入 `_Ready()`

这意味着：

- 不再需要每个系统自己挂 Godot AutoLoad
- 不再需要 deferred 挂树桥接
- 主场景默认可以假定基础系统已经就绪
- 启动日志必须能看到 `SystemManager _Ready 开始启动系统框架`、预设计算出的系统数、已注册描述符数和最终系统状态报告
- `ProjectState` 每次切换后都会重新输出系统状态报告；判断系统是否真的未运行，应以状态切换后的报告为准
- 单个系统 Factory 或 `OnRegistered` 抛异常时，`SystemManager` 会记录具体系统和堆栈并继续尝试装载其他系统

如果某个测试或调试入口必须显式等待系统树完全启动，统一等待：

- `SystemManager.Instance?.IsBootstrapped`
- `SystemManager.BootstrapCompleted`

## 2. 目录结构

`Src/ECS/Runtime/System/` 按职责拆成 4 层：

- 根目录：核心入口
  - `SystemManager.cs`
  - `SystemRegistry.cs`
  - `SystemDescriptor.cs`
- `Lifecycle/`：系统生命周期协议与元数据
  - `ISystem.cs`
  - `ISystemCommandHandler.cs`
  - `SystemExecuteResult.cs`
  - `SystemRunCondition.cs`
  - `SystemRegistrationContext.cs`
- `Preflight/`：AI-first 启动前检查
  - `SystemPreflight.cs`
  - `SystemPreflightReport.cs`
  - `SystemPreflightIssue.cs`
- `Diagnostics/`：AI-first 诊断合同
  - `SystemDiagnosticsSnapshot.cs`
  - `SystemManager_Diagnostics.cs`
  - `SystemBlockedReasonCode.cs`
  - `SystemLifecycleTrace.cs`
- `State/`：项目级运行状态
  - `ProjectStateService.cs`
  - `ProjectStateSnapshot.cs`
  - `ProjectStateChangedEventArgs.cs`
  - `Phase/*`
- `Internal/`：管理器内部运行时结构
  - `ManagedSystemEntry.cs`

系统配置数据来自：

- `Data/DataOS/Snapshots/runtime_snapshot.json`

## 3. System Config：系统设置只在 DataOS 里写

代码注册只回答两个问题：这个系统叫什么、怎么创建实例。

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(nameof(MySystem), static () => ResourceManagement
        .Load<PackedScene>(nameof(MySystem), ResourceCategory.System)
        .Instantiate());
}
```

其余系统级设置统一写在 DataOS authoring，并通过 `system.config` snapshot records 投影；旧资源配置文件不进入运行时。

| 字段 | 语义 |
| --- | --- |
| `SystemId` | 系统唯一 Id，必须与注册字符串和资源文件名一致 |
| `MountGroup` | 挂载 Host，单选：`Base / Gameplay / Combat / UI / Debug / Test / Else` |
| `Tags` | 逻辑分类，多选：`Core / Gameplay / Combat / UI / Debug / Test / Roguelike / Runtime` |
| `Required` | 必需系统，永远装载，Preset 不能禁用 |
| `AutoLoad` | 无激活 Preset 时是否默认装载 |
| `StartEnabled` | 纳入管理后的人工开关默认值 |
| `Priority` | 装载排序，数值越小越早 |
| `Dependencies` | 依赖系统 Id 列表 |
| `AllowedFlowStates` | 允许的游戏流程状态，`None` 表示不限制；优先选 `Gameplay / Session / Runtime` 等预设 |
| `RequiredOverlays` | 必须存在的覆盖层，`None` 表示不要求；可选 `InteractiveUi / Blocking` 等预设 |
| `BlockedOverlays` | 禁止存在的覆盖层，`None` 表示不屏蔽；局内常用 `Blocking` |
| `AllowedSimulationStates` | 允许的模拟状态，`None` 表示不限制；暂停菜单常用 `Any` |

关键边界：不要在 `SystemDescriptor` 里恢复系统形态、生命周期、父节点路径、运行条件、默认装载或默认启用字段。这些字段已经迁移到 `SystemData`，否则系统级解耦会重新变成代码和配置双写。

## 4. ProjectState：三域状态模型

项目状态快照现在只保留三个真实运行域：

| 状态域 | 回答的问题 | 值 |
| --- | --- | --- |
| `GameFlowState` | 游戏流程走到哪里 | `None / Boot / FrontEnd / SessionPreparing / SessionPlaying / SessionResolving / SessionEnded / ShuttingDown`，并提供 `FrontEndFlow / Session / ActiveSession / Gameplay / Runtime / All` 预设 |
| `OverlayFlags` | 当前有哪些覆盖层 | `None / PauseMenu / ModalUi / Cutscene`，并提供 `InteractiveUi / Blocking` 预设 |
| `SimulationState` | 模拟是否推进 | `None / Running / Suspended`，并提供 `Any` 预设 |

旧版应用阶段、局内阶段、覆盖层阶段和执行阶段已合并为这三域：

- `GameFlowState` 承担应用阶段和局内阶段，配置侧可按位组合，运行时当前值仍只允许单一流程状态。
- `OverlayFlags` 是 Flags，允许暂停菜单、模态窗口、过场这类覆盖层并存。
- `SimulationState` 只表达模拟推进或挂起，配置侧可按位组合，不再区分抽象的 `Paused / Blocked`。

`ProjectStateService` 使用实例级 C# event 广播状态切换：

- `BeforeStateChanged`
- `StateChanged`
- `AfterStateChanged`

这里不要改成 `GlobalEventBus`。项目状态是 `SystemManager.ProjectState` 这一份实例状态源，实例事件可以保证只有持有该状态源的 `SystemManager` 响应切换；临时测试服务或局部工具服务调用 `BeginGameplaySession()` 时，不应污染全局运行时系统门禁。需要让系统观察项目状态时，直接实现 `ISystem.OnProjectStateChanged`，由 `SystemManager` 在收到 `StateChanged` 后统一分发。

## 5. RunCondition 与 shouldRun

`SystemRunCondition` 从 `SystemData` 生成，只负责判断当前 `ProjectStateSnapshot` 是否允许系统运行。`None` 规则固定如下：

- `AllowedFlowStates = None`：不限制流程状态。
- `RequiredOverlays = None`：不要求任何覆盖层。
- `BlockedOverlays = None`：不屏蔽任何覆盖层。
- `AllowedSimulationStates = None`：不限制模拟状态。

Gameplay 系统常用配置：

```csharp
new SystemRunCondition
{
    AllowedFlowStates = GameFlowState.Gameplay,
    BlockedOverlays = OverlayFlags.Blocking,
    AllowedSimulationStates = SimulationState.Running
}
```

最终裁决公式：

```text
shouldRun = IsEnabled && IsStateAllowed
```

其中 `IsEnabled` 是系统人工开关，由 `SystemData.StartEnabled` 初始化，运行时可由 `EnableSystem/DisableSystem` 修改；`IsStateAllowed` 是 `SystemRunCondition` 对三域状态的判断结果。系统是否被创建则由 `Required / AutoLoad / SystemPresetData / Dependencies` 在装载阶段决定，不再混进运行态公式。

外部命令也使用同一套运行态裁决。业务代码不要直接调用系统单例执行业务，应通过 `SystemManager.Execute<TSystem, TRequest, TResult>(request)` 进入系统。命令是否能在暂停、前台、局内等状态下执行，只看该系统在 `SystemData` 中的三域运行条件；同一个系统内所有命令共享这套条件。如果某个命令需要不同运行条件，应拆出新的系统，而不是给命令单独增加策略。

`SystemRunCondition.GetBlockedReasonDetail(snapshot)` 返回稳定 `SystemBlockedReasonCode` 和中文 message。自动测试、BDD 和 diagnostics 应优先断言 code，例如 `FlowStateMismatch`、`BlockedOverlay`、`SimulationStateMismatch`；中文 message 只作为 UI / 日志说明。

## 6. SystemPresetData：只做批量选择

`SystemPresetData` 不是高级运行配置，也不承载运行条件。它只解决“本模式要装哪些系统”：

- `Required=true` 的系统永远装载。
- 无激活 Preset 时，装载 `AutoLoad=true` 的系统。
- 有激活 Preset 时，装载 `EnabledTags` 命中的系统和 `EnabledSystemIds` 显式列出的系统。
- `DisabledSystemIds` 可以排除系统，但不能排除 `Required=true` 的系统。

当前 `Default` 预设不会把 `Debug / Test` 标签整体加入默认标签集合；它通过 `EnabledSystemIds` 显式加载 `TestSystem` 和 `MouseSelectionSystem`，避免未来新增的调试系统被意外常驻。

如果要配置系统运行阶段，改对应 `SystemData` 的三域运行条件，不要新增 `SystemRunPreset` 之类的中间层。

## 6.5 生命周期回调

`ISystem` 回调语义固定：

- `OnRegistered/OnUnRegistered`：实例被 `SystemManager` 接管或释放。
- `OnStarted/OnStopped`：实际运行态切换，适合订阅/退订事件、启动/停止驱动逻辑。
- `OnProjectStateChanged`：接收 `ProjectStateChangedEventArgs`，观察状态变化，不等价于启停；业务系统不直接订阅 `ProjectStateService.StateChanged`。
- `GetSystemRuntimeInfo`：只补充系统自定义统计；加载、启用、运行态等基础信息由 `SystemManager` 统一填充。

## 7. 测试与调试注意事项

- `MainTest` 这类测试入口不应再"等一帧"赌启动时序
- 需要系统树时，显式等 `BootstrapCompleted`
- 局内系统不能只依赖 `GameStart` 事件启动关键运行态；如果系统在 `FrontEnd` 被运行条件挡住，应该在 `OnStarted(SessionPlaying)` 中补齐进入局内后的启动逻辑
- 老的 `TestDataRegister` 已删除，测试数据定义应直接走 descriptor 生成的 typed handle 体系，不再回到手写 C# 元数据。
- `SystemRegistry` 遇到空描述符或重复 `SystemId` 时，当前约定是记录 `Log.Error` 并忽略本次注册，避免在模块初始化阶段滥用 `throw`

## 8. SystemPreflight

`SystemPreflight.Run()` 是只读检查，不会写回 DataOS、registry 或 runtime。它当前检查：

| Rule | 级别 | 说明 |
| --- | --- | --- |
| `SYS-PF-001` | error | `system.config` 必须有非空 `SystemId` 和 `Description` |
| `SYS-PF-002` | error | Required system 必须有 descriptor |
| `SYS-PF-003` | error | active preset `EnabledSystemIds` 必须有 config |
| `SYS-PF-004` | error | active preset 不能禁用 Required system |
| `SYS-PF-005` | error | dependency 必须存在 config 和 descriptor |
| `SYS-PF-006` | error | dependency graph 不能有 cycle |
| `SYS-PF-007` | warning/error | descriptor-only system 必须显式进入 test-only allow-list |
| `SYS-PF-010` | warning | Priority 冲突只警告，排序用 SystemId 稳定补充 |

当前 SystemCore 测试允许 `SystemCoreRuntimeTest.` 前缀作为 test-only descriptor。新增测试临时 descriptor 时，优先使用这个前缀或在 `SystemPreflightOptions` 显式 allow-list。

## 9. Diagnostics Snapshot

`SystemManager.GetDiagnosticsSnapshot()` 合并四类事实：

- DataOS `system.config` / `system.preset`
- `SystemRegistry` descriptor
- `SystemManager` runtime entries
- `ProjectStateService` 当前三域状态

snapshot 字段包括：

```text
schemaVersion
projectState
activePreset
configCount / registeredDescriptorCount / loadedCount / runningCount / blockedCount / disabledCount
entries[]
preflightIssues[]
recentTrace[]
```

每个 `entries[]` 条目包含 `systemId`、owner、sourcePath、configRecordId、registered、configured、loaded、enabled、stateAllowed、running、blockedReasonCode、blockedReason、mountGroup、tags、dependencies 和 customStats。

TestSystem 的 System Info 模块已经读取 `SystemDiagnosticsSnapshot`，再适配成原展示快照；添加、移除、启用、禁用仍调用 `SystemManager.TryAddSystem / TryRemoveSystem / TrySetSystemEnabled`，操作语义不变。

## 10. Lifecycle Trace 与 Artifact

`SystemLifecycleTrace` 是固定容量 ring buffer，记录：

- `BootstrapStarted / ConfigLoaded / PresetLoaded / BootstrapCompleted`
- `SystemAdded / SystemRemoved / SystemEnabled / SystemDisabled`
- `StateAllowedChanged / SystemStarted / SystemStopped`
- `CommandBlocked / CommandExecuted`

默认不长期写文件。SystemCore Godot 场景会把 diagnostics snapshot 输出到：

```text
.ai-temp/scene-tests/artifacts/system-core-diagnostics.json
```

场景标准答案见：

```text
Src/ECS/Runtime/System/Tests/SystemCore/README.md
```

运行入口：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果承载游戏 runner 或 Godot CLI 不可用，只能记录 blocker，不能声明 scene gate 通过。
