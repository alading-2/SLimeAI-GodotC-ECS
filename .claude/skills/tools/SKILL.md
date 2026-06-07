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
- `.ai-config/skills/core/resource-path-migration/SKILL.md`
- `DocsAI/ECS/Tools/Input/`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Tools/Timer/`
- `Src/ECS/Tools/ObjectPool/`
- `Data/ResourceManagement/`
- `Src/ECS/Tools/TargetSelector/`
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
- Timer 变更后同步 `DocsAI/ECS/Tools/Timer/Concept.md`、`Usage.md`；新增或修改 Godot validation scene 时补 README 五字段和 PASS artifact。
- ObjectPool 变更后同步 `DocsAI/ECS/Tools/ObjectPool/README.md`、`Concept.md`、`Usage.md`、`Tests.md`。
- Godot `CollisionObject2D` 池化默认 `ParkedInTree`：不脱树、不关碰撞、不改 layer/mask/shape；回池隐藏、停处理、移动到 parking grid，并写 `ObjectPoolRuntimeStateStore`。
- ObjectPoolManager 管理跨泛型池时必须走 `IObjectPoolRuntime`；不要恢复 `Dictionary<string, object>` + `GetMethod(...).Invoke(...)` 反射管理。
- `Activate()` 后第一 physics frame 默认不处理业务碰撞；`Detach` / `disable_mode=REMOVE` 只作为 fallback / control check。
- ObjectPool runtime state 入口是 `Src/ECS/Tools/ObjectPool/RuntimeState/ObjectPoolRuntimeStateStore.cs`、`CollisionLogicGuard.cs`、`Lifecycle/PoolParkingStrategy.cs`、`Lifecycle/DetachFallbackStrategy.cs`；不要把 pool state 写入 `Entity.Data`。
- ObjectPool 自动验证入口包括 `Src/ECS/Tools/ObjectPool/Tests/Contracts/ObjectPoolContractRuntimeTest.tscn` 和 `Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.tscn`；Godot scene claim 必须有 README 五字段、runner `index.json`、per-scene `result.json` 和 PASS artifact。
- 资源加载统一通过 `ResourceManagement` / `ResourceCatalog`，不要在 Capability 中直接 `GD.Load` 或 `ResourceLoader.Load`。
- `res://` 本身不是问题；它是 Godot project root 路径。禁止的是业务 Capability 绕过资源 owner 直接裸 `GD.Load("res://...")`。
- ResourceManagement 当前是资源加载 facade 和 generated catalog 入口，后续可简化为 ResourceLoading；它不是 Godot 资源系统替代品，也不是自动路径修复器。
- 移动/重命名/删除 `.tscn` / `.tres` 后必须用 resource-path migration workflow 替换旧引用，运行 `dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj`，并检查 `ResourcePaths.cs` 和旧路径残留。
- ResourceManagement 目标是 strict lookup：后续实现应删除 `Load<T>` 的 contains fallback；精确 key/category 缺失要通过 diagnostics 修正资源 key / manifest，不允许静默猜相近资源。
- `LoadPath` 只允许 DataOS resource ref、debug/test 或明确来源使用；后续应携带 `ResourceLoadSource`、owner 和 usage，不让业务 Capability 传裸 `res://` 绕过 manifest。
- `ResourceCatalogDiagnostics` 是 ResourceManagement 后续必补验证面，至少覆盖 duplicate key、missing path、stale generated source 和 DataOS selected refs loadable。
- 未来框架仓与游戏仓分离后，框架 ResourceGenerator 不默认拥有游戏资源；游戏仓应在自己的 `project.godot` 根生成 game-local catalog，框架资源在游戏仓中表现为 `res://SlimeAI/...`。
- Node 单例统一优先使用 `NodeSingletonGuard` / `SingletonInstanceGuard` 做绑定、重复实例检测和退出释放；不要新增继承式 `Singleton<T> : Node` 基类，不要用单例工具替代 `SystemManager.Execute`。
- 目标查询优先使用 `TargetQueryEngine.QueryEntities` / `QueryPositions`，通过 `TargetQueryResult<T>.Items` 和 `Diagnostics` 表达 ownership 与截断信息；`EntityTargetSelector` / `PositionTargetSelector` 只作为旧 facade。
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
```
