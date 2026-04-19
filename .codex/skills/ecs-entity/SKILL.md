---
name: ecs-entity
description: 创建新 Entity、管理 Entity 生命周期（Spawn/Register/Destroy）、实现 IEntity 接口、配置对象池时使用。适用于：新建敌人/子弹/玩家/技能等实体，处理 Entity 的生成销毁，实现 IPoolable 接口。触发关键词：新建实体、Entity、IEntity、IPoolable、对象池生成、EntityManager.Spawn。
---

# ECS Entity 规范

## 核心原则

- **Scene 即 Entity**：`.tscn` 场景文件就是 Entity，实现 `IEntity` 接口
- **统一生命周期**：必须通过 `EntityManager.Spawn/Register/Destroy`，禁止直接 `new` 或 `QueueFree()`
- **两种类型**：对象池版（高频：Enemy/Bullet/Item）和非对象池版（低频：Player/Boss）

## VisualRoot / 碰撞约定（2026-04）

- `EntityManager.Spawn` 会在组件注册前按 `VisualScenePath` 注入 `VisualRoot`
- `EntityManager.Spawn` 支持通过 `EntitySpawnConfig.VisualSceneOverride` 显式覆盖视觉场景；未提供时再回退读取 `Config.VisualScenePath`
- 注入不再局限于 `UnitConfig` / `IUnit`；任意配置资源只要暴露 `VisualScenePath`（如 `ProjectileConfig`）即可复用同一套视觉挂载流程
- `SpriteFramesGenerator` / 视觉场景可提供 `VisualRoot/CollisionShape2D` 或 `VisualRoot/CollisionPolygon2D` 作为碰撞模板
- 受击区、拾取区等业务碰撞节点直接作为 `Area2D` 挂在 Entity 场景里
- 视觉体碰撞由 `CollisionComponent` 桥接，受击区碰撞由 `HurtboxComponent` 自身处理
- `EntityManager` 会把模板同步到 Entity 根节点；若根节点现有碰撞节点类型不匹配，会删除旧节点并创建对应类型的新节点；若新的视觉场景未提供碰撞模板，则会删除 Entity 根节点已有的 `CollisionShape2D`，避免残留旧碰撞
- 依赖视觉或碰撞配置的组件，可以假设 `OnComponentRegistered` 执行时 `VisualRoot` 已注入完成

## IEntity 接口实现（必须）

```csharp
public partial class MyEntity : CharacterBody2D, IEntity, IPoolable  // 对象池版加 IPoolable
{
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public MyEntity() { Data = new Data(this); }
}
```

## 生命周期 API

```csharp
// ✅ 生成（对象池）
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = enemyData,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPos
});

// ✅ 生成（非对象池）
var player = EntityManager.Spawn<PlayerEntity>(new EntitySpawnConfig
{
    Config = playerData,
    UsingObjectPool = false,
    Position = Vector2.Zero
});

// ✅ 注册已存在于场景中的 Entity（如编辑器直接放置的）
EntityManager.Register(this);

// ✅ 销毁（自动判断归还对象池还是 QueueFree）
EntityManager.Destroy(enemy);

// ✅ 查询组件
var health = EntityManager.GetComponent<HealthComponent>(entity);
```

## 生成关系绑定约定

```csharp
// ✅ 在 Spawn 阶段统一绑定“父实体 -> 子实体”的归属关系
var projectile = EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
{
    Config = projectileConfig,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.ProjectilePool,
    Position = spawnPos,
    ParentEntity = caster, // 父实体/归属者
    AutoAddParentRelation = true, // 自动补 PARENT，供统一溯源
    ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively, // 归属者销毁时递归销毁子实体
    ParentRelationTypes = [EntityRelationshipType.ENTITY_TO_PROJECTILE] // 业务关系：施法者 -> 投射物
});

// ✅ 自定义生成链路（如 EffectTool）统一复用同一个绑定入口
EntityManager.BindParentRelationships(
    childEntity, // 子实体
    parentEntity, // 父实体/归属者
    autoAddParentRelation: true, // 自动补 PARENT
    parentDestroyPolicy: ParentDestroyPolicy.DestroyRecursively, // 父销毁策略只写入 PARENT
    EntityRelationshipType.ENTITY_TO_EFFECT // 业务关系
);
```

- `ParentEntity` 只要填写，默认就应该同时建立 `PARENT`
- `PARENT` 是统一归属主链，一个子实体只能有一个直接父级
- `ParentDestroyPolicy` 也只挂在 `PARENT` 上；业务关系只做分类查询，不参与生命周期决策
- 默认优先用 `DestroyRecursively`，只有子实体明确需要脱离父级继续存活时才用 `Detach`
- 投射物 / 特效 / 技能这类拥有型实体，不要再手写 `ResolveEntityId + AddRelationship`

## 销毁语义约定

- `EntityManager.Destroy()` 会先读取当前实体的直接 `PARENT` 子实体
- `DestroyRecursively`：先销毁子实体，再注销当前实体
- `Detach`：仅在当前实体注销时断开关系，子实体继续存活
- 不要再在业务层手写“父销毁时顺手 Destroy 子实体”的兜底逻辑，统一走框架

## Entity 迁移约定（2026-04）

```csharp
var migrated = EntityManager.Migrate<VisualPreviewEntity>(
    sourceEntity, // 源实体
    new EntityMigrationConfig
    {
        TargetSpawn = new EntitySpawnConfig
        {
            Config = config, // 目标实体配置
            UsingObjectPool = false // 目标实体生成方式
        },
        Profile = new EntityMigrationProfile
        {
            Name = "ProjectileToPreview", // 迁移 Profile 名称
            ExcludeDataKeys = [DataKey.Name] // 显式排除的 DataKey
        },
        DataOverrides = new Dictionary<string, object>
        {
            [DataKey.Team] = Team.Enemy // 迁移后覆写值
        },
        InheritDirectParent = true // 自动继承直接 PARENT
    }
);
```

- 迁移固定语义是：`新建目标实体 -> 迁移受控 Data -> 销毁源实体`
- 默认继承直接 `PARENT` 归属链与其上的 `ParentDestroyPolicy`
- 框架会自动写入 `DataKey.SourceEntityId / DataKey.OriginEntityId`
- `DataMeta.CanMigrate` 是底层默认过滤开关；`Id / SourceEntityId / OriginEntityId` 这类键默认不参与普通复制
- 允许迁移的值默认只包括值类型、字符串和 `Resource`
- `Node / IEntity / IComponent / Delegate / EventBus` 这类绑定旧实例生命周期的引用一律不迁移
- **不要**试图迁移 `Entity.Events` 订阅、组件私有状态、`VisualRoot` 或整张关系图
- 如果某个状态需要跨迁移保留，先把它数据化，写入 `Data`

## 对象池生成时序（重要）

- 对象池 Entity 出池时，不要立即恢复碰撞与处理。
- 必须先完成 Data 注入、VisualRoot 注入、位置/旋转设置与 `ForceUpdateTransform()`。
- 组件注册完成后，再统一恢复节点激活状态。
- 这样可以避免复用对象在旧位置短暂参与物理，触发伪 `body_entered`。

### 脱树隔离机制（2026-04 实现）

碰撞类型（根节点为 `CollisionObject2D`，如 `EnemyEntity : CharacterBody2D`）回池时自动脱树：

```text
回池：停放到 PoolParkingPosition → SetCollisionTreeActive(false) → parent.RemoveChild(node)
出池：parent.AddChild(node) → ForceDisableCollisionsDirect() → 设置位置 → pool.Activate()
```

- 判定规则：`node is CollisionObject2D` → 脱树；其余（`AbilityEntity : Node`、`EffectEntity : Node2D`、UI）不脱树
- `Get(false)` 只取对象，不提前触发 `OnPoolAcquire`
- `Activate()` 会防御性检查挂树状态，再统一恢复碰撞并触发 `OnPoolAcquire / OnInstanceAcquire`
- `CharacterBody2D` 必须在 `Activate()` 后再 `CallDeferred(MoveAndSlide)`
- 详细说明：`Docs/框架/ECS/Collision/对象池碰撞兼容说明.md`

## IPoolable 接口（对象池版必须实现）

```csharp
public void OnPoolAcquire()
{
    // 从池中取出时：订阅事件
    GlobalEventBus.Global.On<GameEventType.Unit.KilledEventData>(
        GameEventType.Unit.Killed, OnKilled);
    Events.On<GameEventType.Unit.DamagedEventData>(
        GameEventType.Unit.Damaged, OnDamaged);
}

public void OnPoolRelease()
{
    // 归还池时：取消全局事件订阅，重置物理状态
    GlobalEventBus.Global.Off<GameEventType.Unit.KilledEventData>(
        GameEventType.Unit.Killed, OnKilled);
    Velocity = Vector2.Zero;
}

public void OnPoolReset()
{
    // 数据重置（通常留空，Data 由 EntityManager 自动 Clear）
}
```

## 事件处理模式

```csharp
// 全局事件：必须筛选是否是自己
private void OnKilled(GameEventType.Unit.KilledEventData evt)
{
    if (evt.Victim as Node != this) return;
    // 处理自己被击杀
}

// 局部事件（Entity.Events）：无需筛选，天然只属于本实体
private void OnDamaged(GameEventType.Unit.DamagedEventData evt)
{
    // 直接处理
}
```

## 祖先溯源约定

```csharp
// ✅ 统一沿 PARENT 关系回溯归属链
var ownerUnit = EntityRelationshipTraversal.FindAncestorOfType<IUnit>(projectileNode);
var ownerEntity = EntityRelationshipTraversal.FindAncestorOfType<IEntity>(effectNode);
```

- 不要手写 `GetParentEntitiesByChildAndType(...).FirstOrDefault()` 做归属溯源
- 处理“是谁生成了这个派生实体”时，统一优先用 `EntityRelationshipTraversal`
- `ProjectileTool.Spawn(...)` / `EffectTool.Spawn(...)` / `EntityManager.AddAbility(...)` 都应通过 `EntityManager` 的统一绑定入口补关系；业务层只负责把归属者传进去

## 禁止事项

- ❌ 直接 `new EnemyEntity()` 创建实体
- ❌ 直接 `entity.QueueFree()` 销毁实体
- ❌ 在 `_Ready()` 中订阅 Entity.Events（应在 `OnPoolAcquire` 或 `OnComponentRegistered`）
- ❌ 在 Entity 中存储业务状态字段（如 `private float _hp`）→ 必须存 Data

## 关键文件路径

- **标准模板**（新建 Entity 从这里复制）→ `Src/ECS/Base/Entity/TemplateEntity.cs`
- **接口定义** → `Src/ECS/Base/Entity/IEntity.cs`
- **开发规范** → `Src/ECS/Base/Entity/Entity规范.md`
- **API 手册** → `Src/ECS/Base/Entity/Core/EntityManager.md`
- **核心实现** → `Src/ECS/Base/Entity/Core/EntityManager.cs`
- **关系管理** → `Src/ECS/Base/Entity/Core/EntityRelationshipManager.cs`
- **关系追溯** → `Src/ECS/Base/Entity/Core/EntityRelationshipTraversal.cs`
- **架构设计** → `Docs/框架/ECS/Entity/Entity架构设计理念.md`
- **对象池接口** → `Src/ECS/Tools/ObjectPool/IPoolable.cs`
- **对象池初始化** → 搜索 `ObjectPoolInit.cs`
- **特效实体参考** → `Src/ECS/Base/Entity/Effect/EffectEntity.cs`（`Area2D + IEntity + IPoolable`）
- **特效服务入口** → `Src/ECS/Base/System/EffectSystem/EffectTool.cs`
