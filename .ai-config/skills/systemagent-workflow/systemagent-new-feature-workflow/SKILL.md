---
name: systemagent-new-feature-workflow
description: SystemAgent NewFeature workflow 入口。用于新功能、重构、迁移、SDD 实施或跨目录文档治理。
---

# systemagent-new-feature-workflow

## 触发条件

创建新功能、扩展能力、重构、迁移旧逻辑、执行 SDD task 或跨目录文档治理。

## 必读

- `Workspace/SystemAgent/Routes/NewFeature.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/Planner.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`（large 或高风险 medium 时）
- `Workspace/SystemAgent/Actors/TestDesigner.md`
- `Workspace/SystemAgent/Actors/Reviewer.md`
- `Workspace/SystemAgent/Actors/Retrospective.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `Workspace/SystemAgent/Rules/Documentation.md`
- `Workspace/SystemAgent/Rules/DesignDocument.md`（写设计文档时）

## 输出要求

selected workflow、must_read 已读/未读清单、DeepThink 分析结论或跳过原因、实现/验证切片、tasks 更新、owner skill 更新状态和最终验证摘要。写设计文档时必须保留用户原始问题，并优先写清问题分析和解决思路。

新功能如果改变能力边界、路由、验证命令或场景门禁，必须更新对应 `.ai-config/skills/<category>/<owner>/SKILL.md` 源，运行 `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 与 skill-test lint，并在最终摘要说明 sync/lint 结果。涉及 Godot 场景时必须新增或更新专项验证场景；主场景 smoke 不能替代专项验收。

## 禁止

- 不复制 `Workspace/SystemAgent/Routes/`、`Roles/`、`Gates/` 或 `Policies/` 正文。
- 不直接修改 `.codex/skills/`、`.claude/skills/`、`.windsurf/skills/` 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/` 或 `.ai-config/skills/systemagent-workflow/`。
- 不把 DeepThink capability 正文复制进 wrapper；正文以 `Workspace/SystemAgent/Actors/DeepThink.md` 为准。
