# DesignDiscovery Capability

## Responsibility

在实现前把模糊需求转成可审查、可确认、可落盘的设计包，帮助 SystemAgent 识别目标、边界、风险、可选方案、推荐路径、默认假设和必须确认项。

## Invocation conditions

- 用户要求“深度思考”、方案设计、架构取舍、设计确认或不要直接实现。
- 任务是中大型新功能、重构、迁移、workflow/skill/rule/gate/policy 改动。
- 当前任务需要创建、继续或更新 SDD。
- Planner、Reviewer 或 Retrospective 发现设计缺口、范围不清、验证空洞或跨边界风险。

## Modes

| Mode | Trigger | Required output |
| --- | --- | --- |
| `small` | 单文件小修、拼写、链接、低风险机械修改 | 3-5 行自检：目标、默认假设、验证方式 |
| `medium` | 新功能、重构、workflow/skill/rule/gate 改动、需要 SDD 的任务 | 完整 Design Discovery 确认包 |
| `large` | 跨模块、跨 Git 边界、长期架构决策、用户要求深度分析 | 完整确认包、DesignCritic 审查、SDD artifact 更新建议 |

## Required context

- 用户原始请求和验收意图。
- selected workflow、task size、当前 git boundary。
- `Workspace/SystemAgent/README.md`、`Workspace/SystemAgent/README.md` 和相关 workflow。
- 当前 SDD 的 `README.md`、`design/INDEX.md`、`tasks.md`、`progress.md`、`bdd.md`。
- 相关 owner skill、policy、gate 或 capability 正文。

## Output shape

```text
Design Discovery:
- Goal:
- Context Read:
- Main Risks:
- Options:
- Recommendation:
- Must Confirm:
- Should Confirm:
- Defaults I Will Use:
- Not Recommended:
- SDD Updates:
```

## SDD artifact contract

| Output | SDD target |
| --- | --- |
| 关键目标、取舍、推荐方案 | `design/` |
| 用户确认、默认假设、恢复点 | `progress.md` |
| 可执行拆分和阻塞项 | `tasks.md` |
| 行为预期、验收场景、不适用原因 | `bdd.md` |
| 参考来源、开放问题、非目标 | `notes.md` |

## Workflow usage

- **Standalone**：用户直接要求设计发现时，先读取最小事实源，再输出确认包；使用正式 SDD 时同步关键结论。
- **Composed**：NewFeature、WorkflowIteration、ResearchAdoption 等 workflow 可在计划冻结前调用；workflow 负责决定是否进入实现、是否创建 SDD、是否补充 gate。
- **Compressed**：small 任务只做简短自检，不强制完整确认包。

## DesignCritic integration

`large` 或高风险 `medium` 任务应调用 `Workspace/SystemAgent/Actors/DesignCritic.md` 的批判视角，重点检查假设、缺失上下文、设计缺陷、替代方案、用户决策和 SDD 更新建议。

## Forbidden behavior

- 不由 hook 强制触发。
- 不逐问逐答阻塞用户；优先一次性输出确认包。
- 不把确认包只留在聊天里；使用正式 SDD 时必须落盘关键结论。
- 不为 small 任务强制完整 Design Discovery。
- 不替代 Planner 的任务拆解、Reviewer 的 gate verdict 或用户的最终方向选择。
