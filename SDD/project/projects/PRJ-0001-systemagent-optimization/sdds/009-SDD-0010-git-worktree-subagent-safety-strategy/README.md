# SDD-0010 Git Worktree Subagent Safety Strategy

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
  - Workspace/DocsAI
  - .claude/agents
  - .codex/agents
- **Tags**: systemagent, git, worktree, subagent

## What This SDD Is About

本 SDD 统一 Git/worktree 与 subagent 安全策略：每个 Git 边界独立管理 worktree，dirty worktree 不自动清理，subagent 默认只读，写操作由主对话串行执行；暂不实现 tackle 式 dispatcher。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为验收场景
5. `notes.md` — 共享设计引用和开放问题

## Current Resume

- **Current Task**: completed
- **Last Conclusion**: Git/worktree 与 subagent 安全策略已落地：`GitPolicy.md` 明确每仓独立 `.worktrees/`、dirty 不清理、submodule 边界和 SDD worktree record；`SubagentPolicy.md` 与 Claude/Codex launcher 统一只读结构化输出契约；workflow 与 SDD 文档已记录 worktree 恢复字段。
- **Next Action**: 后续仅在独立 SDD 中设计写入型 subagent 或 dispatcher；本 SDD 不创建、删除或清理任何 worktree。
- **Open Blockers**: none
