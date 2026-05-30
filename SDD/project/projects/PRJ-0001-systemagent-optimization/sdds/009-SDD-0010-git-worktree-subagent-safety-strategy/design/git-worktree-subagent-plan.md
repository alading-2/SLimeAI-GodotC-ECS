# Git Worktree Subagent Task Plan

## Target Contracts

| Contract | Expected Result |
| --- | --- |
| Git boundary | 每次任务先确认目标仓，不跨仓混提交 |
| Worktree usage | 中大型、高风险或主工作区 dirty 时建议使用；小任务不强制 |
| Worktree cleanup | dirty worktree 不自动删除，clean 才允许 remove/prune |
| Subagent usage | 默认只读，输出 Evidence / Inference / Unknown / Risks / Main-Thread Action |
| Dispatcher boundary | 暂缓并行写入 dispatcher，直到 work package 和 worktree 策略稳定 |

## Execution Order

1. 先同步 GitPolicy / SubagentPolicy 的策略边界。
2. 再同步 README、manifest、workflow 或 SDD 指南中的 worktree 记录要求。
3. 再检查现有 subagent launcher 是否仍符合只读/独立视角定位。
4. 最后记录 workspace hygiene follow-up，但不清理既有未跟踪目录。
