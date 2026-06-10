---
name: systemagent-config-maintenance-workflow
description: SystemAgent ConfigMaintenance workflow 入口。用于修改 skill、rule、hook、subagent、sync 脚本或 skill-test。
---

# systemagent-config-maintenance-workflow

## 触发条件

任务涉及 .ai-config、.claude、.codex、.windsurf、hook、subagent、rules、commands、sync 或 skill-test。

## 必读

- `Workspace/SystemAgent/Routes/ConfigMaintenance.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`
- `Workspace/SystemAgent/Tools/skill-test/README.md`
- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Actors/Verifier.md`

## 输出要求

维护源判断、修改范围、sync/lint/hook smoke 结果、旧路径命中分类。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/` 或 `.ai-config/skills/systemagent-workflow/`。
