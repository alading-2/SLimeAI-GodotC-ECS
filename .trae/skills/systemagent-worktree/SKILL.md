---
name: systemagent-worktree
description: 围绕 SDD 生命周期管理 git worktree 隔离流程。用于 SDD/中大型/实验性/dirty workspace 任务开始前创建或选择 worktree，任务完成后合并并清理。
---

# systemagent-worktree

## 定位

这是 **SDD / 中大型任务的 worktree 生命周期流程 skill**，不是 Git 命令手册。

核心目标是让一个任务从开始到结束都有清晰隔离边界：

```text
开始前判断 -> 创建/选择 worktree -> 在 worktree 执行 -> 验证和提交 -> 合并回主工作区 -> 清理 worktree -> 记录 SDD
```

命令可以写，但只作为流程中的最小速查。真正必须执行的是边界判断、路径切换、验证、合并、清理和 SDD record。

## 触发条件

- 用户明确说“worktree”“隔离”“单独分支”“在 worktree 做”“合并 worktree”或“清理 worktree”。
- 当前任务使用 SDD，且属于中大型、跨 owner、实验性、可能回滚或主工作区 dirty 的实现任务。
- 当前任务可能与其他对话的改动混在一起。

小修、只读审计、低风险单文件文档修改默认不创建 worktree；但仍需在最终汇报或 SDD `progress.md` 写明 `Worktree: none` 和原因。

## 必读

- `Workspace/SystemAgent/Rules/Git.md`
- 当前任务 SDD 的 `README.md`、`tasks.md`、`progress.md`、`bdd.md`（如有）
- `Workspace/SDD/docs/SDDFormat.md`
- 项目级 worktree 设计：`SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/Worktree激活设计.md`（PRJ-0001 范围内）

## 判断阶段

开始 SDD 或中大型实现前，先判断 worktree 策略：

| 场景 | 决策 |
| --- | --- |
| 用户明确要求隔离 | 创建或切换到 worktree |
| 主工作区有无关 dirty 改动 | 建议创建 worktree |
| 跨 owner / 中大型 / 实验性 / 可能回滚 | 建议创建 worktree |
| 当前已在 linked worktree | 默认继续使用当前 worktree |
| 小修 / 只读审计 / 低风险单文件文档 | 可跳过，记录 `Worktree: none` |

判断后立刻记录或准备记录 SDD Worktree Record。不要等任务完成后再凭记忆补。

## Start 流程

需要 worktree 时，按下面顺序执行：

1. 确认 Git boundary：`git rev-parse --show-toplevel` 必须是本轮真实仓库。
2. 确认不是 `Games/<Game>/SlimeAI/` submodule 镜像。
3. 记录主工作区 baseline：`git status --short`；dirty 只记录，不 stash、不 reset、不 clean。
4. 确认 `.worktrees/` 已被 ignore：`git check-ignore -q .worktrees/example`。
5. 生成任务名：优先使用 SDD ID 或简短任务名，例如 `sdd-0043-worktree-flow-260614`。
6. 创建 worktree：`git worktree add .worktrees/<name> -b <branch>`。
7. 之后所有读写、验证、commit 的 `workdir` 都切到 worktree 绝对路径。
8. 在 SDD `progress.md` 写入 Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status、Submodule Boundary。

停止条件：

- `.worktrees/` 未被 ignore。
- 目标路径或分支已存在，且用户未确认复用。
- 目标仓是 submodule 镜像。
- 创建会要求清理、覆盖或 stash 用户既有改动。

## Execute 流程

任务执行期间：

- 所有文件读写和验证命令都在 worktree 路径执行。
- 不依赖一次 shell `cd` 改变后续工具调用；每次工具调用显式设置 worktree `workdir`。
- 若用户或系统要求切换目标 worktree，先用 `git worktree list --porcelain` 找到绝对路径，再更新 SDD Worktree Record。
- 验证失败、需求变化或任务中断时，保留 worktree，不自动清理。

## Close 流程

任务完成后，按顺序关闭：

1. 在 worktree 内运行必要验证，并记录结果。
2. 在 worktree 内检查 `git status --short`，确认本轮改动范围。
3. 提交 worktree 分支；commit message 写清 What / Why / SDD 来源。
4. 回到主工作区，检查 `git status --short`，确认不会混入用户既有改动。
5. 合并 worktree 分支，优先 fast-forward；冲突时停止并报告。
6. 合并成功后按需要 push。
7. 如果 worktree clean 且分支已合并，执行 `git worktree remove` 和 `git worktree prune`。
8. 更新 SDD `progress.md` 的 Cleanup Status 为 `removed`；如果未删除，写 `dirty-preserve` 或 `clean-removable` 和原因。

## 异常处理

| 情况 | 处理 |
| --- | --- |
| worktree 有未提交改动 | 不删除，记录 `dirty-preserve` |
| main 有无关 dirty 改动 | 不混入提交；必要时停止合并 |
| 合并冲突 | 停止并报告冲突文件，不自动解决用户改动相关冲突 |
| 验证失败 | 保留 worktree，记录失败命令和下一步 |
| 目标是 submodule 镜像 | 禁止创建框架业务 worktree |

## SDD Worktree Record

使用、建议或明确跳过 worktree 时，当前 SDD `progress.md` 的 State 或 Decision 至少记录：

| Field | Meaning |
| --- | --- |
| Git Boundary | 目标仓库绝对路径 |
| Worktree | `none` 或 worktree 绝对路径 |
| Branch | 当前分支或建议分支 |
| Baseline Status | 创建或建议前 `git status --short` 的摘要 |
| Cleanup Status | `not-created`、`clean-removable`、`dirty-preserve` 或 `removed` |
| Submodule Boundary | 是否涉及 submodule；涉及时说明只读、指针更新或禁止直接改动 |

## 命令速查

```bash
git status --short
git worktree list --porcelain
git worktree add .worktrees/<name> -b <branch>
git -C <worktree-path> status --short
git merge --ff-only <branch>
git worktree remove <worktree-path>
git worktree prune
```

命令速查不能替代 Start / Execute / Close 流程；尤其不能跳过 baseline、验证、commit 范围确认和 cleanup 状态记录。

## 输出要求

- worktree 决策：create / reuse / skip / merge / preserve / remove。
- Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status、Submodule Boundary。
- 后续执行的绝对 `workdir`。
- 如果拒绝操作，说明触发的停止条件和安全原因。

## 禁止

- 不直接修改 `.ai-config/sync-targets.json` 定义的同步副本作为源。
- 不跨 Git boundary 创建、合并或清理 worktree。
- 不在 `Games/<Game>/SlimeAI/` submodule 镜像内做框架业务 worktree。
- 不自动 stash、reset、clean、force push 或删除 dirty worktree。
- 不把 worktree 作为所有任务的默认仪式。
