# Movement Strategies 源码入口

本目录存放 `IMovementStrategy` 的具体实现。AI 执行契约见 `DocsAI/Modules/Movement.md`。

## 目录

- `Base/`：`PlayerInput`、`AIControlled`、`AttachToHost` 等常驻或基础策略。
- `Charge/`：直线冲锋、追点、追踪目标。
- `Orbit/`：固定点或目标实体环绕。
- `Wave/`：正弦波前进。
- `Curve/`：贝塞尔、抛物线、圆弧。
- `Projectile/`：回旋镖等投射物轨迹。

## 策略硬规则

- 策略只计算运动意图并写 `DataKey.Velocity`。
- 位移由 `EntityMovementComponent` 统一执行。
- 运行时状态放策略私有字段。
- 输入参数来自 `in MovementParams`。
- 完成时返回 `MovementUpdateResult.Complete()`，不要自行销毁实体。
- 需要视觉朝向时通过 `MovementUpdateResult` 返回 facing direction。
- 需要连续标量演化时优先使用 `ScalarDriver`。

## 新增策略清单

1. 在合适子目录新增策略类。
2. 实现 `IMovementStrategy`。
3. 用 `[ModuleInitializer]` 注册到 `MovementStrategyRegistry`。
4. 如需新模式，更新 `Data/DataKey/Component/Movement/MovementEnums.cs`。
5. 如需新参数，更新 `MovementParams`。
6. 更新 `DocsAI/Modules/Movement.md`、本 README 和项目索引。
7. 运行 Movement 测试场景。

## 测试

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
```
