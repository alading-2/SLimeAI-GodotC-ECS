# TestSystem Dynamic Module Host Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 把 TestSystem 从预挂载固定模块实例重构为模块清单驱动的动态实例化宿主，解除 ModuleHost 对模块布局的限制。

**Architecture:** 宿主只维护模块导航树、当前选中实体和右侧模块挂载槽位。模块元数据由独立清单提供，切换模块时再通过 PackedScene 动态实例化当前模块，模块根节点铺满右侧槽位，自行决定内部滚动和分栏布局。

**Tech Stack:** Godot 4.6、C#、PackedScene、Control、HSplitContainer、ResourceManagement

---

### Task 1: 模块清单与宿主切换链路

**Files:**
- Create: `Src/ECS/Base/System/TestSystem/Core/TestModuleSceneDefinition.cs`
- Create: `Src/ECS/Base/System/TestSystem/Core/TestModuleSceneRegistry.cs`
- Modify: `Src/ECS/Base/System/TestSystem/TestSystem.cs`
- Modify: `Src/ECS/Base/System/TestSystem/TestSystem.tscn`

- [ ] 以模块清单替代预挂载实例
- [ ] 将 ModuleHost 改为动态实例化槽位
- [ ] 切换模块时实例化当前模块并铺满右侧区域

### Task 2: 对象池模块布局收口

**Files:**
- Modify: `Src/ECS/Base/System/TestSystem/Info/ObjectPoolInfoModule.cs`
- Modify: `Src/ECS/Base/System/TestSystem/Info/ObjectPoolInfoModule.tscn`

- [ ] 左侧只保留对象池名称
- [ ] 右侧改成紧凑摘要和详情
- [ ] 避免详情区过宽、列表区过重

### Task 3: 视觉预览模块布局重构

**Files:**
- Modify: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewModule.cs`
- Modify: `Src/ECS/Base/System/TestSystem/VisualPreview/AssetVisualPreviewModule.tscn`

- [ ] 把信息展示拆成分组概览和当前分类详情
- [ ] 给可变长内容补滚动容器
- [ ] 保证无分组时也有明确状态反馈

### Task 4: 文档同步与验证

**Files:**
- Modify: `Src/ECS/Base/System/TestSystem/README.md`
- Modify: `Docs/框架/ECS/System/TestSystem/TestSystem.md`
- Modify: `Docs/框架/项目索引.md`
- Modify: `.codex/skills/test-system/SKILL.md`

- [ ] 同步 TestSystem 宿主职责和动态实例化流程
- [ ] 记录模块布局约束变更
- [ ] 运行构建验证
