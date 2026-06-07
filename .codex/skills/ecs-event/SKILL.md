---
name: ecs-event
description: 修改 SlimeAI ECS EventBus、GlobalEventBus、Capability 事件或事件通信协议时使用；skill ID 暂保留 ecs-event 只为兼容搜索，不表示传统 ECS event layer。
---

# Runtime Event 入口

## 必读入口

- `DocsAI/ECS/Runtime/Event/Event系统说明.md`
- `DocsAI/ECS/Runtime/README.md`
- `DocsAI/ECS/Capabilities/README.md`

## 源码位置

- `Src/ECS/Runtime/Event/`
- `Src/ECS/Capabilities/*/Events/`
- `Src/ECS/Runtime/Event/Global/`
- `Src/ECS/Capabilities/*/`

## 规则

- 实体内通信优先 `Entity.Events`。
- 全局低频广播才用 `GlobalEventBus.Global`。
- 当前仓尚未落地 `RuntimeWorld / CommandBuffer / WorldEvents`；不要按旧 GameOS 路线新增 deferred event queue。
- 新事件 payload 放到对应 Capability 的 `Events/` 目录；全局事件放 `Src/ECS/Runtime/Event/Global/`。`Data/EventType/` 已迁移完毕，不再使用旧路径。
- 框架不持有 game-specific 玩法事件：名称或 payload 依赖波次、主动技能、鼠标选择、卡牌、天赋、具体输入动作、具体游戏 UI / 资产时，放到 `Games/<Game>/Src/Game/Event/`。
- 框架 Runtime event payload 不直接持有 Godot `Vector2 / Rect2 / Node` 等引擎类型；需要这些类型时默认是游戏侧事件。
- 框架仓 `SlimeAI/` 禁止 `using BrotatoLike`、`BrotatoLike.Game.*` 或其它游戏 namespace 反向依赖。
- 订阅方必须可清理，不把事件当状态存储。
- Data 业务变更监听使用 `GameEventType.Data.Changed<T>`；`GameEventType.Data.PropertyChanged(string, object?, object?)` 只允许 TestSystem/debug/migration diagnostic 边界使用。

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
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

## Phase tick 与 event dispatch guard

- 事件 handler 内需要结构变更时，继续调用正式入口（`EntityManager.Spawn / Destroy`、`LifecycleTree.Attach / Detach`），不要新增事件专属 deferred queue。
- Guard 内 `Spawn` 返回 reserved `RuntimeEntity`，可立即写 `Data.Set`，但同 guard 内 `world.Entities.Get(id)` 返回 `null`。
- 如需引入 deferred event / command buffer 语义，先创建 SDD 设计并明确 Runtime 目录、验证场景和迁移范围。
