# 2026-05-31 Data / Event / DocsAI 同步校准

> 更新：2026-05-31
> 状态：current override
> 用途：在执行 Entity / Relationship hard cutover 前，校准 2026-05-29 设计包中已经被 Data、Event 和 DocsAI 更新覆盖的事实源。

## 0. 结论

`3.Entity系统优化/` 的 hard cutover 方向继续成立：

- 删除旧 `EntityRelationshipManager / EntityRelationshipType / ParentRelationTypes / BindParentRelationships`。
- Entity identity 进入 typed `EntityId`。
- lifecycle parent 只进入 `LifecycleTree`。
- Projectile / Effect / Ability / UI / Component owner 不再进入通用 Relationship 图。
- Damage attribution 不再沿 parent chain 猜。

但执行前必须用当前框架事实源覆盖旧文档里的三类旧假设：

| 旧假设 | 当前裁决 |
| --- | --- |
| `DocsNew` / `Src/ECS` 旁文档是 current 入口 | `DocsAI/` 是 SlimeAI 框架文档统一入口；`DocsAI/ECS/` 是功能文档事实源；`Src/ECS` 不保存框架 Markdown 文档。 |
| `DataKey.Id` 和 `GeneratedDataKey.Id` 需要再确认是否等价 | 旧 `DataKey` 别名和 `DataKey<T> -> string` 隐式转换已删除；新代码只使用 `GeneratedDataKey.Id` 作为 EntityId 的 DataOS 字符串投影。 |
| Event 仍可按字符串事件名 / `XxxEventData` 写 | EventBus 当前以 payload 类型作为事件 key；新增 Entity lifecycle 事件必须是 `readonly record struct` payload，不能新增字符串事件名。 |

## 1. Context Read

已读事实源：

- `DocsAI/README.md`
- `DocsAI/ECS/README.md`
- `DocsAI/ECS/Data/Data系统说明.md`
- `DocsAI/ECS/Event/Event系统说明.md`
- `DocsAI/ECS/Entity/`
- `Src/ECS/Base/Entity/Core/`
- `Src/ECS/Base/Data/DataValueType.cs`
- `Data/DataKey/Generated/DataKey_Generated.cs`
- `Data/DataOS/Authoring/DataKeyDescriptors.seed.sql`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Foundation/06-ECS完全重构执行原则.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/3.Entity系统优化/`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/entity-rewrite-execution-prompt.md`

Git boundary：

- 当前框架仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 游戏验证仓：`/home/slime/Code/SlimeAI/Games/BrotatoLike`
- 不把外层 `/home/slime/Code/SlimeAI` 描述为当前 AI 配置仓或当前执行仓。

## 2. Data 同步裁决

### 2.1 EntityId 与 Data 的边界

当前 Data 主链路是：

```text
DataOS SQLite authoring
  -> runtime_snapshot.json descriptors / records
  -> DataDefinitionCatalog
  -> GeneratedDataKey
  -> catalog-bound Data
  -> Entity.Events data changed payload
```

Entity hard cutover 不能恢复：

- 手写 `DataKey.*` 别名作为业务入口。
- `DataKey<T> -> string` 隐式转换。
- `DataRegistry / DataMeta` 作为字段定义事实源。
- `DataKey.Id` 作为 Entity identity 入口。
- 运行时热路径直接查 SQLite 或旧 config object fallback。

执行目标：

- `EntityId` 是 runtime API 的唯一身份类型。
- `IEntity` 在 hard cutover 后应显式暴露 `EntityId EntityId { get; }`，或由 `EntityRegistry.GetEntityId(Node)` 作为唯一公开读取入口；不能继续靠 `Data.Get<string>(GeneratedDataKey.Id)` 在业务系统里散读身份。
- `GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 的字符串投影，由 `EntityDataInitializer` 统一写入并校验与 registry id 一致。
- 旧代码中读取 `GeneratedDataKey.Id` 的业务调用点应迁到 `EntityId` / `EntityRegistry` / owner service；只允许 Data projection、observation、日志和迁移验证读取字符串投影。

### 2.2 业务引用 DataKey 的当前形态

当前 `DataValueType` 还没有 `EntityId` / `EntityIdList` 原生类型，`GeneratedDataKey.SourceEntityId` 当前是 `DataKey<string>`。

因此 Entity 执行 SDD 必须先选择其中一条路径，并写入 tasks：

| 方案 | 说明 | 裁决 |
| --- | --- | --- |
| A. 扩展 DataOS valueType / generator 支持 `entity_id` 和 `entity_id_list` | 让 generated handle 直接生成 `DataKey<EntityId>` / `DataKey<EntityIdList>` | 只有当本次 Entity SDD 明确承担 DataOS schema/generator 改动时才采用。 |
| B. 保持 DataOS 字符串投影，运行时 API 用 typed wrapper | DataOS descriptor 仍是 `string` / `string_array`，Capability 通过 helper 读写 `EntityId` / `EntityIdList` | 默认采用，避免为了 Entity 重构再次扩大 DataOS 类型系统范围。 |

默认执行规则：

- capability public API 不接受 `string entityId`。
- business reference 的存储可以是 generated `DataKey<string>` / `DataKey<string[]>`，但只能通过 owner service / helper 读写，不能在业务代码散落 `.Value` / `EntityId.From` 转换。
- owner list 如果暂用字符串数组或稳定分隔格式，必须有专门 value object 封装，并有去重、移除、空值和 destroyed cleanup 测试。
- 如果后续决定采用方案 A，必须先更新 DataOS descriptor、generator、validator、DataValueConverter 和 DataOS 场景测试，不能只手写 C# `DataKey<EntityId>`。

## 3. Event 同步裁决

Entity spawn / destroying / destroyed 事件应遵守当前 Event 系统：

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

规则：

- payload 类型本身就是事件 key，不新增 `const string EntitySpawned`。
- 不使用 `EntitySpawnedEventData` / `EntityDestroyedEventData` 后缀。
- Entity 内部 component 协作优先 `entity.Events`。
- 跨实体低频广播才用 `GlobalEventBus.Global`。
- framework payload 不直接持有 Godot `Node` / `Vector2` / `Rect2`，除非先证明它属于 GodotBridge 通用协议。
- 事件不是状态存储；spawn/destroy 当前状态仍以 `EntityRegistry / LifecycleTree / Data` 为事实源。

Entity hard cutover 的测试必须覆盖：

- spawn 成功后只发布 typed `GameEventType.Entity.Spawned`。
- destroy lifecycle 顺序中 `Destroying` / `Destroyed` 的发布时间点明确。
- 事件 handler 内若触发结构变更，必须遵守当前 EventBus 能力边界，不新增事件专用隐式 deferred queue。

## 4. DocsAI 同步裁决

Entity hard cutover 完成时，文档更新目标不是 `Src/ECS/**.md` 或 `DocsNew`，而是：

- `DocsAI/ECS/Entity/`
- `DocsAI/ECS/Data/` 中被 Entity 引用的 Data 规则
- `DocsAI/ECS/Event/` 中 Entity lifecycle 事件示例
- 受影响 owner：`DocsAI/ECS/System/AbilitySystem/`、`DocsAI/ECS/System/DamageSystem/`、`DocsAI/ECS/System/Movement/`、`DocsAI/ECS/System/Effect/`、`DocsAI/ECS/UI/`
- `DocsAI/管理/Src文档迁移清单.md` 仅在移动/新增 DocsAI 文档时更新
- 对应 `.ai-config/skills/` 源文件；同步副本不直接维护

`Src/ECS` 不新增 Markdown 指针文档。代码注释可短链到 `DocsAI/ECS/...`，但不复制长文档。

## 5. DesignCritic

### Assumptions

- 当前任务只更新设计和执行文档，不改运行时代码。
- Data 的 `EntityId` 原生 value type 尚未实现。
- Event 当前仍是旧 ECS 的 `GameEventType` payload 类型主键，不是 GameOS `WorldEvents`。
- Entity hard cutover 仍会作为后续独立执行型 SDD 处理。

### Design Defects Found

- 旧 Entity 文档把 `DataKey.Id` 当成仍需确认的身份源，这会诱导执行者恢复旧 DataKey alias。
- 多处 grep gate 从 `/home/slime/Code/SlimeAI` + `SlimeAI/Src` 执行，和当前仓边界不一致。
- 旧设计把 `DataKey<EntityId?>` 写成目标代码形态，但当前 DataOS generator 不支持该类型；直接照做会产生手写第二事实源。
- Event 只写“发布 EntitySpawned”，没有指定 typed payload、scope 和 payload 禁止 Godot Node 的规则。
- 文档完成清单仍要求更新 `Src/ECS` 旁文档和 `DocsNew`，与 DocsAI 统一管理冲突。

### Recommendation

采用“设计校准 + 执行门禁更新”，不在本次文档修订中扩大到运行时代码：

1. 新增本校准文档作为 `3.Entity系统优化/` 的执行前 override。
2. 更新现有 00~05 文档中的旧路径、旧 DataKey、旧 Event、旧 DocsAI 边界。
3. 更新 `Core/entity-rewrite-execution-prompt.md`，让新会话从当前框架仓根执行。
4. 更新项目 `design/INDEX.md` / `Core/roadmap.md` / `Core/progress.md`，避免旧恢复点继续指向 DocsNew。

## 6. Must Confirm

这些问题不阻塞本次文档更新，但阻塞后续代码执行 SDD：

- Entity hard cutover 是否同时扩展 DataOS 原生 `entity_id` / `entity_id_list` 类型。默认不扩展，只做 typed runtime API + generated string projection。
- `IEntity` 是否直接暴露 `EntityId EntityId { get; }`。默认建议暴露，减少业务代码散读 Data projection。
- Entity lifecycle typed events 是否放在 `Data/EventType/Entity/GameEventType_Entity.cs`。默认放这里。
- Destroying / Destroyed 的精确发布时间点。默认 `Destroying` 在 lifecycle recursive children 之前，`Destroyed` 在 unregister + Data/Events clear 后发布 summary。

## 7. Updated Gates

执行 SDD 的 baseline gate 应从框架仓根运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\.Id|GetInstanceId\(\)\.ToString\(\)" Src/ECS Data DocsAI
rg -n "EventData|EventName|const string .*Event|EntitySpawned|EntityDestroyed" Src/ECS Data/EventType DocsAI/ECS
rg -n "DocsNew|Src/ECS/.+\.md|SlimeAI/Src|SlimeAI/DocsAI|cd /home/slime/Code/SlimeAI$" SDD/project/projects/PRJ-0002-ecs-framework-refactor DocsAI
```

最终 gate 应允许命中：

- 历史 SDD / archived docs。
- 本设计包中明确标为旧入口、禁止项、grep gate 的文本。
- `GeneratedDataKey.Id` 字符串投影。

最终 gate 不允许命中：

- runtime hot path 使用 `DataKey.Id`。
- capability public API 使用 raw string entity id。
- current DocsAI 示例推荐旧 Relationship API、旧 Event string key 或 `XxxEventData`。
- test fixture 使用 `GetInstanceId().ToString()` 伪造 entity id。
