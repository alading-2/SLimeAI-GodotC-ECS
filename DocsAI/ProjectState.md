# DocsAI ProjectState

本文记录 AI-First 迁移的当前状态。保持短，只写能帮助下一个 AI 会话继续工作的内容。

## 当前目标

把 `AI-First Godot C# ECS 程序开发总说明` 落地为可执行的工程体系：

- Docs / DocsAI 分层
- Godot CLI 测试和日志闭环
- Skill 短入口重构
- 核心模块 AI 契约
- 长任务上下文恢复
- 功能开发试点
- ECS 核心回归门禁

## 当前阶段

计划 07：ECS 核心回归与人工审查门禁收尾。

## 已完成

- 已将总说明拆成阶段计划：
  - `Plans/Architecture/AI_First_Program_Migration/README.md`
  - `Plans/Architecture/AI_First_Program_Migration/01_DocsAI_Document_System_Migration.md`
  - `Plans/Architecture/AI_First_Program_Migration/02_Godot_CLI_Test_Debug_Loop.md`
  - `Plans/Architecture/AI_First_Program_Migration/03_Skill_System_Refactor.md`
  - `Plans/Architecture/AI_First_Program_Migration/04_Framework_Module_Contract_Docs.md`
  - `Plans/Architecture/AI_First_Program_Migration/05_Long_Task_Context_And_Project_Plans.md`
  - `Plans/Architecture/AI_First_Program_Migration/06_AI_First_Feature_Development_Pilot.md`
  - `Plans/Architecture/AI_First_Program_Migration/07_Core_Regression_And_Review_Gate.md`
- 已新增 `Docs/README.md`。
- 已新增 `DocsAI/README.md`、`DocsAI/INDEX.md`、`DocsAI/ProjectState.md`。
- 已新增 `DocsAI/Workflows/AI开发闭环.md`、`DocsAI/Workflows/文档迁移协议.md`。
- 已增强 `.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs`，输出 `failed / failureReason / errorContext / logSummary`。
- 已新增 `DocsAI/Tests/Godot场景测试.md`、`DocsAI/Tests/测试矩阵.md`、`DocsAI/Tests/日志判定与Debug.md`。
- 已将 `.codex/skills/godot-scene-test/SKILL.md` 缩短为入口文档。
- 已新增 `DocsAI/Skills/Skill设计规则.md` 和 `DocsAI/Skills/Skill到DocsAI映射.md`。
- 已重构 `project-index`、`ecs-component`、`test-system` 为短入口。
- 已新增 `DocsAI/Modules/Component.md` 和 `DocsAI/Modules/TestSystem.md` 初版契约。
- 已清理 `ability-system` 里的旧机器绝对路径。
- 已补齐 `DocsAI/Modules/Entity.md`、`Data.md`、`Event.md`、`SystemCore.md`、`AbilitySystem.md`、`DamageSystem.md`、`UI.md`、`Tools.md`。
- 已新增 `DocsAI/Workflows/长任务上下文协议.md` 和 `DocsAI/Workflows/ECS核心修改门禁.md`。
- 已把项目级计划状态文件统一放在根目录 `Plans/`。
- 已将 06 试点方向修正为复验现有 `LifecycleComponent + DataKey.MaxLifeTime`，不再重复新增 `LifetimeComponent`。

## 未完成

- 部分 `.codex/skills/*` 仍可继续压缩，但已补齐 DocsAI 映射入口。
- `Src/**/*.md` 尚未迁移或改成短跳转。
- 历史绝对路径和旧链接尚未全量清理。

## 下一步

按 `Plans/Architecture/AI_First_Program_Migration/Progress.md` 推进后续真实功能任务。若要继续验证 AI 闭环，优先执行现有 `LifecycleComponent + MaxLifeTime` 复验任务，而不是新增重复组件。

当前 `MainTest` 仍失败：`failureReason=TimedOut`，关键错误是 `PickupComponent.cs` 无法实例化，并伴随大量 `!is_inside_tree()`。这是后续独立 Debug 任务。

## 阻塞问题

暂无。

## 验证方式

当前阶段涉及 DocsAI / Skill / 测试工具验证，推荐命令：

```bash
find .codex/skills -maxdepth 2 -name SKILL.md -print | sort | xargs wc -l
rg -n "/mnt/e|file:///|复刻土豆兄弟" .codex/skills
rg -n "DocsAI/|Skill到DocsAI映射|Skill设计规则" .codex/skills DocsAI
rg -n "职责边界|禁止事项|推荐测试|人工审查重点" DocsAI/Modules
```
