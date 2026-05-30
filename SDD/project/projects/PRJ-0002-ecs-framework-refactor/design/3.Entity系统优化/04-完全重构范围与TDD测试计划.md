# 完全重构范围与 TDD 测试计划

> 更新：2026-05-29
> 目标：把 Entity/Relationship hard cutover 拆成可执行任务，并定义每个任务的测试、grep gate 和完成标准。

## 0. 执行策略

这是一个完整重构 SDD，不拆成长期并存的多条迁移线。

内部可以按任务顺序执行，但最终验收必须满足：

- 旧 Relationship runtime 从运行时路径删除。
- 旧业务 partial 从 Entity core 删除。
- 旧 parent-chain 统计归因删除。
- 旧 raw string entity id API 删除。
- 新 typed Entity / Lifecycle / Reference / Attribution 测试通过。

## 1. 删除清单

### 1.1 直接删除或替换的 Entity core 文件

| 文件 | 处理 | 替代 |
| --- | --- | --- |
| `EntityRelationshipManager.cs` | 删除 | `LifecycleTree` + capability owner indexes |
| `EntityRelationshipType.cs` | 删除 | typed DataKey / service API |
| `EntityRelationshipTraversal.cs` | 删除 gameplay 入口 | `LifecycleTree.GetParent/GetChildren`，Damage 用 attribution |
| `EntityRelationshipLifecycle.cs` | 删除 | `LifecycleLink.DestroyPolicy` |
| `EntityManager_Relationship.cs` | 删除 | `LifecycleTree.Attach` |
| `EntityManager_Migration.cs` | 迁出 Entity core 或删除 | `EntityMigrationService` / 一次性 migration tool |
| `EntityManager_Component.cs` | 改写为 `ComponentRegistrar` | internal owner index |
| `EntityManager_Component_Init.cs` | 改写 | `ComponentRegistrarWarmupSystem` |
| `EntityManager_Collision.cs` | 审计后迁到 collision / visual initializer | Collision owner capability |

### 1.2 直接删除或替换的业务入口

| 文件 | 处理 | 替代 |
| --- | --- | --- |
| `Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs` | 删除 | `AbilityService` / `AbilityInventoryService` |
| `ProjectileTool.Spawn` 中 `ParentRelationTypes` | 删除 | `ProjectileDataKeys.SourceEntityId / SpawnedProjectileIds` |
| `EffectTool.BindParentRelationships` | 删除 | `EffectDataKeys.SourceEntityId / SpawnedEffectIds` + lifecycle parent |
| `StatisticsProcessor.GetAncestorChain` | 删除 | `DamageInfo.Attribution` |
| `MovementCollisionPolicy.FindAncestorOfType<IUnit>` | 改写 | `source attribution / source owner DataKey` |

## 2. 新文件清单

Entity core：

```text
EntityId.cs
EntityIdList.cs
EntityRegistry.cs
EntitySpawnRequest.cs
EntitySpawnPipeline.cs
EntityNodeFactory.cs
EntityDataInitializer.cs
EntityVisualInitializer.cs
EntityTransformInitializer.cs
ComponentRegistrar.cs
LifecycleTree.cs
LifecycleLink.cs
OwnedReferenceDescriptor.cs
OwnedReferenceRegistry.cs
EntityObservationDumper.cs
```

Capability / system：

```text
AbilitySystem/AbilityInventoryService.cs
ProjectileSystem/ProjectileDataKeys.cs 或 DataOS descriptor
EffectSystem/EffectDataKeys.cs 或 DataOS descriptor
DamageSystem/DamageAttribution.cs
UI/UiBindingRegistry.cs
```

测试：

```text
Src/ECS/Test/SingleTest/ECS/Entity/EntityIdRuntimeTest.cs
Src/ECS/Test/SingleTest/ECS/Entity/LifecycleTreeTest.cs
Src/ECS/Test/SingleTest/ECS/Entity/EntitySpawnPipelineTest.cs
Src/ECS/Test/SingleTest/ECS/Entity/ComponentRegistrarTest.cs
Src/ECS/Test/SingleTest/ECS/Entity/OwnedReferenceRegistryTest.cs
Src/ECS/Test/SingleTest/ECS/Damage/DamageAttributionTest.cs
```

具体路径可按现有测试目录调整，但测试分类必须独立，不能只靠 MainTest。

## 3. TDD 任务序

### T1：EntityId 与 Registry

先写测试：

- `EntityId.Empty` 与 `EntityId.From(null/"")` 等价。
- `EntityId.New()` 非空且稳定。
- `EntityRegistry.Register` 拒绝 empty id。
- `EntityRegistry.Register` 拒绝重复 id。
- `EntityRegistry.Register` 拒绝同一 Node 重复注册。
- `EntityRegistry.GetEntityId(node)` 找不到时返回 Empty，不 fallback InstanceId。
- `Snapshot()` 返回 copy。

实现：

- 新增 `EntityId`。
- 新增 `EntityRegistry`。
- 替换 Entity core 内部 raw string id。

验收 grep：

```bash
rg "GetInstanceId\\(\\)\\.ToString\\(\\)" SlimeAI/Src/ECS/Base/Entity/Core
```

允许日志或 Godot 低层映射用 `GetInstanceId()`，不允许作为 entity id 对外返回。

### T2：LifecycleTree

先写测试：

- Attach 成功后 `GetParent(child)` 返回 parent。
- `GetChildren(parent)` 返回 child link snapshot。
- Empty parent / child 拒绝。
- self parent 拒绝。
- child 已有 parent 拒绝。
- cycle 拒绝。
- Detach 后 parent/children 清理。
- Destroy policy 存在 typed field。
- `DetachAll(entity)` 同时清 parent side 和 child side。

实现：

- 新增 `LifecycleLink`。
- 新增 `LifecycleTree`。
- 删除 `EntityRelationshipLifecycle`。
- 删除 `PARENT` relationType 依赖。

验收 grep：

```bash
rg "EntityRelationshipType\\.PARENT|ReadParentDestroyPolicy|CreateParentRelationshipData" SlimeAI/Src
```

应无 runtime 调用命中。

### T3：EntityDestroyPipeline

先写测试：

- 父销毁时 recursive child 被销毁。
- 父销毁时 detach child 存活。
- 销毁时 children snapshot，不因 child destroy 改写列表导致漏删。
- Destroy 顺序：recursive children -> detach links -> owned reference cleanup -> component unregister -> Data/Events clear -> registry unregister -> pool/free。
- 重复 destroy 不抛异常，有明确 false/result。

实现：

- 可在 `EntitySpawnPipeline` 同级新增 `EntityDestroyPipeline`，或放入 `EntityRegistry` 上层 runtime。
- 删除 `EntityManager.Destroy(Node)` 大函数。
- Pool/free 由 `EntityNodeFactory` 负责。

### T4：ComponentRegistrar

先写测试：

- 实现 `IComponent` 的节点被注册。
- owner index 在 `OnComponentRegistered` 前写好。
- `GetOwnerEntityId(component)` 返回 typed id。
- `GetComponent<T>(ownerId)` 不走 Relationship。
- Unregister 清理 owner index。
- Component unregister 发生在 Data clear 前。

实现：

- 从 `EntityManager_Component.cs` 搬迁扫描逻辑。
- 删除 `ENTITY_TO_COMPONENT` 关系写入。
- 逐步删除 `EndsWith("Component")` 兼容规则；如果暂时保留，必须写在任务中并有 grep/审计清单。

验收 grep：

```bash
rg "ENTITY_TO_COMPONENT|GetEntityByComponent\\(|GetChildEntitiesByParentAndType\\(.*COMPONENT" SlimeAI/Src
```

应无 runtime Relationship 调用命中。

### T5：EntitySpawnPipeline

先写测试：

- Data apply 失败时 node 被回滚。
- Visual inject 失败时 node 被回滚。
- Register 失败时 node 被回滚。
- Lifecycle attach 失败时 spawn 失败并回滚。
- 关系绑定失败不允许 warn 后继续，因为没有 Relationship 绑定阶段。
- 对象池实体 transform 发生在 activate 前。
- Component 注册发生在 registry 注册后。
- EntitySpawned 事件在所有阶段成功后发。

实现：

- 新增 `EntitySpawnRequest`。
- 新增 `EntitySpawnPipeline` 和阶段服务。
- 删除旧 `EntitySpawnConfig.ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- 修改 Projectile / Effect / Ability / SpawnTest 调用。

验收 grep：

```bash
rg "ParentRelationTypes|AutoAddParentRelation|ParentEntity =" SlimeAI/Src
```

应无新 spawn request 调用命中。

### T6：OwnedReferenceRegistry 和 typed references

先写测试：

- `EntityIdList.Add` 去重。
- `EntityIdList.Remove` 返回新值。
- Destroy child 后 owner list 移除 child id。
- Destroy owner 不通过 owner list 销毁 child。
- Projectile source/list 同步。
- Effect source/list 同步。
- Ability owner/list 同步。

实现：

- 新增 `EntityIdList`。
- 新增 `OwnedReferenceDescriptor`。
- 新增 `OwnedReferenceRegistry`。
- 各 capability 初始化注册 descriptor。
- DataOS descriptor 补 `SourceEntityId / SpawnedProjectileIds / SpawnedEffectIds / OwnedAbilityIds` 等字段，类型能力不足时先以 string 序列化但 API 使用 EntityId。

### T7：AbilityService 替换 `EntityManager_Ability`

先写测试：

- AddAbility 创建 AbilityEntity。
- Ability owner DataKey 写入。
- Owner ability list 添加 ability id。
- RemoveAbility 清 owner list。
- GetAbilities 从 owner list / service index 获取，不查 Relationship。
- Owner destroy 时 ability 按 lifecycle policy 销毁。

实现：

- 新增 `AbilityInventoryService` 或合入现有 `AbilitySystem` owner service。
- 删除 `EntityManager_Ability.cs`。
- 所有调用点从 `EntityManager.AddAbility` 改到新 service。

验收 grep：

```bash
rg "AddAbility\\(|GetAbilities\\(|RemoveAbility\\(" SlimeAI/Src
rg "public static partial class EntityManager" SlimeAI/Src/ECS/Base/System
```

第一条用于检查调用点归属，第二条应无业务系统命中。

### T8：Projectile / Effect 迁移

Projectile 测试：

- Spawn 写 `SourceEntityId`。
- Spawn append `SpawnedProjectileIds`。
- Destroy projectile 清 owner list。
- Projectile hit 构造 `DamageAttribution`。

Effect 测试：

- Spawn 写 `SourceEntityId / TargetEntityId / AbilityEntityId`。
- Spawn append `SpawnedEffectIds`。
- Destroy effect 清 owner list。
- Attached effect lifecycle parent 是 host。
- Independent effect lifecycle parent 是 source 或 scene scope。

实现：

- 删除 `EntityRelationshipType.ENTITY_TO_PROJECTILE` 调用。
- 删除 `EntityRelationshipType.ENTITY_TO_EFFECT` 调用。
- 删除 `EffectTool.DestroyByHost` 的 Relationship 查询。

### T9：DamageAttribution

先写测试：

- DamageInfo 必须携带 attribution。
- StatisticsProcessor 记录 player/enemy credit。
- StatisticsProcessor 记录 weapon stats。
- StatisticsProcessor 记录 ability stats。
- 缺 `DamageCreditEntityId` 失败。
- 改 lifecycle parent 不影响统计结果。

实现：

- 新增 `DamageAttribution`。
- 修改 DamageInfo。
- 修改 DamageTool / AttackService / Projectile hit / Effect tick / ContactDamage 创建 attribution。
- 删除 `StatisticsProcessor.GetAncestorChain`。
- 删除 Crit/Lifesteal/MovementCollision 中对 parent chain 的业务归因依赖，改读 source owner / attribution。

验收 grep：

```bash
rg "GetAncestorChain|FindAncestorOfType<IUnit>|EntityRelationshipTraversal" SlimeAI/Src/ECS/Base/System
```

应无 runtime 归因调用命中。

### T10：Observation 和 Debug

先写测试：

- Entity dump 包含 typed entity id。
- Lifecycle dump 不输出 relationType。
- Typed reference dump 输出 owner lists。
- Damage attribution trace 输出 credit/source/weapon/ability。
- Component owner dump 输出 owner/component pairs。

实现：

- 新增 `EntityObservationDumper`。
- 删除 `EntityRelationshipManager.GetDebugInfo` 依赖。

## 4. Godot 场景验证

建议新增或重建独立场景：

```text
res://SlimeAI/Src/Validation/Entity/EntityIdRegistryValidation.tscn
res://SlimeAI/Src/Validation/Entity/LifecycleTreeValidation.tscn
res://SlimeAI/Src/Validation/Entity/EntitySpawnPipelineValidation.tscn
res://SlimeAI/Src/Validation/Entity/BusinessReferenceValidation.tscn
res://SlimeAI/Src/Validation/Damage/DamageAttributionValidation.tscn
```

每个场景必须输出：

- 固定 PASS / FAIL marker。
- JSON artifact。
- checks 列表。
- failureReasons。

不允许只用主场景 smoke 代替专项验证。

## 5. 构建和测试命令

框架验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

Godot 场景验证：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

新增 Entity validation scene 后，必须逐个跑独立场景，再跑 Main smoke。

## 6. 最终 grep gate

必须全部执行：

```bash
cd /home/slime/Code/SlimeAI

rg "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal" SlimeAI/Src
rg "public static partial class EntityManager" SlimeAI/Src/ECS/Base/System
rg "GetAncestorChain|FindAncestorOfType<IUnit>" SlimeAI/Src/ECS/Base/System
rg "DataKey\\.Id" SlimeAI/Src/ECS/Base/Entity SlimeAI/Src/ECS/Base/System
rg "GetInstanceId\\(\\)\\.ToString\\(\\)" SlimeAI/Src/ECS/Base/Entity SlimeAI/Src/ECS/Base/System
```

解释：

- 第一条：旧 Relationship runtime 不再参与运行时。
- 第二条：业务系统不能扩展 EntityManager partial。
- 第三条：伤害/阵营归因不走 parent chain。
- 第四条：Entity identity 不读旧 DataKey alias。
- 第五条：Godot InstanceId 不作为 runtime entity id。允许日志/底层 node map，但需要人工审计每个命中。

## 7. BDD 验收

```gherkin
Feature: Entity identity
  Scenario: EntityId is the only runtime identity
    Given a spawned entity
    Then registry id, data id and lifecycle id are the same EntityId
    And no runtime API accepts raw string entity id

Feature: Lifecycle tree
  Scenario: Parent destroy recursively destroys lifecycle children
    Given parent P and child C are attached with DestroyRecursively
    When P is destroyed
    Then C is destroyed
    And P/C lifecycle links are removed

Feature: Business reference
  Scenario: Projectile source is typed reference, not relationship type
    Given source S spawns projectile P
    Then P.SourceEntityId is S
    And S.SpawnedProjectileIds contains P
    And no ENTITY_TO_PROJECTILE relationship exists

Feature: Damage attribution
  Scenario: Projectile damage credits player and weapon explicitly
    Given projectile P was fired by weapon W owned by player U
    When P deals damage
    Then DamageAttribution.DamageCreditEntityId is U
    And DamageAttribution.WeaponEntityId is W
    And StatisticsProcessor does not traverse lifecycle parent chain
```

## 8. 执行完成定义

完成时必须更新：

- `SlimeAI/Src/ECS/Base/Entity/**` 旁文档
- `SlimeAI/DocsNew/` 中仍作为 current 的相关入口
- 对应 Ability / Projectile / Effect / Damage / Movement / UI 模块文档
- owner skills 若有 Entity 规则引用
- PRJ-0002 progress
- 新执行 SDD tasks / progress / bdd / validation artifacts

完成时不能只说“构建通过”。必须给出：

- 删除清单完成证据。
- grep gate 输出摘要。
- 单元测试结果。
- Godot validation scene 结果。
- Main scene smoke 结果。
