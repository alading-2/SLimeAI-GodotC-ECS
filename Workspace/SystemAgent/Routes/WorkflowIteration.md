# Workflow Iteration

> yaml: Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.workflow_iteration

## Trigger

用户指出 AI 流程、方向、验证、文档或配置治理不足，或 retrospective 发现流程缺口。

## Route and task size

Route 输出必须包含 workflow、task_size、sdd、must_read、mode、subagent。small 表示单文件规则或索引修正；medium 表示有限 workflow、role、gate 或 policy 调整；large 表示跨目录 SystemAgent 重构、影响默认入口或需要多轮 SDD 恢复。

## SDD strategy

small 默认不创建 SDD；medium 可选；large 必须使用 SDD，并把问题归因、设计裁决、任务拆分和验证证据写入当前 SDD。

## Phases

1. Route：输出 route 摘要，确认要修复的是入口、workflow、capability、role、gate、policy、tool 还是 catalog。
2. Discover：用户要求深度分析或存在多方案取舍时调用 DesignDiscovery / DesignCritic。
3. Plan：使用 gap classification 形成最小修改清单，明确目标事实源和同步要求。
4. Execute：修改 SystemAgent 正文、`.ai-config` 源或运行配置；禁止直接维护同步副本。
5. Validate：运行 SDD validate、必要 sync/lint/hook smoke 和路径一致性检查。
6. Close：回填 SDD Latest Resume、项目 roadmap/progress 和 retrospective follow-up。

## Required inputs

- 用户原始请求与验收条件。
- 当前 git boundary 的 `git status --short`。
- `Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/README.md`。
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml#workflows.workflow_iteration`。

## Required roles

Retrospective, Planner, Implementer, Reviewer, Documentarian。

Design phase: 当流程迭代涉及 workflow/role/gate/policy 语义变化、长期规则或用户要求深度分析时，先调用 `Workspace/SystemAgent/Actors/DesignDiscovery.md`；存在关键假设、替代方案或设计缺陷风险时追加 `Workspace/SystemAgent/Actors/DesignCritic.md`。

Conditional senior roles: when the iteration changes validation gates, scene gate, batch analyzer, validation manifest, SystemAgent review routing, or gameplay validation governance, add `SeniorGameDeveloper` and `SeniorProgrammer` as review inputs before Reviewer aggregation.

## Must-read paths

以 `Catalog/workflow-catalog.yaml` 中 `workflows.workflow_iteration.must_read` 为准；读取后汇报 selected workflow、must_read 清单与已读/未读状态。

## Execution steps

1. **Retrospective**：识别问题，输出 findings / actionItems / processUpdates。
2. **DesignDiscovery / DesignCritic（条件）**：对 medium/large 或用户要求深度分析的迭代输出确认包、风险、替代方案和 SDD 更新建议。
3. **Planner**：按下方缺口分类体系归因，拆解为最小修改任务列表（目标文件 + 修改内容 + 验证方式）。
4. **Implementer**：按计划最小范围修改 workflow / role / gate / policy / hook 文件。
5. **Reviewer**：跑 RV-BEHAVIOR-COMPLIANCE + RV-DOC-SYNC + RV-CONFIG-SYNC gate。
6. **Documentarian**：同步 INDEX / README / Catalog 路径引用。

If conditional senior roles are triggered, their findings become Reviewer input and must be resolved or explicitly scoped as follow-up before closure.

## Gap classification

| 缺口类型 | 说明 | 修复目标 |
| --- | --- | --- |
| role-missing | workflow 需要但未定义的角色 | 新增 Role.md |
| role-overreach | 角色做了不该做的事 | 修改 Role.md Forbidden behavior |
| gate-missing | 某类问题没有对应 gate | 新增 gate 到 ReviewGates.md |
| gate-vague | gate Prompt 不够具体 | 修改 gate Prompt |
| hook-blind | hook 无法检测某种错误模式 | 修改 systemagent_hook.py |
| must-read-skipped | AI 跳过了 must-read 文件 | 加强 gate 或 hook |
| workflow-chain-break | 角色之间没有衔接 | 修改 Workflow.md required_roles |
| policy-gap | policy 没有覆盖某种边界情况 | 修改 Policy.md |

## Validation and evidence

问题归因（使用缺口分类）、修复任务列表、修改后 gate 通过证据、SDD `progress.md` / Latest Resume 同步确认、索引同步确认。

## Gates

- 按 `Workspace/SystemAgent/Rules/ReviewGates.md` 选择 `Catalog/workflow-catalog.yaml` 中映射的 gate ID。
- 进入 review 前解析 `Workspace/SystemAgent/Registry/review-mode.txt`：`full|lean|solo`。
- Verdict 必须遵守 `Workspace/SystemAgent/Rules/VerdictVocabulary.md`。

## External resource policy

默认不预加载 `Resources/*`。如当前任务确实需要外部资源，先按 `Workspace/SystemAgent/Rules/ExternalResources.md` 记录 `externalResources.enabled / scope / reason / expires: current-task`，最终汇报结论。

## Completion criteria

缺口已按分类体系归因、修复已由 Implementer 执行、gate 已通过、索引已同步，或记录 follow-up。
