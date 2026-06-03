# System 现状证据与 AI-first 裁决

> 状态：current
> 更新：2026-06-03
> 目标：用 AI-first 视角复查旧 System Core 是否需要完善，并冻结后续优化方向。

## DeepThink

### Goal

本轮要解决的问题：

- 判断旧 ECS System 系统是否需要被 AI-first 重构。
- 明确现有 System Core 的可靠部分、真实缺口和不建议方向。
- 在 `design/8.System优化/` 下生成可恢复的共享设计包。

非目标：

- 本轮不修改 `Src/ECS/Runtime/System` 代码。
- 本轮不创建执行型 SDD，不切换 PRJ-0002 当前 SDD。
- 本轮不把 System Core 改成 Bevy / Flecs / Unity DOTS 式 scheduler。

### Context Read

本地事实源：

- `DocsAI/ECS框架与AIFirst方向决策.md`
- `DocsAI/ECS/Runtime/System/README.md`
- `DocsAI/ECS/Runtime/System/Concept.md`
- `DocsAI/ECS/Runtime/System/Usage.md`
- `DocsAI/ECS/Runtime/System/Concepts/系统与状态分层总览.md`
- `DocsAI/ECS/Runtime/System/Concepts/其他/系统生命周期三案设计.md`
- `DocsAI/ECS/Runtime/System/Concepts/其他/系统生命周期与项目状态设计.md`
- `DocsAI/ECS/Runtime/System/Concepts/其他/系统配置与预设重构方案.md`
- `Src/ECS/Runtime/System/**`
- `Src/ECS/Capabilities/TestSystem/System/System/SystemInfoService.cs`
- `Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config` / `system.preset`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`

本地参考资料：

- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/05-DefaultEcs-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/09-Unreal-GAS-文档对照报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`

外部资料：

- Context7：`/websites/rs_bevy_bevy`，查询 Bevy ECS systems、schedule、run condition、diagnostics。
- Bevy ECS docs：<https://docs.rs/bevy/latest/bevy/ecs/index.html>
- Bevy `run_if` / schedule config docs：<https://docs.rs/bevy/latest/bevy/ecs/prelude/trait.IntoScheduleConfigs.html>
- Unity Entities systems intro：<https://docs.unity.cn/Packages/com.unity.entities@1.0/manual/systems-intro.html>
- Flecs Systems manual：<https://www.flecs.dev/flecs/md_docs_2Systems.html>
- Godot Node docs：<https://docs.godotengine.org/en/stable/classes/class_node.html>
- Anthropic context engineering：<https://www.anthropic.com/engineering/effective-context-engineering-for-ai-agents>

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 本轮开始前 `git status --short` 已有大量既有修改、`.uid` 删除和未跟踪 `.ai-temp` / `__pycache__`；本设计只新增/修改 System 优化相关 SDD 设计文档和索引，不覆盖既有改动。

未读上下文：

- 未逐个打开所有 capability system 的实现，只用 `rg` 扫描注册和 `SystemManager.Execute` 调用点。
- 未运行 Godot 场景验证，因为本轮不改代码；验证计划写入 `03-调用点迁移与验证计划.md`。

### Problem Shape

现有 System Core 对人类开发已经比较清楚，但 AI 使用时仍有四类缺口：

1. **三源对齐缺口**：`system.config`、`SystemRegistry.Register`、运行时 `_entries` 之间没有独立 preflight artifact。AI 只能从启动日志或 TestSystem 面板推断配置是否齐。
2. **manifest 缺口**：DocsAI 没有一张 current 系统清单，列出 `SystemId`、owner、源码、配置 record、命令、运行条件、测试入口和风险。
3. **诊断结构缺口**：`BlockedReason` 和管理接口 message 是可读字符串，但不是稳定 reason code；AI 很难据此写 BDD 或自动分类失败。
4. **验证闭环缺口**：`SystemCoreRuntimeTest` 覆盖基础行为，但没有输出标准 JSON / artifact，无法像 DataOS 或 ObjectPool 验证那样被后续 agent 自动复盘。

隐藏假设：

- 当前系统数量仍小，14 条 `system.config` 与 14 个常规注册点可以人工核对；未来一旦 owner 增多，人工 grep 不够。
- `Priority` 当前主要用于装载排序，不等于完整 frame schedule。不要把它误读成 Bevy / Flecs 的执行阶段。
- Godot `ProcessMode.Disabled` 只能停 Node process；事件订阅、Timer、外部命令仍必须由 `OnStarted/OnStopped` 和 `SystemManager.Execute` 配合治理。

### Main Risks

| 风险 | 影响 | 判断 |
| --- | --- | --- |
| 直接重写 scheduler | 破坏现有 Godot Node 生命周期和已验证门禁 | 高风险，不建议 |
| 恢复代码侧生命周期元数据 | 让 DataOS config 与代码重复，AI 会遇到双事实源 | 高风险，不建议 |
| 继续只靠日志 | AI debug 依赖人工读日志，难以自动判断 | 中风险，需要补 diagnostics artifact |
| typed `SystemId` 过早 hard cutover | 影响 `nameof(...)` 注册、snapshot record、preset、依赖、TestSystem | 中高风险，需要单独确认 |
| 只写文档不建 gate | AI 会读懂但后续仍可能写错配置 | 中风险，后续执行应补 preflight |

### Options

#### 方案 A：保留现状，只补 README

做法：只把当前 System Core 入口说明写清楚。

优点：成本最低。

缺点：不能解决 AI 自动核对、失败分类和验证 artifact 问题。

结论：不足以满足 AI-first。

#### 方案 B：重写为显式 RuntimeSchedule

做法：引入 Bevy / Flecs 风格 schedule phase、system set、order graph、run condition、deferred command。

优点：理论上表达能力强，后续可接 RuntimeCommandBuffer。

缺点：当前核心问题不是 scheduler 表达力不足，而是 manifest、diagnostics 和验证缺口。直接重写会扩大影响面。

结论：不作为当前路线；只作为未来触发条件明确后的扩展。

#### 方案 C：保留 System Core，补 AI-first Contract Layer

做法：不改生命周期模型，新增或完善：

- `SystemManifest`：AI 可读系统清单。
- `SystemPreflight`：检查 config / registry / preset / dependencies / run condition。
- `SystemDiagnosticsSnapshot`：输出运行态 JSON。
- `SystemLifecycleTrace`：记录启动、启停、状态门禁、命令阻断。
- DocsAI / TestSystem / 场景验证同步使用同一诊断模型。

优点：小步、可验证、不破坏现有模型；最贴合 PRJ-0002 “保留旧 ECS 主线、按真实问题优化”的方向。

缺点：需要设计好 artifact schema，避免新建第二套事实源。

结论：推荐。

### Recommendation

推荐采用方案 C。

System Core 当前不需要整体重写。它已经具备 AI-first 需要的基础结构：

- 少入口：`SystemManager` 是唯一 autoload，`SystemRegistry` 是注册入口。
- 少事实源：代码注册只保留 `SystemId + Factory`，其余来自 DataOS snapshot。
- 解耦：运行条件由 `ProjectState + SystemRunCondition` 裁决，外部命令由 `SystemManager.Execute` 门禁。
- 可调试：`SystemRuntimeInfo` 和 TestSystem 已经能展示 config / registry / runtime 合并视图。

真正要补的是“AI 可独立判断”的证据层，而不是替换现有核心。

### Must Confirm

进入代码实施前必须确认：

1. 是否允许把首个执行型 SDD 限定为“无行为变化的 manifest / preflight / diagnostics”，暂不做 typed `SystemId` hard cutover。
2. diagnostics artifact 的落点是否使用 `.ai-temp/scene-tests/artifacts/`，并由 Godot scene / TestSystem 模块输出 JSON。

### Should Confirm

建议确认但可用默认值推进：

1. System manifest 是生成 Markdown + JSON，还是只生成 JSON 并让 DocsAI README 引用摘要。
2. `BlockedReason` 是否立即升级为 enum reason code，还是先在 diagnostics 中增加 `ReasonCategory` 并保留现有 message。
3. 是否把 `SystemInfoService` 提升到 Runtime System diagnostics，还是继续放在 TestSystem owner 内部调用 Runtime API。

### Defaults I Will Use

若用户后续只说“按建议执行”，默认采用：

- 不重写 SystemManager 生命周期。
- 不引入第三方 ECS。
- 首切片只做 manifest / preflight / diagnostics / tests。
- 保留 `nameof(MySystem)` 注册方式，typed `SystemId` 只作为后续设计项。
- diagnostics 输出 JSON schema version，文本日志只作为辅助。
- DocsAI `Runtime/System` 仍是 current 文档入口；SDD `design/8.System优化/` 是共享设计事实源。

### Not Recommended

- 不建议复制 Bevy/Flecs scheduler API。
- 不建议复制 Unity DOTS system group / world bootstrap。
- 不建议恢复 `SystemProfile` 或旧四维 phase。
- 不建议让 system 内部自己散写 `if paused return`。
- 不建议让 AI 通过 `SystemManager.Resolve<T>()` 绕过 owner command handler；业务命令优先走 `Execute<TSystem, TRequest, TResult>`。

### Artifact Updates

本轮写入：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/01-现状证据与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/02-目标架构与优化路线.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/03-调用点迁移与验证计划.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`

## 当前系统证据

### runtime snapshot 系统配置

当前 `system.config` records 共 14 个：

| SystemId | Group | Tags | Required | Priority | Dependencies |
| --- | --- | --- | --- | --- | --- |
| `ObjectPoolInit` | Base | Core, Runtime | true | 0 | - |
| `TimerManager` | Base | Core, Runtime | true | 1 | - |
| `ProjectStateBridge` | Base | Core, Runtime | true | 2 | - |
| `EntityManager` | Base | Core, Runtime | true | 5 | - |
| `DamageService` | Combat | Core, Combat, Runtime | false | 10 | - |
| `DamageStatisticsSystem` | Combat | Core, Combat, Runtime | false | 11 | `DamageService` |
| `RecoverySystem` | Combat | Core, Combat, Runtime | false | 12 | - |
| `SpawnSystem` | Gameplay | Gameplay, Runtime | false | 13 | - |
| `TargetingManagerRuntime` | Combat | Core, Combat, Runtime | false | 14 | - |
| `PauseMenuSystem` | UI | UI, Runtime | false | 20 | - |
| `UIManager` | UI | Core, UI, Runtime | false | 21 | - |
| `DamageNumberRuntimeBridge` | UI | Combat, UI, Runtime | false | 22 | - |
| `TestSystem` | Test | Debug, Test | false | 100 | - |
| `MouseSelectionSystem` | Debug | Debug, Test | false | 101 | - |

当前 `system.preset` 激活项：

| Preset | Active | EnabledTags | EnabledSystemIds |
| --- | --- | --- | --- |
| `Default` | true | Core, Gameplay, Combat, UI, Roguelike, Runtime | `TestSystem`, `MouseSelectionSystem` |

### 注册点扫描

常规注册点覆盖 14 个系统，分布在 Runtime、Capabilities、Tools 和 UI：

```text
DamageService
DamageStatisticsSystem
RecoverySystem
SpawnSystem
ProjectStateBridge
MouseSelectionSystem
TimerManager
PauseMenuSystem
UIManager
ObjectPoolInit
DamageNumberRuntimeBridge
TargetingManagerRuntime
EntityManager
TestSystem
```

这说明当前 `system.config` 与常规注册点数量对齐，但这种对齐目前是人工 grep 结论，不是可复用 gate。

### 现有测试基线

`SystemCoreRuntimeTest` 已覆盖：

- `ProjectStateService` 默认值和 helper。
- 实例级 `StateChanged` 不污染全局 `SystemManager`。
- phase preset。
- `SystemRunCondition` 和 `None` 规则。
- `SystemConfigService` / `SystemPresetService` 基础非空与启用集合。
- 核心系统描述符注册。
- Required 系统不可禁用/移除。
- 缺失系统管理接口失败信息。
- `SystemManager.Execute` 受运行态门禁约束。
- 重复 `SystemId` 保留首个 descriptor。

缺口是：这些测试验证了行为，但没有输出 AI 可恢复的 manifest / diagnostics artifact。

## 外部资料裁剪

### Bevy / Context7

Context7 拉取的 Bevy 文档显示，Bevy system 被加入 schedule 后执行，schedule 支持 `run_if`、system set、before/after 和 diagnostics 插件。这对 SlimeAI 的启发是：

- 运行条件应该是调度层事实，而不是散在系统内部。
- 诊断插件或等价观察面应成为框架默认能力。
- SlimeAI 不需要复制 Bevy 的 API；当前 `SystemRunCondition` 已覆盖最重要的 run condition 心智模型。

### Unity Entities

Unity Entities 的 system 文档强调 system lifecycle、system group、更新顺序和系统窗口诊断。对 SlimeAI 的启发是：

- 系统 owner、分组、排序和运行态应该能被调试面板展示。
- SlimeAI 当前 `SystemGroup + SystemTag + Priority + TestSystem SystemInfo` 已有雏形，应该补 preflight 和 artifact。

### Flecs

Flecs systems 把 query、callback、pipeline 和 module scope 结合。对 SlimeAI 的启发是：

- System / module scope 可以帮助 AI 路由 owner。
- Pipeline / query DSL 不应直接开放给当前 Godot C# runtime，否则会绕过 capability owner。

### Godot

Godot Node 生命周期和 `ProcessMode` 支持对 Node 的 process 行为做统一控制。对 SlimeAI 的启发是：

- `ProcessMode.Disabled` 是必要但不充分的暂停手段。
- 事件订阅、Timer、外部命令必须通过 `OnStarted/OnStopped` 和 `Execute` 协议收口。

### Anthropic context engineering

Context engineering 的核心启发是：AI agent 需要高信号、按需加载、可检索和可压缩的上下文，而不是靠一次性读完整代码库。对 SlimeAI System 的具体落点是：

- System manifest 是给 AI 的高信号入口。
- diagnostics artifact 是失败后可恢复上下文。
- preflight gate 是防止 AI 在缺少事实时继续实现的边界。

## DesignCritic

### Assumptions

- 当前 System Core 行为没有用户提出的具体 bug。
- 未来系统数量会增长，AI 不能长期靠 grep 人工核对。
- PRJ-0002 当前仍以“保留旧 ECS 主线，小步优化真实问题”为原则。

### Missing Context

- 未确认用户是否希望本轮后立即创建执行型 SDD。
- 未确认 typed `SystemId` 是否要作为 System 优化的一部分进入 hard cutover。
- 未确认 diagnostics JSON 的消费方是 TestSystem、scene runner，还是 SystemAgent hook。

### Design Defects Found

- `DocsAI/ECS/Runtime/System/Concepts/其他/系统生命周期与项目状态设计.md` 内仍引用旧路径和旧接口语义，属于历史文档漂移，需要在未来文档同步中标注得更清楚。
- `SystemInfoService` 已经能合并 config / registry / runtime，但它是 TestSystem 内部服务；Runtime System 没有稳定 diagnostics contract。
- `SystemConfigService` / `SystemPresetService` 初始化后没有明显 reset API，后续 tests 或 editor reload 需要谨慎处理，避免缓存导致 AI 误判。

### Better Options

更小、更安全的替代方案是先做 docs-only manifest。它能降低 AI 误路由，但仍不能在配置变更时自动失败。因此推荐 docs manifest 与 preflight 一起做，但首轮不触碰生命周期。

### Recommendation

先建 AI-first Contract Layer，再视运行证据决定是否扩展 scheduler。当前不做 System Core hard rewrite。
