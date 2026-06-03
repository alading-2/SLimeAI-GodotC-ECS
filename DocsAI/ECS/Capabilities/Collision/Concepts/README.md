# Collision Concepts

> 状态：current
> 定位：Collision 关键概念入口，收敛碰撞层、Godot 物理时序、对象池协作和 `Node2D` 父链约束。
> 更新：2026-06-03

## 当前裁决

Collision Capability 只负责把 Godot 碰撞信号和移动碰撞候选转成 ECS 事件或候选输入，并在进入业务前提供过滤契约。它不负责对象复用、池容量、伤害结算、移动停止或 Entity 初始化。

对象池相关当前目标策略是 `ParkedInTree + CollisionLogicGuard + ActivationFrameEmbargo`：

- 根节点为 `CollisionObject2D` 的池化对象目标默认不脱树、不关碰撞、不改 layer/mask/shape。
- 回池只隐藏、停处理、移动到分散 parking grid，并写 pool runtime state。
- `Activate()` 后第一 physics frame 默认不处理业务碰撞。
- Collision / Movement / Damage / ContactDamage 入口必须检查 pool runtime state、ready frame、实体有效性、team、owner 和 lifecycle。
- `Detach`、`disable_mode=REMOVE`、关闭 `Monitoring/Monitorable`、禁用 shape 或改 layer/mask 只作为组件真实卸载、fallback 或对照验证路径。

> 注意：当前 `Src/ECS/Tools/ObjectPool/ObjectPool.cs` 仍保留旧默认脱树 / 关碰撞实现。本目录记录的是后续 ObjectPool + Collision SDD 的目标裁决，不表示源码已经迁移完成。

## 当前文档

| 文档 | 职责 |
| ---- | ---- |
| [Godot物理时序与对象池碰撞.md](Godot物理时序与对象池碰撞.md) | 总体状况、Godot 源码/资料调研、旧脱树为何过时、`ParkedInTree` 目标裁决和 owner 边界。 |
| [Node2D父链约束.md](Node2D父链约束.md) | 普通 `Node` 阻断 `Node2D` / `Area2D` transform 继承的问题、症状、修复和验证。 |
| [History/](History/) | 旧碰撞、幽灵碰撞、对象池兼容和脱树分析原文。只作追溯，不作为当前执行入口。 |

## 碰撞层级设置

Layer / Mask 规则：

```text
Layer = 我是谁
Mask  = 我关心谁
```

当前层定义：

| Layer | 值 | 名称 | 用途 |
| --- | ---: | --- | --- |
| 1 | `1` | `Terrain` | 地形 / 障碍物 |
| 2 | `2` | `Player` | 玩家物理体 |
| 3 | `4` | `Enemy` | 敌人物理体 |
| 4 | `8` | `PlayerHurtbox` | 玩家受击感应区 |
| 5 | `16` | `PlayerPickup` | 玩家拾取感应区 |
| 6 | `32` | `Projectile` | 子弹 / 投射物 |
| 7 | `64` | `EnemyHurtbox` | 敌人受击感应区 |
| 8 | `128` | `WeaponHitbox` | 武器 / 技能命中区 |
| 9 | `256` | `SelectionPickable` | 鼠标选择拾取层 |

常量事实源：`Data/DataKey/Base/CollisionLayers.cs`。业务代码禁止直接写碰撞层魔法数字。

常用配置：

| 对象 | Layer | Mask | 说明 |
| ---- | ---- | ---- | ---- |
| Player 物理体 | `2+256` | `1` | 玩家根物理体，同时允许鼠标选择拾取。 |
| Enemy 物理体 | `4+256` | `1+4` | 敌人根物理体，同时允许鼠标选择拾取。 |
| Player Hurtbox | `8` | `4` | 直接挂在 `HurtboxComponent`。 |
| Enemy Hurtbox | `64` | `128` | 直接挂在 `HurtboxComponent`。 |
| Player Pickup | `16` | `0` | 拾取感应区身份层。 |
| Projectile | `32` | 视业务而定 | 投射物身份层，命中解释归 Movement / Projectile / Ability。 |
| WeaponHitbox | `128` | `64` | 武器或技能命中区。 |
| SelectionPickable | `256` | `0` | 只作为鼠标选择查询身份层。 |

## 当前链路

```text
Area2D 视觉体
  -> CollisionComponent
  -> CollisionEntered / CollisionExited
  -> Movement / Projectile / Ability 业务解释

Hurtbox 受击区
  -> HurtboxComponent
  -> HurtboxEntered / HurtboxExited
  -> ContactDamage / Damage 业务解释

CharacterBody2D 移动
  -> MoveAndSlide()
  -> slide collision candidates
  -> MovementCollisionPolicy
```

## 不再作为 current 入口的旧结论

- “对象池默认脱树才能安全”已经过时；脱树现在只作为 fallback / control check。
- “回池时默认关闭 `Monitoring/Monitorable` 或禁用 shape”已经过时；这会重新引入 deferred 和 pair 重建窗口。
- “`Node2D` 父链问题修完后就不需要 pool runtime guard”是错误结论；父链问题和池化旧 signal / 旧引用是两类问题。
- “ObjectPool 负责判断业务碰撞是否有效”是错误边界；ObjectPool 只提供 runtime state 和 parking/fallback 事实。
