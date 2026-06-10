---
name: skill-test
description: 维护 systemagent skill 库的静态 lint。改动 .ai-config/skills/ 任意 skill 后主动跑 lint，报告 R001-R006 违规并给出修订动作。
tags:
  - maintenance
  - lint
  - skill-quality
---

# Skill Test — Static Lint

## 触发条件

- 改动了 `.ai-config/skills/` 下任意 `SKILL.md` 之后。
- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 运行后。
- 用户显式要求检查 skill 质量。

## 必读

- `Workspace/SystemAgent/Tools/skill-test/README.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`
- `Workspace/SystemAgent/Registry/skills.yaml`

## 入口命令

```bash
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

## 输出要求

报告 R001-R006 的 critical/advisory 摘要、JSON report 路径和建议修订动作。

## 禁止

- 不复制 runner 实现或 wrapper policy 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
