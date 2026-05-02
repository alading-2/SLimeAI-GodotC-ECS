# Movement 源码入口

本目录存放移动策略系统源码。AI 执行规则见 `DocsAI/Modules/Movement.md`，长设计背景见 `Docs/框架/ECS/System/Movement/移动系统设计说明.md`。

## 目录职责

- `Core/`：策略接口、参数、注册表、停止协调、运动碰撞协议。
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

旧文档中的 `FixedDirection / TargetPoint / TargetEntity / OrbitPoint / OrbitEntity / Spiral` 已不是当前代码模式。

## AI 修改前必读

- `DocsAI/Modules/Movement.md`
- 涉及碰撞读 `DocsAI/Modules/Collision.md`
- 涉及 AI 意图读 `DocsAI/Modules/AI.md`
- 涉及技能命中读 `DocsAI/Modules/AbilitySystem.md`

## 最短修改流程

1. 先确认现有策略是否可复用。
2. 新策略实现 `IMovementStrategy`。
3. 新模式更新 `Data/DataKey/Component/Movement/MovementEnums.cs`。
4. 用 `[ModuleInitializer]` 注册到 `MovementStrategyRegistry`。
5. 输入参数放 `MovementParams`，策略运行态放策略私有字段。
6. 策略只写 `DataKey.Velocity`，不直接改 `GlobalPosition`。
7. 更新 `DocsAI/Modules/Movement.md` 和 `Docs/框架/项目索引.md`。

## 测试场景

- `res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn`
- `res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn`

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
```
