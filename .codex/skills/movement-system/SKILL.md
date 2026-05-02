---
name: movement-system
description: 修改或新增 Movement 运动系统时使用。适用于：EntityMovementComponent、EntityOrientationComponent、MovementParams、MoveMode、IMovementStrategy、MovementStrategyRegistry、MovementStopCoordinator、MovementCollisionPolicy、移动碰撞、运动朝向、运动测试场景。
---

# MovementSystem 入口

## 什么时候用

- 新增或修改 `MoveMode` / `IMovementStrategy`。
- 修改 `MovementParams`、策略注册、停止流程。
- 修改运动朝向、速度合成、`EntityOrientationComponent`。
- 修改运动碰撞、穿透计数、碰撞停止和销毁语义。
- 调试 Movement 测试场景。

## 转向其它 Skill

- AI 决策写移动意图 -> `@ai-system`
- 碰撞层、Hurtbox、接触伤害 -> `@collision-system`
- DataKey 定义 -> `@data-authoring`
- 伤害结算 -> `@damage-system`
- Entity 生成 / 对象池 -> `@ecs-entity`

## 必读

- `DocsAI/Modules/Movement.md`
- 涉及碰撞读 `DocsAI/Modules/Collision.md`
- 涉及组件生命周期读 `DocsAI/Modules/Component.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`

## 最短流程

1. 查现有策略是否能复用。
2. 读 `DocsAI/Modules/Movement.md`。
3. 新策略实现 `IMovementStrategy` 并注册到 `MovementStrategyRegistry`。
4. 参数写入 `MovementParams`，运行态留在策略私有字段。
5. 每帧只写 `DataKey.Velocity`，不要直接改位置。
6. 需要视觉朝向时返回 facing direction 或配置 `OrientationParams`。
7. 运行 Movement 场景测试并更新索引 / DocsAI。

## 禁止事项

- 不要在策略中直接改 `GlobalPosition`。
- 不要在 Movement 中做 AI、伤害、动画业务。
- 不要把无限制数值从 `-1` 改成 `0`。
- 不要对外暴露弧度参数。
- 不要引用已不存在的旧 MoveMode。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
```
