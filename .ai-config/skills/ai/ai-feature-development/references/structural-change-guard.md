# Structural Change Guard

> Baseline 由 `SlimeAI/DocsAI/GameOS/Contracts.md`、Runtime CommandBuffer 文档与当前 SDD 管理。CommandBuffer kind 或 guard 语义变化必须走独立 SDD。

读取时机：在 event dispatch、lifecycle callback、GodotBridge callback、Capability tick 或测试中调用 `Spawn / Destroy / Attach / Detach`、queued event、resource request、Godot node deferred request 时读取。

## 核心语义

`RuntimeCommandBuffer.EnterGuard(reason)` 标记受保护区域。guard 内结构变更不会立即改变 world state，而是进入 `world.Commands`，在目标 `SchedulePhase` playback 时执行。

```csharp
using var guard = world.Commands.EnterGuard("event-dispatch:" + typeof(TEvent).Name);
var reserved = world.Entities.Spawn(config);
world.Schedule.RunPhase(SchedulePhase.EndOfFrame);
```

guard 内 `Spawn` 返回 captured / reserved `EntityId`，但实体注册和数据可见性延迟到 playback。不要在同一个 handler 内立即读取该实体 Data 或依赖 lifecycle link 已存在。

## 自动 guard 上下文

- world event handler dispatch：`event-dispatch:<EventName>`
- lifecycle callback publish：`lifecycle-callback`
- GodotBridge component registered / unregistered callback：`godot-bridge-callback`

## Playback

- `world.Schedule.RunPhase(SchedulePhase.BeforeSystemTick)` 等只播放目标 phase 的命令。
- `SchedulePhase.Manual` 只用于测试或显式验证，不由 frame loop 自动触发。
- Dispose 时 `Commands.Clear()` 丢弃 pending commands，报告 `Skipped / WorldDisposing`，不得 drain。

## 测试模式

使用 sandbox world：

```csharp
using var world = RuntimeWorld.CreateScoped();
using (world.Commands.EnterGuard("test"))
{
    world.Entities.Spawn(config);
}

var report = world.Schedule.RunPhase(SchedulePhase.EndOfFrame);
```

必须断言 report、queued count、entity visibility 或 failure reason。不要用 `RuntimeWorld.Default.Clear()` 复位全局状态。

## 反模式

- event handler 内 `Spawn` 后立即读取 entity data。
- 为某个 subsystem 新增专属 deferred queue。
- 新增 `object Payload` 或 `Dictionary<string, object>` 命令。
- 在 dispose 中 drain buffer。
