---
name: systemagent-design-critic
description: 设计冻结前用批判视角审查方案，找出假设、遗漏、风险、缺陷和替代方案。用于用户要求"分析缺陷/完善方案"或架构取舍时。
---

# systemagent-design-critic

## 触发条件

- 用户要求"深度思考""分析缺陷""完善方案"或架构取舍。
- DeepThink 判断任务为 `large`，或高风险 `medium`。
- Planner 的计划存在关键未知、跨边界风险、验证空洞或多个可选方案。
- WorkflowIteration、Retrospective 或 Reviewer 发现设计阶段缺口。

## 必读

- `Workspace/SystemAgent/Actors/DesignCritic.md`
- 用户原始请求和验收目标。
- 当前 selected workflow、task size、SDD artifact。
- 已提出的方案、默认假设、验证策略和开放问题。

## 输出要求

按 `DesignCritic.md` 正文的 Output shape 输出：Assumptions、Missing Context、Design Defects、Better Options、Trade-offs、User Decisions、Recommendation、Artifact Updates。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/DesignCritic.md` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
