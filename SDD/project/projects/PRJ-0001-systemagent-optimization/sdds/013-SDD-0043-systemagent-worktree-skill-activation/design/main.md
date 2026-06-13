# SystemAgent Worktree Skill Activation

> 用户原始问题：`SDD任务刚刚完成，看看下个任务是什么是不是worktree,是的话生成SDD然后执行`

## Goal

把 `Worktree激活设计.md` 的 Phase 1 落成可触发的 `systemagent-worktree` skill：AI 能在用户明确要求隔离、主工作区 dirty、中大型或实验性任务中创建/使用 worktree，并把 worktree 决策写入 SDD `progress.md`。本 SDD 不实现 SDD CLI 自动 `--worktree`，也不启用 hook 自动创建 worktree。

## Context

- SDD-0042 已完成 SDD progress 精简和 FeatureSpec 集成，`progress.md` 已具备 Worktree Record 六字段记录规则。
- SDD-0010 已完成 Git / worktree / subagent 安全策略，但只停留在规则层，尚无可触发 skill。
- 当前主工作区在任务开始前已有 `.uid` 删除、`Workspace/Resources/tool/codlogs` 未跟踪和若干 `__pycache__` 修改；本 SDD 已在单独 worktree 中执行，避免混入主工作区 dirty baseline。
- AI 配置统一源是 `.ai-config/skills/<category>/<name>/SKILL.md`，同步副本必须由 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 生成。

## Design

本轮只实现 Phase 1：新增 `systemagent-worktree` skill 作为手动/语义触发入口，并同步文档与 registry。

### Scope

- 新增 `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`。
- 将新 skill 登记到 `Workspace/SystemAgent/Registry/skills.yaml`。
- 更新 `Workspace/SystemAgent/Docs/08-Git-Worktree策略.md` 和相关设计索引，标记 Phase 1 已落地。
- 运行 AI config sync 生成 Codex / Claude / Trae skill 副本。
- 在 SDD-0043 `progress.md` 记录本轮实际 worktree 六字段。

### Out of Scope

- 不新增 `sdd.py start --worktree` 或 `done --merge-worktree`。
- 不新增 hook 自动创建/清理 worktree。
- 不自动提交、合并或删除任意 dirty worktree。
- 不修改游戏仓或 `Games/*/SlimeAI/` submodule 镜像。

## Verification

- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- `python3 Workspace/SDD/sdd.py validate SDD-0043`
- `python3 Workspace/SDD/sdd.py validate --all`
- `git diff --check`
