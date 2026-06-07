# SDD-0022 Data Projection Diagnostics Contract Hardening

## Index Card

- **Status**: done
- **Created**: 2026-05-30
- **Updated**: 2026-05-30
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - SlimeAI/Data
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Src/ECS/Base/Entity
  - SlimeAI/Src/ECS/Base/Component/Movement
- **Tags**: data, diagnostics, projection, no-compat

## What This SDD Is About

本 SDD 承接 SDD-0021 Data no-compat hard cutover 后新增的 4 份残余问题文档，目标不是再清一轮旧兼容入口，而是把 Data 的中层契约硬化到“结构正确且行为可启动”。

核心收口面包括：

- DataOS / snapshot projection 不再让 generator 手写字段类型和关键业务字段。
- `unit.player` / `unit.enemy` / `ability` records 有 completeness contract，能驱动 Movement 与 Ability 首帧行为。
- runtime projection、spawn boundary、Data write diagnostics、`object_ref`、catalog freeze、display name query 和 current docs 不再继续制造软契约。
- 验证从 DataOS 结构校验扩展到 final snapshot、runtime apply、Godot 行为和文档 grep gate。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `execution-prompt.md` — 后续执行会话的一次性提示词
4. `Core/progress.md` — 最近结论和恢复点
5. `bdd.md` — 行为场景
6. `Core/notes.md` — 参考与开放问题

## Source Design Docs

1. `design/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md`
2. `design/04-BUG:Data无兼容重构后移动与施法失败根因说明.md`
3. `design/05-Data残余问题代码修复分解.md`
4. `design/06-Data文档更新与门禁清单.md`

## Current Resume

- **Current Task**: done
- **Last Conclusion**: T1.2-T1.12 已完成。DataOS final snapshot completeness、descriptor-first projection、runtime diagnostics、typed projection、spawn boundary、catalog freeze、display name query 和 current docs gate 已收口；验证中同步修正 `AbilityDamageBonus` 默认值语义和 Movement demo headless smoke。
- **Next Action**: PRJ-0002 后续转入 Entity / Relationship hard cutover 主线，从 `design/Runtime/3.Entity系统优化/README.md` 和 `Core/entity-rewrite-execution-prompt.md` 创建下一条执行 SDD。
- **Open Blockers**: none
