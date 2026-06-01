# Entity Relationship Full Rewrite

## Goal

用 hard cutover 方式重构旧 ECS Entity / Relationship runtime，删除通用字符串关系图和业务 `EntityManager` partial，让 Entity runtime 回到 AI-first 的身份、生命周期和可观察事实基础设施。

完成后：

- Entity identity 使用 typed `EntityId`，不把 Godot instance id 或 raw string 当 runtime API。
- `EntityRegistry` 是唯一注册表；`GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 字符串投影。
- `LifecycleTree` 是唯一保留的 Relationship 语义，只表达单 parent 生命周期树和 parent destroy policy。
- Projectile / Effect / Ability / Item / UI / Component owner 不进入通用 Relationship 图，改为 typed runtime reference、generated Data projection、owner list 或 owner service。
- Damage attribution 使用明确 `DamageAttribution`，不沿 parent chain 猜。
- Entity lifecycle events 使用 typed payload，不新增字符串事件名或 `XxxEventData`。
- `EntityManager` 不再兼任 Ability / Projectile / Effect / Damage / UI 业务入口。

## Context

### 必读事实源

按顺序阅读：

1. `../../design/06-ECS完全重构执行原则.md`
2. `../../design/3.Entity系统优化/README.md`
3. `../../design/3.Entity系统优化/1.初级修改/06-2026-05-31-DataEventDocsAI同步校准.md`
4. `../../design/3.Entity系统优化/1.初级修改/00-研究证据与裁决.md`
5. `../../design/3.Entity系统优化/1.初级修改/01-目标架构与模块拆分.md`
6. `../../design/3.Entity系统优化/1.初级修改/02-代码实现说明.md`
7. `../../design/3.Entity系统优化/1.初级修改/03-LifecycleTree与业务引用设计.md`
8. `../../design/3.Entity系统优化/1.初级修改/04-完全重构范围与TDD测试计划.md`
9. `../../design/3.Entity系统优化/1.初级修改/05-源码调用点迁移清单.md`
10. `../../design/3.Entity系统优化/2.重构/main.md`
11. `../../entity-rewrite-execution-prompt.md`
12. `../../../../../DocsAI/ECS/Entity/README.md`
13. `../../../../../DocsAI/ECS/Data/Data系统说明.md`
14. `../../../../../DocsAI/ECS/Event/Event系统说明.md`

### 当前约束

- 当前 Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- 游戏验证仓：`/home/slime/Code/SlimeAI/Games/BrotatoLike`，只在 Godot scene smoke 时进入。
- Data 当前事实源是 DataOS descriptor -> runtime snapshot -> generated handle -> catalog-bound Data。
- Event 当前事实源是 `readonly record struct` payload 类型主键。
- DocsAI 是框架文档统一入口；`Src/ECS` 不保存框架 Markdown 文档。
- 当前 DataOS 没有原生 `entity_id/entity_id_list` valueType。本 SDD 默认不扩展 DataOS 类型系统，只使用 typed runtime API + generated string / string_array projection。若执行中决定扩展，必须先单独完成 schema / generator / validator / converter / DataOS 场景测试。

## Design

### Target Runtime Modules

| 模块 | 职责 |
| --- | --- |
| `EntityId` | typed runtime identity；不提供 implicit string conversion |
| `EntityRegistry` | id -> node、node -> id 注册表；找不到返回 `EntityId.Empty` |
| `EntitySpawnPipeline` | 编排 spawn 阶段；不包含业务 owner/source/target |
| `EntityNodeFactory` | 对象池 / 场景实例化 |
| `EntityDataInitializer` | runtime snapshot apply、`GeneratedDataKey.Id` 投影和一致性校验 |
| `EntityVisualInitializer` | 视觉场景注入 |
| `EntityTransformInitializer` | 位置、旋转和物理同步 |
| `ComponentRegistrar` | Component 注册、反查、卸载和内部 owner index |
| `LifecycleTree` | 单 parent 生命周期树、cycle guard、destroy policy |
| `OwnedReferenceRegistry` | owner list cleanup hook；不决定 lifecycle destroy |
| `EntityDestroyPipeline` | lifecycle child destroy、detach、owner cleanup、component unregister、data/events clear、registry unregister、pool return / queue free |
| `EntityObservationDumper` | AI / debug 可读 facts，不作为 gameplay API |

### 删除旧入口

本 SDD 不保留兼容 facade，不做双写：

- `EntityRelationshipManager`
- `EntityRelationshipType`
- `EntityRelationshipTraversal`
- `EntityRelationshipLifecycle`
- `EntityManager_Relationship`
- `EntityManager_Ability`
- `ParentRelationTypes`
- `AutoAddParentRelation`
- `BindParentRelationships`
- `EntityManager.AddAbility/GetAbilities/RemoveAbility`（T1.9 后仅保留兼容 facade；新事实源为 `AbilityInventoryService.Runtime`）
- parent-chain damage attribution
- raw string entity id public API

### Data Boundary

- runtime API 使用 `EntityId` / `EntityIdList`。
- `GeneratedDataKey.Id` 是 `EntityId.Value` 的字符串投影。
- `SourceEntityId / OriginEntityId / owner list` 等 DataOS 字段默认仍是 generated string / string_array projection。
- 转换集中在 owner service / helper 内，不让 raw string entity id 扩散到 capability public API。

### Event Boundary

Entity lifecycle 事件目标形态：

```csharp
public static partial class GameEventType
{
    public static partial class Entity
    {
        public readonly record struct Spawned(EntityId EntityId);
        public readonly record struct Destroying(EntityId EntityId);
        public readonly record struct Destroyed(EntityId EntityId);
    }
}
```

不新增 `const string EntitySpawned`，不新增 `EntitySpawnedEventData`。

### Execution Rule

每个切片必须先写 RED tests，再改实现。每完成一个切片：

1. 更新 `tasks.md` checkbox。
2. 追加 `progress.md` 记录结论和验证。
3. 同步必要 `DocsAI/ECS/Entity/` 或 owner 文档。
4. 运行该切片验证。

## Verification

### Baseline / Final Grep Gates

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\\.Id|GetInstanceId\\(\\)\\.ToString\\(\\)" Src/ECS Data DocsAI
rg -n "EventData|EventName|const string .*Event|EntitySpawned|EntityDestroyed" Src/ECS Data/EventType DocsAI/ECS
```

最终 gate 允许命中：

- 本 SDD / 项目设计文档里的禁止项、历史证据和 grep gate。
- `GeneratedDataKey.Id` 的 DataOS / snapshot / observation 投影。

最终 gate 不允许命中：

- runtime hot path 使用 `DataKey.Id`。
- capability public API 使用 raw string entity id。
- current DocsAI 示例推荐旧 Relationship API、旧 Event string key 或 `XxxEventData`。
- test fixture 用 `GetInstanceId().ToString()` 伪造 entity id。

### Build / Tests

完成 runtime 改动后必须运行：

```bash
Tools/run-build.sh
Tools/run-tests.sh
python3 Workspace/SDD/sdd.py validate SDD-0024
python3 Workspace/SDD/sdd.py validate --all
```

若影响 Godot scene / BrotatoLike glue，追加：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
