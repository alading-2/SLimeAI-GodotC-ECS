# SDD-0003 SDD CLI Information Quality Hardening

## Index Card

- **Status**: done
- **Created**: 2026-05-24
- **Updated**: 2026-05-24
- **Type**: workflow
- **Scope**: Workspace/SDD
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SDD
  - .ai-config/skills/sdd
- **Tags**: sdd, cli-hardening

## What This SDD Is About

加固 SDD CLI 的信息质量边界：修复 README 被整体覆盖的 BUG，避免 `done` 写入泛化结论，增强 `validate` 对空壳完成、弱证据和冗余风险的检查，并同步更新 SDD 文档与使用 skill。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 设计文档已由 `../../design/SDD/SDD-CLI信息质量加固设计.md` 给出，本轮按 TDD 与分切片实施必做范围。
- **Next Action**: 先写失败测试覆盖 README 保护、done 继承 Latest Resume、validate 质量与冗余规则。
- **Open Blockers**: none
