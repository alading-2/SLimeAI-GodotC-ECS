# Structural Change Call Site Inventory

Date: 2026-05-15
Change: `refactor-runtime-command-buffer-with-phases`

## Commands

```bash
rg -n "EntityManager\.Spawn|EntityManager\.Destroy|LifecycleTree\.Attach|LifecycleTree\.Detach|world\.Entities\.Spawn|world\.Lifecycle\.Attach" --type cs SlimeAI/ Games/BrotatoLike/Src/
rg -n "WorldEvents\.World\.Publish|world\.Events\.Publish|\.Publish<" --type cs SlimeAI/ Games/BrotatoLike/Src/
```

## Decision Legend

- `a-deferred-safe`: 如果未来处于 guard 内，deferred 语义可接受；当前调用不要求立刻 `Get` 注册表结果。
- `b-must-not-guard`: 该调用必须留在 guard 外；当前实现未把所在调用面包进 guard。
- `c-explicit-wait`: 需要同步等待；本次 inventory 未发现。

## Production Call Sites

| file:line | call | context | will-be-guarded | decision | note |
| --- | --- | --- | --- | --- | --- |
| `Games/BrotatoLike/Src/Game/GameBootstrap.cs:39` | `EntityManager.Spawn` | explicit smoke probe | no | b-must-not-guard | smoke 随后直接读写 entity data 和断言，入口不在 callback guard 内。 |
| `Games/BrotatoLike/Src/Game/GameBootstrap.cs:40` | `EntityManager.Spawn` with parent | explicit smoke probe | no | b-must-not-guard | 验证 parent relationship 立即可见，保持 guard 外。 |
| `Games/BrotatoLike/Src/Game/GameBootstrap.cs:92` | `EntityManager.Spawn` | explicit smoke probe | no | b-must-not-guard | collision probe 立即写 data 并发事件，入口 guard 外。 |
| `Games/BrotatoLike/Src/Game/GameBootstrap.cs:99` | `EntityManager.Spawn` | explicit smoke probe | no | b-must-not-guard | Movement collision probe 立即参与 tick，入口 guard 外。 |
| `Games/BrotatoLike/Src/Game/BrotatoLikeDataOSBootstrap.cs:81` | `EntityManager.Spawn` | explicit DataOS factory | no | b-must-not-guard | 生成后立即 `snapshot.ApplyRecord(entity.Data, record)`；Data 写入支持 guarded，但当前调用面不应被 bridge/event guard 包裹。 |
| `Games/BrotatoLike/Src/Game/Main.cs:349` | `EntityManager.Spawn` | explicit smoke helper | no | b-must-not-guard | `CreateAbilityRuntimeTarget` 立即设置 data 并给 AbilityService 使用。 |
| `Games/BrotatoLike/Src/Game/Main.cs:1053` | `EntityManager.Spawn` | explicit DataOS ability probe | no | b-must-not-guard | smoke probe 立即参与目标过滤。 |
| `Games/BrotatoLike/Src/Game/Main.cs:1067` | `EntityManager.Spawn` | explicit DataOS ability probe | no | b-must-not-guard | dash caster 立即作为 AbilityService caster。 |
| `Games/BrotatoLike/Src/Game/Main.cs:1087` | `EntityManager.Spawn` | explicit DataOS ability probe | no | b-must-not-guard | circle damage probe 立即参与目标过滤。 |
| `Games/BrotatoLike/Src/Game/Main.cs:168` | `WorldEvents.World.Publish` | explicit main startup | no | b-must-not-guard | `GameStarted` 是游戏侧事件，不进入 `QueuedEvent` replay；Main 启动流程保持同步。 |
| `SlimeAI/GameOS/Capabilities/Projectile/ProjectileTool.cs:45` | `EntityManager.Spawn` | capability tool | conditional | a-deferred-safe | 若从 event/bridge guard 调用，返回 reserved entity 后 data writes 保留；当前实现没有立即 `EntityManager.Get`。`Spawned` event 使用 entity handle，不依赖 registry。 |
| `SlimeAI/GameOS/Capabilities/Projectile/ProjectileTool.cs:232` | `EntityManager.Destroy` | capability movement callback | conditional | a-deferred-safe | Movement tick 当前不在 guard；若未来被 guard 包裹，destroy 可延迟到 phase，不需要同步返回实体。 |
| `SlimeAI/GameOS/Capabilities/Movement/MovementSystem.cs:162` | `EntityManager.Destroy` | capability tick | no | a-deferred-safe | `DestroyOnStop` 后当前 tick 返回；不要求同栈后续读取 registry。 |
| `SlimeAI/GameOS/Capabilities/Effect/EffectTool.cs:41` | `EntityManager.Spawn` | capability tool | conditional | a-deferred-safe | 与 Projectile 一致：data writes 保留，`Spawned` event 使用 entity handle。 |
| `SlimeAI/GameOS/GodotBridge/GameOSGodotBridge.cs:55` | `EntityManager.Destroy` | explicit bridge unregister | no | b-must-not-guard | guard 只包 component callback，不包 Entity 注册/销毁本身；避免 Godot unregister 后实体仍滞留到 phase。 |
| `SlimeAI/Src/Validation/Runtime/Event/RuntimeEventValidationScene.cs:173` | `WorldEvents.World.Publish` | validation scene explicit | no | b-must-not-guard | validation scene 主动验证 event bus；不在 production callback guard 内。 |

## Test Call Sites

`SlimeAI/Tests/SlimeAI.GameOS.Tests/**` 中的 `world.Entities.Spawn / world.Lifecycle.Attach / EntityManager.Spawn` 均为 explicit Runtime test setup；不属于 game/runtime production callback，默认 `will-be-guarded=no`。新增 CommandBuffer 专项测试显式使用 `world.Commands.EnterGuard(...)` 覆盖 deferred 路径。

## Open Questions

未发现 `c-explicit-wait`。本变更不新增 `EnqueueAndWait`。
