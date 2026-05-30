# Git Worktree Subagent Safety Strategy Design

## Goal

为 SystemAgent 后续中大型任务提供隔离和并行前置安全栏：明确什么时候使用 worktree、如何记录到 SDD、subagent 能做什么、不能做什么，以及为什么当前不实现并行写入 dispatcher。

## Context

SlimeAI 工作区有多个 Git 边界，根仓还存在大量既有未跟踪资源目录。与此同时，AICLI 已有 Planner/Reviewer/TestDesigner/Retrospective subagent 配置，但它们当前更适合作为只读研究或独立评审视角，而不是并行写代码团队。

## Design

本 SDD 采用保守策略：

1. worktree 按仓库独立管理，默认放在目标仓 `.worktrees/`，使用前确认 ignore、dirty 状态和 submodule 边界。
2. SDD 记录 worktree path、branch、baseline status 和 cleanup 状态，但不自动创建 worktree。
3. subagent 默认只读，可做资料搜索、多源研究、独立评审和大型只读审计。
4. 写操作由主对话统一执行；多个 subagent 不同时写同一事实源。
5. dispatcher 延后，直到 SDD work package、worktree、validation artifact、cleanup/timeout/conflict 策略稳定。

## Non-goals

- 不清理当前未跟踪 Resources 或旧游戏目录。
- 不自动创建或删除 worktree。
- 不实现并行代码写入 dispatcher。
- 不把 Windsurf 工具侧搜索能力当成项目可维护 subagent 配置事实源。

## Verification

完成时 GitPolicy、SubagentPolicy、manifest、workflow 文档和 SDD 指南应一致；旧未跟踪资源只记录为 workspace hygiene follow-up，不在本 SDD 中清理。
