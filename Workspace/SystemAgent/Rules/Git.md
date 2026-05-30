# Git Policy

## Source-of-truth boundary

工作区根、`SlimeAI/`、`Games/*/` 和游戏内 submodule 是不同 git 边界。任何 git 操作前必须在目标仓库目录运行 `git status --short`。

每个仓库独立管理自己的 worktree，默认目录为目标仓内的 `.worktrees/`。不使用全局 worktree 根目录，不跨仓复用 worktree。

使用 worktree 前必须确认：

- 目标仓库是本轮任务的真实 Git boundary。
- 当前目录不是游戏仓内的框架 submodule 只读镜像。
- 目标仓已忽略 `.worktrees/`；未忽略时先补齐该仓 `.gitignore` 并单独说明。
- 主工作区 `git status --short` 已记录，dirty 状态不会被清理、覆盖或回滚。
- 若当前已经在 linked worktree 中，继续使用当前 worktree，除非用户明确要求新建隔离 worktree。

## Allowed actions

- AI 可在策略和用户任务允许时自动 commit，但必须先确认范围、写清 What/Why/来源 SDD（如有），并且不混入用户既有改动。
- 文档和配置治理默认只在 `/home/slime/Code/SlimeAI` 根仓处理。
- 中大型、高风险、实验性或主工作区 dirty 的任务，可以建议使用 worktree；小修、只读审计和低风险文档修改不强制。
- clean worktree 完成验证后可以建议 `git worktree remove` 和 `git worktree prune`；dirty worktree 只报告状态，不自动删除。

## Forbidden actions

- 默认不 push。
- 禁止 `git push --force`、`git reset --hard`、`git clean -fd`、历史改写等高风险操作，除非用户明确要求并说明预案。
- 禁止跨 git 边界 add/commit。
- 禁止在工作区根或嵌套仓误执行 `git init`。
- 禁止为了创建或清理 worktree 而删除、覆盖或 stash 用户既有改动。
- 禁止在 `Games/<Game>/SlimeAI/` submodule 镜像内直接创建业务 worktree 或写入框架改动。

## SDD worktree record

使用或建议 worktree 的 SDD 任务，必须在当前 SDD `progress.md` 或任务 README 中记录：

| Field | Meaning |
| --- | --- |
| Git Boundary | 目标仓库绝对路径 |
| Worktree | `none` 或 worktree 绝对路径 |
| Branch | 当前分支或建议分支 |
| Baseline Status | 创建或建议前的 `git status --short` 摘要 |
| Cleanup Status | `not-created`、`clean-removable`、`dirty-preserve` 或 `removed` |
| Submodule Boundary | 是否涉及 submodule；涉及时说明只读或指针更新策略 |

## Required validation or reporting

最终汇报必须给出本轮目标 git boundary 的 `git status --short` 结果摘要，并区分本轮改动和任务前已有用户改动。

如果任务没有使用 worktree，最终汇报应说明原因，例如“只读审计”“低风险文档修改”或“用户要求直接在当前工作区执行”。如果任务建议但未创建 worktree，必须说明未创建原因和 dirty workspace 风险。
