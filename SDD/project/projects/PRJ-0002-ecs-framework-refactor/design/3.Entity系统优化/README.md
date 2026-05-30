# Entity 系统完整重构设计包

> 更新：2026-05-29
> 状态：current design package
> 入口：`00-研究证据与裁决.md`
> 裁决：Entity/Relationship 不做兼容 facade，不做旧 Relationship 双写，不保留 legacy parent-chain 归因；按 hard cutover 完整重构。

## 0. 本设计包回答什么

Entity 重构不是一个“把 `EntityRelationshipManager` 换成 `LifecycleTree`”的小改动。当前旧 ECS 的 Entity core 同时牵动：

- Entity identity：当前 raw string / Godot InstanceId / Data id 混用。
- Spawn：对象池、场景实例化、DataOS apply、视觉注入、Transform、Relationship、Component 注册、事件广播塞在一个大函数。
- Lifecycle：`PARENT` 关系承担递归销毁，但生命周期策略存在 `Dictionary<string, object>` 里。
- Business reference：Projectile / Effect / Ability / Item / UI / Component owner 都塞进 `EntityRelationshipType`。
- Damage attribution：伤害统计沿 parent chain 猜玩家、武器、技能。
- Component：Component 注册用 Relationship 反查 owner。
- Migration：Entity migration 也塞在 `EntityManager` partial 里。
- Observation：调试依赖 Relationship debug info，而不是 typed lifecycle/reference dump。

本设计包把这些点逐个拆开，给出完整重构后的代码形状、删除范围、替代 API、调用点迁移方式和 TDD 验证计划。

## 1. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `00-研究证据与裁决.md` | research-decision | 当前代码事实、外部 ECS / 引擎对照、AiFirst 参考采纳、hard cutover 裁决和 Design Discovery 记录。 |
| `README.md` | design-index | 本文件。给出总裁决、阅读顺序、模块边界和完成定义。 |
| `01-目标架构与模块拆分.md` | architecture | 从 AI-first 角度定义新 Entity runtime 的职责、非职责和模块边界。 |
| `02-代码实现说明.md` | code-shape | 按目标文件和类说明怎么改，包括新类型、删除旧文件、核心 API 和伪代码。 |
| `03-LifecycleTree与业务引用设计.md` | relationship-design | 详细说明 lifecycle tree、typed business reference、owner cleanup、damage attribution、UI/Component binding。 |
| `04-完全重构范围与TDD测试计划.md` | rewrite-test-plan | 删除清单、执行任务序、grep gate、单元/场景测试和验收标准。 |
| `05-源码调用点迁移清单.md` | callsite-migration | 基于当前源码 grep 把旧 Relationship / EntityManager 调用点按模块拆成迁移表、测试重写矩阵和最终门禁。 |

## 2. 总裁决

采用 hard cutover：

```text
旧 EntityManager / Relationship runtime
  -> 删除业务兼任、删除通用字符串关系图、删除 parent-chain 归因

新 Entity runtime
  -> typed EntityId
  -> EntityRegistry
  -> EntitySpawnPipeline
  -> LifecycleTree
  -> ComponentRegistrar
  -> OwnedReferenceRegistry
  -> typed business reference
  -> explicit DamageAttribution
  -> Observation dumps
```

不采用：

- 不做旧 `EntityManager` static facade 兼容层。
- 不保留 `EntityRelationshipManager` 作为 runtime hot path。
- 不保留 `EntityRelationshipType`。
- 不保留 `ParentRelationTypes`。
- 不做 `PARENT` 双写。
- 不让 `StatisticsProcessor` fallback 到 parent chain。
- 不让 `EntityManager_Ability` 继续作为 `EntityManager` partial。

## 3. AI-first 原则

本次重构的 AI-first 目标不是“抽象更多”，而是让 AI 无需猜语义：

| 旧问题 | AI-first 规则 |
| --- | --- |
| `string relationType` 看不出语义 | 每种实体引用必须有 typed 字段名和 owner 文档。 |
| `ParentEntity` 既像销毁 parent 又像伤害 owner | Lifecycle parent 只负责生命周期；伤害归因走 `DamageAttribution`。 |
| `EntityManager` partial 太多 | Entity core 不包含 Ability / Projectile / Effect / Item / UI 业务方法。 |
| Data id / InstanceId 混用 | Runtime API 只接受 `EntityId`；序列化才转 string。 |
| Debug 靠关系图 | Debug 输出 Lifecycle dump、TypedReference dump、Component owner dump、DamageAttribution trace。 |
| 旧调用点可以绕过新规则 | 完成标准包含 grep gate，旧 API 残留即失败。 |

## 4. 目标模块边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `EntityId` | Runtime entity identity typed value | 不隐式转换 string，不表达 Godot InstanceId。 |
| `EntityRegistry` | 注册、反查、snapshot、销毁入口 | 不知道 Ability、Projectile、Effect、UI。 |
| `EntitySpawnPipeline` | 编排 spawn 阶段，集中失败回滚 | 不直接处理业务 relationType。 |
| `EntityNodeFactory` | 对象池/场景实例化和场景树 attach | 不写 Data，不发业务事件。 |
| `EntityDataInitializer` | DataOS record apply 和 `GeneratedDataKey.Id` 一致性 | 不读 Relationship。 |
| `EntityVisualInitializer` | VisualScene 注入和碰撞模板同步 | 不注册 Component。 |
| `EntityTransformInitializer` | Position / Rotation / ForceUpdateTransform / 池化物理同步 | 不决定生命周期 parent。 |
| `ComponentRegistrar` | Component 扫描、注册、注销、owner index | 不通过 public Relationship 表。 |
| `LifecycleTree` | 单 parent 生命周期树、防环、detach、recursive destroy | 不表达 owner/source/target/ability/item/ui。 |
| `OwnedReferenceRegistry` | owner list 自动清理 | 不决定 child 的生命周期。 |
| `EntityObservationDumper` | 可观察输出 | 不作为 gameplay 查询源。 |

## 5. 完成定义

重构完成不是“新类能编译”。

必须同时满足：

- `rg "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal" SlimeAI/Src` 对 runtime 调用无命中。
- `rg "public static partial class EntityManager" SlimeAI/Src/ECS/Base/System` 对业务系统无命中。
- `EntitySpawnConfig` 不包含 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- `DamageInfo` 或等价伤害上下文包含 explicit attribution。
- Projectile / Effect / Ability 归属写 typed DataKey / owner list。
- Component owner 反查走 `ComponentRegistrar` 内部 index。
- Lifecycle attach/detach/destroy 有独立测试。
- Spawn pipeline 阶段顺序有测试。
- Godot headless 场景 smoke 通过。
- DocsAI / SDD / owner skill 同步新入口，不再指向旧 Relationship runtime。

## 6. 阅读顺序

1. 先读 `00-研究证据与裁决.md`，确认为什么不保留旧 Relationship runtime。
2. 再读 `01-目标架构与模块拆分.md`，确认重构后的模块边界。
3. 再读 `02-代码实现说明.md`，看每个目标类怎么写。
4. 再读 `03-LifecycleTree与业务引用设计.md`，确认旧 Relationship 的每一种语义怎么替换。
5. 再读 `04-完全重构范围与TDD测试计划.md`，确认 hard cutover 的 T1~T10 任务序。
6. 最后读 `05-源码调用点迁移清单.md`，按实际文件和旧 API 命中开始改测试和调用点。

执行会话入口使用项目根的 `../../entity-rewrite-execution-prompt.md`。

## 7. 事实源边界

Entity / Relationship 的长期设计文档只保留在本目录。旧根目录 Entity 分析文档已迁入 `00-研究证据与裁决.md`，不再作为独立入口存在。
