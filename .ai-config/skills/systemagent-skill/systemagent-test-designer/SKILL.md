---
name: systemagent-test-designer
description: 把需求变成标准答案和验证场景。用于新行为、Godot scene、Runtime test、DataOS validator 或验收标准缺失时。
---

# systemagent-test-designer

## 触发条件

- 新行为、Godot scene、Runtime test、DataOS validator 或验收标准缺失。
- 用户要求"帮我设计测试方案"或"写验证场景"。

## 必读

- `Workspace/SystemAgent/Actors/TestDesigner.md`
- SDD design/tasks/bdd、BDDSceneFormat。
- 相关 gameplay lifecycle BDD（涉及多系统时）。
- 现有 tests/scene README、失败模式。

## 输出要求

按 `TestDesigner.md` 正文的 Output shape 输出：expectedInputs、expectedObservations、passCriteria、failCriteria、artifactPath 和最小验证项。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/TestDesigner.md` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
