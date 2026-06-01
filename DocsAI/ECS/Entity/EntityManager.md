# EntityManager 当前说明

> 状态：current
> 更新：2026-05-31
> 范围：`Src/ECS/Base/Entity/Core/EntityManager.cs`、`Src/ECS/Base/Entity/Core/EntityManager_*.cs`、`Src/ECS/Base/Entity/IEntity.cs`。
> 设计事实源：`../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`。

## 1. 定位

`EntityManager` 当前仍是旧 ECS 的统一 Node 生命周期入口。Spawn 已改为 `EntitySpawnPipeline` 的薄 facade；Ability owner 已迁到 `AbilityInventoryService`，Projectile / Effect owner 已迁到对应 ownership service。

它目前负责：

- Entity spawn / register / destroy。
- 通过 `EntitySpawnPipeline` 调度对象池 / 场景实例化、runtime snapshot record apply、视觉场景注入、Transform 初始化、registry、Component、LifecycleTree 和 activate。
- Component 扫描、注册、反查和卸载；当前已委托给 `ComponentRegistrar` 内部 owner 索引。
- 业务 owner 引用的 descriptor 注册、添加、移除和 destroy cleanup；当前由 `OwnedReferenceRegistry` 同步 `EntityId / EntityIdList` 与 `string / string_array` Data projection。
- typed lifecycle parent 查询/绑定；手工注册实体或迁移路径使用 `AttachLifecycleParent / GetLifecycleParentId / GetLifecycleLink`，不再通过旧 PARENT relationship 读取生命周期父级。
- 迁移期仍保留旧 Relationship 清理代码；新业务 owner 清单已迁到 `AbilityInventoryService`、`ProjectileOwnershipService`、`EffectOwnershipService`，`EntityManager_Ability` 只保留旧调用点转发。

这就是本轮 Entity hard cutover 要拆的核心问题：`EntityManager` 同时承担了太多生命周期细节和业务 owner 逻辑。后续实现不应继续往 `EntityManager` partial 里加 Ability、Projectile、Effect、Damage 或 UI 专属入口。

## 2. 当前可用入口

创建 Entity：

```csharp
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = enemyConfig,
    RuntimeDataRecordTable = "unit.enemy",
    RuntimeDataRecordId = "enemy.yuren",
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPosition
});
```

生命周期父级只用 `LifecycleParentId / ParentDestroyPolicy`：

```csharp
var child = EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
{
    Config = projectileConfig,
    RuntimeDataRecord = projectileRecord,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.ProjectilePool,
    LifecycleParentId = ownerId,
    ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively
});
```

不要在 spawn config 中恢复 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。业务 owner/source/target 放到对应 service 或 typed Data projection。

业务 owner list 只用 owned-reference helper：

```csharp
ProjectileOwnershipService.Runtime.Attach(owner, projectile);
EffectOwnershipService.Runtime.Attach(hostOrOwner, effect);
AbilityInventoryService.Runtime.AddAbility(owner, ability);
```

这些 service 内部使用 `OwnedReferenceRegistry` 同步 Data projection 和 destroy cleanup，不创建 lifecycle child，也不会在 owner destroy 时通过 owner list 销毁 child。

销毁 Entity：

```csharp
EntityManager.Destroy(enemy);
```

查询 Component：

```csharp
var movement = EntityManager.GetComponent<EntityMovementComponent>(enemy);
var owner = EntityManager.GetEntityByComponent(component);
```

规则：

- 不直接 `new Entity()` 后绕过注册。
- 不直接 `QueueFree()`。
- 不从 Entity 类里实现业务流程。
- 不新增 `EntityManager_Ability` 这类业务 partial。
- 不用 `GetInstanceId().ToString()` 生成业务身份。

## 3. Data 边界

`EntityManager.Spawn` 当前已经接入 descriptor-first DataOS 链路：

```text
EntitySpawnConfig.RuntimeDataBootstrap / RuntimeDataRecord / RuntimeDataRecordTable+Id
  -> DataRuntimeBootstrap
  -> RuntimeDataSnapshotLoader.ApplyRecord
  -> catalog-bound Data
  -> GeneratedDataKey
```

新代码只使用 generated typed handle：

```csharp
var hp = enemy.Data.Get<float>(GeneratedDataKey.CurrentHp);
enemy.Data.Set(GeneratedDataKey.CurrentHp, hp - 10f);
```

不要恢复：

- `DataKey.*` 旧别名。
- `DataKey<T> -> string` 隐式转换。
- `DataRegistry / DataMeta` 作为字段事实源。
- 手写 stable key 字符串。

`GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 的字符串投影。Entity hard cutover 后，runtime identity 应通过 typed `EntityId` 或 `EntityRegistry.GetEntityId(Node)` 读取，不让业务系统散读 `GeneratedDataKey.Id`。

## 4. Event 边界

Event 当前以 payload 类型作为事件 key：

```csharp
enemy.Events.On<GameEventType.Unit.Damaged>(OnDamaged);
enemy.Events.Emit(new GameEventType.Unit.Damaged(enemy, amount));
```

不要恢复：

- 字符串事件名。
- `XxxEventData` 双写 payload。
- `EventName` 常量。

Entity lifecycle 事件的目标形态：

```csharp
public static partial class GameEventType
{
    public static partial class Entity
    {
        public readonly record struct Spawned(EntityId EntityId);
        public readonly record struct Destroying(EntityId EntityId);
        public readonly record struct Destroyed(EntityId EntityId);
    }
}
```

Entity 内部协作优先 `entity.Events`；跨实体、跨系统的低频广播才用 `GlobalEventBus.Global`，长期订阅者必须显式清理。

## 5. Relationship 边界

旧 `EntityRelationshipManager` 不再作为新设计入口。它把下面几类语义混在一个字符串关系图里：

- lifecycle parent。
- Component owner。
- Projectile / Effect / Ability owner。
- UI binding。
- Debug graph。
- Damage attribution 的间接 parent-chain 推断。

hard cutover 后只保留 lifecycle parent：

```text
LifecycleTree
  EntityId child
  EntityId parent
  ParentDestroyPolicy destroyPolicy
```

业务引用进入各自 owner：

| 语义 | 新归属 |
| --- | --- |
| Projectile source / owner list | Projectile service + typed runtime reference + generated Data projection |
| Effect source / host | Effect service + typed runtime reference + generated Data projection |
| Ability owner / owner ability list | `AbilityInventoryService` + `AbilityOwnerEntityId` / `OwnedAbilityIds` projection |
| Component owner 反查 | ComponentRegistrar 内部 index |
| UI binding | UI / GodotBridge registry |
| Damage attribution | `DamageAttribution` |

当前 DataOS 尚无原生 `entity_id/entity_id_list` valueType。默认执行路径是 generated string / string_array projection + owner service/helper 转换；如果要生成 `DataKey<EntityId>`，必须先扩展 DataOS schema、generator、validator 和 converter。

## 6. 目标拆分

后续 Entity hard cutover 不继续扩大 `EntityManager`，而是拆成：

| 模块 | 职责 |
| --- | --- |
| `EntityId` | typed runtime identity，不提供 implicit string conversion |
| `EntityRegistry` | id -> node、node -> id 注册表 |
| `EntitySpawnPipeline` | 编排 spawn 阶段 |
| `EntityNodeFactory` | 对象池 / 场景实例化 |
| `EntityDataInitializer` | runtime snapshot apply 与 `GeneratedDataKey.Id` 投影 |
| `EntityVisualInitializer` | 视觉场景注入 |
| `EntityTransformInitializer` | 位置和旋转初始化 |
| `ComponentRegistrar` | Component 注册、反查和卸载 |
| `EntityIdList` | typed 多实体引用，不可变 add/remove/dedup |
| `OwnedReferenceRegistry` | owner-list Data projection 与 child destroy cleanup |
| `LifecycleTree` | 单 parent 生命周期树 |
| `EntityDestroyPipeline` | lifecycle destroy、owner cleanup、component unregister、data/events clear、pool/queue free |
| `EntityObservationDumper` | AI / debug 可读事实导出 |

## 7. 删除目标

hard cutover 完成时，这些入口应退出 runtime：

- `EntityRelationshipManager`
- `EntityRelationshipType`
- `EntityRelationshipTraversal`
- `EntityRelationshipLifecycle`
- `EntityManager_Relationship`
- `EntityManager_Ability`
- `ParentRelationTypes`
- `AutoAddParentRelation`
- `BindParentRelationships`
- parent-chain damage attribution
- raw string entity id public API

## 8. 验证

文档更新：

```bash
python3 Workspace/SDD/sdd.py validate --all
find Src/ECS -type f -name '*.md' | sort
```

代码实现阶段：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```
