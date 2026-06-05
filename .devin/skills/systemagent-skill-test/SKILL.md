---
name: systemagent-skill-test
description: SystemAgent skill-test 短入口。用于运行 wrapper skill 静态 lint 并检查目录/catalog/sync 漂移。
---

# systemagent-skill-test

## 触发条件

改动 .ai-config/skills/ 任意 SKILL.md、运行 sync 后，或用户要求检查 skill 质量。

## 必读

- `Workspace/SystemAgent/Tools/skill-test/README.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`
- `Workspace/SystemAgent/Registry/skills.yaml`

## 输出要求

lint 命令、R001-R006 摘要、critical/advisory 分类和修订动作。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent/`。
