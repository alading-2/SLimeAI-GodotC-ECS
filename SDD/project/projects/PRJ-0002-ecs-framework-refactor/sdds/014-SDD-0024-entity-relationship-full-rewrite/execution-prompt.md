# SDD-0024 Execution Prompt

## 使用方式

把本文件整体交给新的执行会话、主执行 agent 或编排会话。它是 `SDD-0024 Entity Relationship Full Rewrite` 的执行提示词。

本提示词不是让多个 agent 并行改 Entity core。主会话必须按 TDD 顺序推进：先 baseline，再 RED tests，再实现，再验证，再更新 SDD progress / tasks / DocsAI。

## 角色定位

你是 SDD-0024 的主执行者和集成者。

你的职责：

- 按 hard cutover 重建旧 ECS Entity / Relationship runtime。
- 不保留旧 facade、legacy adapter、双写或 parent-chain fallback。
- 每个切片先写新行为测试，再改实现。
- 每完成一个切片，更新 `tasks.md` checkbox、`progress.md` 和必要 DocsAI。
- 最后用 grep gate、build/test、必要 Godot scene smoke 证明旧路径退出。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/014-SDD-0024-entity-relationship-full-rewrite/`
- **Design Package**: `design/3.Entity系统优化/`

执行任何 git 操作前先确认：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

当前仓可能已有 unrelated `.uid` 删除和 `__pycache__` 未跟踪；不要清理、回滚或混入提交，除非用户明确要求。

## 必读顺序

先读项目事实源：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/06-ECS完全重构执行原则.md`

再读 Entity 设计包：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/00-研究证据与裁决.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/01-目标架构与模块拆分.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/02-代码实现说明.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/03-LifecycleTree与业务引用设计.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/04-完全重构范围与TDD测试计划.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/05-源码调用点迁移清单.md`

再读当前框架事实源：

1. `DocsAI/README.md`
2. `DocsAI/ECS/README.md`
3. `DocsAI/ECS/Entity/README.md`
4. `DocsAI/ECS/Entity/Entity使用说明.md`
5. `DocsAI/ECS/Data/Data系统说明.md`
6. `DocsAI/ECS/Event/Event系统说明.md`
7. `Src/ECS/Base/Entity/Core/`
8. `Src/ECS/Base/Entity/IEntity.cs`
9. `Data/DataKey/Generated/DataKey_Generated.cs`
10. `Data/EventType/`

最后读本 SDD：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/design/main.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/tasks.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/bdd.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/progress.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/notes.md`

## 核心裁决

- **完整重构**：Entity / Relationship 不做兼容迁移。
- **Entity identity**：runtime API 只用 `EntityId`；raw `string entityId` 只允许出现在 JSON / log / DataOS projection 边界。
- **Registry**：`EntityRegistry` 是唯一注册表；找不到 Node 返回 `EntityId.Empty`，不 fallback Godot InstanceId。
- **Data boundary**：`GeneratedDataKey.Id` 只作为 `EntityId.Value` 的 DataOS / snapshot / observation 字符串投影；不恢复旧 `DataKey.Id`。
- **DataOS 类型边界**：默认不新增原生 `entity_id/entity_id_list`；业务引用使用 typed runtime API + generated string / string_array projection helper。若要 `DataKey<EntityId>`，必须先扩展 DataOS schema / generator / validator / converter / scenes。
- **Event boundary**：Entity lifecycle 事件必须是 `readonly record struct` payload；不新增字符串事件名或 `XxxEventData`。
- **Lifecycle**：`LifecycleTree` 是唯一保留的 Relationship 语义，只表达单 parent 生命周期树。
- **Business reference**：Projectile / Effect / Ability / Item / UI / Component owner 全部迁到 typed runtime reference、generated Data projection、owner list 或 owner service。
- **Damage attribution**：统计归因只读 `DamageAttribution`；缺 attribution 是错误，不走 parent chain。
- **Debug**：AI 调试看 observation dump，不查 runtime Relationship graph。
- **删除旧入口**：`EntityRelationshipManager`、`EntityRelationshipType`、`EntityRelationshipTraversal`、`EntityRelationshipLifecycle`、`EntityManager_Relationship`、`EntityManager_Ability`、`ParentRelationTypes`、`AutoAddParentRelation`、`BindParentRelationships` 必须退出 runtime。

## 执行纪律

### 主会话职责

主会话是 owner / integrator / 最终写入者。

可以使用 subagent 做：

- 只读 grep 调用点分桶。
- 建议 RED test 矩阵。
- 局部迁移草案。
- 审查某个切片是否仍有旧 API 残留。

不允许 subagent：

- 并行修改 Entity runtime core。
- 并行删除旧 Relationship 文件。
- 添加兼容 facade、双写 adapter 或 fallback。
- 修改 DataOS descriptor schema 后不回到主会话统一合并。

### 每个切片固定循环

1. 读对应设计和源码。
2. 写 RED tests，记录预期失败。
3. 实现最小代码改动。
4. 跑目标测试。
5. 跑当前切片 grep gate。
6. 更新 `tasks.md` checkbox。
7. 追加 `progress.md` 记录：Context / Conclusion / Evidence / Impact / Resume。
8. 同步 `DocsAI/ECS/Entity/` 或相关 owner 文档。
9. 不混入 unrelated dirty 文件。

## T1.1 Readiness Baseline

目标：

- 确认 git boundary 和当前 dirty 范围。
- 读取 SDD-0024 事实源。
- 跑旧入口 baseline grep，分桶记录到 `progress.md`。
- 不改 runtime 代码。

命令：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\.Id|GetInstanceId\(\)\.ToString\(\)" Src/ECS Data DocsAI
rg -n "EventData|EventName|const string .*Event|EntitySpawned|EntityDestroyed" Src/ECS Data/EventType DocsAI/ECS
rg -n "DocsNew|Src/ECS/.+\.md|SlimeAI/Src|SlimeAI/DocsAI|cd /home/slime/Code/SlimeAI$" SDD/project/projects/PRJ-0002-ecs-framework-refactor DocsAI
python3 Workspace/SDD/sdd.py validate SDD-0024
```

记录要求：

- 哪些命中是 runtime 必改。
- 哪些命中是 SDD 历史 / 禁止项 / grep gate，允许保留。
- 哪些命中是 current DocsAI 示例风险。
- 当前 unrelated dirty 状态，不得清理。

完成后勾选 `T1.1`。

## T1.2 EntityId 与 EntityRegistry RED Tests

先写测试，不改实现。

测试覆盖：

- `EntityId.Empty` 为空引用语义。
- `EntityId.New()` 非空且稳定。
- `EntityId.From(string?)` 过滤 null / empty / whitespace。
- 不存在 implicit string conversion。
- `EntityRegistry.Register` 拒绝 empty id。
- `EntityRegistry.Register` 拒绝重复 id。
- `EntityRegistry.Register` 拒绝重复 node。
- `GetEntityId(node)` 找不到返回 `EntityId.Empty`。
- `GetNode(id)` 找不到返回 null 或明确 result。
- `Snapshot()` 返回 copy，不暴露内部集合。

记录 RED 失败摘要到 progress。

## T1.3 EntityId 与 EntityRegistry 实现

实现：

- 新增 `EntityId`。
- 新增 `EntityRegistry`。
- 替换 Entity core 内部 raw string id。
- `GeneratedDataKey.Id` 只由 `EntityDataInitializer` 写入和校验。
- 业务系统不散读 `GeneratedDataKey.Id`；需要身份时用 `IEntity.EntityId` 或 `EntityRegistry.GetEntityId(Node)`。

禁止：

- 不提供 implicit string conversion。
- 不用 `GetInstanceId().ToString()` 生成 runtime id。
- 不把 DataOS projection 当 registry。

验证：

```bash
Tools/run-build.sh
Tools/run-tests.sh
rg -n "GetInstanceId\(\)\.ToString\(\)|DataKey\.Id|string entityId" Src/ECS/Base/Entity Src/ECS/Base/System
```

## T1.4 LifecycleTree

RED tests：

- attach / detach / get parent / get children。
- empty id 拒绝。
- self parent 拒绝。
- cycle 拒绝。
- second parent 拒绝。
- destroy policy 是 typed field。
- `DetachAll(entity)` 同时清 parent side 和 child side。

实现：

- 新增 `LifecycleLink`。
- 新增 `LifecycleTree`。
- 删除 `EntityRelationshipLifecycle` 能力。
- 删除 `PARENT` relationType 写入。

验证：

```bash
rg -n "EntityRelationshipType\.PARENT|ReadParentDestroyPolicy|CreateParentRelationshipData" Src/ECS
```

## T1.5 EntityDestroyPipeline

RED tests：

- recursive child destroy。
- detach child 存活。
- children snapshot 防止边遍历边漏删。
- destroy 顺序固定。
- 重复 destroy 返回明确 result。
- component unregister 在 Data clear 前。
- owner cleanup 不决定 child 是否销毁。

实现顺序：

```text
Destroy(entityId)
  lifecycle recursive children
  lifecycle detach links
  owned reference cleanup
  component unregister
  data/events clear
  registry unregister
  pool return or queue free
```

禁止：

- 不通过 `EntityRelationshipManager.RemoveAllRelationships` 清理。
- 不在 owner list 上决定 lifecycle destroy。

## T1.6 ComponentRegistrar

RED tests：

- 扫描子节点并注册 Component。
- 通过 component 找 owner entity。
- remove component 清理 owner index。
- component unregister 后不能再反查 owner。
- 不写 `ENTITY_TO_COMPONENT` Relationship。

实现：

- 从 `EntityManager_Component*` 拆出 `ComponentRegistrar`。
- owner index 是 internal runtime infrastructure，不作为 public relationship type。

## T1.7 EntitySpawnPipeline

RED tests：

- spawn 阶段顺序固定。
- Data record apply 失败阻断 spawn 并清理实体。
- `GeneratedDataKey.Id` 投影等于 registry id。
- visual / transform / component register 在正确阶段。
- lifecycle parent attach 显式，不通过 `ParentRelationTypes`。

实现：

- `EntityNodeFactory`
- `EntityDataInitializer`
- `EntityVisualInitializer`
- `EntityTransformInitializer`
- `EntitySpawnPipeline`
- `EntitySpawnConfig` 删除 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。

验证：

```bash
rg -n "ParentRelationTypes|AutoAddParentRelation|ParentEntity =" Src/ECS
```

## T1.8 OwnedReferenceRegistry 与 Typed References

RED tests：

- `EntityIdList.Add` 去重。
- `EntityIdList.Remove` 返回新值。
- destroy child 后 owner list 自动清理。
- destroy owner 不通过 owner list 销毁 child。
- string / string_array projection 只在 helper 内出现。

实现：

- 新增 `EntityIdList`。
- 新增 `OwnedReferenceDescriptor`。
- 新增 `OwnedReferenceRegistry`。
- 各 capability 初始化注册 descriptor。
- DataOS 默认存 generated string / string_array projection；public API 仍用 `EntityId` / `EntityIdList`。

## T1.9 Ability Owner Service

RED tests：

- AddAbility 创建 AbilityEntity。
- Ability owner projection 写入。
- owner list 添加 ability id。
- RemoveAbility 清 owner list。
- GetAbilities 不查 Relationship。
- owner destroy 时 ability 按 lifecycle policy 销毁。

实现：

- 新增 `AbilityInventoryService` 或合入明确 Ability service。
- 删除 `EntityManager_Ability.cs` runtime 调用。
- Feature / TestSystem / UI / Ability components 改为 service + typed runtime reference helper。

验证：

```bash
rg -n "EntityManager\.(AddAbility|GetAbilities|RemoveAbility)|EntityManager_Ability|ENTITY_TO_ABILITY" Src/ECS DocsAI
```

## T1.10 Projectile / Effect / Damage / Movement 迁移

Projectile：

- 删除 `ParentRelationTypes = [ENTITY_TO_PROJECTILE]`。
- source / owner 使用 typed runtime reference + generated projection helper。
- owner projectile list 由 Projectile owner service 管理。

Effect：

- 删除 `BindParentRelationships(... ENTITY_TO_EFFECT)`。
- source / host 使用 typed runtime reference + generated projection helper。
- `DestroyByHost` 不查 Relationship graph。

Damage：

- 新增或接入 `DamageAttribution`。
- `StatisticsProcessor` 不调用 `GetAncestorChain`。
- `CritProcessor` / `LifestealProcessor` 不通过 parent chain 查 attacker unit。

Movement：

- `MovementCollisionPolicy` 不通过 `FindAncestorOfType<IUnit>` 判断 source owner。
- 需要 owner 时读 source attribution / explicit reference。

验证：

```bash
rg -n "ENTITY_TO_PROJECTILE|ENTITY_TO_EFFECT|BindParentRelationships|GetAncestorChain|FindAncestorOfType<IUnit>|EntityRelationshipTraversal" Src/ECS/Base/System Src/ECS/Base/Component DocsAI
```

## T1.11 DocsAI / SDD / Final Validation

同步文档：

- `DocsAI/ECS/Entity/README.md`
- `DocsAI/ECS/Entity/Entity使用说明.md`
- `DocsAI/ECS/Entity/EntityManager.md`
- 受影响 owner：Ability、Projectile / Effect、Damage、Movement、Component、UI、TestSystem。
- `.ai-config/skills/` 源文件如接口 / 流程变化影响 owner skill；改后运行 sync。

最终验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
python3 Workspace/SDD/sdd.py validate SDD-0024
python3 Workspace/SDD/sdd.py validate --all
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\.Id|GetInstanceId\(\)\.ToString\(\)" Src/ECS Data DocsAI
rg -n "EventData|EventName|const string .*Event|EntitySpawned|EntityDestroyed" Src/ECS Data/EventType DocsAI/ECS
find Src/ECS -type f -name '*.md' | sort
```

如影响 BrotatoLike scene：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

完成时更新：

- `tasks.md` 全部 checkbox。
- `progress.md` Latest Resume。
- 项目 `progress.md`。
- 必要 DocsAI。
- 验证摘要。

## 禁止项

- 不新增兼容 facade。
- 不新增 legacy adapter。
- 不双写 Relationship 和新结构。
- 不让 DataOS projection 变成 runtime identity API。
- 不恢复旧 `DataKey.Id`。
- 不恢复 `XxxEventData` / 字符串事件名。
- 不把 UI / Component / Ability / Projectile owner 重新塞进通用关系图。
- 不直接修改游戏仓 `SlimeAI/` submodule。
