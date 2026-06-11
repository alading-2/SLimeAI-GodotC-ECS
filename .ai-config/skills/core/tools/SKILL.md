---
name: tools
description: 修改 SlimeAI ECS Timer、Pool、ResourceManagement、Target 查询或通用 Runtime 工具时使用。
---

# Runtime Tools 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Tools/Timer/`
- `DocsAI/ECS/Tools/ObjectPool/`
- `DocsAI/ECS/Tools/TargetSelector/`
- `DocsAI/ECS/Tools/ResourceManagement/`
- `DocsAI/ECS/Tools/CommonUtilities/`
- `DocsAI/ECS/Runtime/Mount/`
- `DocsAI/ECS/Runtime/NodeLifecycle/`
- `.ai-config/skills/core/project-filesystem/SKILL.md`
- `DocsAI/ECS/Tools/Input/`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Tools/Timer/`
- `Src/ECS/Tools/ObjectPool/`
- `Src/ECS/Runtime/Mount/`
- `Src/ECS/Runtime/NodeLifecycle/`
- `Data/ResourceManagement/`
- `Src/ECS/Tools/ResourceLoading/`
- `Src/ECS/Tools/CommonUtilities/`
- `Src/ECS/Tools/TargetSelector/`
- `Src/ECS/Tools/Math/`
- `Src/ECS/Tools/Input/`
- `Src/ECS/Tools/Singleton/`
- `Src/ECS/Capabilities/Ability/Component/ActiveSkillInputComponent/`
- `Src/ECS/Capabilities/Unit/Component/TargetingIndicatorControlComponent/`
- `Src/ECS/Capabilities/Movement/System/Strategies/Base/PlayerInputStrategy.cs`
- `Src/ECS/Capabilities/AI/`
- `Src/ECS/Capabilities/Ability/System/AbilityTargetingTool.cs`

## 规则

- 计时统一用 `TimerManager`，由外部 Tick 驱动；`TimerManager` 只是 Godot 生命周期 adapter，调度核心是纯 C# `TimerScheduler`。
- 新 gameplay timer 必须优先使用 `TimerHandle + TimerOptions`，显式声明 `TimerOwner`、`TimerPurpose`、`TimerClock` 和 cancel point；旧 `GameTimer` 链式 API 只作为兼容 facade。
- Timer owner/purpose 不能由 tag 替代；loop、countdown、DoT、AI wait、attack validation 等长生命周期 timer 必须能按 handle 或 owner/purpose 取消。
- Timer core 不依赖 Godot API，不直接调用 `GD.Print`、`SceneTree`、`Node` 或 Godot 时间 API；callback 由 `TimerManager` 主线程 dispatch。
- Gameplay 禁止直接使用 Godot `Timer` / `GetTree().CreateTimer(...)` / `.NET Timer` / `PeriodicTimer` / `Task.Delay` 执行业务 callback。
- Timer 变更后同步 `DocsAI/ECS/Tools/Timer/README.md`、`Concept.md`、`Usage.md`；新增或修改 Godot validation scene 时补 README 五字段和 artifact。
- Timer diagnostics 输出走 Log structured diagnostics：`owner=Timer`，`operation=TimerDiagnosticsSummary|TimerDiagnosticsDump|ExportTimerDiagnosticsJson`；普通 `_Process` / `TimerScheduler.Tick` 不逐帧写日志。
- ObjectPool 变更后同步 `DocsAI/ECS/Tools/ObjectPool/README.md`、`Concept.md`、`Usage.md`、`Tests.md`。
- Godot `CollisionObject2D` 池化默认 `ParkedInTree`：不脱树、不关碰撞、不改 layer/mask/shape；回池隐藏、停处理、移动到 parking grid，并写 `ObjectPoolRuntimeStateStore`。
- ObjectPoolManager 管理跨泛型池时必须走 `IObjectPoolRuntime`；不要恢复 `Dictionary<string, object>` + `GetMethod(...).Invoke(...)` 反射管理。
- `Activate()` 后第一 physics frame 默认不处理业务碰撞；`Detach` / `disable_mode=REMOVE` 只作为 fallback / control check。
- ObjectPool runtime state 入口是 `Src/ECS/Tools/ObjectPool/RuntimeState/ObjectPoolRuntimeStateStore.cs`、`CollisionLogicGuard.cs`、`Lifecycle/PoolParkingStrategy.cs`、`Lifecycle/DetachFallbackStrategy.cs`；不要把 pool state 写入 `Entity.Data`。
- ObjectPool 自动验证入口包括 `Src/ECS/Tools/ObjectPool/Tests/Contracts/ObjectPoolContractRuntimeTest.tscn` 和 `Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn`；Godot scene claim 必须有 README 五字段、runner `index.json`、per-scene `result.json` 和 PASS artifact。
- ObjectPool acquire/release flow 使用 `owner=ObjectPool`、`operation=ObjectPoolAcquire|ObjectPoolRelease`，并写 `poolName / activeCount / idleCount / budgetKey`；重复归还等非错误路径用 `outcome=Skipped`。
- ObjectPool / TargetSelector 高频日志排查必须先跑 `logctl analyze`，从 `summary.md`、`noise/top-contexts.md`、`missing-fields/index.md` 和 `flows/index.md` 判断是否需要 owner cleanup；普通 `operation` 不能被当作 flow，flow 只来自 `channel=Flow`、显式 `entryType` 或完整 OperationTrace 契约。
- Runtime mount 当前入口是 `RuntimeMountService` / `RuntimeMountRegistry`，默认 root 为 `/root/SlimeAIRuntime`；不要恢复 `ParentManager.GetOrRegister(name, path)` 或 `ParentNames` current API。
- Runtime mount 只管理 SceneTree 挂载和 `Pending/InTree/Invalid` diagnostics；ObjectPool parking state 继续归 `ObjectPoolRuntimeStateStore`，Entity 生命周期归 Entity runtime。
- NodeLifecycle 当前是 Runtime 底层 registry，入口为 `Src/ECS/Runtime/NodeLifecycle/`；业务查询不得直接调用 `NodeLifecycleManager.GetAllNodes()` / `GetNodesByInterface<T>()`。
- Entity/UI/Component 注册到 NodeLifecycle 时必须带 `NodeLifecycleOwner` 和 source；Ability/AI/Feature/TargetSelector 获取目标应走 `EntityManager`、`UIManager` 或 `TargetQueryEngine` candidate source。
- 资源加载 current public API 是 `ResourceLoading` / `ResourceLoadResult` / `ResourceLoadSource`；`ResourceCatalog` 和 `ResourceGenerator` 只提供 catalog / diagnostics 输入，不是运行时大管理器。不要在 Capability 中直接 `GD.Load` 或 `ResourceLoader.Load`。
- `res://` 本身不是问题；它是 Godot project root 路径。禁止的是业务 Capability 绕过资源 owner 直接裸 `GD.Load("res://...")`。
- `ResourceManagement` 只保留迁移期薄转发，不作为 current 文档入口；新调用必须写 `ResourceLoading`。
- 新增、移动、重命名、删除或检查目录 / `.tscn` / `.tres` 后必须用 project directory / resource-path migration workflow 替换旧引用，运行 `dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj`，并检查 `ResourcePaths.cs` 和旧路径残留。
- ResourceLoading 是 strict lookup：`Load<T>` 不允许 contains fallback；精确 key/category 缺失要通过 diagnostics 修正资源 key / manifest，不允许静默猜相近资源。
- `LoadPath` 只允许 DataOS resource ref、debug/test 或明确来源使用，必须携带 `ResourceLoadSource`、owner 和 usage，不让业务 Capability 传裸 `res://` 绕过 manifest。
- `ResourceCatalogDiagnostics` 覆盖 duplicate key、missing path、stale generated source 和 DataOS selected refs loadable；它是验证/诊断入口，不进入 gameplay 热路径全量刷新。
- Common Utilities current 入口是 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`，只接收没有明确 owner 的纯 helper；资源加载、Entity 查询、Timer、TargetSelector、ObjectPool、Capability 公式不得进入 CommonUtilities。
- 未来框架仓与游戏仓分离后，框架 ResourceGenerator 不默认拥有游戏资源；游戏仓应在自己的 `project.godot` 根生成 game-local catalog，框架资源在游戏仓中表现为 `res://SlimeAI/...`。
- Node 单例统一优先使用 `NodeSingletonGuard` / `SingletonInstanceGuard` 做绑定、重复实例检测和退出释放；不要新增继承式 `Singleton<T> : Node` 基类，不要用单例工具替代 `SystemManager.Execute`。
- 目标查询必须使用 `TargetQueryEngine.QueryEntities` / `QueryPositions`，通过 `TargetQueryResult<T>.Items` 和 `Diagnostics` 表达 ownership、resolved origin/forward、过滤计数、warnings/errors 与截断信息；旧 `EntityTargetSelector` / `PositionTargetSelector` list-only facade 已删除。
- TargetSelector query flow 使用 `owner=TargetSelector`、`operation=TargetQueryEntities|TargetQueryPositions` 写 summary；日志不能替代 `TargetQueryResult<T>.Diagnostics` 合同。
- TargetSelector 高频成功路径默认应通过 budget / sample / aggregate 控制输出；失败、warning、truncated 和 diagnostics 细节保留结构化字段，不能要求 AI 直接读取全量 raw JSONL。
- `TargetSorting.Random` 和位置采样必须通过 `RandomSeed` / `RandomSource` 支持 deterministic replay；TargetSelector 内部不得用当前毫秒播种或无 seed 的静态随机。
- Math current 入口只保留纯数学、曲线、几何和可复现随机工具：`Geometry2D`、`BezierCurve`、`Curves/*`、`WaveMath`、`ProbabilityTool`、`DeterministicRandom`。不要恢复 `MyMath` 作为公式杂项聚合。
- 概率单位统一为百分比 0-100；gameplay 概率用 `ProbabilityTool.RollPercent(chancePercent, rng)`，测试/replay 用 `DeterministicRandom.Create(seed)` 注入随机源。
- `GeometryCalculator` 旧门面已删除；纯几何直接调用 `Geometry2D`，`GeometryType` / `TargetSelectorQuery` / diagnostics 仍归 TargetSelector。
- Damage 护甲公式归 `DamageFormula`，Ability 冷却/充能公式归 `AbilityFormula`；CommonUtilities 和 Math 不接收 capability 业务公式杂项。
- `BezierTemplateBuilder` 带 Movement/Ability 曲线弹模板语义，不是通用数学核心；新增模板语义时先确认是否应迁到 Movement 或 Ability owner。
- Input 物理绑定事实源是 `project.godot` `[input]`；AI 先读 `DocsAI/ECS/Tools/Input/README.md`，manifest 表可以在 README 或可选 `InputMap.md` 中，不强制固定文件名。
- Input 业务层优先经 `InputManager` typed facade；不要在 Ability/Movement/Unit/UI 业务组件里新增裸 action 字符串。
- Ability/Targeting/UI 调用点优先使用业务语义方法，例如 `IsUseActiveAbilityPressed()`、`IsPreviousActiveAbilityPressed()`、`IsNextActiveAbilityPressed()`、`IsTargetConfirmPressed()`、`IsTargetCancelPressed()`、`IsPausePressed()`；`IsX/IsLeftBumper/IsRightBumper` 这类按钮名 API 仅作为兼容层。
- `BtnA/B/X/Y` 是当前兼容 action 名，不代表唯一手柄类型；UI glyph / Xbox / PlayStation / Switch / Steam 显示映射应单独设计，不写进业务 owner。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj
dotnet run --project Tools/SingletonGuardTdd/SingletonGuardTdd.csproj
dotnet run --project Tools/TimerSchedulerTdd/TimerSchedulerTdd.csproj
```

Timer Godot 场景验证（承载游戏具备 runner 时）：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
# Workspace/Tools/logctl/logctl analyze --run-dir <latest-run-dir> --out <latest-run-dir>/analysis
```
