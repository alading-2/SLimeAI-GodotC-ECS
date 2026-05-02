# Collision 模块契约

本文是 AI 修改碰撞层、CollisionComponent、Hurtbox、ContactDamage、运动碰撞或对象池碰撞隔离前必须阅读的执行契约。长设计背景见 `Docs/框架/ECS/Collision/碰撞系统说明.md`。

## 职责边界

Collision 负责把 Godot 物理接触转换为项目事件和碰撞语义。它不直接决定玩法伤害数值，也不替代 Movement / DamageSystem。

## 核心入口

- 碰撞层常量：`Data/DataKey/Base/CollisionLayers.cs`
- 碰撞事件：`Data/EventType/Base/Collision/GameEventType_Collision.cs`
- 视觉体碰撞桥接：`Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.cs`
- 受击区：`Src/ECS/Base/Component/Collision/HurtboxComponent/HurtboxComponent.cs`
- 接触伤害：`Src/ECS/Base/Component/Collision/ContactDamageComponent/ContactDamageComponent.cs`
- 运动碰撞：`Src/ECS/Base/System/Movement/Core/Collision/`
- 运动碰撞消费：`Src/ECS/Base/Component/Movement/EntityMovementComponent.Collision.cs`
- 碰撞模板注入：`Src/ECS/Base/Entity/Core/EntityManager_Collision.cs`
- 对象池碰撞说明：`Docs/框架/ECS/Collision/对象池碰撞兼容说明.md`

## 分层语义

- 视觉体碰撞：`CollisionComponent` 只在 Entity 根节点是 `Area2D` 时桥接根节点 `BodyEntered/AreaEntered` 等信号。
- 受击区：`HurtboxComponent` 本身是 `Area2D` 传感器，挂在实体下并转发 `HurtboxEntered/HurtboxExited`。
- 接触伤害：`ContactDamageComponent` 监听本实体的 Hurtbox 事件，维护接触目标和 `TimerManager` 循环伤害。
- 运动碰撞：`MovementCollisionPolicy` 从原始候选中筛选有效碰撞，负责过滤、去重、计数和停止语义。
- 碰撞模板：视觉场景可带 `CollisionShape2D` 或 `CollisionPolygon2D` 模板，`EntityManager.InjectVisualScene` 同步到 Entity 根节点。
- 对象池碰撞：池化实体回收和再激活必须避免残留碰撞，相关规则优先查对象池碰撞兼容说明。

## 当前层常量

`CollisionLayers` 当前定义：

- `Terrain`
- `Player`
- `Enemy`
- `PlayerHurtbox`
- `PlayerPickup`
- `Projectile`
- `EnemyHurtbox`
- `WeaponHitbox`
- `SelectionPickable`
- `All`

代码和文档中禁止写魔法数字；新增层必须同步 `CollisionLayers.cs`、Godot Layer 名称、项目索引和本契约。

## 事件与实体解析

- `CollisionEntered/Exited` 携带 `Source` 和 `Target`，供视觉体碰撞和运动碰撞使用。
- `HurtboxEntered/Exited` 携带 `Source`、`Hurtbox`、`Target`、`TargetEntity`。
- `HurtboxComponent` 可沿父链解析 `TargetEntity`。
- 运动碰撞当前约定候选目标直接是 `IEntity` 根节点；不要在业务层散落宿主解析逻辑。

## 接触伤害规则

- 接触伤害只消费 `HurtboxEntered/HurtboxExited`。
- 敌对判断读取双方 `DataKey.Team`。
- 伤害值读取攻击者 `DataKey.FinalAttack`。
- 持续伤害计时必须用 `TimerManager`。
- 实际伤害请求必须通过 `SystemManager.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(...)`。
- 实体死亡时暂停计时器，复活后若仍重叠再恢复。

## 修改流程

1. 判断改动属于层常量、视觉体桥接、Hurtbox、接触伤害、运动碰撞或对象池隔离。
2. 涉及运动碰撞时同步阅读 `DocsAI/Modules/Movement.md`。
3. 涉及伤害时同步阅读 `DocsAI/Modules/DamageSystem.md`。
4. 涉及对象池时同步阅读 `DocsAI/Modules/Entity.md` 和 `DocsAI/Modules/Tools.md`。
5. 修改层或事件协议后同步更新项目索引、DocsAI 和相关 Skill。
6. 运行构建和 MovementCollision / Damage / MainTest 中相关场景。

## 禁止事项

- 禁止用魔法数字写 layer / mask。
- 禁止把 `CollisionComponent` 当 Hurtbox 使用。
- 禁止绕过 `DamageService` 直接扣血。
- 禁止用 Godot Signal 承载业务核心逻辑，信号只在桥接组件内转为 `Entity.Events`。
- 禁止对象池回收后保留战场碰撞状态。
- 禁止在业务层重复实现宿主实体解析。

## 推荐测试

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn --build
```
