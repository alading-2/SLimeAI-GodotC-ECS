# CollisionComponent 源码入口

`CollisionComponent` 是实体视觉体碰撞桥接组件。AI 执行契约见 `DocsAI/Modules/Collision.md`，系统总览见 `Docs/框架/ECS/Collision/碰撞系统说明.md`。

## 职责

把 Entity 根节点为 `Area2D` 的 `BodyEntered/BodyExited/AreaEntered/AreaExited` 信号转发为 `Entity.Events`：
- `GameEventType.Collision.CollisionEntered`
- `GameEventType.Collision.CollisionExited`

只桥接，不解释业务语义。不负责伤害、拾取、移动停止/销毁决策。

## 不桥接的范围

- `CharacterBody2D` slide collision（由 `EntityMovementComponent.ApplyMovement()` 中 `GetSlideCollision(i)` 处理）
- Hurtbox 受击区语义（由 `HurtboxComponent` 独立桥接 `HurtboxEntered/HurtboxExited`）

## 事件结构

`GameEventType_Collision.cs` 核心字段：`Source`（当前实体）、`Target`（进入/离开的目标节点）。不携带 `CollisionType` 标记。

运动碰撞链路约定：`CollisionEntered/Exited.Target` 直接是 `IEntity` 根节点，不依赖 `EntityManager.ResolveOwningIEntity(...)` 做宿主回溯。

## 标准节点结构

特效/子弹 (`Area2D`)：`CollisionComponent` + 可选 `EntityMovementComponent`，`VisualRoot` 下 `CollisionShape2D`

角色 (`CharacterBody2D`)：`HurtboxComponent` + 可选 `ContactDamageComponent`，`CollisionShape2D` + `VisualRoot`

## 与其它系统

- **EntityMovementComponent**：非默认运动模式下消费 `CollisionEntered` → 交给 `MovementCollisionPolicy` 过滤/去重/计数
- **HurtboxComponent**：受击区专用桥接，接触伤害/受击判定消费 Hurtbox 事件
- **投射物**：挂 `CollisionComponent` + 在 `MovementParams.CollisionParams` 中声明碰撞策略

完整分工和禁止事项见 `DocsAI/Modules/Collision.md`。

## 关键文件

- 组件实现：`CollisionComponent.cs`
- 受击区：`../HurtboxComponent/HurtboxComponent.cs`
- 接触伤害：`../ContactDamageComponent/ContactDamageComponent.cs`
- 碰撞事件：`Data/EventType/Base/Collision/GameEventType_Collision.cs`
- 碰撞层：`Data/DataKey/Base/CollisionLayers.cs`
