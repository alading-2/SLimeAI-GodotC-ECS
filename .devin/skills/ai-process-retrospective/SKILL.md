---
name: ai-process-retrospective
description: SystemAgent retrospective 兼容入口。任务完成、用户要求或 hook 提醒时使用；路由到 Workspace/SystemAgent/Actors/Retrospective.md、WorkflowIteration.md 和 VerdictVocabulary.md。
---

# AI Process Retrospective

## 触发条件

- 任何 AI 任务完成前或验证后。
- 用户显式要求复盘流程、方向、验证或文档缺口。
- Hook 提醒需要 final 前检查。

## 必读

- `Workspace/SystemAgent/Actors/Retrospective.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `Workspace/SystemAgent/Rules/VerdictVocabulary.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `SDD/active/<sdd>/progress.md`（当任务使用 SDD 时）

## 输出要求

输出必须给出证据化流程缺口判断、已更新的事实源、SDD progress 状态（如适用）、follow-up 候选，以及与 `VerdictVocabulary.md` 一致的最终 verdict。

## 禁止

- 不复制 retrospective role 或 workflow 正文。
- 不写空泛“流程正常”；必须引用任务、文件、验证命令或 artifact 证据。
- 不把旧 `Workspace/DocsAI/AgentWorkflow/` 当作当前角色事实源。
