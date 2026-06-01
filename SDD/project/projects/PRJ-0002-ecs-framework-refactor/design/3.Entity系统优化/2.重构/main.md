# Entity Spawn 统一与业务 Facade 重构

> 更新：2026-05-31
> 目标：统一 Entity runtime 的底层创建管线，同时保留业务 facades，让 AI 既能找到唯一事实源，又不会被一个巨型 Spawn 参数袋拖垮。

## 0. 结论

当前 Entity spawn 的问题，不是“有没有一个 `Spawn` 函数”，而是 **创建语义散在不同层**：

- `EffectTool` 自己做池化、实例化、Data 写入、视觉注入、关系绑定、注册。
- `ProjectileTool` 走了 `EntityManager.Spawn`，但又把业务关系写回 `EntitySpawnConfig`。
- `EntityManager_Ability` 作为 `EntityManager` partial 继续承载技能生命周期。
- `SpawnSystem`、`TargetingManager`、测试场景又各自有自己的生成面。

这会让 AI 看到两种相互冲突的信号：

1. “所有东西都应该统一到 `EntityManager.Spawn`。”
2. “实际项目里又到处都有自己的 `Spawn`。”

真正正确的做法是：

```text
业务 facade
  -> 单一 EntitySpawnPipeline
      -> EntityNodeFactory
      -> EntityDataInitializer
      -> EntityVisualInitializer
      -> EntityTransformInitializer
      -> EntityRegistry
      -> ComponentRegistrar
      -> LifecycleTree
      -> typed spawn event
```

也就是说，**统一的是底层管线，保留的是业务入口**。

## 1. 什么算 Entity spawn，什么不算

### 1.1 需要统一的 Entity spawn

下面这些都属于 Entity spawn，必须经过同一条底层管线：

- `EffectTool.Spawn`
- `ProjectileTool.Spawn`
- `AbilityService.AddAbility` 或等价技能授予入口
- `SpawnSystem` 的实体批量生成
- `TargetingManager` 的指示器生成
- 任何会创建 `IEntity`、需要 `Data` / `Events` / `EntityRegistry` / `LifecycleTree` 的 runtime 节点

### 1.2 不需要统一的场景实例化

下面这些不是 Entity spawn，不应该强行塞进 EntitySpawnPipeline：

- `PauseMenuSystem` 这类 UI 场景实例化
- `TestSceneHelper` 这类测试辅助场景实例化
- 纯粹的 Godot Node / Control / 系统节点创建

区分这两类很重要。否则 “统一 Spawn” 会错误地扩大成“所有 `Instantiate()` 都进 EntityManager”，这会把 UI、测试和 runtime 实体混在一起。

## 2. 设计裁决

### 2.1 底层只有一条管线

Entity 生成的实际事实源只能有一条：

1. 分配 `EntityId`
2. 创建 Node 或对象池实例
3. 应用 DataOS snapshot record
4. 注入视觉场景
5. 应用变换
6. 注册到 `EntityRegistry`
7. 注册 Component
8. 连接 `LifecycleTree`
9. 激活对象池对象
10. 发布 typed spawned event

任何业务入口都不准重复实现这条管线。

### 2.2 业务入口必须保留，但要变薄

`EffectTool`、`ProjectileTool`、`AbilityService`、`SpawnSystem`、`TargetingManager` 这些入口不应该消失。

原因很直接：

- AI 要的是“语义明确的入口”，不是一堆通用字段。
- `EffectTool.Spawn(...)` 比 `EntityManager.Spawn<T>(...)` 更容易让人知道业务意图。
- domain facade 才知道该填哪些 Data、该绑哪个 owner、该发哪个业务事件。

它们要做的只有一件事：

> 把领域参数翻译成 `EntitySpawnRequest`，然后调用统一管线。

### 2.3 `EntityManager.Spawn<T>` 只做薄 facade

`EntityManager.Spawn<T>` 可以继续存在，但它应该只做两件事：

- 接收一个通用的 `EntitySpawnRequest`
- 转发给 `EntitySpawnPipeline`

它不应该继续承担：

- `ParentEntity`
- `AutoAddParentRelation`
- `ParentRelationTypes`
- 业务 owner/source/target
- 对象池 / 场景实例化 / Data apply / 组件注册 / lifecycle attach 的具体编排

如果某个业务真的需要额外参数，那应该去 domain facade 里加，不应该让 `EntityManager.Spawn` 变成万能袋。

## 3. 目标 API 层次

### 3.1 低层请求

`EntitySpawnRequest` 只允许表达实体创建的通用事实：

- `Config`
- `RuntimeDataBootstrap`
- `RuntimeDataRecord`
- `RuntimeDataRecordTable`
- `RuntimeDataRecordId`
- `EntityId`
- `LifecycleParentId`
- `ParentDestroyPolicy`
- `UsingObjectPool`
- `PoolName`
- `Position`
- `Rotation`
- `VisualSceneOverride`

它不允许表达：

- owner
- source
- target
- ability
- 业务 relationType
- 任何 domain-specific 列表

### 3.2 底层管线

`EntitySpawnPipeline` 是唯一事实源，负责：

- `EntityNodeFactory`
- `EntityDataInitializer`
- `EntityVisualInitializer`
- `EntityTransformInitializer`
- `EntityRegistry`
- `ComponentRegistrar`
- `LifecycleTree`
- spawn 事件
- 失败回滚

### 3.3 领域 facade

领域 facade 是 AI 最容易用的入口：

- `EffectTool.Spawn`
- `ProjectileTool.Spawn`
- `AbilityService.AddAbility`
- `SpawnSystem` 的批量生成
- `TargetingManager.SpawnIndicator`

它们只负责把业务参数变成 request，不负责底层管线。

## 4. 当前散点应该怎么归类

| 位置 | 当前问题 | 目标角色 |
| --- | --- | --- |
| `Src/ECS/Base/System/EffectSystem/EffectTool.cs` | 自己做池化、Data、视觉、关系、注册 | 领域 facade，调用 pipeline |
| `Src/ECS/Base/System/ProjectileSystem/ProjectileTool.cs` | 入口统一了，但 request 里塞了关系语义 | 领域 facade，删掉 `ParentRelationTypes` 这类业务字段 |
| `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs` | 作为 `EntityManager` partial 承载技能生命周期 | 迁为 `AbilityService` 或等价 owner service |
| `Src/ECS/Base/System/Spawn/SpawnSystem.cs` | 批量生成入口存在，但仍直连旧 SpawnConfig | 领域 facade，构造 request 后交给 pipeline |
| `Src/ECS/Base/System/TargetingSystem/TargetingManager.cs` | 指示器生成和 Entity 创建混杂 | 领域 facade，调用 pipeline |
| `PauseMenuSystem` / `TestSceneHelper` | 不是 Entity spawn，却容易被误认为“还没统一” | 保持为普通 scene instantiate |

这张表的核心意思是：

> 不是所有生成入口都要被消灭，而是所有 Entity runtime 生成都要走同一条底层管线。

## 5. 为什么不把所有业务都塞进一个万能 Spawn

看上去“所有地方都调用 `EntityManager.Spawn`”很统一，但实际上会把三个层次混掉：

1. 实体创建机制
2. 业务语义
3. 业务引用事实源

一旦把它们混到一个参数袋里，AI 会很难判断：

- 哪些字段是生命周期字段，哪些是业务字段。
- 哪些字段必须进入 request，哪些字段应该留在 domain facade。
- `ParentEntity` 到底是生命周期 parent 还是业务 owner。

这也是过去 `EntityManager` 变重的根因。

所以正确方向不是“一个超大 Spawn 参数袋”，而是：

- **一个底层管线**
- **多个窄业务 facade**
- **一个薄 `EntityManager.Spawn`**

## 6. 错误处理和回滚

Spawn 必须是可回滚的阶段式过程，而不是“先生成再补救”。

要求：

- Data apply 失败，必须回滚对象池或销毁节点。
- Visual 注入失败，必须回滚。
- Registry 注册失败，必须回滚。
- Lifecycle attach 失败，必须让 spawn 失败，不允许 warn 后继续。
- `EntitySpawnPipeline` 每一步都要返回可测试、可记录的错误。

推荐的销毁顺序是：

```text
create -> data -> visual -> transform -> register -> component -> lifecycle -> activate -> spawned event
```

失败时必须反向回滚，不能把部分成功对象留在世界里。

## 7. AI-first 使用规则

AI 写代码时，应该按这个顺序找入口：

1. 先找 domain facade。
2. 再看它是不是只是在组装 `EntitySpawnRequest`。
3. 只有当确实是无业务语义的通用实体创建时，才直接调用 `EntityManager.Spawn<T>`。
4. 任何时候都不应该自己复制对象池、实例化、注册、生命周期 attach 的步骤。

这比“全都调用同一个巨型 Spawn”更适合 AI，因为：

- 入口语义明确。
- 责任边界更小。
- 代码审查时更容易 grep。
- 后续删旧 Relationship 时，不会顺手把业务 facade 也打碎。

## 8. 迁移顺序

建议按这个顺序收敛：

1. 先引入 `EntitySpawnPipeline` 和 `EntitySpawnRequest`。
2. 把 `EntityManager.Spawn<T>` 改成薄 facade。
3. 把 `EffectTool`、`ProjectileTool`、`TargetingManager` 改成领域 facade。
4. 把 `EntityManager_Ability` 拆成真正的 `AbilityService`。
5. 收掉所有直接 `pool.Get`、`Instantiate`、`RegisterComponents` 的 Entity runtime 入口。
6. 删除旧 `ParentRelationTypes`、`ParentEntity`、`AutoAddParentRelation`。

## 9. 验证门禁

最低门禁应该检查：

- domain facade 中不再直接出现 `pool.Get(...)`
- domain facade 中不再直接出现 `RegisterComponents(...)`
- domain facade 中不再直接写 `GeneratedDataKey.Id = GetInstanceId().ToString()`
- `ParentRelationTypes` 不再进入 spawn request
- `EntitySpawnPipeline` 是唯一真正编排 spawn 阶段的地方
- `PauseMenuSystem` / `TestSceneHelper` 不被错误迁移成 Entity spawn

## 10. 备选方案

### 方案 A：所有业务都直呼 `EntityManager.Spawn`

优点：看起来简单。  
缺点：参数袋会膨胀，业务语义会回流到 core，AI 很快又会失去边界感。

### 方案 B：每个系统完全自己 spawn，但内部偷偷统一

优点：改动局部。  
缺点：容易再次分裂成多个“隐性管线”，散点会复发。

### 方案 C：统一底层管线 + 保留业务 facade

优点：

- AI 最容易理解。
- 统一性和可读性兼得。
- 删除旧 Relationship 和旧业务 partial 时最稳。

缺点：

- 需要先把现有散点收敛成 facade，短期修改量更大。

**推荐方案：C。**

