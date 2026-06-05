# RuntimeWorld Facade

> Baseline 由当前 SDD design 和 `DocsAI/ECS/` 文档管理。本文件只提供 AI 路由级摘要。

读取时机：新增 Runtime / Capability service、测试 fixture、CommandBuffer / Schedule 相关逻辑、或准备使用 static facade 时读取。

## 入口

- `RuntimeWorld.Default`：生产 eager singleton，承载旧 static facade。
- `RuntimeWorld.CreateScoped()`：测试和局部运行域 sandbox，返回可 dispose 的独立 world。
- `world.Entities / Lifecycle / Events / Resources / Pools / Schedule / Commands`：world-scoped subsystem 入口。

现有 static facade 保留并转发到 `Default`：`EntityManager`、`LifecycleTree`、`WorldEvents.World`、`ResourceCatalog`、`ObjectPoolManager`。新代码优先显式持有 `RuntimeWorld`，不要新增 static singleton。

## 测试规则

```csharp
using var world = RuntimeWorld.CreateScoped();
var entity = world.Entities.Spawn(config);
```

- 测试必须用 `CreateScoped()` 隔离 state。
- deferred path 用 `world.Schedule.RunPhase(SchedulePhase.Manual)` 或目标 phase 主动 playback。
- 不 mock `IEntityRegistry / ILifecycleTree / IWorldEventBus / IResourceCatalog / IObjectPoolManager / IRuntimeSchedule / IRuntimeCommandBuffer`；这些是 RuntimeWorld 组合用 internal-only abstraction。
- 不 mutate `RuntimeWorld.Default` 来清理测试状态。

## Dispose

`RuntimeWorld.Default.Dispose()` 必须抛错。scoped world dispose 后 subsystem getter 抛 `ObjectDisposedException`。

当前固定 dispose 顺序：

```text
Schedule -> Commands -> Pools -> Resources -> Lifecycle -> Entities -> Events
```

`Commands.Clear()` 在 dispose 中 discard pending commands，并记录 `Skipped / WorldDisposing`。后续 change 不得重定义此顺序。

## 事实源

- Contract / API：`DocsAI/ECS/` 文档、当前 SDD design
- Historical specs：`openspec/specs/runtime-world-container/spec.md`、`openspec/specs/runtime-command-buffer/spec.md`
- Tests：`Tests/SlimeAI.GameOS.Tests/World/RuntimeWorldTests.cs`
