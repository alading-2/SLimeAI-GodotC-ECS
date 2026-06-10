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
- 当前会话或用户指定范围的 ChatHistory coverage：优先用 `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py stale-report --source-root <codex-session-root>` 或 `list-digests` 定位。
- 可用 digest 的 `derived/ai-context.md`、`derived/efficiency.md`、`derived/tool-failures.md`；缺失时报告 `coverage=stale|partial-current|unknown`。

## 输出要求

conclusion/verdict 一致性结论、证据化 findings、process updates、follow-up 与 `sessionEvidence`。
效率洞察：验证循环、文件读放大、skill 重复加载、tool failure category / recovered / final impact。
`sessionEvidence` 至少写 digest path、source session、coverage、stale/missing 摘要、partial/current 状态、efficiency summary 和 failure summary。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/` 或 `.ai-config/skills/systemagent-workflow/`。
