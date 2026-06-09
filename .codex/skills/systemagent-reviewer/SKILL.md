---
name: systemagent-reviewer
description: 按 gate 独立检查方向、边界、遗漏和回归风险。用于计划、实现、验证或归档前需要独立审查时。
---

# systemagent-reviewer

## 触发条件

- 用户要求"帮我审查这个方案/PR/改动"。
- 计划、实现、验证或归档前需要独立检查。
- Retrospective 发现需要独立 review。

## 必读

- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `Workspace/SystemAgent/Rules/VerdictVocabulary.md`

## 输出要求

按 `Reviewer.md` 正文的 Output shape 输出：每个 gate 的证据与 verdict，末尾必须保留一行以 `APPROVE`、`CONCERNS` 或 `REJECT` 开头的聚合 verdict。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/Reviewer.md` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.trae/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
