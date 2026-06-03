# EntityManager 设计理念

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Entity/Manager/EntityManager.cs`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`

## 1. 定位

`EntityManager` 是 Runtime Entity 的**统一薄 facade**。不是业务容器，不是关系管理器，不是数据注入器。

当前职责：
- Spawn / register / destroy 入口
- 组装对象池/场景实例化回调，调度 `EntitySpawnPipeline`
- 委托 `ComponentRegistrar` 处理 Component 注册/反查
- 委托 `OwnedReferenceRegistry` 处理 owner-list projection

## 2. 为什么拆分为 Pipeline

旧 `EntityManager.Spawn` 是一个巨大的函数：对象池获取 -> 数据注入 -> 视觉加载 -> Transform 设置 -> 关系建立 -> Component 注册 -> 事件广播，全部塞在一起。

问题：
- 修改一个阶段需要理解整个函数
- 测试无法单独验证某个阶段
- 新业务（如迁移、预览）被迫复制或修改同一个函数

当前拆分：

```
EntitySpawnPipeline
  -> create（对象池/场景实例化）
  -> data（runtime snapshot record apply）
  -> visual（视觉场景注入）
  -> transform（位置/旋转）
  -> registry（EntityRegistry 注册）
  -> component（ComponentRegistrar 注册）
  -> lifecycle（LifecycleTree attach）
  -> activate（恢复处理/碰撞）
  -> spawned event（typed payload 事件）
```

## 3. 为什么不让 EntityManager 兼任业务

旧 `EntityManager` 有大量业务 partial：`EntityManager_Ability`、`EntityManager_Relationship`、`EntityManager_Migration`...

问题：
- Entity core 牵一发而动全身
- AI 修改一个业务功能需要理解整个 EntityManager
- 不同业务的 lifecycle 需求互相干扰

当前规则：
- Ability owner -> `AbilityInventoryService`
- Projectile owner -> `ProjectileOwnershipService`
- Effect owner -> `EffectOwnershipService`
- Damage attribution -> `EntityAttributionResolver`

EntityManager 只提供 Spawn/Destroy/Register 入口，不决定业务语义。

## 4. 创建与销毁的统一入口

```csharp
// 唯一创建方式
var enemy = EntityManager.Spawn<EnemyEntity>(config);

// 唯一销毁方式
EntityManager.Destroy(enemy);
```

禁止：
- 直接 `new Entity()` 后挂树
- 直接 `QueueFree()`
- 在 Entity 类中实现业务逻辑

## 5. 历史判断

EntityManager 从"万能 facade"退化为"薄入口 + pipeline 调度器"。核心判断：Entity core 越薄，AI 越容易理解和修改。
