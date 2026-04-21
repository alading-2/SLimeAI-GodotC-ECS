# System Core 使用说明

## 0. 开发者从哪里开始看

如果你是第一次接这套 System Core，建议按下面顺序看：

1. 先看 `Docs/框架/ECS/System/Core/系统与状态分层总览.md`
2. 再看 `Docs/框架/ECS/System/Core/其他/系统生命周期与项目状态设计.md`
3. 接着回来看本文，建立“怎么接入新系统”的源码级心智模型
4. 最后按目的跳源码：
   - 想看启动链路：`SystemManager.cs`
   - 想看注册约束：`SystemRegistry.cs`
   - 想看元数据长什么样：`SystemDescriptor.cs`
   - 想看状态门禁：`Lifecycle/SystemRunCondition.cs`

推荐这样看的原因是：

- 总览文档先帮你分清系统治理、项目状态、实体状态、AI 决策四层边界。
- 正式设计文档回答“为什么现在是这个模型”。
- 本文才回答“我现在要新增一个系统，具体怎么写”。

## 1. 启动模型

`SystemManager` 现在是项目唯一的 Godot autoload。

启动顺序固定为：

1. Godot 先实例化 `SystemManager`
2. `SystemManager._EnterTree()` 初始化 `ParentManager`
3. `SystemManager._Ready()` 创建 Host 并启动全部已注册系统
4. 主场景随后进入 `_Ready()`

这意味着：

- 不再需要 `AutoLoad`
- 不再需要 deferred 挂树桥接
- 主场景默认可以假定基础系统已经就绪

如果某个测试或调试入口必须显式等待系统树完全启动，统一等待：

- `SystemManager.Instance?.IsBootstrapped`
- `SystemManager.BootstrapCompleted`

## 2. 目录结构

`Src/ECS/Base/System/Core/` 按职责拆成 4 层：

- 根目录：核心入口
  - `SystemManager.cs`
  - `SystemRegistry.cs`
  - `SystemDescriptor.cs`
- `Lifecycle/`：系统生命周期协议与元数据
  - `ISystemRuntime.cs`
  - `SystemLifetime.cs`
  - `SystemKind.cs`
  - `SystemRunCondition.cs`
  - `SystemRegistrationContext.cs`
- `State/`：项目级运行状态
  - `ProjectStateService.cs`
  - `ProjectStateSnapshot.cs`
  - `ProjectStateChangedEventArgs.cs`
  - `Phase/*`
- `Internal/`：管理器内部运行时结构
  - `ManagedSystemEntry.cs`

## 3. SystemKind：三种系统形态怎么选

注册系统时第一个要决定的就是 `SystemKind`——你的系统本质上是个什么东西？

| SystemKind | 实例类型 | 需要挂树？ | 启停方式 | 什么时候用 |
| --- | --- | --- | --- | --- |
| `NodeScene` | Node（来自 .tscn） | ✅ 挂到 Host | `ProcessMode` + `ISystemRuntime` 钩子 | 有场景骨架、需要子节点树 |
| `NodeScript` | Node（纯代码 new） | ✅ 挂到 Host | `ProcessMode` + `ISystemRuntime` 钩子 | 无 .tscn 但需要 `_Process` 帧驱动 |
| `PureService` | 纯 C# 对象 | ❌ 不挂树 | 仅 `ISystemRuntime` 钩子 | 纯逻辑服务，无帧驱动需求 |

**关键区别**：`NodeScene` / `NodeScript` 挂树后，`SystemManager` 通过切 `ProcessMode`（Inherit/Disabled）控制帧调度；`PureService` 不挂树，没有 `ProcessMode` 可切，启停完全靠 `ISystemRuntime.OnSystemEnabled/OnSystemDisabled` 钩子手动订阅/退订。

## 4. 新系统怎么接入

新系统唯一接入方式：`[ModuleInitializer]` + `SystemRegistry.Register(new SystemDescriptor(...))`

### 4.1 Node 场景系统

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(new SystemDescriptor(nameof(MySystem), SystemKind.NodeScene, SystemLifetime.Persistent)
    {
        Dependencies = new[] { nameof(TimerManager) },
        ParentPath = "Gameplay/Utility",
        Factory = static () => ResourceManagement
            .Load<PackedScene>(nameof(MySystem), ResourceCategory.System)
            .Instantiate()
    });
}
```

### 4.2 Node 脚本系统

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(new SystemDescriptor(nameof(MyRuntimeBridge), SystemKind.NodeScript, SystemLifetime.Persistent)
    {
        Factory = static () => new MyRuntimeBridge()
    });
}
```

### 4.3 纯服务系统

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(new SystemDescriptor(nameof(MyService), SystemKind.PureService, SystemLifetime.Persistent)
    {
        Factory = static () => new MyServiceRuntime()
    });
}
```

### 4.4 复杂 Gameplay 系统示例

下面这个例子比基础模板更接近真实业务：

- 依赖 `TimerManager`、`TargetingManager`、`DamageService`
- 挂到 `GameplayHost/Wave/Spawner`
- 只在"局内进行中且未暂停"时运行
- 创建失败时交给 `SystemManager` 记录日志并跳过，不用在注册阶段滥用 `throw`

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(new SystemDescriptor(nameof(EliteSpawnSystem), SystemKind.NodeScene, SystemLifetime.Gameplay)
    {
        Dependencies = new[]
        {
            nameof(TimerManager),
            nameof(TargetingManager),
            nameof(DamageService)
        },
        ParentPath = "Wave/Spawner",
        RunCondition = SystemRunCondition.GameplayRunning(),
        Factory = static () => ResourceManagement
            .Load<PackedScene>(nameof(EliteSpawnSystem), ResourceCategory.System)
            .Instantiate()
    });
}
```

### 4.5 Debug 桥接系统示例

调试系统通常不是主玩法逻辑，但它又必须接入正式系统树。这个例子说明 `Debug` 生命周期和自定义 `RunCondition` 的用法：

```csharp
[ModuleInitializer]
public static void Initialize()
{
    SystemRegistry.Register(new SystemDescriptor(nameof(WaveDebugPanelRuntime), SystemKind.NodeScript, SystemLifetime.Debug)
    {
        Dependencies = new[]
        {
            nameof(TestSystem),
            nameof(TargetingManager)
        },
        ParentPath = "Panels/Wave",
        RunCondition = new SystemRunCondition
        {
            AllowedAppPhases = [AppPhase.InSession],
            AllowedExecutionPhases = [ExecutionPhase.Running, ExecutionPhase.Paused]
        },
        Factory = static () => new WaveDebugPanelRuntime()
    });
}
```

关键点：
- 调试入口也应该走 `SystemRegistry + SystemManager`
- 调试系统和 Gameplay 系统可以共享依赖链，但生命周期域不同
- 运行条件要声明在描述符里，而不是散落到 `_Ready()` / `_Process()`

## 5. Lifetime、RunCondition 与 shouldRun

### 5.1 SystemLifetime：系统属于哪个分组

| Lifetime | 含义 | 挂到哪个 Host | 典型系统 |
| --- | --- | --- | --- |
| `Persistent` | 跨流程常驻基础设施 | `PersistentHost` | `TimerManager`、`ObjectPoolInit`、`DamageService` |
| `Gameplay` | 局内主玩法系统 | `GameplayHost` | `SpawnSystem`、`DamageStatisticsSystem` |
| `Overlay` | 覆盖层 / 菜单 | `OverlayHost` | `PauseMenuSystem` |
| `Debug` | 调试系统 | `DebugHost` | `MouseSelectionSystem`、`TestSystem` |
| `Test` | 运行时测试专用 | `TestHost` | — |

Lifetime 只决定系统挂到哪个 Host 容器下，**不直接决定系统是否运行**。系统是否运行由下面的 shouldRun 公式裁决。

### 5.2 shouldRun 公式：系统到底跑不跑

```
shouldRun = IsEnabled（人工开关） && IsStateAllowed（Phase 条件裁决）
```

| 要素 | 谁控制 | 含义 |
| --- | --- | --- |
| `IsEnabled` | 外部调用 `EnableSystem/DisableSystem` | 人工开关：强制启用或禁用某个系统 |
| `IsStateAllowed` | `RunCondition.Evaluate(Snapshot)` | 状态门禁：当前 Phase 是否匹配该系统的准入规则 |
| `shouldRun` | `SystemManager.ApplyEntryState` | 两者 AND 的结果，决定系统是否真正运行 |

**SystemRunCondition 只回答"Phase 条件是否允许"，不等于"系统真的在跑"。** 即使 Phase 条件通过，只要 `IsEnabled=false`，系统也不会运行。

### 5.3 SystemRunCondition 实战推演

以 TestSystem 为例，它想表达"局内可用，暂停也不停，只挡过场"：

```csharp
new SystemRunCondition
{
    AllowedAppPhases = [AppPhase.InSession],                              // 只在局内
    AllowedExecutionPhases = [ExecutionPhase.Running, ExecutionPhase.Paused], // 暂停也行
    BlockedOverlayPhases = [OverlayPhase.Cutscene],                       // 过场挡住
}
```

规则：**Allowed 为空 = 不限制，非空 = 当前 Phase 必须在里面；Blocked 命中 = 禁止运行。**

| 游戏状态 | AppPhase | ExecutionPhase | OverlayPhase | AllowedApp? | AllowedExec? | BlockedOverlay? | IsStateAllowed |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 主菜单 | FrontEnd | Running | None | ❌ | — | — | **false** |
| 局内打牌 | InSession | Running | None | ✅ | ✅ | ✅ | **true** |
| 局内暂停 | InSession | Paused | PauseMenu | ✅ | ✅ | ✅ PauseMenu 不在 Blocked | **true** |
| 局内过场 | InSession | Blocked | Cutscene | ✅ | ❌ | ❌ Cutscene 在 Blocked | **false** |

再比如 `SpawnSystem`，用预设工厂方法 `GameplayRunning()` 等价于：

```csharp
new SystemRunCondition
{
    AllowedAppPhases = [AppPhase.InSession],
    AllowedSessionPhases = [SessionPhase.Playing],
    AllowedExecutionPhases = [ExecutionPhase.Running]
}
```

只有 `InSession + Playing + Running` 三维同时满足时才允许运行——暂停、结算、菜单全停。

### 5.4 不设 RunCondition 会怎样

`SystemDescriptor` 的默认值是 `SystemRunCondition.Always`（所有集合为空），意味着**任意 Phase 下都通过 Evaluate**。

如果某个系统不需要 Phase 过滤（比如 `TimerManager` 这种跨流程基础设施），直接不设 `RunCondition` 即可。

### 5.5 ProjectStateBridge：谁负责切换 ProjectState

`SystemRunCondition` 只负责"读"状态，不负责"写"状态。项目状态的切换由 `ProjectStateBridge` 负责——它把 `GameStart / GamePause / GameResume / GameOver` 这类流程事件统一映射到 `ProjectStateService` 的 Phase 切换。

## 6. ParentPath 语义

`ParentPath` 表示：**相对 `SystemLifetime` 对应 Host 的路径**。

例如：
- `SystemLifetime.Debug + ParentPath=""` -> `DebugHost/MySystem`
- `SystemLifetime.Debug + ParentPath="Panels/Runtime"` -> `DebugHost/Panels/Runtime/MySystem`

## 7. 测试与调试注意事项

- `MainTest` 这类测试入口不应再"等一帧"赌启动时序
- 需要系统树时，显式等 `BootstrapCompleted`
- 老的 `TestDataRegister` 已删除，测试数据定义应直接走新的 `DataKey_* / DataMeta` 体系
- `SystemRegistry` 遇到空描述符或重复 `SystemId` 时，当前约定是记录 `Log.Error` 并忽略本次注册，避免在模块初始化阶段滥用 `throw`
