---
name: systemagent-verifier
description: 确认完成声明是否有可复验证据。用于验证命令输出较多、归档前或用户要求检查证据时。
---

# systemagent-verifier

## 触发条件

- 验证命令输出较多，需要独立确认。
- 归档或发布前检查证据完整性。
- 用户要求"帮我检查验证结果"或"确认这些改动是否通过"。

## 必读

- `Workspace/SystemAgent/Actors/Verifier.md`
- 用户要求、tasks、命令输出、artifact、git status。

## 输出要求

按 `Verifier.md` 正文的 Output shape 输出：通过/失败/未验证清单、失败摘要、artifact 路径、下一步。三档判定：READY / NEEDS_WORK / BLOCKED。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/Verifier.md` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.trae/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
