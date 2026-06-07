# SDD-0036 Target Query Engine Hard Cutover

## Index Card

- **Status**: done
- **Created**: 2026-06-07
- **Updated**: 2026-06-08
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/target-selector
- **Tags**: tools, target-selector, ai-first, hard-cutover

## What This SDD Is About

本 SDD 执行 `design/Tool/其他Tool/05-TargetSelector查询契约.md`：把旧 `EntityTargetSelector` / `PositionTargetSelector` 的 list-only 静态查询 hard cutover 为 `TargetQueryEngine` / `TargetQueryResult`。

目标不是“保留旧 selector 名字继续补功能”，而是让 Ability / AI / Feature 能拿到一份可诊断、可复现、可替换候选来源的目标查询报告：resolved origin/forward、candidate count、几何命中、过滤计数、排序、limit、warnings/errors。

建议在 `SDD-0035 Runtime Mount And Node Lifecycle Hard Cutover` 之后执行；如果 SDD-0035 未完成，本 SDD 必须避免继续把 NodeLifecycle 全局扫描暴露为 public fallback。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 目标架构和边界
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为场景
5. `progress.md` — 最近结论和恢复点
6. `execution-prompt.md` — 新会话执行提示词
7. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0036 已生成执行胶囊。用户已确认 TargetSelector 完全重构，不保 `EntityTargetSelector.Query(query)` 兼容桥。
- **Next Action**: 进入实现前先读 `execution-prompt.md`，从 T1.1 readiness baseline 开始。
- **Open Blockers**: none
