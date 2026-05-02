---
name: tools
description: 使用 TimerManager 计时器、ObjectPool 对象池、TargetSelector 目标查询、ResourceManagement 资源加载时使用。适用于：需要延迟/循环定时器，高频生成销毁对象，范围内查找敌人，加载场景或配置资源。触发关键词：TimerManager、定时器、延迟执行、对象池、ObjectPool、TargetSelector、范围查找、ResourceManagement、加载资源。
---

# Tools 入口

## 什么时候用

- 使用或修改 `TimerManager`。
- 使用或修改 `ObjectPool` / `ObjectPoolManager`。
- 范围查找、阵营过滤、目标排序。
- 加载资源、维护 `ResourceManagement` / `ResourcePaths` / `ResourceCatalog`。
- 修改通用 Math / Logger / NodeLifecycle 工具。

## 转向其它 Skill

- Entity 对象池生成和销毁 -> `@ecs-entity`
- 技能目标和效果 -> `@ability-system`
- UI 高频对象 -> `@ui-bind`
- 数据配置和资源路径协议 -> `@data-authoring`

## 必读

- `DocsAI/Modules/Tools.md`
- 涉及对象池、TimerManager、ResourceManagement 核心实现时读 `DocsAI/Workflows/ECS核心修改门禁.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 先查是否已有现成 Tool。
2. 定时统一用 `TimerManager`，并设计取消点。
3. 高频对象统一走对象池；Entity 生成仍由 `EntityManager.Spawn` 编排。
4. 目标查询用 `EntityTargetSelector.Query`。
5. 资源加载用 `ResourceManagement` 或项目允许的最终加载工具。
6. 运行对应工具场景或相关模块场景。
7. 更新 `DocsAI/Modules/Tools.md` 或资源说明。

## 禁止事项

- 不要 `new Timer()` / `GetTree().CreateTimer()`。
- 不要高频 `new` + `QueueFree()`。
- 不要 `GetTree().GetNodesInGroup()` 手写范围查找。
- 不要业务代码直接 `GD.Load<T>("res://...")`。
- 不要把具体玩法逻辑塞进通用 Tools。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/ObjectPool/ObjectPoolManagerTest.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.tscn --build
```
