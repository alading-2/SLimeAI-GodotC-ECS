# Entity Rewrite Execution Prompt

## 使用方式

把本文件整体交给新的执行会话、主执行 agent 或编排会话。它是 PRJ-0002 Entity / Relationship hard cutover 的总入口。

本提示词不是让 10 个任务并行改源码。它要求主会话按 TDD 顺序推进，直到旧 Relationship runtime、业务 `EntityManager` partial、parent-chain attribution 和 raw string entity id 入口全部删除。

## 角色定位

你是 PRJ-0002 Entity 系统完整重构的主执行者和集成者。

你的职责：

- 按 hard cutover 重建 Entity runtime core。
- 不保留旧 facade、legacy adapter、双写或 parent-chain fallback。
- 每个切片先写新行为测试，再改实现。
- 每完成一个切片更新 SDD tasks/progress 和必要 DocsAI。
- 最后用 grep gate、单元测试、Godot validation scene 和 Main smoke 证明旧路径消失。

## 工作区与项目

- **Workspace / Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Design Package**: `design/Runtime/3.Entity系统优化/`
- **Suggested SDD Title**: `Entity Relationship Full Rewrite`
- **Suggested Slug**: `entity-relationship-full-rewrite`

## 必读入口

先读项目事实源：

1. `README.md`
2. `roadmap.md`
3. `progress.md`
4. `design/INDEX.md`
5. `design/main.md`

再读 Entity 完整重构事实源：

1. `design/Runtime/3.Entity系统优化/README.md`
2. `design/Runtime/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md`
3. `design/Runtime/3.Entity系统优化/00-研究证据与裁决.md`
4. `design/Runtime/3.Entity系统优化/01-目标架构与模块拆分.md`
5. `design/Runtime/3.Entity系统优化/02-代码实现说明.md`
6. `design/Runtime/3.Entity系统优化/03-LifecycleTree与业务引用设计.md`
7. `design/Runtime/3.Entity系统优化/04-完全重构范围与TDD测试计划.md`
8. `design/Runtime/3.Entity系统优化/05-源码调用点迁移清单.md`

再读当前框架事实源：

1. `DocsAI/README.md`
2. `DocsAI/ECS/README.md`
3. `DocsAI/ECS/Entity/`
4. `DocsAI/ECS/Data/Data系统说明.md`
5. `DocsAI/ECS/Event/Event系统说明.md`
6. `Src/ECS/Base/Entity/Core/`
7. `Data/DataKey/Generated/DataKey_Generated.cs`
8. `Data/EventType/`
9. 相关 owner skill：`ecs-entity`、`ecs-data`、`ecs-event`、`damage-system`、`ability-system`、`projectile-effect-system`、`movement-system`、`test-system`

最后读执行 SDD：

1. `sdds/<current>/README.md`
2. `sdds/<current>/design/main.md`
3. `sdds/<current>/tasks.md`
4. `sdds/<current>/bdd.md`
5. `sdds/<current>/progress.md`
6. `sdds/<current>/notes.md`

## 核心裁决

- **完整重构**：Entity / Relationship 不做兼容迁移。
- **Entity identity**：runtime API 只用 `EntityId`，raw `string entityId` 只允许出现在 JSON/log/DataOS 投影边界。
- **Registry**：`EntityRegistry` 是唯一注册表；找不到 Node 时返回 `EntityId.Empty`，不 fallback Godot InstanceId。
- **Spawn**：`EntitySpawnPipeline` 编排 spawn 阶段；`EntitySpawnRequest` 不包含业务 owner/source/target。
- **Lifecycle**：`LifecycleTree` 是唯一保留的 Relationship 语义，只表达单 parent 生命周期树。
- **Business reference**：Projectile / Effect / Ability / Item / UI / Component owner 全部迁到 typed runtime API、generated Data projection、owner list 或 capability-owned index。
- **Data boundary**：`GeneratedDataKey.Id` 只作为 `EntityId.Value` 的 DataOS / snapshot / observation 字符串投影；不恢复旧 `DataKey.Id`。
- **Event boundary**：Entity lifecycle 事件必须是 `readonly record struct` payload；不新增字符串事件名或 `XxxEventData`。
- **Damage attribution**：统计归因只读 `DamageAttribution`；缺 attribution 是错误，不走 parent chain。
- **Debug**：AI 调试看 observation dump，不查 runtime Relationship graph。
- **删除旧入口**：`EntityRelationshipManager`、`EntityRelationshipType`、`EntityRelationshipTraversal`、`EntityRelationshipLifecycle`、`EntityManager_Relationship`、`EntityManager_Ability`、`ParentRelationTypes`、`BindParentRelationships` 必须退出 runtime。

## 执行策略

### 主原则

按一个执行型 SDD 内的任务序推进，不要把 Entity core、Ability、Projectile、Effect、Damage、Movement 并行改成多个互不集成的分支。

推荐模式：

```text
主会话 = owner / integrator / 最终写入者
subagent = 只读侦察 / gap report / 测试建议 / 局部迁移草案
```

允许 subagent：

- 只读 grep 调用点。
- 按模块输出迁移草案。
- 建议 RED 测试矩阵。
- 审查某个切片是否仍有旧 API 残留。

不允许 subagent：

- 并行修改 Entity runtime core。
- 并行删除旧 Relationship 文件。
- 添加兼容 facade、双写 adapter 或 fallback。
- 修改 DataOS descriptor schema 后不回到主会话统一合并。

### Git 边界

改 SDD 文档和框架源码都在框架仓：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

游戏场景验证才进入游戏仓：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
git status --short
```

不要在游戏仓 `SlimeAI/` submodule 内直接做框架业务改动。

## TDD 任务序

### T0：执行 SDD 与基线扫描

目标：

- 创建或打开 `Entity Relationship Full Rewrite` 执行 SDD。
- 把设计包 6 个文件登记到 SDD design 入口或引用为项目级事实源。
- 运行当前 grep baseline，保存摘要到 SDD progress。
- 写第一批 RED 测试文件骨架，不改实现。

必须记录：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\.Id|GetInstanceId\(\)\.ToString\(\)" Src/ECS/Base Src/ECS/Test DocsAI/ECS
rg -n "EventData|EventName|const string .*Event|EntitySpawned|EntityDestroyed" Src/ECS Data/EventType DocsAI/ECS
```

### T1：EntityId 与 EntityRegistry

RED tests：

- `EntityId.Empty` 为空引用语义。
- `EntityId.New()` 非空且稳定。
- `EntityRegistry.Register` 拒绝 empty id、重复 id、重复 node。
- `GetEntityId(node)` 找不到返回 `EntityId.Empty`。
- `Snapshot()` 返回 copy。

实现：

- 新增 `EntityId`。
- 新增 `EntityRegistry`。
- 替换 Entity core 内部 raw string id。
- `GeneratedDataKey.Id` 只作为 `EntityId.Value` 的 DataOS / snapshot / observation 投影。
- 业务系统不散读 `GeneratedDataKey.Id`；需要身份时用 `IEntity.EntityId` 或 `EntityRegistry.GetEntityId(Node)`。

禁止：

- 不提供 implicit string conversion。
- 不使用 `GetInstanceId().ToString()` 生成 entity id。

### T2：LifecycleTree

RED tests：

- attach / detach / get parent / get children。
- empty/self/cycle/second parent 拒绝。
- destroy policy 是 typed field。
- `DetachAll(entity)` 清 parent side 和 child side。

实现：

- 新增 `LifecycleLink`。
- 新增 `LifecycleTree`。
- 删除 `EntityRelationshipLifecycle` 能力。
- 删除 `PARENT` relationType 写入。

### T3：EntityDestroyPipeline

RED tests：

- recursive child destroy。
- detach child 存活。
- children snapshot 防止漏删。
- destroy 顺序固定。
- 重复 destroy 返回明确 result。

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
- 不在 owner list 上决定 child 是否销毁。

### T4：ComponentRegistrar

RED tests：

- `IComponent` nodes 被注册。
- `OnComponentRegistered` 前 owner index 可查。
- `GetOwnerEntityId(component)` 返回 typed id。
- unregister 清 owner index。

实现：

- 把 `EntityManager_Component.cs` 扫描逻辑迁到 `ComponentRegistrar`。
- 删除 `ENTITY_TO_COMPONENT` 关系写入。
- 先保留 `EndsWith("Component")` 时必须写 TODO 和测试；最终目标是 `IComponent`。

### T5：EntitySpawnPipeline

RED tests：

- Data apply 失败 rollback。
- Visual inject 失败 rollback。
- Register 失败 rollback。
- Lifecycle attach 失败让 spawn fail。
- Component 注册在 registry 注册后。
- EntitySpawned 在所有阶段成功后发布。

实现：

- 新增 `EntitySpawnRequest`。
- 新增 `EntitySpawnPipeline`。
- 新增 `EntityNodeFactory` / `EntityDataInitializer` / `EntityVisualInitializer` / `EntityTransformInitializer`。
- 删除 `EntitySpawnConfig.ParentEntity / AutoAddParentRelation / ParentRelationTypes`。

### T6：OwnedReferenceRegistry 与 typed references

RED tests：

- `EntityIdList.Add` 去重。
- `EntityIdList.Remove` 返回新值。
- destroy child 后 owner list 自动清理。
- destroy owner 不通过 owner list 销毁 child。

实现：

- 新增 `EntityIdList`。
- 新增 `OwnedReferenceDescriptor`。
- 新增 `OwnedReferenceRegistry`。
- 各 capability 初始化注册 descriptor。
- 默认 DataOS 存储使用 generated string / string_array projection，转换集中在 owner service / helper 内；如果要 `DataKey<EntityId>`，必须先扩展 DataOS valueType / generator / validator / converter。

### T7：Ability owner service

RED tests：

- AddAbility 写 `Ability.OwnerEntityId`。
- owner list 添加 ability id。
- RemoveAbility 清 owner list。
- GetAbilities 不查 Relationship。
- owner destroy 时 ability 按 lifecycle policy 销毁。

实现：

- 新增 `AbilityInventoryService` 或合入明确 Ability service。
- 删除 `EntityManager_Ability.cs`。
- Feature/TestSystem/Ability components 改为 service + typed runtime reference / generated Data projection helper。

### T8：Projectile / Effect typed reference

Projectile RED tests：

- Spawn 写 `SourceEntityId` / `AbilityEntityId` / `TargetEntityId`。
- source `SpawnedProjectileIds` append。
- projectile destroy 清 owner list。
- projectile hit 构造 `DamageAttribution`。

Effect RED tests：

- Spawn 写 `SourceEntityId` / `TargetEntityId` / `AbilityEntityId`。
- attached effect lifecycle parent 是 host。
- DestroyByHost 不查 Relationship。
- effect component 不通过 parent chain 查 host。

实现：

- 删除 `ENTITY_TO_PROJECTILE` / `ENTITY_TO_EFFECT` 调用。
- Projectile / Effect spawn 都走新 spawn pipeline。

### T9：DamageAttribution 与 movement owner

RED tests：

- DamageInfo 必须携带 attribution。
- StatisticsProcessor 读 player/weapon/ability attribution。
- 缺 `DamageCreditEntityId` 失败。
- 改 lifecycle parent 不影响统计。
- Movement collision 阵营判断读 explicit source owner。

实现：

- 新增 `DamageAttribution`。
- 修改 `DamageInfo` / `DamageTool` / `AttackService` / projectile hit / effect tick / contact damage。
- 删除 `StatisticsProcessor.GetAncestorChain`。
- 删除 `CritProcessor` / `LifestealProcessor` / `MovementCollisionPolicy` parent-chain 归因。

### T10：Observation、Docs 和最终删除

RED tests：

- Entity dump 包含 typed entity id。
- Lifecycle dump 不输出 relationType。
- Typed reference dump 输出 owner lists。
- Damage trace 输出 attribution。
- Component owner dump 输出 owner/component pairs。

实现：

- 新增 `EntityObservationDumper`。
- 删除 `EntityRelationshipManager.GetDebugInfo` 依赖。
- 更新 `DocsAI/ECS/Entity/`、相关 owner 文档和 owner skills。
- 删除旧 Relationship 文件和旧测试。

## 每个任务的固定循环

1. 读当前 SDD `tasks.md` 和最新 `progress.md`。
2. 在正确 git 边界运行 `git status --short`。
3. RED：写失败测试或记录当前旧行为失败。
4. GREEN：实现最小代码。
5. REFACTOR：清理命名、边界、重复逻辑。
6. 运行最小测试。
7. 更新 `tasks.md` checkbox。
8. 更新当前 SDD `progress.md`。
9. 必要时更新项目 `progress.md` / `roadmap.md`。
10. 运行相关 grep gate。

## 验证命令

框架构建/测试：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

Godot scene smoke：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

SDD 校验：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
python3 Workspace/SDD/sdd.py validate <当前SDD>
python3 Workspace/SDD/sdd.py validate --all
```

最终 grep gate：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI

rg "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal" Src/ECS Data DocsAI
rg "public static partial class EntityManager" Src/ECS/Base/System
rg "EntityManager\.(AddAbility|GetAbilities|RemoveAbility)" Src/ECS
rg "GetAncestorChain|FindAncestorOfType<IUnit>" Src/ECS/Base/System Src/ECS/Base/Component
rg "DataKey\.Id" Src/ECS/Base/Entity Src/ECS/Base/System Src/ECS/Test DocsAI/ECS
rg "GetInstanceId\(\)\.ToString\(\)" Src/ECS/Base/Entity Src/ECS/Base/System Src/ECS/Test
rg "EventData|EventName|const string .*Event" Src/ECS Data/EventType DocsAI/ECS
```

## 完成定义

完成时必须给出：

- 旧文件/旧 API 删除清单。
- 新 Entity runtime 文件清单。
- grep gate 输出摘要。
- 单元测试结果。
- Entity validation scene 结果。
- Main scene smoke 结果。
- DocsAI / SDD / skill 更新摘要。

完成时不能留下：

- `EntityRelationshipManager` runtime 调用。
- `EntityRelationshipType` gameplay 调用。
- `BindParentRelationships`。
- `ParentRelationTypes`。
- `EntityManager_Ability`。
- `EntityManager.AddAbility/GetAbilities/RemoveAbility`。
- parent-chain damage attribution fallback。
- raw string entity id public API。
- test fixture 用 `GetInstanceId().ToString()` 伪造 entity id。
- string event name / `XxxEventData` lifecycle event。
