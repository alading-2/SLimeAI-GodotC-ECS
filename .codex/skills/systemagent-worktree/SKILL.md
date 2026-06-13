---
name: systemagent-worktree
description: 管理 SlimeAI 当前 Git 边界的 worktree 判断、创建、查看、切换、合并和清理。用于用户明确要求 worktree/隔离/单独分支，或中大型、实验性、dirty workspace 任务需要隔离时。
---

# systemagent-worktree

## 触发条件

- 用户明确说“worktree”“隔离”“单独分支”“创建 worktree”“切换 worktree”“合并 worktree”或“清理 worktree”。
- 当前任务为中大型、跨 owner、实验性改动，或主工作区已有 dirty 状态且本轮实现可能混入既有改动。
- 当前 SDD / FeatureSpec 要求记录 worktree decision record。

小修、只读审计、低风险单文件文档修改默认不创建 worktree；如果不创建，仍在最终汇报或 SDD `progress.md` 说明 `Worktree: none` 和原因。

## 必读

- `Workspace/SystemAgent/Rules/Git.md`
- `Workspace/SDD/docs/SDDFormat.md`
- 当前任务 SDD 的 `README.md`、`tasks.md`、`progress.md`、`bdd.md`（如有）
- 项目级 worktree 设计：`SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/Worktree激活设计.md`（PRJ-0001 范围内）

## 通用安全前置

执行任何子命令前先确认：

1. `git rev-parse --show-toplevel` 是本轮真实 Git boundary。
2. 当前目录不是 `Games/<Game>/SlimeAI/` submodule 镜像。
3. `git status --short` 已记录为 baseline；dirty 状态只记录，不 stash、不 reset、不 clean。
4. 仓内 `.worktrees/` 已被 ignore：`git check-ignore -q .worktrees/example`。
5. 如果当前已经在 linked worktree 中，默认继续使用当前 worktree；除非用户明确要求再创建新的隔离 worktree。

如果 `.worktrees/` 未被 ignore，先补目标仓 `.gitignore` 的 `.worktrees/` 规则并单独说明；不要在未 ignore 的项目本地目录创建 worktree。

## 子命令语义

### `create <name>`

用途：创建仓内隔离 worktree。

步骤：

1. 确认 Git boundary、submodule 边界和 baseline status。
2. 生成安全名称：`<name>-<YYMMDD>`，只使用小写字母、数字和 `-`。
3. 确认目标路径不存在：`<repo>/.worktrees/<name>-<YYMMDD>`。
4. 创建分支和 worktree：

```bash
git worktree add ".worktrees/<name>-<YYMMDD>" -b "<name>-<YYMMDD>"
```

5. 在新 worktree 中运行最小 baseline 验证；无法运行时在 SDD 或最终汇报说明原因。
6. 后续所有文件读写和验证命令的 `workdir` 必须切到新 worktree 绝对路径；不要依赖一次 shell `cd` 持久改变会话目录。
7. 若使用 SDD，在 `progress.md` 记录 Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status、Submodule Boundary。

停止条件：

- `.worktrees/` 未被 ignore。
- 目标路径或分支已存在且用户未确认复用。
- 目标仓是 submodule 镜像。
- 创建会要求清理、覆盖或 stash 用户既有改动。

### `list`

用途：列出当前仓 worktree。

命令：

```bash
git worktree list --porcelain
```

输出时按 worktree path、branch、HEAD、dirty status 摘要展示；不要把完整噪音写入 SDD。

### `status`

用途：说明当前 shell 所在工作区和 worktree 状态。

检查项：

- `pwd`
- `git rev-parse --show-toplevel`
- `git branch --show-current`
- `git status --short`
- `git worktree list --porcelain`

输出必须区分 main workspace dirty 和当前 worktree dirty。

### `switch <name|path>`

用途：切换后续任务操作目标。

规则：

- 不假设单次 shell `cd` 会改变后续工具调用目录。
- 找到目标 worktree 绝对路径后，后续所有工具调用显式使用该路径作为 `workdir`。
- 如果使用 SDD，更新或确认 `progress.md` 的 Worktree / Branch 与实际路径一致。

### `merge <name|path>`

用途：把已完成 worktree 分支合并回目标主分支。

前置：

1. worktree 内 `git status --short` 已检查。
2. dirty worktree 不自动提交；先按普通 Git Safety 确认本轮改动范围和 commit。
3. main workspace `git status --short` 已检查，不能混入用户既有改动。
4. 验证证据已记录到当前 SDD 或最终汇报。

推荐流程：

```bash
git -C "<worktree-path>" status --short
git -C "<worktree-path>" branch --show-current
git status --short
git merge "<worktree-branch>"
```

如果出现冲突，立即停止并报告冲突文件；不要自动解决与用户既有改动相关的冲突。

### `clean <name|path>`

用途：清理已合并且干净的 worktree。

前置：

1. `git -C "<worktree-path>" status --short` 为空。
2. 分支已合并或用户明确说明保留/删除策略。
3. SDD `progress.md` 或最终汇报已记录 cleanup 状态。

允许命令：

```bash
git worktree remove "<worktree-path>"
git worktree prune
```

禁止：

- dirty worktree 上执行 `git worktree remove`。
- 为了清理而执行 `git clean -fd`、`git reset --hard` 或删除大量文件。

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

## 输出要求

- 当前子命令：create/list/status/switch/merge/clean。
- Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status、Submodule Boundary。
- 下一步要在哪个绝对路径继续执行。
- 如果拒绝操作，说明触发的停止条件和安全原因。

## 禁止

- 不直接修改 `.ai-config/sync-targets.json` 定义的同步副本作为源。
- 不跨 Git boundary 创建、合并或清理 worktree。
- 不在 `Games/<Game>/SlimeAI/` submodule 镜像内做框架业务 worktree。
- 不自动 stash、reset、clean、force push 或删除 dirty worktree。
- 不把 worktree 作为所有任务的默认仪式。
