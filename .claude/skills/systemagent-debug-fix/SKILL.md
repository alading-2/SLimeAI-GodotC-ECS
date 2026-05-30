---
name: systemagent-debug-fix
description: SystemAgent Debug Fix 短入口。用于 bug、测试失败、验证失败或运行异常定位。
---

# systemagent-debug-fix

## 触发条件

出现 bug、构建/测试/scene/sync/hook 失败，或用户要求基于证据定位问题。

## 必读

- `Workspace/SystemAgent/Routes/DebugFix.md`
- `Workspace/SystemAgent/Actors/Debugger.md`
- `Workspace/SystemAgent/Actors/Verifier.md`
- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`

## 输出要求

复现证据、假设树、根因、最小修复、回归验证与剩余风险。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent/`。
