# Entity 文档入口

> 状态：current
> 更新：2026-05-31
> 范围：`Src/ECS/Base/Entity/`、`Src/ECS/Base/Event/`、`Data/DataKey/Generated/`、`Data/EventType/`。
> 设计事实源：`../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`。

## 1. 先读什么

当前 Entity 文档按下面顺序读取：

1. `Entity使用说明.md`：当前可执行用法和 hard cutover 边界。
2. `../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md`：Data / Event / DocsAI 更新后的 Entity 执行前 override。
3. `../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`：Entity / Relationship hard cutover 设计包入口。
4. `Entity规范.md`、`EntityManager.md`：从 `Src/ECS` 迁入的历史原文，只用于理解旧实现，不作为新代码示例。

## 2. 当前裁决

Entity hard cutover 方向不变：

- Entity 仍是纯身份和运行时状态容器；业务逻辑放 Component / System / Service。
- 创建统一走 `EntityManager.Spawn/Register`，销毁统一走 `EntityManager.Destroy`。
- `EntityManager.Spawn<T>` 当前已是 `EntitySpawnPipeline` 薄 facade；`EntitySpawnConfig` 只保留通用创建事实和 `LifecycleParentId / ParentDestroyPolicy`，不再保留 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- 旧 `EntityRelationshipManager / EntityRelationshipType / ParentRelationTypes / EntityManager_Ability` 不再作为新设计入口；Ability owner 清单统一走 `AbilityInventoryService.Runtime`。
- Entity identity 的目标 runtime API 是 typed `EntityId`；`GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 的字符串投影。
- Component owner 反查已经迁到 `ComponentRegistrar` 内部索引；新代码不要再用 `ENTITY_TO_COMPONENT` 关系表达组件归属。
- 业务多引用的 runtime 值对象是 `EntityIdList`；`OwnedReferenceRegistry` 负责把 owner 引用同步到 generated `string / string_array` Data projection，并在 child destroy 时自动 cleanup。当前 DataOS 还没有原生 `entity_id/entity_id_list` valueType，projection 只允许封装在 helper 内。
- Ability owner 使用 `AbilityOwnerEntityId` + `OwnedAbilityIds` projection，由 `AbilityInventoryService.OwnerDescriptor` 注册到 `OwnedReferenceRegistry`；新代码不要再用 `EntityManager.AddAbility/GetAbilities/GetAbilityByName` 作为事实源。
- Projectile owner 使用 `ProjectileOwnerEntityId` + `OwnedProjectileIds` projection，由 `ProjectileOwnershipService.Runtime` 接管；Effect host/owner 使用 `EffectHostEntityId` + `OwnedEffectIds` projection，由 `EffectOwnershipService.Runtime` 接管。
- Damage / Movement 的来源归因统一走 `EntityAttributionResolver`，它读取 Projectile / Effect / Source / Origin projection，不再沿旧 parent-chain 猜 owner。
- Event 当前以 payload 类型作为事件 key；新增 Entity lifecycle 事件必须用 `readonly record struct`，不新增字符串事件名或 `XxxEventData`。

## 3. 不再采用的旧写法

下面写法只能作为旧代码审计对象：

- 在 Entity 模板里手写 `public string EntityId` 并用 `GetInstanceId().ToString()` 生成身份。
- 用 `DataKey.Id` 当 Entity identity 入口。
- 在 `EntitySpawnConfig` 里恢复 `ParentEntity / AutoAddParentRelation / ParentRelationTypes` 这类业务关系字段。
- 用 `EntityRelationshipManager` 表达 projectile / effect / ability / UI / component owner。
- 用 `GameEventType.Unit.DeadEventData`、`PropertyChangedEventData` 这类 `XxxEventData` 双写事件。
- 在 current 文档或新代码中恢复 `DocsNew`、`Src/ECS/**.md` 作为框架文档入口。

## 4. 验证入口

文档类更新至少运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
python3 Workspace/SDD/sdd.py validate --all
find Src/ECS -type f -name '*.md' | sort
```

代码实现阶段再按影响面运行：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```
