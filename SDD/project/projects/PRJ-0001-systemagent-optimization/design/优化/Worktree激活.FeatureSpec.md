# Worktree 激活 FeatureSpec

## Source Design

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/Worktree激活设计.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/04-Git与Worktree策略.md`
- `Workspace/SystemAgent/Rules/Git.md`
- `Workspace/SystemAgent/Docs/08-Git-Worktree策略.md`

## Feature List

| ID | Feature | Priority | Status | Notes |
| --- | --- | --- | --- | --- |
| FS-1 | Worktree 触发判断 | P0 | planned | 自然语言或 SDD/dirty workspace 场景触发，但不默认创建 |
| FS-2 | Worktree 生命周期 skill | P0 | planned | 围绕 SDD Start / Execute / Close 管理 worktree，命令只做速查 |
| FS-3 | SDD worktree record | P0 | planned | 使用或建议 worktree 时写入六字段恢复上下文 |
| FS-4 | AI 配置同步与目录登记 | P1 | planned | 新 skill 从 `.ai-config` 源生成到工具副本，并登记到 SystemAgent catalog |

## FS-1: Worktree 触发判断

### Goal

让 AI 在多对话、dirty workspace、中大型或实验性任务中能主动判断是否建议或创建 worktree，同时保持小修不增加流程负担。

### Behavior

- Given 用户明确说“worktree”“隔离”“单独分支”“创建 worktree”
- When AI 进入任务路由或准备执行
- Then AI 使用 `systemagent-worktree` skill，并记录目标 Git boundary、baseline status 和是否已在 worktree 中。

- Given 任务只是单文件小修或低风险文档修正
- When 用户没有要求隔离
- Then AI 可以记录 `Worktree: none`，不创建 worktree。

### Implementation Guidance

- Owner: SystemAgent skill / Git policy。
- Key files / areas:
  - `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`
  - `Workspace/SystemAgent/Rules/Git.md`
  - `Workspace/SystemAgent/Docs/08-Git-Worktree策略.md`
- Public API: skill 触发语义，不新增 SDD CLI 参数。
- Constraints:
  - worktree 不是默认行为。
  - dirty workspace 不能被 stash、clean、reset 或覆盖。
  - submodule 镜像内禁止创建框架业务 worktree。
- Forbidden:
  - 不由 hook 自动创建 worktree。
  - 不把所有 SDD 强制绑定 worktree。

### TDD Handoff

- expectedInputs: 用户请求包含 worktree/隔离/单独分支，或当前任务为中大型且主工作区 dirty。
- expectedObservations: skill 要求先确认 Git boundary、`git status --short`、`.worktrees/` ignore、是否已在 linked worktree。
- passCriteria: skill 明确给出建议/创建/跳过判断，并要求把判断写入 SDD progress。
- failCriteria: skill 直接创建 worktree，未记录 dirty baseline 或未检查 `.worktrees/` ignore。
- artifactPath: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

### Review Checklist

- 是否把“建议 worktree”和“创建 worktree”分开。
- 是否保留小任务不使用 worktree 的路径。
- 是否禁止为创建 worktree 清理用户既有改动。

## FS-2: Worktree 生命周期 skill

### Goal

提供一个可被用户自然语言或 SDD 任务触发的 SystemAgent skill，指导 AI 在 SDD 开始前创建或选择 worktree，在执行中固定 worktree 路径，在完成后验证、提交、合并并清理。`create/list/status/switch/merge/clean` 只是速查语义，不是 skill 主体。

### Behavior

- Given 用户要求创建 worktree
- When `.worktrees/` 已被 ignore 且目标仓库不是 submodule 镜像
- Then AI 创建 `<repo>/.worktrees/<name>-<YYMMDD>` 和同名分支，后续 tool calls 使用该 worktree 路径。

- Given 用户要求清理 worktree
- When 目标 worktree 仍有未提交改动
- Then AI 不删除，只报告 dirty 状态和保留原因。

### Implementation Guidance

- Owner: `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`
- Public API: SDD lifecycle 语义 `judge/start/execute/close/preserve/remove`；`create/list/status/switch/merge/clean` 只作为命令速查。
- Log / Validation artifact: sync 输出、skill-test lint、`git worktree list`。
- Constraints:
  - 创建前必须确认 `.worktrees/` ignore。
  - merge 前必须确认 main 工作区状态，不能混入用户既有改动。
  - clean 只对 clean 且已合并的 worktree 建议或执行。
- Forbidden:
  - 不自动 commit dirty worktree。
  - 不自动删除 dirty worktree。
  - 不跨 Git boundary 合并。

### TDD Handoff

- expectedInputs: SDD 开始前需要隔离、主工作区 dirty、中大型任务执行、SDD 完成后需要合并清理。
- expectedObservations: skill 主体按判断、Start、Execute、Close、异常处理和 SDD record 组织；命令只在速查中出现。
- passCriteria: `systemagent-worktree` 源和同步副本一致，lint 不出现 critical failure。
- failCriteria: skill 允许直接 `git clean -fd`、`git reset --hard`、force push 或在 dirty main 上无检查 merge。
- artifactPath: `.ai-temp/skill-test/static-*.json`

### Review Checklist

- skill 主体是否围绕 SDD Start / Execute / Close，而不是围绕 Git 子命令展开。
- `switch` 是否说明 Codex/Claude 不能依赖单次 shell `cd` 保持会话状态，后续工具调用必须使用 worktree path。
- `merge` 和 `clean` 是否有 dirty 保护。

## FS-3: SDD worktree record

### Goal

让使用、建议或跳过 worktree 的 SDD 可从 `progress.md` 恢复 Git boundary、worktree 路径、branch、baseline、cleanup 和 submodule 边界。

### Behavior

- Given 当前任务使用 SDD
- When AI 创建、建议或明确跳过 worktree
- Then `progress.md` 的 State 或 Decision 至少记录六字段：Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status、Submodule Boundary。

### Implementation Guidance

- Owner: SDD artifact / SystemAgent skill。
- Key files / areas:
  - `Workspace/SDD/docs/SDDFormat.md`
  - `Workspace/SystemAgent/Rules/Git.md`
  - 当前 SDD `progress.md`
- Constraints:
  - 不把完整 `git status` 噪音复制进 progress，只写摘要。
  - dirty baseline 只记录，不清理。

### TDD Handoff

- expectedInputs: 当前 SDD ID、git boundary、worktree path、branch、baseline status。
- expectedObservations: progress 中有六字段，且与实际 `git worktree list` / `git status --short` 可对照。
- passCriteria: SDD-0043 validate 通过，progress 可恢复当前 worktree。
- failCriteria: 使用 worktree 但 progress 没有记录，或只写“已使用 worktree”。
- artifactPath: `python3 Workspace/SDD/sdd.py validate SDD-0043`

### Review Checklist

- SDD progress 是否包含六字段。
- 是否区分本轮 worktree 和主工作区既有 dirty 状态。

## FS-4: AI 配置同步与目录登记

### Goal

让新 skill 可被 Codex/Claude/Trae 同步发现，并让 SystemAgent registry 能追踪维护源。

### Behavior

- Given 新增 `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`
- When 运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- Then `.codex/skills/systemagent-worktree/`、`.claude/skills/systemagent-worktree/`、`.trae/skills/systemagent-worktree/` 由同步脚本生成。

### Implementation Guidance

- Owner: AI config source。
- Key files / areas:
  - `.ai-config/skills/systemagent-skill/systemagent-worktree/SKILL.md`
  - `Workspace/SystemAgent/Registry/skills.yaml`
  - sync 生成副本。
- Constraints:
  - 不手写同步副本。
  - 不修改 `.ai-config/sync-targets.json`。

### TDD Handoff

- expectedInputs: 新 skill 源和 sync targets。
- expectedObservations: sync 后三套副本存在且内容一致；skill-test critical 为 0。
- passCriteria: `sync-ai-config.sh` 成功；skill-test summary `Critical 0`。
- failCriteria: 同步副本缺失、直接手写副本、或 lint critical failure。
- artifactPath: sync/lint 命令输出。

### Review Checklist

- 新 skill 是否登记到 `Workspace/SystemAgent/Registry/skills.yaml`。
- 是否只改 `.ai-config` 源，由 sync 生成副本。
