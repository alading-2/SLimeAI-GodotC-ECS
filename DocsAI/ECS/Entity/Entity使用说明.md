# Entity 使用说明

> 状态：current
> 更新：2026-05-31
> sourcePaths: `Src/ECS/Base/Entity/`
> relatedDocs: `README.md`、`../Data/Data系统说明.md`、`../Event/Event系统说明.md`

## 1. 源码入口

- `Src/ECS/Base/Entity/IEntity.cs`：Entity 纯容器接口，当前只暴露 `Data` 和 `Events`。
- `Src/ECS/Base/Entity/Core/EntityManager.cs`：当前旧 ECS 统一 spawn / register / destroy 入口。
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

`EntityManager.Spawn` 当前仍负责对象池 / 场景实例化、runtime snapshot record apply、视觉注入、位置旋转、Component 注册和旧关系绑定。Entity hard cutover 会把这些阶段拆成 `EntitySpawnPipeline / EntityRegistry / ComponentRegistrar / LifecycleTree`，但创建入口仍必须由 framework 管理，不能直接 `new` 后挂树。

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
DataOS projection: GeneratedDataKey.SourceEntityId / string_array owner list
转换位置: owner service / helper
```

这不是恢复旧 Relationship，而是在当前 DataOS 类型能力下避免手写第二套 DataKey 事实源。

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
- projectile / effect / ability / item / UI / component owner：进入对应 capability service、typed Data projection 或 owner index。
- damage attribution：进入明确的 `DamageAttribution`，不沿 parent chain 猜。

当前代码中的 `ParentEntity / ParentRelationTypes / EntityRelationshipType` 不能作为新功能模板继续复制。

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
