---
name: systemagent-retrospective
description: SystemAgent retrospective 短入口。完成前检查流程缺口、verdict 一致性和 follow-up。
---

# systemagent-retrospective

## 触发条件

任务完成前、验证后、用户要求复盘、或 hook 提醒进行 retrospective。

## 必读

- `Workspace/SystemAgent/Actors/Retrospective.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `Workspace/SystemAgent/Rules/VerdictVocabulary.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- 当前会话的 ChatHistory digest `derived/efficiency.md`（如果存在）

## 输出要求

conclusion/verdict 一致性结论、证据化 findings、process updates 与 follow-up。
效率洞察：验证循环、文件读放大、skill 重复加载。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/` 或 `.ai-config/skills/systemagent-workflow/`。
