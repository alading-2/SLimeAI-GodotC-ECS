# Validation Release

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.validation_release

## Trigger

大改完成后、archive 前、发布前或用户要求完整验证。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单项命令或文档检查；medium 表示一个子系统或一组配置验证；large 表示 release-batch、archive、跨仓库或多能力验证。

## SDD strategy

已有 SDD 时优先读取 README、tasks、progress Latest Resume、bdd 和 validation evidence；archive 或 large 验证必须使用 SDD，small 单项验证可使用聊天上下文。

## Phases

1. Route：输出 route 摘要，确认验证范围是 owner、interaction、feature-slice 还是 release-batch。
2. Collect：读取当前 SDD、workflow catalog、受影响 owner docs 和既有验证 artifact。
3. Matrix：列出必跑、可跳过和无法运行的命令，并给出原因。
4. Execute：运行验证命令或检查 artifact，不把 no error / exit code 0 当成唯一证据。
5. Review：按 gate 检查旧路径、sync、lint、scene artifact、catalog 和 SDD 状态。
6. Close：汇总 pass/fail/blocked，回填 SDD 和最终风险。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.validation_release`。

## Required roles

Verifier, Reviewer, Retrospective。

Conditional senior roles: when release evidence includes feature-slice gameplay, GodotBridge presentation, validation tooling, scene gate, manifest, or release-batch changes, add `SeniorGameDeveloper` and `SeniorProgrammer` before final Reviewer verdict. Pure documentation archive checks may stay on the default role set.

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.validation_release.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Validation and evidence

SDD 状态、路径搜索、sync/lint、build/test/scene 或文档验证。

Release scene evidence must cite `index.json`、per-scene `result.json`、scene artifact checks and any generated gate report; no error, exit code 0 or PASS marker is not sufficient by itself.

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

验证命令结果已记录，旧路径命中已分类，剩余风险明确。
