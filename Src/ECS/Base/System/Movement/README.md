# Movement 源码入口

本目录存放移动策略系统源码。AI 执行契约见 `DocsAI/Modules/Movement.md`，设计背景见 `Docs/框架/ECS/System/Movement/移动系统设计说明.md`。

## 目录职责

- `Core/`：策略接口、参数、注册表、停止协调、运动碰撞、朝向协议。
- `Strategies/`：各 `MoveMode` 的具体策略实现。
- `ScalarDriver/`：运动策略内连续标量参数的通用驱动器。
- `Utils/`：速度合成、移动辅助函数。

## 关键入口

- 调度器：`Src/ECS/Base/Component/Movement/EntityMovementComponent.cs`
- 碰撞处理：`Src/ECS/Base/Component/Movement/EntityMovementComponent.Collision.cs`
- 朝向输出：`Src/ECS/Base/Component/Movement/EntityOrientationComponent.cs`
- 策略接口：`Core/IMovementStrategy.cs`
- 参数：`Core/MovementParams.cs`
- 策略注册：`Core/MovementStrategyRegistry.cs`
- 停止协调：`Core/Stop/MovementStopCoordinator.cs`
- 碰撞策略：`Core/Collision/MovementCollisionPolicy.cs`

## 当前 MoveMode

`None / Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc`

旧模式 `FixedDirection / TargetPoint / TargetEntity / OrbitPoint / OrbitEntity / Spiral` 已废弃。

## 修改前必读

- `DocsAI/Modules/Movement.md` — AI 执行契约
- `DocsAI/Modules/Collision.md` — 涉及碰撞时
- `DocsAI/Modules/AI.md` — 涉及 AI 意图时

## 新增策略流程概要

1. 确认现有策略不可复用
2. 实现 `IMovementStrategy`，用 `[ModuleInitializer]` 注册
3. 新模式更新 `MovementEnums.cs`，新参数放 `MovementParams`
4. 策略只写 `DataKey.Velocity`，返回 `MovementUpdateResult`
5. 更新 `DocsAI/Modules/Movement.md` 和项目索引
6. 运行 Movement 测试场景

## 测试

```bash
dotnet build
./.claude/skills/GodotSkill/scripts/run-test.sh --build res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn
./.claude/skills/GodotSkill/scripts/run-test.sh res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
```
