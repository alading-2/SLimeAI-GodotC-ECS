---
name: systemagent-workflow-iteration
description: SystemAgent 流程迭代短入口。用于分析 AI 流程缺口并更新 workflow、role、gate、policy 或文档治理。
---

# systemagent-workflow-iteration

## 触发条件

用户指出 AI 流程方向不对、要求复盘对话、或 retrospective 发现流程缺口。

## 必读

- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`（设计缺口或多方案取舍时）
- `Workspace/SystemAgent/Actors/Retrospective.md`
- `Workspace/SystemAgent/Actors/Planner.md`
- `Workspace/SystemAgent/Actors/Implementer.md`
- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Actors/Documentarian.md`
- `Workspace/SystemAgent/Rules/DesignDocument.md`（写设计文档时）
- `Workspace/SystemAgent/Rules/VerdictVocabulary.md`

## 输出要求

问题归因、缺口分类、DeepThink/DesignCritic 结论或跳过原因、目标事实源、最小修改方案、验证结果和 follow-up。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/` 或 `.ai-config/skills/systemagent-workflow/`。
- 不把 DeepThink capability 正文复制进 wrapper；正文以 `Workspace/SystemAgent/Actors/DeepThink.md` 为准。
