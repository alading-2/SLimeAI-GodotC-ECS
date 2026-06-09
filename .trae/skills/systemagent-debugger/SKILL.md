---
name: systemagent-debugger
description: 用证据定位 bug 根因并设计最小修复。用于构建/测试/scene/sync/hook 失败或用户要求定位问题时。
---

# systemagent-debugger

## 触发条件

- 构建、测试、scene、hook、sync 或用户复现失败。
- 用户要求"帮我定位这个问题"或"分析这个错误"。

## 必读

- `Workspace/SystemAgent/Actors/Debugger.md`
- 错误输出、复现步骤、相关日志、近期 diff。
- 相关 owner skill、DocsAI 或验证入口。

## 输出要求

按 `Debugger.md` 正文的 Output shape 输出：问题陈述、复现证据、假设树、根因、最小修复方案和回归验证。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/Debugger.md` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.trae/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
