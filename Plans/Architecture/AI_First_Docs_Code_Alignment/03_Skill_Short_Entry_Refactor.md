# 03 Skill Short Entry Refactor

## 目标

Skill 只做短入口，不承载长篇领域知识。模块细节放 `DocsAI/Modules/`，测试流程放 `DocsAI/Tests/`，人类设计背景放 `Docs/` 或源码旁 README。

## 本轮压缩对象

- `damage-system`
- `data-authoring`
- `ecs-data`
- `ecs-entity`
- `ecs-event`
- `feature-system`
- `tools`
- `ui-bind`
- 轻量补充 `ability-system`

## 保持不变

- Skill 名称不改。
- frontmatter 的触发描述只做必要修正。
- `godot-scene-test` 当前已有外部改动，本轮不覆盖其脚本和现有使用说明。

## 验收标准

- `find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l` 总行数明显下降。
- 每个 Skill 至少包含：职责、转向其它 Skill、必读、最短流程、禁止事项、验证命令。
- Skill 中不再复制长代码示例和完整处理器列表。
