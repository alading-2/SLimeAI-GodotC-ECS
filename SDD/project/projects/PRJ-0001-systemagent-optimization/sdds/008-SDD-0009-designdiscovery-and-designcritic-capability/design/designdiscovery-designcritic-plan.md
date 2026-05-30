# DesignDiscovery and DesignCritic Task Plan

## Target Contracts

| Contract | Expected Result |
| --- | --- |
| Capability input | 用户目标、上下文范围、约束、当前 SDD 或创建 SDD 的条件 |
| Capability output | 确认包、推荐方案、默认假设、必须确认项、SDD 更新建议 |
| Role boundary | DesignCritic 找缺陷和风险，不写实现，不替代 Reviewer |
| SDD integration | 关键结论写入 design/progress/tasks/bdd/notes |
| Workflow integration | NewFeature / WorkflowIteration 可按 task_size 调用 DesignDiscovery |

## Frozen Capability Contract

### Invocation Modes

| Mode | Trigger | Required Output |
| --- | --- | --- |
| `small` | 单文件小修、拼写、链接、低风险机械修改 | 3-5 行自检：目标、默认假设、验证方式 |
| `medium` | 新功能、重构、workflow/skill/rule/gate 改动、需要 SDD 的任务 | 完整 Design Discovery 确认包 |
| `large` | 跨模块、跨 Git 边界、长期架构决策、用户要求“深度思考” | 完整确认包 + DesignCritic 审查 + SDD artifact 更新建议 |

### Design Discovery Confirmation Package

| Field | Meaning |
| --- | --- |
| Goal | 本轮真正要解决的问题和非目标 |
| Context Read | 已读事实源、未读但可能相关的上下文、当前 git boundary |
| Main Risks | 实施、验证、维护、边界和用户体验风险 |
| Options | 2-3 个可选方案及取舍，不把单一路径伪装成唯一选择 |
| Recommendation | 推荐方案、理由和最小可执行切片 |
| Must Confirm | 不确认就不能安全推进的问题 |
| Should Confirm | 建议确认但可用默认假设推进的问题 |
| Defaults I Will Use | 用户不补充时采用的默认假设 |
| Not Recommended | 明确不建议做的方向及原因 |
| SDD Updates | 应写入当前 SDD 的 design、tasks、progress、bdd、notes 更新 |

### DesignCritic Review Package

| Field | Meaning |
| --- | --- |
| Assumptions | 当前设计依赖但尚未证明的假设 |
| Missing Context | 缺少但可能改变方案的重要上下文 |
| Design Defects | 方案中的矛盾、过度设计、边界穿透或验证空洞 |
| Better Options | 更小、更安全或更易验证的替代方案 |
| Trade-offs | 推荐方案带来的取舍和代价 |
| User Decisions | 必须由用户决定或接受默认值的问题 |
| Recommendation | 继续、缩小范围、拆分 SDD、暂停或改方案 |
| SDD Updates | 需要同步到 SDD artifact 的结论和任务 |

### Forbidden Behavior

- 不由 hook 强制触发，不逐问逐答阻塞用户。
- 不把 DesignCritic 做成独立 workflow 或替代 Reviewer。
- 不把确认包散落在聊天里；使用正式 SDD 时必须落盘关键结论。
- 不为 small 任务强制完整 Design Discovery。
- 不把 wrapper skill 写成完整 capability 正文。

## Execution Order

1. 先定义 capability 正文和输出模板。
2. 再新增或更新 DesignCritic role。
3. 再接入 workflow-catalog 和 workflow 文档。
4. 最后处理 wrapper skill、sync/lint 和 SDD 验证。
