# SDD-0021 Data No-Compatibility Hard Cutover

## Index Card

- **Status**: done
- **Created**: 2026-05-30
- **Updated**: 2026-05-30
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Data/DataOS
  - SlimeAI/Data/DataKey
  - SlimeAI/DocsAI
- **Tags**: data, dataos, no-compat, refactor

## What This SDD Is About

本 SDD 承接项目设计 `design/Runtime/2.Data系统优化/06-无兼容完全重构总审计/`，目标是把 SDD-0020 后暴露的 Data 类型回归和旧兼容残留一次性收口。

核心裁决：Data 主链路不再允许兼容层。SQLite descriptor 是字段定义事实源；snapshot records 不能重新定义字段类型；generated handle 必须表达真实 CLR 类型；业务层不能再通过 `DataKey<T> -> string`、`DataKey.Xxx` alias、public string-key API、`new Data()` 未绑定窗口或 Resource/tres authoring 旧入口绕回旧系统。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/README.md` — 06 无兼容总审计的自包含副本
3. `design/main.md` — 本 SDD 执行设计
4. `tasks.md` — 当前任务拆分
5. `bdd.md` — 行为验收场景
6. `execution-prompt.md` — 新执行会话一次性提示词
7. `Core/progress.md` — 最近结论和恢复点
8. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Data no-compat hard cutover 已完成；descriptor 是字段定义事实源，generated handle 已使用真实 CLR 类型，旧 DataKey/string/RuntimeTables/Resource authoring 兼容路线已删除或迁出当前事实源。
- **Next Action**: 从 PRJ-0002 `design/Runtime/3.Entity系统优化/` 和 `Core/entity-rewrite-execution-prompt.md` 创建并执行 Entity Relationship Full Rewrite SDD。
- **Open Blockers**: none
