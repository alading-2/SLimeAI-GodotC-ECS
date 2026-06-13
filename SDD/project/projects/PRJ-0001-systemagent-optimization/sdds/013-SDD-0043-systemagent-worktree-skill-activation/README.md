# SDD-0043 SystemAgent Worktree Skill Activation

## Index Card

- **Status**: done
- **Created**: 2026-06-13
- **Updated**: 2026-06-13
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
  - .ai-config/skills
- **Tags**: systemagent, worktree, sdd

## What This SDD Is About

把 PRJ-0001 的 `Worktree激活设计` Phase 1 落成可触发的 SystemAgent skill：新增 `systemagent-worktree`，让 AI 能安全判断、创建、查看、切换、合并和清理 git worktree，并把使用或跳过 worktree 的六字段上下文写入 SDD `progress.md`。本 SDD 不实现 SDD CLI worktree 参数，不启用 hook 自动创建 worktree。

## Reading Order

1. `../../design/优化/Worktree激活设计.md` — 项目级 worktree 激活设计
2. `../../design/优化/Worktree激活.FeatureSpec.md` — 本轮功能实现规格
3. `design/main.md` — SDD-0043 局部范围和不做什么
4. `tasks.md` — 当前任务拆分
5. `progress.md` — State / Decisions / Validation 与 Worktree Record
6. `bdd.md` — 本轮执行的 FeatureSpec 场景摘录

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0043 已完成 Worktree Skill Phase 1：`systemagent-worktree` skill、registry/docs、FeatureSpec 和 SDD worktree record 已落地。
- **Next Action**: 后续如需 `sdd start --worktree` / `done --merge-worktree` 或 hook 提醒，另建 SDD。
- **Open Blockers**: none
