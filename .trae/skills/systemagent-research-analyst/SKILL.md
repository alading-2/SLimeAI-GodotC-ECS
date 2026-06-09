---
name: systemagent-research-analyst
description: 把外部资料转成可采纳或拒绝的证据。用于用户要求研究外部资料或内部事实源不足以判断设计时。
---

# systemagent-research-analyst

## 触发条件

- 用户要求研究外部资料。
- 当前任务需要参考 Resources、官方文档、开源项目才能判断方案。

## 必读

- `Workspace/SystemAgent/Actors/ResearchAnalyst.md`
- `Workspace/SystemAgent/Rules/Boundary.md`
- ExternalResources policy、用户指定资源、最小范围搜索结果。

## 输出要求

按 `ResearchAnalyst.md` 正文的 Output shape 输出：Evidence / Inference / Unknown，Adopt Now / Later / Reject，SlimeAI 落点。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/ResearchAnalyst.md` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.trae/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
