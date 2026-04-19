# Bezier Template Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 为技能侧贝塞尔弹道提供 3-5 阶模板化选点，并让追踪模式在目标移动时保持整体曲线风格稳定。

**Architecture:** 新增 `Bezier` 子域模板生成器，负责按模式与阶数生成相对模板并解析为控制点；`BezierCurveStrategy` 在追踪模式下基于模板重建剩余曲线；技能侧只选择模式与阶数，不再手写控制点。`MovementShotMath` 删除，出生点工具迁移到 `AbilityTool`。

**Tech Stack:** Godot 4.6、C#、.NET 8、现有 Movement/Ability/SingleTest 体系

---

### Task 1: 锁定测试入口与参数面

**Files:**
- Modify: `Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.cs`
- Modify: `Src/ECS/Base/System/Movement/Core/MovementParams.cs`

- [ ] 写一个最小失败入口，先让测试场景能构造模板化贝塞尔参数
- [ ] 运行构建，确认当前代码还不支持新参数而失败
- [ ] 为 `MovementParams` 补充模板字段，测试场景切到新入口
- [ ] 再跑构建，确认参数层通过

### Task 2: 实现贝塞尔模板生成器

**Files:**
- Create: `Src/ECS/Tools/Math/Bezier/BezierPatternType.cs`
- Create: `Src/ECS/Tools/Math/Bezier/BezierCurveTemplate.cs`
- Create: `Src/ECS/Tools/Math/Bezier/BezierTemplateBuilder.cs`
- Modify: `Src/ECS/Tools/Math/Bezier/BezierCurve.cs`

- [ ] 先写最小调用路径，让编译器报缺失类型
- [ ] 实现 3/5 阶模板、模式枚举、控制点解析与必要注释
- [ ] 为 `BezierCurve` 补最小辅助方法，避免策略层重复拼装
- [ ] 运行构建，确认模板工具通过

### Task 3: 重构 BezierCurveStrategy 追踪语义

**Files:**
- Modify: `Src/ECS/Base/System/Movement/Strategies/Curve/BezierCurveStrategy.cs`

- [ ] 先让策略编译依赖新模板类型并产生失败
- [ ] 实现静态控制点兼容逻辑
- [ ] 实现模板模式与追踪时剩余曲线重建
- [ ] 运行构建，确认策略通过

### Task 4: 迁移技能调用并删除旧工具

**Files:**
- Modify: `Src/ECS/Base/System/AbilitySystem/AbilityTool.cs`
- Modify: `Data/Data/Ability/Ability/Movement/BezierShot/BezierShot.cs`
- Modify: `Data/Data/Ability/Ability/Movement/SineWaveShot/SineWaveShot.cs`
- Modify: `Data/Data/Ability/Ability/Movement/ParabolaShot/ParabolaShot.cs`
- Delete: `Data/Data/Ability/Ability/Movement/MovementShotMath.cs`

- [ ] 先把技能改到新工具入口，让旧工具引用暴露完全
- [ ] 迁移 `ResolveSpawnPosition`
- [ ] `BezierShot` 切到模板模式
- [ ] `SineWaveShot` / `ParabolaShot` 内联原本无必要的辅助逻辑
- [ ] 运行构建，确认旧工具可删除

### Task 5: 文档同步与最终验证

**Files:**
- Modify: `Docs/框架/项目索引.md`
- Modify: `Src/ECS/Base/System/Movement/Strategies/README.md`
- Modify: `Src/ECS/Base/Component/Movement/EntityMovementComponent说明.md`
- Modify: `.codex/skills/ability-system/SKILL.md`

- [ ] 更新项目索引与移动文档，记录模板化贝塞尔和追踪语义
- [ ] 同步 Skill 边界描述，避免文档与代码脱节
- [ ] 运行 `dotnet build`
- [ ] 记录验证结果与剩余风险
