---
name: ecs-entity
description: 修改 SlimeAI.GameOS Runtime Entity 身份容器、EntityManager、LifecycleTree、EntityIdList 或 owner cleanup hook 时使用；skill ID 暂保留 ecs-entity 只为兼容搜索，不表示传统 ECS archetype entity。
---

# Runtime Entity 入口

## 必读入口

- `DocsAI/ECS/Entity/README.md`
- `DocsAI/ECS/Entity/Entity使用说明.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`

## 源码位置

- `Src/ECS/Base/Entity/Core/Identity/`（`EntityId / EntityIdList` typed runtime identity）
- `Src/ECS/Base/Entity/Core/Registry/`（`EntityRegistry`）
- `Src/ECS/Base/Entity/Core/Spawn/`（`EntitySpawnPipeline / EntitySpawnRequest / EntitySpawnResult`）
- `Src/ECS/Base/Entity/Core/Lifecycle/`（`LifecycleTree / LifecycleLink / ParentDestroyPolicy / EntityDestroyPipeline`）
- `Src/ECS/Base/Entity/Core/Components/`（`ComponentRegistrar / EntityManager_Component*`）
- `Src/ECS/Base/Entity/Core/References/`（`OwnedReferenceDescriptor / OwnedReferenceRegistry`）
- `Src/ECS/Base/Entity/Core/Attribution/`（`EntityAttributionResolver`）
- `Src/ECS/Base/Entity/Core/Migration/`（`EntityManager_Migration / EntityMigration*`）
- `Src/ECS/Base/Entity/Core/LegacyRelationship/`（旧 `EntityRelationship*` 隔离区；只做兼容清理，不作为新入口）
- `Src/ECS/Base/Entity/Core/Manager/`（`EntityManager / EntityManager_Collision` 薄 facade）
- `Src/ECS/Base/Entity/`（`IEntity` 与具体 Entity Node）
- `Src/ECS/Base/Component/`（`IComponent` 与 Godot Component Node）
- `Src/ECS/Test/SingleTest/ECS/Entity/`（Entity runtime scene tests）

## 规则

- 创建实体走 `EntityManager.Spawn/Register`；`EntityManager.Spawn<T>` 当前是 `EntitySpawnPipeline` 的薄 facade，底层阶段顺序是 create -> data -> visual -> transform -> registry -> component -> lifecycle -> activate -> spawned event。
- 销毁实体走 `EntityManager.Destroy`；新销毁顺序事实源是 `EntityDestroyPipeline`：recursive lifecycle children -> detach links -> owner cleanup -> component unregister -> Data/Events clear -> registry unregister -> pool return / `QueueFree`。
- `IEntity` 是运行时对象身份容器，只暴露 `Data`、`Events`，不是 archetype entity 或行为继承根。
- 业务逻辑放 Capability service/tool/handler、Runtime Process 或 GodotBridge Adapter，不给 `IEntity` 增加玩法方法。
- **Entity 引用必须用 typed `EntityId`（`readonly record struct EntityId(string Value)`），不允许 raw `string` 表达 entity-id**：禁止在 capability / framework API 中使用 `string entityId` 形参或 `"player"` 字面量；必须 `new EntityId(...)` 或 `EntityId.From(string?)` 显式构造。
- 表达 "无引用" 用 `EntityId.Empty`；判空用 `IsEmpty`，不要用 `string.IsNullOrWhiteSpace(entityId.Value)` 这种间接检查。
- **Lifecycle 父子关系走 `LifecycleTree.Attach / Detach`**，遵守单 parent 假设；业务多引用不进 `LifecycleTree`。Spawn 配置只允许 `LifecycleParentId / ParentDestroyPolicy`，手工注册实体或迁移路径用 `EntityManager.AttachLifecycleParent / GetLifecycleParentId / GetLifecycleLink` 这组 typed facade，不要恢复 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- **Component owner 反查走 `ComponentRegistrar` 内部索引**：不要再用 `EntityRelationshipType.ENTITY_TO_COMPONENT` 表达 component 归属。
- **业务引用 runtime API 走 typed `EntityId / EntityIdList`**：当前 DataOS 尚未原生生成 `DataKey<EntityId?> / DataKey<EntityIdList>`，所以 `OwnedReferenceDescriptor` 暂用 `DataKey<string>`（child -> owner）和 `DataKey<string[]>`（owner -> child list）作为 generated projection；不要把 projection 泄漏成 capability public API。
- `EntityIdList` 是不可变 record struct，`Add / Remove` 返回新值，自动忽略 `EntityId.Empty` 并去重。
- **Owner cleanup hook**：Capability 初始化时调用 `EntityManager.RegisterOwnedReference(new OwnedReferenceDescriptor(ChildToOwnerKey, OwnerListKey))` 注册一次；spawn/attach owner 语义时调用 `EntityManager.AddOwnedReference(owner, child, descriptor)`。framework 在 `EntityManager.Destroy` 销毁路径会自动从 owner 的 owner-list projection 中移除被销毁 child。不要手动同步 owner-list。
- Projectile / Effect / Ability owner 不直接调用 `AddOwnedReference`；优先使用 `ProjectileOwnershipService.Runtime`、`EffectOwnershipService.Runtime`、`AbilityInventoryService.Runtime`。Damage / Movement 归因读取走 `EntityAttributionResolver`，不要沿旧 parent-chain 或 `EntityRelationshipTraversal` 猜 owner。
- `LegacyRelationship/` 下旧代码只允许用于兼容清理和历史审计；新增业务、测试和文档示例不得引用 `EntityRelationshipManager / EntityRelationshipType / EntityRelationshipTraversal`。
- AI 不允许在 capability API 用 raw string 表达 entity-id；也不允许用 `DataKey<List<string>>` 表达 entity-id 多引用。新增 public API 必须 `EntityId` / `EntityIdList` 参数类型；只有 DataOS projection helper 内可见 `string` / `string[]`。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0024
```
