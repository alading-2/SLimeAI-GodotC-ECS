# Research Adoption

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.research_adoption

## Trigger

用户要求研究外部资料、本地 Resources、官方文档、开源项目或参考框架。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单资料核对；medium 表示一个参考项目或一组文档采纳判断；large 表示多来源研究并会转化为长期 workflow、policy、GameOS 或 DataOS 改动。

## SDD strategy

纯研究 small/medium 可不创建 SDD；研究结论如果要转成长期改动，medium 建议使用 SDD，large 必须使用 SDD 记录证据、取舍和采纳边界。

## Phases

1. Route：输出 route 摘要，确认外部资源范围、是否需要联网或只读 Resources。
2. Collect：只读取任务相关资料，记录 externalResources enabled/scope/reason/expires。
3. Analyze：区分 Evidence、Inference、Unknown，避免把参考资料直接升为事实源。
4. Decide：对候选机制给出 Adopt Now、Adopt Later 或 Reject。
5. Land：需要长期生效时写入 SystemAgent 或 SlimeAI DocsAI 正文事实源。
6. Close：说明复制风险、未采纳项、验证或后续 SDD。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.research_adoption`。

## Required roles

ResearchAnalyst, Reviewer。

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.research_adoption.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Validation and evidence

externalResources 记录、Evidence/Inference/Unknown、每个候选机制的 Adopt Now / Adopt Later / Reject 决策。

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

参考资料未变成事实源，`Workspace/DocsAI/Reviews/` 只作为历史分析，未复制外部代码/资产；需要长期生效的采纳结论已落入 `Workspace/SystemAgent/` 正文事实源或 `SlimeAI/DocsAI/`。
