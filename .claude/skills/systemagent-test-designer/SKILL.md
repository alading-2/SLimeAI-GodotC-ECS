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
- 设计旁 `.FeatureSpec.md`，优先读取 `TDD Handoff`。
- SDD design/tasks/bdd、BDDSceneFormat。
- 相关 gameplay lifecycle BDD（涉及多系统时）。
- 现有 tests/scene README、失败模式。

## 输出要求

按 `TestDesigner.md` 正文的 Output shape 输出：expectedInputs、expectedObservations、passCriteria、failCriteria、artifactPath 和最小验证项。优先消费 `.FeatureSpec.md` 的 `TDD Handoff`；缺失时先补标准答案或说明默认假设。

先把行为和标准答案讲清楚，再进入测试实现。SlimeAI 当前默认优先使用 Godot headless scene、DataOS validator、ValidationSession artifact 和 structured log；不要为了“快速测试”默认引入脱离 Godot 运行语义的新测试框架。

## 禁止

- 不复制 `Workspace/SystemAgent/Actors/TestDesigner.md` 正文。
- 不直接修改 `.ai-config/sync-targets.json` 定义的 skill 同步副本作为源。
- 不把 owner capability skill 迁入 `.ai-config/skills/systemagent-skill/`。
