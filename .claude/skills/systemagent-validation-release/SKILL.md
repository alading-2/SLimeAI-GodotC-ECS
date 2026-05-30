---
name: systemagent-validation-release
description: SystemAgent 验证发布短入口。用于大改后完整验证、归档前检查和发布前证据闭环。
---

# systemagent-validation-release

## 触发条件

大改完成后、SDD 完成前、发布前或用户要求完整验证。

## 必读

- `Workspace/SystemAgent/Routes/ValidationRelease.md`
- `Workspace/SystemAgent/Actors/Verifier.md`
- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Actors/Retrospective.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `Workspace/SystemAgent/Rules/VerdictVocabulary.md`

## 输出要求

验证矩阵、命令结果、未验证原因、旧路径分类、owner skill/docs 同步状态、SDD readiness。

归档或发布前必须确认受影响 owner skill 的 `.ai-config` 源已同步到 `.codex/.claude/.windsurf` 副本；若无需更新，必须说明理由。涉及 Godot 验证场景时必须检查 README 五字段、`index.json`、`result.json`、scene artifact 和 ValidationCatalog。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent/`。
