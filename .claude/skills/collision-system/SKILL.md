---
name: collision-system
description: 修改碰撞系统时使用。适用于：CollisionLayers、CollisionComponent、HurtboxComponent、ContactDamageComponent、GameEventType_Collision、MovementCollisionParams、MovementCollisionPolicy、运动碰撞、对象池碰撞隔离、layer/mask 配置问题。
---

# CollisionSystem 入口

## 什么时候用

- 修改碰撞层或 layer / mask 语义。
- 修改 `CollisionComponent`、`HurtboxComponent`、`ContactDamageComponent`。
- 修改碰撞事件协议。
- 调整运动碰撞过滤、计数、停止、销毁。
- 处理对象池复用后的碰撞残留问题。

## 转向其它 Skill

- 运动策略和 `MovementParams` -> `@movement-system`
- 伤害计算和 `DamageService` -> `@damage-system`
- 对象池生命周期 -> `@ecs-entity` / `@tools`
- DataKey / EventType 定义 -> `@data-authoring`

## 必读

- `DocsAI/Modules/Collision.md`
- 涉及运动碰撞读 `DocsAI/Modules/Movement.md`
- 涉及伤害读 `DocsAI/Modules/DamageSystem.md`
- 涉及对象池读 `DocsAI/Modules/Entity.md` 和 `DocsAI/Modules/Tools.md`
- SlimeAI 迁移目标已存在 Collision 第一批、Movement 运动碰撞第一批、同帧多命中派发、Godot Physics broadphase 查询、ContactDamage 第一批、Damage 处理器管线、HealService 和 DamageTool 第一批：`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Collision`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Movement/MovementCollision*`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Movement/IMovementCollisionTargetQuery.cs`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Damage`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotCollision*`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotContactDamageComponent.cs` 和 `/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotPhysicsMovementCollisionTargetQuery.cs`

## 最短流程

1. 判断问题属于视觉体、Hurtbox、接触伤害、运动碰撞还是对象池隔离。
2. 层常量统一改 `CollisionLayers.cs`，不要写魔法数字。
3. Godot Signal 只在桥接组件内转为 `Entity.Events`。
4. 接触伤害走 `ContactDamageComponent -> DamageService`；迁移目标当前对应 `GodotContactDamageComponent -> DamageService.Instance.Process`，并进入 Damage 默认处理器管线。
5. 运动碰撞走 `MovementCollisionPolicy`，Godot 场景 broadphase 走 `GodotPhysicsMovementCollisionTargetQuery` 注入。
6. 更新 DocsAI、项目索引和测试说明。
7. 运行构建和相关碰撞 / 伤害 / Movement 测试。

## 禁止事项

- 不要把 `CollisionComponent` 当 Hurtbox 使用。
- 不要绕过 `DamageService` 直接扣血。
- 不要在业务层复制宿主实体解析。
- 不要让池化实体回收后保留战场碰撞。
- 不要用魔法数字写 layer / mask。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn --build
```

## 迁移验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI && Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike && Tools/run-godot-smoke.sh
```
