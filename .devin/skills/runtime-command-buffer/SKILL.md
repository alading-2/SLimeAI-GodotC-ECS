---
name: runtime-command-buffer
description: 设计或落地 SlimeAI ECS RuntimeCommandBuffer、SchedulePhase、结构变更 guard、phase playback 或 deferred command payload 时使用。
---

# Runtime CommandBuffer 设计入口

> 当前仓尚未落地独立 `RuntimeWorld / RuntimeCommandBuffer` 源码目录；本 skill 保留为后续 Runtime 结构变更设计入口。实现前必须先创建或更新 SDD，不要把历史 GameOS 路径当作当前事实源。

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Runtime/README.md`
- `DocsAI/ECS/Runtime/Entity/`
- `DocsAI/ECS/Runtime/Event/`
- `DocsAI/ECS/Runtime/System/`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/`

## 源码位置

- 当前已落地 Runtime：`Src/ECS/Runtime/`
- 当前 Entity runtime：`Src/ECS/Runtime/Entity/`
- 当前 Event runtime：`Src/ECS/Runtime/Event/`
- 当前 System runtime：`Src/ECS/Runtime/System/`
- Runtime 测试入口：`Src/ECS/Runtime/**/Tests/`
- Capability 测试入口：`Src/ECS/Capabilities/<owner>/Tests/`

## 设计规则

- 以下规则来自待落地设计约束；若当前代码尚无对应类型，先补 SDD 设计和测试计划。
- Deferred command 第一阶段固定为 8 种：`Spawn / Destroy / Attach / Detach / QueuedEvent / ResourceRequest / GodotNodeInstantiate / GodotNodeFree`。新增 kind 必须先建 SDD。
- `DeferredRuntimeCommand` 使用 typed nullable payload fields；不要改成 `object Payload`、`Dictionary<string, object>`、`string PayloadKey / PayloadValue`。
- 每种 command 必须通过 `DeferredRuntimeCommand.ForSpawn / ForDestroy / ForAttach / ForDetach / ForQueuedEvent / ForResourceRequest / ForGodotInstantiate / ForGodotFree` 构造，保持 `Kind` 与唯一 payload 字段一致。
- `QueuedEvent` 第一阶段只支持 framework-known `IGlobalEvent` record，经 `FrameworkEventTypeRegistry` 注册后用 `JsonSerializer.SerializeToUtf8Bytes` 入队；不要塞 game-specific dynamic event。
- `ResourceRequestCommandPayload` 保持 `ResourceKey / ResourcePath`，playback 注册到 `ResourceCategory.Other`。
- `GodotNodeInstantiate / GodotNodeFree` 通过可注入 `IGodotNodeCommandHandler` 处理；未注入 handler 时返回 `BridgeTargetUnavailable`，不要在纯 Runtime 中直接依赖 Godot Node API。

## Guard 接入点

- World event handler dispatch 会进入 `event-dispatch:<EventName>` guard。
- Lifecycle attach/detach callback publish 会进入 `lifecycle-callback` guard。
- GodotBridge adapter 注册 / 反注册 callback 需要进入 `godot-bridge-callback` guard。
- 不要在 static bridge facade 上新增 `_Process / _EnterTree / _ExitTree`；phase tick 由承载游戏节点显式编排。

## Captured Entity 语义

- Guard 内 `Spawn` 返回 reserved `RuntimeEntity`，但不会立刻注册到 `EntityRegistry`。
- 同一 guard 内 `world.Entities.Get(capturedId)` 必须返回 `null`。
- 返回的 `RuntimeEntity.Data.Set(...)` 仍可使用；playback 注册同一个 reserved entity，并保留 guard 内写入的数据。
- Guard 内 `Destroy / Attach / Detach` 自动入队；单个 outer guard scope 超过 1000 条 command 必须抛 `InvalidOperationException("Guard scope command limit exceeded")`。

## Phase Playback

- `RuntimeSchedule.RunPhase(SchedulePhase phase)` 只调用 `RuntimeCommandBuffer.Playback(phase)`，不 tick capability service。
- 承载游戏 `_Process(delta)` 顺序固定为：`BeginTick -> BeforeSystemTick -> capability tick -> AfterSystemTick -> AfterEventDispatch -> EndOfFrame`。
- `SchedulePhase.Manual` 只用于测试和显式完成，不由游戏主循环自动触发。
- Playback 按 `Sequence` 排序，只播放 `TargetPhase == phase` 的命令，其他 phase 留在队列。

## Dispose 策略

- `RuntimeWorld.Dispose` 顺序固定为 `Schedule -> Commands -> Pools -> Resources -> Lifecycle -> Entities -> Events`。
- `Schedule.Clear()` 后 `RunPhase` 抛 `ObjectDisposedException`。
- `Commands.Clear()` 丢弃 pending command，`LastDiscardReport` 记录 `Skipped / WorldDisposing`；dispose 期间不要 drain pending command。

## 测试模式

- 使用 `using var world = RuntimeWorld.CreateScoped();`，不要用 `RuntimeWorld.Default` 做新测试隔离。
- 显式覆盖 guarded 路径：`using var guard = world.Commands.EnterGuard("test");` 后调用 `world.Entities.Spawn / Destroy` 或 `world.Lifecycle.Attach / Detach`。
- 播放默认 guarded structural mutation 用 `world.Schedule.RunPhase(SchedulePhase.EndOfFrame)`；test-only command 可用 `SchedulePhase.Manual`。
- Godot node command 成功/失败测试使用 fake `IGodotNodeCommandHandler`，不要启动 Godot 引擎。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate --all
```
