# EntityMovementComponent 源码入口

运动系统调度器。设计说明见 `Docs/框架/ECS/System/Movement/移动系统设计说明.md`，AI 执行契约见 `DocsAI/Modules/Movement.md`。

## 核心文件

- `EntityMovementComponent.cs` — 调度器主逻辑
- `EntityMovementComponent.Collision.cs` — 移动碰撞处理
- `EntityOrientationComponent.cs` — 朝向最终输出层

## 关键私有状态

- `_params`：本次运动输入参数（`MovementParams`，由事件传入）
- `_moveCompleted`：停止标志（防重复收口）
- `_currentStrategy`：当前策略实例（每次切换新建）
- `_collisionPolicy`：本实体本次运动的局部碰撞过滤/计数状态

## 执行顺序

每帧 `RunMovementLogic()` → 策略 `Update` 写 `DataKey.Velocity` → `AccumulateTravel()` → `CheckEndConditions()` → `ApplyMovement()`（`VelocityResolver` 分层合成 → `CharacterBody2D.MoveAndSlide()` 或直接位移）

## 停止流程

统一入口 `StopMovement(...)`，来源包括：策略主动完成、时间/距离阈值、碰撞策略自动停止、外部 `MovementStopRequested`。停止协调器 `MovementStopCoordinator` 负责解析 `MovementStopReason`，决定是否发 `MovementCompleted`、销毁实体、或回退 `DefaultMoveMode`。

## Velocity 分层合成

`VelocityResolver.Resolve(data)` 优先级：`IsMovementLocked → Zero` > `VelocityOverride ≠ Zero → VelocityOverride` > `Velocity + VelocityImpulse`（用后清零）

## 移动碰撞链路

原始候选 → `MovementCollisionPolicy.TryAccept(...)` → `OnCollision` 回调 / `MovementCollisionEventData` 事件 → 达到 `StopAfterCollisionCount` 时发 `MovementStopRequested`

`MovementCollisionPolicy` 负责：阵营过滤、实体类型过滤、目标匹配（`Any / TrackedTargetOnly / SpecificNode`）、去重计数。

## DefaultMoveMode 与打断

- 实体初始化时写 `DataKey.DefaultMoveMode`，组件注册后自动进入
- 临时运动结束：按 `NextMode` 切换或回退 `DefaultMoveMode`
- 当前为临时模式且 `CanBeInterrupted = false`：拒绝新 `MovementStarted`

## 朝向

- `EntityMovementComponent` 只发布 `MovementFacingDirection`，不直接写 `RotationDegrees`/`FlipH`
- `EntityOrientationComponent` 是唯一最终朝向输出层，`Sink` 可选 `RootRotation` 或 `VisualFlipX`
- `MovementParams.Orientation` 附加朝向控制参数

## 测试场景

- `res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn`
- `res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn`

```bash
dotnet build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build
```
