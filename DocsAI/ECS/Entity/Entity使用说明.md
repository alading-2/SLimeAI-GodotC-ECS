# Entity 使用说明

> status: current
> sourcePaths: Src/ECS/Base/Entity/
> relatedDocs: ./Entity规范.md

## 1. 源码入口

- `Src/ECS/Base/Entity/Core/EntityManager.cs` — 统一入口
- `Src/ECS/Base/Entity/IEntity.cs` — 接口定义
- `Src/ECS/Base/Entity/TemplateEntity.cs` — 标准模板
- [Component规范.md](../Component/Component规范.md) — Component 注册、通信、数据驱动规范

## 2. 常见调用流程

### Spawn

```csharp
var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPos
});
```

EntityManager 自动完成：从对象池获取 → 注入配置数据 → 加载视觉场景 → 注册所有 Component → 建立关系 → 注册到全局索引。

### Destroy

```csharp
EntityManager.Destroy(enemy);
```

自动处理：注销 Entity → 清理 Events/Data → 注销 Component → 归还对象池或 QueueFree。

### 查询

```csharp
var health = EntityManager.GetComponent<HealthComponent>(entity);
var entity = EntityManager.GetEntityByComponent(component);
var enemies = EntityManager.GetEntitiesByType<Enemy>("Enemy");
```

### 迁移

```csharp
var target = EntityManager.Migrate<TTarget>(sourceEntity, new EntityMigrationConfig
{
    TargetSpawn = new EntitySpawnConfig { Config = config },
    Profile = new EntityMigrationProfile { Name = "ProjectileToPreview" },
    DataOverrides = new Dictionary<string, object> { ... }
});
```

迁移固定为：新建目标 → 迁移受控 Data → 销毁源。不迁移 Events 订阅、Component 私有状态、视觉节点树。

## 3. 数据和事件

### Spawn 后数据设置（Spawn → Configure → Use）

```csharp
var enemy = EntityManager.Spawn<Enemy>(config);
enemy.Data.Set(DataKey.SkillLevel, 10);   // 触发 PropertyChanged
enemy.Data.Set(DataKey.Summoner, this);
```

### 事件订阅（在 OnPoolAcquire 中）

```csharp
public void OnPoolAcquire()
{
    Events.On<GameEventType.Unit.DeadEventData>(
        GameEventType.Unit.Dead, OnDied);
}
```

无需手动解绑，`EntityManager.Destroy` 自动调用 `Events.Clear()`。

## 4. 修改边界

- **必须通过 EntityManager**：Spawn / Destroy / GetComponent / Migrate
- **禁止直接操作**：`entity.QueueFree()`、`entity.GetNode<T>()`
- **Entity 类不超过 50 行**：业务逻辑移至 Component

### 碰撞型对象池 Entity 时序

当根节点参与物理世界（`CollisionObject2D`）时：
- 回池：先禁用处理与显示 → 泊车位 → 脱树
- 出池：先挂树（碰撞关闭）→ Spawn 设置位置/ForceUpdateTransform → `pool.Activate()` 恢复碰撞
- `CharacterBody2D`：`Activate()` 后 `CallDeferred(MoveAndSlide)`

## 5. Debug 入口

- `EntityManager.GetEntityById(entityId)` — 按 ID 查询
- `EntityManager.GetEntitiesByType<T>(type)` — 按类型查询
- `EntityManager.GetEntityByComponent(component)` — 通过 Component 反查 Entity
- TestSystem 运行时调试面板可查看 Entity 状态
