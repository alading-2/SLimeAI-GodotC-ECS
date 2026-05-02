# 01 Code And Docs Audit

## 范围

审计当前代码、`DocsAI/`、`.codex/skills/`、`Src/**/*.md` 与项目索引。本文只记录事实，不修改正文。

## 发现的问题

- `DocsAI/Modules/` 已有 Entity / Component / Data / Event / SystemCore / AbilitySystem / DamageSystem / TestSystem / UI / Tools，但缺少独立 `FeatureSystem.md` 与 `DataAuthoring.md`。
- `DocsAI/ProjectState.md` 仍停在旧 `AI_First_Program_Migration` 计划 07，未指向本轮二次收敛计划。
- `DocsAI/INDEX.md` 仍只列旧 AI-First 迁移计划，未列本轮文档与代码对齐计划。
- `.codex/skills/feature-system/SKILL.md`、`ecs-entity`、`ecs-data`、`data-authoring`、`tools`、`damage-system`、`ui-bind` 等 Skill 偏长，包含大量应下沉到 `DocsAI/Modules/` 的细节。
- `DocsAI/Skills/Skill到DocsAI映射.md` 中 `feature-system` 仍指向 `DocsAI/Modules/AbilitySystem.md` 与人类长文档，缺少 `DocsAI/Modules/FeatureSystem.md`。
- `data-authoring` 与 `ecs-data` 的边界已有说明，但 AI 模块契约尚未独立拆分。
- `Docs/框架/项目索引.md` 同时列出 `.codex/skills/` 和旧 `.windsurf/skills/`，容易让 AI 误读当前入口。
- `Src/**/*.md` 中仍有旧 Windows 机器绝对链接和 `file:///` 链接，集中在 EntityManager、Entity 规范、NodeLifecycle、AttackComponent 等文档。

## 代码现状要点

- `FeatureSystem` 当前代码入口在 `Src/ECS/Base/System/FeatureSystem/`，核心对象包括 `FeatureContext`、`FeatureInstance`、`FeatureHandlerRegistry`、`IFeatureHandler`、`FeatureSystem` 和 Action 目录。
- `AbilitySystem` 已通过 `FeatureContext.ActivationData = CastContext` 接入 Feature 生命周期，并从 `ExecuteResult` 读取执行结果。
- 数据配置运行时主线是 `Data/DataNew/` 纯 C# 数据表，旧 `Data/Data/` 与 `.tres` 作为归档和对照迁移。
- 系统元数据运行时主线是 `Data/DataNew/System/SystemData.cs` 与 `SystemPresetData.cs`。
- 运行时 Data 容器在 `Src/ECS/Base/Data/`，数据目录协议在 `Data/`，两者需要在 DocsAI 中分工。

## 审计命令

```bash
find DocsAI -maxdepth 3 -type f | sort
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
find Src -name "*.md" -print | sort
rg -n "/mnt/e|file:///|复刻土豆兄弟|\\.windsurf" DocsAI .codex/skills Docs/框架/项目索引.md Src -g "*.md"
```
