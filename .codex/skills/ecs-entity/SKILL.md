---
name: ecs-entity
description: 修改 SlimeAI.GameOS Runtime Entity 身份容器、EntityManager、LifecycleTree、EntityIdList 或 owner cleanup hook 时使用；skill ID 暂保留 ecs-entity 只为兼容搜索，不表示传统 ECS archetype entity。
---

# Runtime Entity 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/Runtime/World/`（`RuntimeWorld / IEntityRegistry / ILifecycleTree / IWorldEventBus / IResourceCatalog / IObjectPoolManager`）
- `GameOS/Runtime/Entity/`（`EntityId / EntityManager / EntitySpawnConfig / LifecycleTree / LifecycleLink / ParentDestroyPolicy / EntityIdList / OwnedReferenceDescriptor / IOwnedReferenceCleaner / RuntimeOwnedReferenceRegistry`）
- `GameOS/GodotBridge/GodotEntity.cs`
- `GameOS/GodotBridge/GodotEntity2D.cs`
- `Tests/SlimeAI.GameOS.Tests/`

## 规则

- **World facade 入口**：新 runtime / capability 代码可显式传入 `RuntimeWorld world`，优先使用 `world.Entities / Lifecycle / Events / Resources / Pools`；旧 `EntityManager / LifecycleTree / WorldEvents.World / ResourceCatalog / ObjectPoolManager` static facade 仍可用，但只作为 `RuntimeWorld.Default` 兼容入口。
- `RuntimeWorld.CreateScoped()` 是测试和沙箱隔离入口；不要通过清理 `RuntimeWorld.Default` 模拟隔离。
- `RuntimeWorld.Dispose` 顺序事实源是 `DocsAI/GameOS/Contracts.md#runtime-world-契约`：P2b 为 `Pools -> Resources -> Lifecycle -> Entities -> Events`，P4 扩展为 `Schedule -> Commands -> Pools -> Resources -> Lifecycle -> Entities -> Events`。
- 创建实体走 `EntityManager.Spawn/Register`。
- 销毁实体走 `EntityManager.Destroy`；lifecycle parent 销毁策略通过 `EntitySpawnConfig.ParentDestroyPolicy` 或 `EntityManager.AttachLifecycleParent` 表达。
- `IEntity` / `RuntimeEntity` 是运行时对象身份容器，只暴露 `EntityId`、`Data`、`Events`，不是 archetype entity 或行为继承根。
- 业务逻辑放 Capability service/tool/handler、Runtime Process 或 GodotBridge Adapter，不给 `IEntity` 增加玩法方法。
- Godot Node 生命周期适配放 `GameOS/GodotBridge/`，不要污染纯 Runtime。
- **Entity 引用必须用 typed `EntityId`（`readonly record struct EntityId(string Value)`），不允许 raw `string` 表达 entity-id**：禁止在 capability / framework API 中使用 `string entityId` 形参或 `"player"` 字面量；必须 `new EntityId(...)` 或 `EntityId.From(string?)` 显式构造。
- 表达 "无引用" 用 `EntityId.Empty`；判空用 `IsEmpty`，不要用 `string.IsNullOrWhiteSpace(entityId.Value)` 这种间接检查。
- **Lifecycle 父子关系走 `LifecycleTree.Attach / Detach`**，遵守单 parent 假设；业务多引用不进 `LifecycleTree`。推荐在 `EntitySpawnConfig.ParentEntityId` 中指定 parent，`EntityManager.Spawn` 会自动 attach。
- **业务引用走 typed DataKey**：单引用用 `DataKey<EntityId?>`（如 `Projectile.SourceEntity / Effect.SourceEntity / Ability.OwnerEntity`），多引用用 `DataKey<EntityIdList>`（如 `Projectile.SpawnedProjectileIds / Effect.SpawnedEffectIds / Ability.OwnedAbilityIds`）。`EntityIdList` 是不可变 record struct，`Add / Remove` 返回新值。
- **Owner cleanup hook**：Capability 在 `Initialize` 时调用 `RuntimeOwnedReferenceRegistry.Register(new OwnedReferenceDescriptor(ChildToOwnerKey, OwnerListKey))` 注册一次；framework 在 `EntityManager.Destroy` 销毁路径会自动从 owner 的 `EntityIdList` 中移除被销毁实体。不要手动同步 owner-list。
- AI 不允许在 capability API 用 raw string 表达 entity-id；也不允许用 `DataKey<List<string>>` 表达 entity-id 多引用。新增 API 必须 `EntityId` / `EntityIdList` 参数类型。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
```
