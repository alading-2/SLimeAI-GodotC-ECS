---
name: projectile-effect-system
description: 修改 SlimeAI ECS Projectile / Effect Capability、投射物命中生命周期、穿透、视觉实例化或 Effect 动画播放时使用。
---

# Projectile / Effect 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/Projectile/README.md`
- `DocsAI/ECS/Capabilities/Effect/README.md`
- `DocsAI/ECS/Capabilities/Movement/README.md`
- `DocsAI/ECS/Capabilities/Damage/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/Projectile/`
- `Src/ECS/Capabilities/Effect/`
- `Src/ECS/Capabilities/Movement/`
- `Src/ECS/Capabilities/Damage/`
- `Data/DataKey/Effect/`

## 规则

- Runtime 只保存 `res://` 场景路径，不在纯 Runtime 加载 Godot 资源。
- 投射物命中通过 MovementCollision 转 `DamageService` 请求。
- 对已锁定敌方目标的伤害型投射物，`MovementCollision` 负责沿途命中；锁定目标只作为瞄准/兜底参考，不得截短弹道生命周期、最大距离或 `StopAfterCollisionCount=-1` 的无限碰撞语义。若弹道自然完成但未收到任何有效碰撞，可在 `MovementParams.OnStop(Completed)` 对锁定目标兜底结算一次，防止释放成功但无伤害。异步投射物返回的 `AbilityExecutedResult.TargetsHit` 不代表后续碰撞伤害上限。无敌人时才走随机/默认方向降级。
- 穿透、最大命中数、生命周期统一读 `ProjectileDataKeys` / `ProjectileMovementOptions`。
- 视觉实例化和动画播放放 GodotBridge 或游戏侧，不写进纯 Runtime。
- **Source / target / spawned-id 引用 public API 走 typed `EntityId / EntityIdList`**。当前 DataOS 尚未原生生成 `DataKey<EntityId?> / DataKey<EntityIdList>`，因此 owner projection 暂通过 ownership service 封装 generated `DataKey<string>` / `DataKey<string[]>`；不要在业务 API 暴露 raw string。
- **Projectile owner**：`ProjectileTool.Spawn` 生成后调用 `ProjectileOwnershipService.Runtime.Attach(owner, projectile)`，projection 是 `ProjectileOwnerEntityId` + `OwnedProjectileIds`。
- **Effect host/owner**：`EffectTool.Spawn` 当前仍是领域 facade，使用 `EntityManager.AttachLifecycleParent` 接入 lifecycle，使用 `EffectOwnershipService.Runtime.Attach(hostOrOwner, effect)` 接入 `EffectHostEntityId` + `OwnedEffectIds`。
- **Damage / Movement attribution**：统一通过 `EntityAttributionResolver.ResolveUnit/ResolveChain` 读取 Projectile / Effect / Source / Origin projection；不要恢复 `EntityRelationshipTraversal.FindAncestorOfType` 或旧 parent-chain 推断。
- **Owner cleanup hook**：`ProjectileOwnershipService / EffectOwnershipService` 内部通过 `OwnedReferenceRegistry` 同步 owner-list；销毁 child 时 framework 自动同步 owner-list，不要手动改。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
