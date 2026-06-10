# 新功能 / 重构 / 迁移 / SDD 实施

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.new_feature

## Trigger

用户要求新增功能、扩展能力、执行 SDD task、迁移旧逻辑、跨模块重构或跨目录治理。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单文件或低风险少量文档修正；medium 表示 2-5 个文件或有限影响面；large 表示跨模块、跨配置、影响 contract 或需要多轮恢复。

## SDD strategy

small 默认不创建 SDD；medium 可选，用户要求深度分析、后续继续或有恢复需求时创建或选用；large 必须使用 SDD，并优先读取当前 SDD 的 README、tasks、progress Latest Resume 和 bdd。

## Phases

1. Route：输出 route 摘要，确认 git boundary、task_size、SDD 策略和 worktree 判断。
2. Discover：medium/large 或高风险任务调用 `Workspace/SystemAgent/Actors/DeepThink.md`，必要时追加 `Workspace/SystemAgent/Actors/DesignCritic.md`。
3. Plan：用 SDD 或本次上下文冻结任务切片、验收标准、owner skill 和验证方式。
4. Execute：按 owner capability skill 做最小范围实现，不把 workflow、role 或 gate 正文复制到 skill。
5. Validate：按影响面运行 owner、interaction、feature-slice 或 release-batch 验证并保存证据。
6. Close：更新 SDD progress/tasks/bdd，执行 retrospective，输出剩余风险和 git status。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- worktree 使用判断：`none`、已在 worktree、建议创建或用户要求不使用，并记录原因。
- `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.new_feature`。

## Required roles

Planner, TestDesigner, Implementer, Reviewer, Verifier, Retrospective。

Design phase: medium/large 新功能、重构、迁移或 SDD 实施在计划冻结前调用 `Workspace/SystemAgent/Actors/DeepThink.md`；large 或高风险 medium 任务追加 `Workspace/SystemAgent/Actors/DesignCritic.md`，再进入 Planner 的可执行拆分。

Conditional senior roles: when the change touches multi-system gameplay, GodotBridge presentation, validation tooling, or SystemAgent validation gates, add `SeniorGameDeveloper` and `SeniorProgrammer` before final Reviewer aggregation. Single owner-scoped Runtime/DataOS changes do not require these senior roles by default.

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.new_feature.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Validation and evidence

SDD tasks/progress、Runtime/DataOS/Godot/文档验证证据、必要时 BDD 场景。若当前任务使用 SDD，应提供 `progress.md` Latest Resume 和最近一次更新的证据。

DeepThink 输出的是方向分析，不是设计文档模板。使用 SDD 时，关键结论、默认假设和必须确认的问题不得只保留在聊天中；用户裁决和采用的默认假设写入 `progress.md`。如需要写入 `design/`，按 `Workspace/SystemAgent/Rules/DesignDocument.md` / `systemagent-design-document` 保留用户原始问题、问题分析和解决思路，不把 DeepThink 内部检查点原样写成标题。

Validation scope must be reported separately from pass/fail as `owner`、`interaction`、`feature-slice` or `release-batch`.

使用 SDD 的任务若涉及 worktree 判断，`progress.md` 必须记录 Git Boundary、Worktree、Branch、Baseline Status、Cleanup Status 和 Submodule Boundary；未创建 worktree 也要记录 `Worktree: none` 与原因。调用 subagent 时，主对话必须保存或压缩其 Scope/Evidence/Inference/Unknown/Risks/Recommended Main-Thread Action/Files Touched 输出，且最终写入仍由主对话执行。

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

全部 tasks 与验证证据闭环，DocsAI/skill/rule/hook/subagent 如受影响已同步；SDD 长任务的 `progress.md` 已同步更新并写入 Latest Resume。

新功能、迁移或重构若改变 capability / GodotBridge / DataOS / 测试入口的路由、源码位置、验证命令或门禁，必须同步对应 owner skill 的 `.ai-config/skills/<category>/<name>/SKILL.md`，运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 与 `Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`，并在最终验证摘要说明 sync/lint 结果。涉及 GodotBridge、Godot Node 生命周期、Physics、Input、Resource、UI、动画或游戏侧 glue 的新功能，必须新增或更新独立 Godot 验证场景；主场景 smoke 只能作为回归补充。涉及 ≥2 个 Capability 或 GodotBridge 表现层的改动，必须对照相关 gameplay lifecycle BDD 或当前 SDD `bdd.md` 检查集成场景覆盖，并参照 `SlimeAI/DocsAI/GameOS/GodotPitfalls.md` 排除已知陷阱。
