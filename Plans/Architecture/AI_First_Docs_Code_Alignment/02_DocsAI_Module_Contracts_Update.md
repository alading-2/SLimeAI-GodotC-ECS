# 02 DocsAI Module Contracts Update

## 目标

按当前代码更新 `DocsAI/Modules/`，让 Skill 能通过短入口加载稳定契约。

## 本轮修改

- 新增 `DocsAI/Modules/FeatureSystem.md`。
- 新增 `DocsAI/Modules/DataAuthoring.md`。
- 更新 `DocsAI/INDEX.md`，把本轮计划、新增模块和新增 Skill 映射列为入口。
- 更新 `DocsAI/ProjectState.md`，把当前阶段切换到本轮文档与代码对齐计划。
- 更新 `DocsAI/Skills/Skill到DocsAI映射.md`，让 `feature-system` 指向新 FeatureSystem 契约，让 `data-authoring` 指向 DataAuthoring 契约。
- 在 `AbilitySystem.md` 与 `Data.md` 中补充跨模块边界指向。

## 验收标准

- `find DocsAI -maxdepth 3 -type f | sort` 能看到新增模块。
- `rg -n "FeatureSystem.md|DataAuthoring.md" DocsAI .codex/skills` 能找到入口映射。
- 新模块包含职责边界、核心入口、禁止事项、修改流程、推荐测试、人工审查重点。
