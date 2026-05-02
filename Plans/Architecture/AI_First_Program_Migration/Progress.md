# 当前计划（历史归档）

## 当前目标

旧 AI-First Godot C# ECS 程序开发体系迁移已完成并归档。它把原始总说明落地为：

- `Docs/` 给人看。
- `DocsAI/` 给 AI 执行。
- `.codex/skills/*` 作为短入口。
- 项目级执行计划统一在根目录 `Plans/`。

## 当前阶段

历史计划已完成。后续文档与 Skill 按代码对齐工作转入：

- `Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`

## 已完成

- 01 Docs / DocsAI 文档体系迁移。
- 02 Godot CLI 场景测试和日志 Debug 闭环。
- 03 高频 Skill 短入口重构。
- 04 核心模块 AI 契约补齐。
- 05 长任务上下文协议，计划文件统一在本目录。
- 06 功能试点方向修正：优先复验已有 `LifecycleComponent + DataKey.MaxLifeTime`，不重复新增 `LifetimeComponent`。
- 07 ECS 核心修改门禁初版。

## 移交到后续计划

- `Src/**/*.md` 仍需按模块逐步判断留在源码旁、迁到 `Docs/` 或迁到 `DocsAI/`。
- Skill 仍需按真实 AI 开发任务继续验证可执行性。
- `MainTest` 当前存在历史 C# script instantiate 失败和大量 `!is_inside_tree()`，需要单独修复，不属于本次文档迁移。

## 下一步

从 `DocsAI/ProjectState.md` 和 `Plans/Architecture/AI_First_Docs_Code_Alignment/Progress.md` 继续。

## 验证方式

```bash
find DocsAI/Modules -maxdepth 1 -type f | sort
rg -n "职责边界|禁止事项|推荐测试|人工审查重点" DocsAI/Modules
rg -n "DocsAI/Modules|ECS核心修改门禁|长任务上下文协议" DocsAI .codex/skills
```

## 人工审查重点

- Docs / DocsAI / Skill 边界是否清晰。
- 计划是否集中在本目录。
- 06 试点是否接受“复验现有 LifecycleComponent”而非新增重复组件。
