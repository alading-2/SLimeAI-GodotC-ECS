# Entity 使用说明

> 状态：current
> 更新：2026-05-31
> sourcePaths: `Src/ECS/Base/Entity/`
> relatedDocs: `README.md`、`../Data/Data系统说明.md`、`../Event/Event系统说明.md`

## 1. 源码入口

- `Src/ECS/Base/Entity/IEntity.cs`：Entity 纯容器接口，当前只暴露 `Data` 和 `Events`。
- `Src/ECS/Base/Entity/Core/EntityManager.cs`：当前旧 ECS 统一 spawn / register / destroy 入口。
- `Src/ECS/Base/Entity/Core/EntityId.cs`、`EntityIdList.cs`：runtime typed identity 和不可变多引用列表。
- `Src/ECS/Base/Entity/Core/OwnedReferenceRegistry.cs`：业务 owner 引用 Data projection 与 destroy cleanup hook。
- `Src/ECS/Base/Entity/TemplateEntity.cs`：当前模板；后续 Entity hard cutover 需要按 `3.Entity系统优化/` 更新。
- `Data/DataKey/Generated/DataKey_Generated.cs`：当前 Data 访问唯一 generated handle。
- `Data/EventType/`：当前 Event payload contract。

## 2. 当前可用流程

### Spawn

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

`EntityManager.Spawn` 当前仍负责对象池 / 场景实例化、runtime snapshot record apply、视觉注入、位置旋转、Component 注册和旧关系绑定。Component 注册/反查已由 `ComponentRegistrar` 内部索引接管，不再通过 `ENTITY_TO_COMPONENT` 关系表达。Entity hard cutover 会继续把 spawn 阶段拆成 `EntitySpawnPipeline / EntityRegistry / ComponentRegistrar / LifecycleTree`，但创建入口仍必须由 framework 管理，不能直接 `new` 后挂树。

当前 `EntityManager.Spawn<T>` 已是 `EntitySpawnPipeline` 的薄 facade，底层阶段顺序是：

```text
create -> data -> visual -> transform -> registry -> component -> lifecycle -> activate -> spawned event
```

如果需要生命周期父级，只能使用 `LifecycleParentId / ParentDestroyPolicy`：

```csharp
var projectile = EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
{
    Config = projectileConfig,
    RuntimeDataRecord = projectileRecord,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.ProjectilePool,
    Position = spawnPosition,
    LifecycleParentId = ownerId,
    ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively
});
```

`LifecycleParentId` 只表示销毁树，不表示 source、damage credit、owner list 或 UI binding。

### Destroy

```csharp
EntityManager.Destroy(enemy);
```

销毁必须通过 `EntityManager.Destroy`。不要直接调用 `QueueFree()`，否则 Data / Events / Component / Pool / lifecycle cleanup 会绕开统一入口。

### 查询

```csharp
var movement = EntityManager.GetComponent<EntityMovementComponent>(enemy);
var owner = EntityManager.GetEntityByComponent(component);
var enemies = EntityManager.GetEntitiesByType<EnemyEntity>(nameof(EnemyEntity));
```

查询只用于当前旧 ECS 运行时。后续 Entity hard cutover 后，身份查询应收口到 typed `EntityId` / `EntityRegistry`，业务 owner 查询收口到对应 capability service。

## 3. Data 使用

新增和修改 Entity 运行时状态必须先进入 DataOS descriptor，再生成 `GeneratedDataKey`。新代码不要恢复旧 `DataKey.*` 别名，也不要手写 stable key 字符串。

```csharp
var hp = enemy.Data.Get<float>(GeneratedDataKey.CurrentHp);
enemy.Data.Set(GeneratedDataKey.CurrentHp, hp - 10f);
enemy.Data.Set(GeneratedDataKey.DefaultMoveMode, MoveMode.None);
```

Entity identity 的当前边界：

- `GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 的字符串投影。
- 业务代码不要散读 `GeneratedDataKey.Id` 来表达引用。
- Entity hard cutover 目标是 runtime API 使用 typed `EntityId`；如果需要 DataOS 原生 `entity_id/entity_id_list`，必须先扩展 DataOS schema / generator / validator / converter。

业务引用的默认过渡形态：

```text
public API: EntityId / EntityIdList
DataOS projection: DataKey<string> child owner id / DataKey<string[]> owner child ids
转换位置: EntityManager.RegisterOwnedReference / AddOwnedReference / RemoveOwnedReference 或 capability helper
```

这不是恢复旧 Relationship，而是在当前 DataOS 类型能力下避免手写第二套 DataKey 事实源。

owner 引用示例：

```csharp
ProjectileOwnershipService.Runtime.Attach(owner, projectile);
var ownerProjectiles = ProjectileOwnershipService.Runtime.GetProjectiles(owner);

EffectOwnershipService.Runtime.Attach(hostOrOwner, effect);
var hostEffects = EffectOwnershipService.Runtime.GetEffects(hostOrOwner);
```

`ProjectileOwnershipService / EffectOwnershipService / AbilityInventoryService` 是业务可用入口；`OwnedReferenceRegistry` 只是它们内部的 projection + cleanup helper。owner list 不决定 child 是否跟随 owner 销毁，销毁语义仍只看 `LifecycleTree`。

## 4. Event 使用

EventBus 当前用 payload 类型作为事件 key：

```csharp
private void OnPoolAcquire()
{
    Events.On<GameEventType.Unit.Damaged>(OnDamaged);
}

private void OnPoolRelease()
{
    Events.Off<GameEventType.Unit.Damaged>(OnDamaged);
}

private void OnDamaged(GameEventType.Unit.Damaged evt)
{
    // 只处理局部事件需要的响应；业务流程放 Component / System / Service。
}
```

规则：

- 不新增字符串事件名。
- 不新增 `XxxEventData` 双写类型。
- Entity 内部 Component 协作优先 `entity.Events`。
- 全局低频广播才用 `GlobalEventBus.Global`，长期订阅者必须显式 `Off<T>`。
- 后续 Entity lifecycle event 应是 typed payload，例如 `GameEventType.Entity.Spawned(EntityId EntityId)`。

## 5. Relationship 边界

旧 `EntityRelationshipManager` 只作为待删除旧实现理解对象。新设计只保留 lifecycle parent 语义：

- lifecycle parent：进入 `LifecycleTree`，表达单 parent、销毁策略和层级遍历。
- component owner：进入 `ComponentRegistrar` 内部索引，不进入通用 Relationship 图。
- projectile owner：进入 `ProjectileOwnershipService.Runtime`，projection 是 `ProjectileOwnerEntityId` + `OwnedProjectileIds`。
- effect host/owner：进入 `EffectOwnershipService.Runtime`，projection 是 `EffectHostEntityId` + `OwnedEffectIds`。
- ability owner：进入 `AbilityInventoryService.Runtime`，projection 是 `AbilityOwnerEntityId` + `OwnedAbilityIds`。
- item / UI owner：进入对应 capability service、typed Data projection、`EntityIdList` owner list 或 owner index。
- damage / movement attribution：进入 `EntityAttributionResolver`，读取 Projectile / Effect / Source / Origin projection，不沿 parent chain 猜。

当前代码中的旧 `ParentEntity / AutoAddParentRelation / ParentRelationTypes / EntityRelationshipType` 不能作为新功能模板继续复制。

## 6. Entity 模板边界

Entity 类保持纯容器：

```csharp
public partial class ProjectileEntity : Area2D, IEntity, IPoolable
{
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public ProjectileEntity()
    {
        Data = new Data(this);
    }

    public void OnPoolAcquire()
    {
        Data.Set(GeneratedDataKey.DefaultMoveMode, MoveMode.None);
    }

    public void OnPoolRelease()
    {
        Events.Clear();
    }
}
```

不要在 Entity 上实现移动、攻击、伤害、AI、冷却、归因等业务逻辑。它们应放到 Component / System / Service。

## 7. 验证入口

文档更新：

```bash
python3 Workspace/SDD/sdd.py validate --all
find Src/ECS -type f -name '*.md' | sort
```

代码更新：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```
