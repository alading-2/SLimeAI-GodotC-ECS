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

## 3. 新系统怎么接入

新系统唯一接入方式：

- `[ModuleInitializer]`
- `SystemRegistry.Register(new SystemDescriptor(...))`

### 3.1 Node 场景系统

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

### 3.2 Node 脚本系统

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

### 3.3 纯服务系统

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

### 3.4 复杂 Gameplay 系统示例

下面这个例子比基础模板更接近真实业务：

- 依赖 `TimerManager`、`TargetingManager`、`DamageService`
- 挂到 `GameplayHost/Wave/Spawner`
- 只在“局内进行中且未暂停”时运行
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
        RunCondition = new SystemRunCondition
        {
            AllowedAppPhases = [AppPhase.InSession],
            AllowedSessionPhases = [SessionPhase.Playing],
            AllowedExecutionPhases = [ExecutionPhase.Running],
            BlockedOverlayPhases = [OverlayPhase.PauseMenu]
        },
        Factory = static () => ResourceManagement
            .Load<PackedScene>(nameof(EliteSpawnSystem), ResourceCategory.System)
            .Instantiate()
    });
}
```

这类系统适合回答的问题是：

- “它属于哪个生命周期域？”
- “它依赖哪些基础设施先就绪？”
- “项目状态切到暂停 / 菜单时，它到底要不要继续跑？”
- “它应该挂到哪个 Host 路径，方便调试树结构？”

### 3.5 Debug 桥接系统示例

调试系统通常不是主玩法逻辑，但它又必须接入正式系统树，下面这个例子用来说明 `Debug` 生命周期的价值：

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
            BlockedExecutionPhases = [ExecutionPhase.Paused]
        },
        Factory = static () => new WaveDebugPanelRuntime()
    });
}
```

这个例子强调的是：

- 调试入口也应该走 `SystemRegistry + SystemManager`
- 调试系统和 Gameplay 系统可以共享依赖链，但生命周期域不同
- 运行条件要声明在描述符里，而不是散落到 `_Ready()` / `_Process()`

## 4. Lifetime 与 RunCondition

- `Persistent`：跨流程常驻基础设施
- `Gameplay`：局内主玩法系统
- `Overlay`：覆盖层 / 菜单
- `Debug`：调试系统
- `Test`：运行时测试专用系统

当前正式分层：

- `UIManager`、`TimerManager`、`ObjectPoolInit`、`DamageService`、`RecoverySystem`、`TargetingManager`、`DamageNumberSystem`：`Persistent`
- `SpawnSystem`、`DamageStatisticsSystem`：`Gameplay`
- `MouseSelectionSystem`、`TestSystem`：`Debug`

`SystemRunCondition` 负责声明“什么时候能跑”，而不是把状态判断散落到系统内部。

## 5. ParentPath 语义

`ParentPath` 表示：

> 相对 `SystemLifetime` 对应 Host 的路径

例如：

- `SystemLifetime.Debug + ParentPath=""` -> `DebugHost/MySystem`
- `SystemLifetime.Debug + ParentPath="Panels/Runtime"` -> `DebugHost/Panels/Runtime/MySystem`

## 6. 测试与调试注意事项

- `MainTest` 这类测试入口不应再“等一帧”赌启动时序
- 需要系统树时，显式等 `BootstrapCompleted`
- 老的 `TestDataRegister` 已删除，测试数据定义应直接走新的 `DataKey_* / DataMeta` 体系
- `SystemRegistry` 遇到空描述符或重复 `SystemId` 时，当前约定是记录 `Log.Error` 并忽略本次注册，避免在模块初始化阶段滥用 `throw`
