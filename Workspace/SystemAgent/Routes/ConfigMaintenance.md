# Config Maintenance

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.config_maintenance

## Trigger

任务修改 skill、rule、command、hook、subagent、sync 脚本或 SystemAgent 工具配置。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单个配置或 skill 源修正；medium 表示 sync/lint 规则或多个 wrapper 调整；large 表示跨工具配置策略、hook/subagent 协议或同步机制变化。

## SDD strategy

small 默认不创建 SDD；medium 可选；large 或改变长期配置边界时必须使用 SDD，并记录源/副本边界、sync 结果和验证证据。

## Phases

1. Route：输出 route 摘要，确认维护源、生成副本、运行配置和禁止触碰路径。
2. Inspect：读取 AIConfigBoundary、WrapperSkillPolicy、目标源文件和 catalog。
3. Modify：只改合法维护源；同步副本必须由 sync 生成。
4. Sync：涉及 skill/rule/command 时运行 ai-config sync。
5. Verify：运行 skill-test、hook smoke 或配置解析检查。
6. Close：记录同步范围、旧路径命中分类、验证结果和 git status。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.config_maintenance`。

## Required roles

Planner, Reviewer, Verifier。

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.config_maintenance.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Validation and evidence

源/副本边界、同步结果、hook smoke、skill-test lint。

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

只改合法维护源，sync/lint/smoke 验证完成，副本仅由脚本生成。
