---
name: ecs-entity
description: 修改 SlimeAI ECS Runtime Entity 身份容器、EntityManager、LifecycleTree、EntityIdList 或 owner cleanup hook 时使用；skill ID 暂保留 ecs-entity 只为兼容搜索，不表示传统 ECS archetype entity。
---

# Runtime Entity 入口

## 必读入口

- `DocsAI/ECS/Runtime/Entity/README.md`
- `DocsAI/ECS/Runtime/Entity/Entity使用说明.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`

## 源码位置

- `Src/ECS/Runtime/Entity/Identity/`（`EntityId / EntityIdList` typed runtime identity）
- `Src/ECS/Runtime/Entity/Registry/`（`EntityRegistry`）
- `Src/ECS/Runtime/Mount/`（Entity runtime mount，默认 `/root/SlimeAIRuntime/ECS/Entity`）
- `Src/ECS/Runtime/NodeLifecycle/`（底层 Node 注册 diagnostics，不是 Entity 查询事实源）
- `Src/ECS/Runtime/Entity/Spawn/`（`EntitySpawnPipeline / EntitySpawnRequest / EntitySpawnResult`）
- `Src/ECS/Runtime/Entity/Lifecycle/`（`LifecycleTree / LifecycleLink / ParentDestroyPolicy / EntityDestroyPipeline`）
- `Src/ECS/Runtime/Entity/Components/`（`ComponentRegistrar / EntityManager_Component*`）
- `Src/ECS/Runtime/Entity/References/`（`OwnedReferenceDescriptor / OwnedReferenceRegistry`）
- `Src/ECS/Runtime/Entity/Attribution/`（`EntityAttributionResolver`）
- `Src/ECS/Runtime/Entity/Migration/`（`EntityManager_Migration / EntityMigration*`）
- `Src/ECS/Runtime/Entity/LegacyRelationship/`（旧 `EntityRelationship*` 隔离区；只做兼容清理，不作为新入口）
- `Src/ECS/Runtime/Entity/Manager/`（`EntityManager / EntityManager_Collision` 薄 facade）
- `Src/ECS/Runtime/Entity/IEntity.cs`（运行时 Entity 接口）
- `Src/ECS/Runtime/Entity/TemplateEntity.cs`（运行时 Entity 模板）
- `Src/ECS/Runtime/Component/IComponent.cs`（Godot Component Node 接口）
- `Src/ECS/Runtime/Component/TemplateComponent.cs`（Component 模板）
- `Src/ECS/Capabilities/*/Entity/`（具体 Entity Node）
- `Src/ECS/Capabilities/*/Component/`（具体业务 Component）
- `Src/ECS/Capabilities/*/Presets/`（组件组合预设，不是 Runtime Component 类型）
- `Src/ECS/Runtime/Entity/Tests/`（Entity runtime scene tests）

## 规则

- 创建实体走 `EntityManager.Spawn/Register`；`EntityManager.Spawn<T>` 当前是 `EntitySpawnPipeline` 的薄 facade，底层阶段顺序是 create -> data -> visual -> transform -> registry -> component -> lifecycle -> activate -> spawned event。
- 非对象池 Entity 加入 SceneTree 走 `RuntimeMountService` / `RuntimeMountRegistry`，不要恢复 `ParentManager.GetOrRegister(name, path)` 或按 `typeof(T).Name` 临时创建自由字符串 parent。
- Entity 查询走 `EntityRegistry` / `EntityManager` / `TargetQueryEngine`；不要把 `NodeLifecycleManager.GetAllNodes()` 或 `GetNodesByInterface<IEntity>()` 当业务查询入口。
- 销毁实体走 `EntityManager.Destroy`；新销毁顺序事实源是 `EntityDestroyPipeline`：recursive lifecycle children -> detach links -> owner cleanup -> component unregister -> Data/Events clear -> registry unregister -> pool return / `QueueFree`。
- `IEntity` 是运行时对象身份容器，只暴露 `Data`、`Events`，不是 archetype entity 或行为继承根。
- `IComponent` 是 Godot 可挂节点接入 Entity 注册/注销生命周期的契约；Component owner 反查走 `ComponentRegistrar` 内部索引。
- 具体 Entity / Component / Preset 按功能 owner 放入 `Src/ECS/Capabilities/<owner>/Entity|Component|Presets/`，不要恢复 `Src/ECS/Base` 或顶层技术分类。
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
- Entity owner 使用 `owner=Entity`；spawn/destroy/lifecycle/owned-reference cleanup 的日志字段使用 `EntityId.Value` 作为投影，但 public API 仍必须是 typed `EntityId` / `EntityIdList`。
- Entity runtime tests 断言走 `ValidationSession` / artifact / structured log，不新增 `[PASS]` / `[FAIL]` 或裸 stdout marker。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0024
# Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Entity
```
