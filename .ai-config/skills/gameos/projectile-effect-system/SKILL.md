---
name: projectile-effect-system
description: 修改 SlimeAI.GameOS ProjectileTool、EffectTool、投射物命中生命周期、穿透、视觉实例化或 Effect 动画播放时使用。
---

# Projectile / Effect 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/Capabilities/Projectile/`
- `GameOS/Capabilities/Effect/`
- `GameOS/Capabilities/Movement/`
- `GameOS/Capabilities/Damage/`
- `GameOS/GodotBridge/GodotProjectileEffectSpawner.cs`
- `Tests/SlimeAI.GameOS.Tests/`

## 规则

- Runtime 只保存 `res://` 场景路径，不在纯 Runtime 加载 Godot 资源。
- 投射物命中通过 MovementCollision 转 `DamageService` 请求。
- 穿透、最大命中数、生命周期统一读 `ProjectileDataKeys` / `ProjectileMovementOptions`。
- 视觉实例化和动画播放放 GodotBridge 或游戏侧，不写进纯 Runtime。
- **Source / target / spawned-id 引用走 typed DataKey**：单引用 `DataKey<EntityId?>`（`ProjectileDataKeys.SourceEntity / TargetEntity / AbilityEntity`、`EffectDataKeys.SourceEntity / TargetEntity / AbilityEntity`），多引用 `DataKey<EntityIdList>`（`ProjectileDataKeys.SpawnedProjectileIds`、`EffectDataKeys.SpawnedEffectIds`）。不再使用 `RelationshipManager` 或 `RelationshipType.EntityToProjectile / EntityToEffect / Source / Target`。
- **`ProjectileTool.Spawn / EffectTool.Spawn` 自带 lifecycle parent**：`EntitySpawnConfig.ParentEntityId = source.EntityId`，默认 `ParentDestroyPolicy.DestroyRecursively`；spawn 后还会同步 source 的 `SpawnedProjectileIds / SpawnedEffectIds`。
- **Owner cleanup hook**：`ProjectileTool.Initialize` / `EffectTool.Initialize` 在 capability 启动处调用 `RuntimeOwnedReferenceRegistry.Register(new OwnedReferenceDescriptor(SourceEntity, SpawnedProjectileIds/SpawnedEffectIds))`；spawn / 销毁时 framework 自动同步 owner-list，不要手动改。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Projectile/ProjectileCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Effect/EffectCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
