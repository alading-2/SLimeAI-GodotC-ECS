# SDD-0024 Entity Relationship Full Rewrite

## Index Card

- **Status**: active
- **Created**: 2026-05-31
- **Updated**: 2026-05-31
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

1. `../../design/06-ECS完全重构执行原则.md` — 项目级 hard cutover 原则
2. `../../design/3.Entity系统优化/README.md` — Entity 设计包入口
3. `../../design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md` — Data/Event/DocsAI current override
4. `../../entity-rewrite-execution-prompt.md` — 总执行提示词
5. `../../../../../DocsAI/ECS/Entity/README.md` — DocsAI Entity current 入口
6. `design/INDEX.md` — 本 SDD 设计入口
7. `design/main.md` — 本 SDD 执行设计
8. `execution-prompt.md` — 可直接交给新执行会话的执行计划提示词
9. `tasks.md` — 当前任务拆分
10. `progress.md` — 最近结论和恢复点
11. `bdd.md` — 行为验收场景
12. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T1.5
- **Last Conclusion**: T1.1~T1.4 已完成；`EntityId` / `EntityRegistry` / `LifecycleTree` 最小 runtime API 和 RED/GREEN 测试已落地，`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过。
- **Next Action**: 写 `EntityDestroyPipeline` RED tests；旧 Relationship runtime 暂不删除，等 destroy/spawn/component 切片接入后再做 hard cutover 删除。
- **Open Blockers**: none
