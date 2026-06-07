# SDD-0024 Entity Relationship Full Rewrite

## Index Card

- **Status**: done
- **Created**: 2026-05-31
- **Updated**: 2026-06-01
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/entity
- **Tags**: entity, relationship, hard-cutover, data-event-docsai-sync

## What This SDD Is About

本 SDD 执行 PRJ-0002 的 Entity / Relationship hard cutover。

目标不是给旧 `EntityRelationshipManager` 增加兼容层，而是按当前 Data / Event / DocsAI 契约完整重构 Entity runtime：

- runtime identity 收口到 typed `EntityId` / `EntityRegistry`。
- lifecycle parent 收口到 `LifecycleTree`。
- Projectile / Effect / Ability / UI / Component owner 迁出通用 Relationship 图。
- `GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 字符串投影。
- Entity lifecycle event 使用 typed payload，不恢复字符串事件名或 `XxxEventData`。
- Damage attribution 不再沿 parent chain 猜。

## Reading Order

1. `../../design/Foundation/06-ECS完全重构执行原则.md` — 项目级 hard cutover 原则
2. `../../design/Runtime/3.Entity系统优化/README.md` — Entity 设计包入口
3. `../../design/Runtime/3.Entity系统优化/1.初级修改/06-2026-05-31-DataEventDocsAI同步校准.md` — Data/Event/DocsAI current override
4. `../../design/Runtime/3.Entity系统优化/2.重构/main.md` — Entity spawn 统一底层管线与业务 facade 当前裁决
5. `../../entity-rewrite-execution-prompt.md` — 总执行提示词
6. `../../../../../DocsAI/ECS/Entity/README.md` — DocsAI Entity current 入口
7. `design/INDEX.md` — 本 SDD 设计入口
8. `design/main.md` — 本 SDD 执行设计
9. `execution-prompt.md` — 可直接交给新执行会话的执行计划提示词
10. `tasks.md` — 当前任务拆分
11. `Core/progress.md` — 最近结论和恢复点
12. `bdd.md` — 行为验收场景
13. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: T1.1~T1.11 已完成；Entity Relationship hard cutover 当前 checkout 已收口，Projectile / Effect / Ability owner 已迁到对应 service，Damage / Movement 归因已迁到 `EntityAttributionResolver`，DocsAI 与 skill 已同步。
- **Next Action**: 后续只处理收尾治理：继续删除 `LegacyRelationship/` 中剩余兼容代码、提交/归档打包，或按新问题创建新的 SDD。
- **Open Blockers**: none
