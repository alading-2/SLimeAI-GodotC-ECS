# SDD-0042 SystemAgent SDD Slimming and FeatureSpec Integration

## Index Card

- **Status**: done
- **Created**: 2026-06-13
- **Updated**: 2026-06-13
- **Type**: workflow
- **Scope**: Workspace/SDD
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Workspace/SDD
  - Workspace/SystemAgent
  - .ai-config/skills
- **Tags**: systemagent, sdd, featurespec

## What This SDD Is About

把 PRJ-0001 已冻结的 `SDD精简设计` 和 `FeatureSpec-功能实现规格设计` 落成第一批可执行改造：先修 SDD CLI / 模板 / validate / skill 口径，让新任务默认使用轻量状态面板和设计旁 `.FeatureSpec.md`。本 SDD 不执行 Worktree、TDD Data 试点或 Hook 启用。

## Reading Order

1. `../../design/优化/SDD精简设计.md` — SDD 轻量状态容器设计
2. `../../design/优化/FeatureSpec-功能实现规格设计.md` — FeatureSpec 设计
3. `../../design/优化/SDD精简与FeatureSpec集成.FeatureSpec.md` — 本轮功能实现规格
4. `design/main.md` — 本 SDD 局部范围和执行边界
5. `tasks.md` — 当前任务拆分
6. `progress.md` — 状态面板和验证摘要

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0042 已创建，范围限定为第一批：SDD 精简 + FeatureSpec 工具/文档/skill 集成。
- **Next Action**: 启动 SDD 后从 T1.1 开始，为模板、CLI 写入和 validate 行为补失败先行测试。
- **Open Blockers**: none
