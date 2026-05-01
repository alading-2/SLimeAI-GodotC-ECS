---
name: test-system
description: 开发或扩展运行时测试系统时使用。适用于：新增测试模块、接入实体选择、实现属性测试/技能测试、通过 FeatureDebugService 调试能力生命周期、在测试场景中主动指定选中实体。触发关键词：TestSystem、FeatureDebugService、AttributeTestModule、AbilityTestModule、运行时测试、测试模块、临时加成、调试面板。
---

# TestSystem 入口

## 什么时候用

- 新增或修改运行时测试模块。
- 修改 TestSystem 面板、模块切换、实体选择。
- 扩展属性测试、技能测试、系统监控、对象池信息、资源目录、生成测试。
- 通过 `FeatureDebugService` 调试 Feature / Ability 生命周期。

## 转向其它 Skill

- Feature 生命周期设计 -> `@feature-system`
- 技能执行链路 -> `@ability-system`
- Data 运行时读写 -> `@ecs-data`
- Data 目录配置 -> `@data-authoring`
- Godot 场景运行 -> `@godot-scene-test`

## 必读

- `DocsAI/Modules/TestSystem.md`
- 涉及 SystemManager / SystemRegistry 时读 `DocsAI/Modules/SystemCore.md`
- `DocsAI/Tests/Godot场景测试.md`
- `DocsAI/Tests/测试矩阵.md`
- `Src/ECS/Base/System/TestSystem/README.md`
- `Docs/框架/ECS/System/TestSystem/TestSystem.md`

## 最短流程

1. 读 `DocsAI/Modules/TestSystem.md`，确认边界。
2. 查相近模块实现和 `.tscn`。
3. 新增模块时继承 `TestModuleBase`，并在 `TestModuleSceneRegistry` 注册。
4. 系统能力调用先封装到 Service / Adapter，不散落在 UI 回调。
5. 运行 `dotnet build`。
6. 用 `godot-scene-test` 运行相关场景。
7. 更新 Docs / DocsAI / Skill 映射。

## 关键禁止事项

- 不要把 TestSystem 做成正式玩家 UI。
- 不要在 TestSystem 内新增技能主动触发前台。
- 不要绕开 `FeatureSystem` / `EntityManager` 复制能力生命周期。
- 不要把复杂业务逻辑堆进 `TestSystem.cs`。
- 不要让后台模块失活后继续响应实体事件。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build
```
