# 优化优先级与 SDD 拆分建议

> 更新：2026-05-29
> 目的：把本轮问题分析转成后续可执行 SDD 的候选顺序。
> 原则：小步、可验证、不再做整体 ECS 重构；Data 子系统已完整重构；Entity / Relationship 按最新裁决 hard cutover 完整重构。

## 0. 2026-05-29 状态更新

本文是早期拆分建议，Data 部分已经由 SDD-0012 至 SDD-0019 执行完成。

Entity / Relationship 部分不再按本文旧的 `SDD-C` + `SDD-D` 拆成“关系类型统一”和“生命周期契约审计”两条弱优化线。用户已明确要求不做兼容和不做兼任，因此新的事实源是：

- `design/3.Entity系统优化/README.md`
- `design/3.Entity系统优化/01-目标架构与模块拆分.md`
- `design/3.Entity系统优化/02-代码实现说明.md`
- `design/3.Entity系统优化/03-LifecycleTree与业务引用设计.md`
- `design/3.Entity系统优化/04-完全重构范围与TDD测试计划.md`
- `design/3.Entity系统优化/05-源码调用点迁移清单.md`
- `entity-rewrite-execution-prompt.md`

新的 Entity 执行 SDD 应创建为单个 hard cutover SDD，内部按 TDD 任务序推进，不保留 `EntityRelationshipManager`、`EntityRelationshipType`、`ParentRelationTypes`、`BindParentRelationships`、业务 `EntityManager` partial 或 parent-chain damage attribution。

## 1. 不再执行的旧任务语义

以下旧语义应停止：

- 把旧 ECS 当作临时输入，目标是搬到另一套框架。
- 先改目录结构，再按新目录反推模块设计。
- 一次性替换 Event / Entity / Relationship。
- 为了追求新架构而保留两套并行 API。

这些思路可以作为历史背景理解，但不再作为 PRJ-0002 当前事实源。Data 是例外：它不是整体 ECS 替换，而是 Data 子系统内部完整重构，且必须删除旧 Data 输入路径和重建测试场景。

## 2. 建议 SDD 顺序

### SDD-A：Data System Full Rewrite 第一切片

优先级：P0

目标：

- 先写新 Data 系统 TDD 红灯测试。
- 建立旧 Data 定义一次性审计报告，而不是长期兼容层。
- 明确 `runtime_snapshot.json.descriptors` 是 Data 字段定义事实源。
- 明确 `SlimeAI/Data/Data/` 与 `SlimeAI/Data/DataNew/` 不作为新 Data 输入路径保留。
- 明确 `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/` 不修补旧测试，按新 Data TDD 矩阵重建。
- 建立 `DataDefinitionCatalog` 最小闭环。
- 补充 DataOS descriptor authoring 规范。

验收：

- 有 RED / GREEN / REFACTOR 记录。
- 有旧 Data 定义一次性审计 artifact。
- 新增规则能说明哪些字段必须来自 descriptor，哪些旧路径必须删除。
- Data catalog 小测试通过。

### SDD-B：Event 定义事实源恢复与主键优化

优先级：P0

目标：

- 解释并修复当前 `Data/EventType` 只有 `.uid`、缺少 `.cs` 的事实源断裂。
- 生成 `GameEventType` 调用清单。
- 按 owner 分类事件。
- 选择一个低风险事件域试点 typed event key 或 typed payload API。

验收：

- `GameEventType` 定义事实源可读、可构建。
- 新事件禁止继续新增裸字符串。
- 事件试点有测试或场景验证。

### SDD-C：Relationship type 与关系数据键统一（已替换）

优先级：已替换为 `Entity Relationship Full Rewrite`

旧目标：

- 建立 `EntityRelationshipType` 清单和 owner。
- 明确每种关系的约束、方向、是否参与生命周期。
- 统一 `parent_destroy_policy` 等关系数据键。
- 文档化 PARENT 与业务关系的边界。

旧验收：

- 关系类型不再只是一组字符串。
- 父销毁策略读取有明确契约。
- 关系绑定/销毁测试通过。

### SDD-D：Entity 生命周期契约文档与直接操作审计（已替换）

优先级：已替换为 `Entity Relationship Full Rewrite`

旧目标：

- 写清 `EntityManager.Spawn` 阶段顺序。
- 审计直接 `new Entity`、`QueueFree`、手动 Register、绕过对象池的调用。
- 明确 Component 识别规则和 `IComponent` 推荐路径。

旧验收：

- 生命周期契约进入 DocsAI/模块文档。
- 高风险绕过入口有扫描结果。
- 不改变玩法行为。

## 3. 历史说明

以下内容保留为 2026-05-28 的历史决策背景：当时不建议立刻改代码，是因为 Data / Event / Entity 边界尚未冻结，容易把旧大重构残留或兼容路径混进实现。

现在状态已变化：

- Data 完整重构已完成。
- Entity / Relationship 已冻结为 hard cutover 完整重构。
- 后续可以创建 Entity 执行型 SDD，但必须按 `entity-rewrite-execution-prompt.md` 的 TDD 任务序推进。

## 4. 推荐下一步

推荐创建 Entity 执行型 SDD：

```text
Title: Entity Relationship Full Rewrite
Scope: SlimeAI/Src/ECS/Base/Entity/Core, AbilitySystem, ProjectileSystem, EffectSystem, DamageSystem, Movement, UI/TestSystem, DocsAI, Entity validation scenes
```

原因：

- 用户已裁决 Entity / Relationship 不做兼容、不做兼任、完整重构。
- `design/3.Entity系统优化/` 已提供目标架构、代码实现说明、业务引用替换、TDD 计划和源码调用点迁移清单。
- 当前旧 Relationship 命中覆盖 Entity core、Ability、Projectile、Effect、Damage、Movement 和测试，不能再用弱优化方式局部修补。
