---
name: collision-system
description: 修改 SlimeAI ECS Collision Capability、碰撞层、Hurtbox、ContactDamage、MovementCollision 或对象池碰撞隔离时使用。
---

# Collision Capability 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/Collision/README.md`
- `DocsAI/ECS/Capabilities/Movement/README.md`
- `DocsAI/ECS/Capabilities/Damage/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/Collision/`
- `Src/ECS/Capabilities/Movement/`
- `Src/ECS/Capabilities/Damage/`
- `Src/ECS/Capabilities/Unit/Component/`
- `Src/ECS/Tools/ObjectPool/`

## 规则

- 碰撞过滤统一读取 Collision DataKeys / layer / mask / team 规则。
- 接触伤害只把碰撞转成 DamageService 请求，不直接改 HP。
- 对象池回收 Godot `CollisionObject2D` 默认走 `ParkedInTree`，Collision / Movement / Damage 业务入口必须查 pool runtime state 和 `CollisionReadyPhysicsFrame`。
- `CollisionComponent` / `HurtboxComponent` 发业务事件前必须通过 `CollisionLogicGuard`，不能把回池对象或激活首帧对象转成有效业务碰撞。
- `ContactDamageComponent` timer tick 前必须验证 attacker 仍允许处理碰撞；guard 失败时取消 timer 并清理旧引用。
- MovementCollision 负责命中检测和派发，伤害结算仍走 Damage Capability。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
