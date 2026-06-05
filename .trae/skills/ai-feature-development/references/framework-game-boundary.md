# Framework / Game Boundary

> Baseline 由 `Workspace/SystemAgent/Protocols/FrameworkVsGameBoundary.md` 与当前 SDD 管理。框架 / 游戏归属规则变化必须走 SDD。

读取时机：迁移旧逻辑、移动事件 / component / DataKey、把 BrotatoLike 能力上提框架、或判断 GodotBridge 是否应承载某段逻辑时读取。

## 五层归属

1. Framework Runtime：typed identity、data、events、schedule、command buffer、world facade、observation 基础设施。
2. Framework Capability：Damage、Movement、Ability、AI 等可复用服务、DataKeys、Events、selectors、tools。
3. GodotBridge Adapter：`Node`、`SceneTree`、Physics、Input、Resource、可视化实例化和生命周期桥接；不承载玩法规则。
4. Game：BrotatoLike 玩法、UI、场景路径、输入动作名、波次、具体 ability / feature handler、seed 内容。
5. Old input repo：`Resources/Else/brotato-my` 只作为迁移输入和历史对照，不是新事实源。

## 快速判断

- 是否依赖 `Games/<Game>` namespace、scene path、Input Map action、UI 文案、资产路径？是则默认游戏侧。
- 是否是可跨游戏复用的伤害、移动、目标选择、资源、对象池、事件基础设施？是则可能属于框架或 Capability。
- 是否只是 Godot 对象生命周期或物理 API 适配？是则属于 GodotBridge。
- 是否只是旧项目命名和资源结构？是则只作为迁移输入，不能上提为框架默认。

## 案例

- `DamageInfo`：框架 Damage capability，跨游戏复用。
- `MouseSelection*`、`Wave*`、`GameStart / GameOver / GamePause / GameResume`：BrotatoLike 语义，不属于 framework Runtime events。
- `InputUseSkill / InputPreviousSkill / InputNextSkill`：游戏输入事件，位于 `Games/BrotatoLike/Src/Game/Event/BrotatoLikeInputEvents.cs`。
- `BrotatoLikePlayerInputComponent`：游戏桥接，位于 `Games/BrotatoLike/Src/Game/Bridge/`。
- 资源路径、Spawn wave config、具体 Ability handler 参数：游戏 DataOS seed / handler，不上提框架默认。

## 禁止上提

- 把 `Wave`、`Deck`、`Talent`、`MouseSelection`、`ActiveSkill` 等玩法词放进 `SlimeAI/GameOS/Runtime/Events/`。
- 让 framework 依赖 `BrotatoLike` namespace。
- 把游戏场景路径写成框架默认资源。
- 为一个游戏的 UI 或输入动作新增框架级 public API。
