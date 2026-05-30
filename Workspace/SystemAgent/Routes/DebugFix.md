# Debug Fix

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.debug_fix

## Trigger

用户报告 bug、测试失败、构建失败、验证 artifact 失败或运行行为异常。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单点失败且可快速复现；medium 表示影响多个文件或需要补 smoke；large 表示跨 capability、跨工具链或需要长期追踪的稳定性问题。

## SDD strategy

small 默认不创建 SDD；medium 可选；large 或反复失败问题必须使用 SDD 记录复现、假设树、根因和回归证据。

## Phases

1. Route：输出 route 摘要，确认失败类型、复现入口和最小读取范围。
2. Reproduce：先获得失败证据或说明无法复现原因。
3. Diagnose：形成假设树，定位根因，不直接改表面症状。
4. Fix：做最小修复并避免扩大重构范围。
5. Verify：运行回归验证或补 smoke，记录命令和结果。
6. Close：同步 SDD 或最终报告中的根因、证据、剩余风险和 git status。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.debug_fix`。

## Required roles

Debugger, Verifier, Reviewer, Retrospective。

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.debug_fix.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Validation and evidence

复现证据、假设树、最小修复、回归验证。

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

根因被证据支持，最小修复通过回归验证，无表面 workaround。
