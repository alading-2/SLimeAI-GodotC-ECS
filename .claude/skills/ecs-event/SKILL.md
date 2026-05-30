---
name: ecs-event
description: 修改 SlimeAI.GameOS EventBus、WorldEvents、Capability 事件或事件通信协议时使用；skill ID 暂保留 ecs-event 只为兼容搜索，不表示传统 ECS event layer。
---

# Runtime Event 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/Runtime/Event/`
- `GameOS/Runtime/Data/EventDataChangeSink.cs`
- `Tests/SlimeAI.GameOS.Tests/`

## 规则

- 实体内通信优先 `RuntimeEntity.Events`。
- 全局低频广播才用 `WorldEvents.World` / world bus。
- World event handler 派发期间会进入 `RuntimeWorld.Commands.EnterGuard("event-dispatch:" + typeof(TEvent).Name)`；handler 内调用 `Spawn / Destroy / Attach / Detach` 会进入 Runtime CommandBuffer，并在目标 `SchedulePhase` playback。
- `QueuedEvent` deferred replay 第一阶段只支持 framework-known `IGlobalEvent` record；不要把 game-specific dynamic event、输入事件或 UI 事件塞进 `QueuedEventCommandPayload`。
- 新事件 payload 优先放到对应 Capability 的 `Events/` 目录；不要恢复旧 `GameEventType` 作为新入口。
- 框架不持有 game-specific 玩法事件：名称或 payload 依赖波次、主动技能、鼠标选择、卡牌、天赋、具体输入动作、具体游戏 UI / 资产时，放到 `Games/<Game>/Src/Game/Event/`。
- 框架 Runtime event payload 不直接持有 Godot `Vector2 / Rect2 / Node` 等引擎类型；需要这些类型时默认是游戏侧事件。
- 框架仓 `SlimeAI/` 禁止 `using BrotatoLike`、`BrotatoLike.Game.*` 或其它游戏 namespace 反向依赖。
- 订阅方必须可清理，不把事件当状态存储。

## 事件归属判定树

1. 多个 game profile 都有意义、且 payload 只用 framework-owned 类型：放 framework Runtime/Events 或 framework Capability `Events/`。
2. 名称或 payload 绑定具体游戏玩法 / UI / 输入动作 / 资产：放 `Games/<Game>/Src/Game/Event/`，namespace 用 `<Game>.Game.Events`。
3. payload 依赖 Godot 引擎类型：默认放游戏侧；除非先有 SDD 证明这是 GodotBridge 通用协议。
4. 不确定时先放游戏侧，等第二个 game profile 证明通用后再上提。

旧事件迁移按三桶分类：

- Bucket A：无 producer / consumer 的死代码，直接删除，不创建空替换。
- Bucket B：真实调用链但语义属于游戏，迁到游戏侧并删除 framework 声明。
- Bucket C：framework 自身使用但名称带游戏术语，留 framework 并改中性名，不能反向依赖游戏 namespace。

参考：P3 `refactor-runtime-events-purge-game-leakage` 删除 Bucket A `MouseSelection* / Wave* / GameStart / GameOver / GamePause / GameResume`；迁移 Bucket B `InputUseSkill / InputPreviousSkill / InputNextSkill` 和 `GodotPlayerInputComponent` 到 BrotatoLike。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
```

## Phase tick 与 event dispatch guard

- 事件 handler 内需要结构变更时，继续调用正式入口（`EntityManager.Spawn / Destroy`、`LifecycleTree.Attach / Detach`），不要新增事件专属 deferred queue。
- Guard 内 `Spawn` 返回 reserved `RuntimeEntity`，可立即写 `Data.Set`，但同 guard 内 `world.Entities.Get(id)` 返回 `null`。
- 需要验证 deferred 语义时，用 scoped world：`using var world = RuntimeWorld.CreateScoped(); using var guard = world.Commands.EnterGuard("test"); ...; world.Schedule.RunPhase(SchedulePhase.Manual);`。
