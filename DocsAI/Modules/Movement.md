# Movement 模块契约

本文是 AI 修改移动策略、朝向、运动碰撞或运动测试前必须阅读的执行契约。长设计背景见 `Docs/框架/ECS/System/Movement/移动系统设计说明.md`。

## 职责边界

Movement 负责把“移动意图”转换为实体位移，并统一处理策略切换、速度合成、运动停止、运动碰撞和朝向意图发布。

Movement 不负责：

- AI 决策。
- 攻击状态机。
- 伤害结算。
- 动画播放。
- 技能目标选择。

## 核心入口

- 调度器：`Src/ECS/Base/Component/Movement/EntityMovementComponent.cs`
- 碰撞分支：`Src/ECS/Base/Component/Movement/EntityMovementComponent.Collision.cs`
- 朝向输出：`Src/ECS/Base/Component/Movement/EntityOrientationComponent.cs`
- 策略接口：`Src/ECS/Base/System/Movement/Core/IMovementStrategy.cs`
- 参数：`Src/ECS/Base/System/Movement/Core/MovementParams.cs`
- 模式枚举：`Data/DataKey/Component/Movement/MovementEnums.cs`
- 注册表：`Src/ECS/Base/System/Movement/Core/MovementStrategyRegistry.cs`
- 停止协调：`Src/ECS/Base/System/Movement/Core/Stop/MovementStopCoordinator.cs`
- 移动碰撞：`Src/ECS/Base/System/Movement/Core/Collision/`
- 策略目录：`Src/ECS/Base/System/Movement/Strategies/`

## 当前模式

当前 `MoveMode` 包含：

- `None`
- `Charge`
- `Orbit`
- `SineWave`
- `BezierCurve`
- `Boomerang`
- `AttachToHost`
- `PlayerInput`
- `AIControlled`
- `Parabola`
- `CircularArc`

不要引用旧模式名，如 `FixedDirection`、`TargetPoint`、`TargetEntity`、`OrbitPoint`、`OrbitEntity`、`Spiral`。

## 数据 / 事件 / 生命周期

- 常驻移动由 `DataKey.DefaultMoveMode` 进入，如玩家 `PlayerInput`、敌人 `AIControlled`。
- 临时移动通过 `GameEventType.Unit.MovementStarted` 传入完整 `MovementParams`。
- 策略只写 `DataKey.Velocity`，不得直接修改 `GlobalPosition`。
- 最终位移由 `EntityMovementComponent` 通过 `VelocityResolver` 执行。
- `CharacterBody2D` 路径使用 `MoveAndSlide()`，非 `CharacterBody2D` 的 `Node2D/Area2D` 直接位移。
- 策略完成、外部请求、碰撞停止、打断和组件注销都走统一停止流程。
- 结束语义通过 `MovementStopReason` 和 `MovementStopContext` 传给策略 `OnStop` 与 `MovementParams.OnStop`。

## 朝向规则

- `Velocity` 表示“怎么移动”。
- `MovementFacingDirection` 表示“朝哪看”。
- 策略如需让视觉朝向不同于移动方向，返回带 facing direction 的 `MovementUpdateResult`。
- `EntityMovementComponent` 只发布朝向意图，不直接写 `RotationDegrees` 或 `FlipH`。
- 最终输出由 `EntityOrientationComponent` 处理：
  - `RootRotation` 写 root `RotationDegrees`。
  - `VisualFlipX` 写 `VisualRoot` 下 `AnimatedSprite2D.FlipH`。
- 对外角度参数统一使用“度”；Godot 2D 语义为 `0=右、90=下、180=左、正值顺时针`。

## 移动碰撞规则

- `MovementParams.CollisionParams == null` 表示不启用移动碰撞语义。
- 原始候选来自 `CollisionComponent` 或 `CharacterBody2D.MoveAndSlide()`。
- `MovementCollisionPolicy` 负责阵营过滤、实体类型过滤、目标匹配、去重、计数。
- `MovementCollision` 表示“有效碰撞通知”，不等价于“运动结束”。
- 只有达到 `StopAfterCollisionCount` 阈值时才发 `MovementStopRequested`。
- `DestroyOnStop` 只在触发碰撞停止时生效。
- 命中逻辑优先写在 `MovementCollisionParams.OnCollision` 或 `MovementParams.OnStop`，事件监听只用于旁路观察或调试。

## 新增 / 修改策略流程

1. 确认是否已有策略能表达该运动。
2. 修改 `MoveMode` 时同步更新 `MovementEnums.cs`、本契约、项目索引和相关 Skill。
3. 新策略实现 `IMovementStrategy`。
4. 用 `[ModuleInitializer]` 注册到 `MovementStrategyRegistry`。
5. 输入参数放 `MovementParams` 的 `init` 字段；策略运行态放策略私有字段。
6. 每帧只写 `DataKey.Velocity` 并返回 `MovementUpdateResult`。
7. 如果需要连续标量演化，优先复用 `ScalarDriver`。
8. 补充或运行 Movement 场景测试。

## ScalarDriver 规则

`ScalarDriver` 是 Movement 内部的通用标量驱动器（`Src/ECS/Base/System/Movement/ScalarDriver/`），用于让单个数值随时间推进。源码见 `ScalarMotion.cs`。

- 适用场景：`OrbitRadius` 随时间的扩张/收缩/反弹，`WaveAmplitude` 动态变大或衰减，`WaveFrequency` 按边界模式往返或冻结。
- ScalarDriver 只推进标量值、速度、边界响应和完成状态；不直接移动实体，不写 `Data`。
- 具体策略决定该标量如何参与轨迹公式。
- 当前接入：`OrbitStrategy.OrbitRadiusScalarDriver`、`SineWaveStrategy.WaveAmplitudeScalarDriver`、`SineWaveStrategy.WaveFrequencyScalarDriver`。

使用步骤：
1. `MovementParams` 中保留基础值字段，可选增加 `ScalarDriverParams?` 字段。
2. 策略 `OnEnter` 创建 `ScalarDriverState`。
3. 策略 `Update` 调用 `ScalarDriver.Step()`。
4. 日志上下文传入 `MoveMode` 和参数名。

新增策略若只需驱动一个连续标量，优先复用 `ScalarDriver`，不要在策略里重复实现边界响应。

## 禁止事项

- 禁止策略直接改 `GlobalPosition`。
- 禁止策略承担伤害结算、索敌、动画播放。
- 禁止把 `-1` 无限语义改成 `0`。
- 禁止对外使用弧度参数。
- 禁止把临时运动参数拆散写入多个 DataKey。
- 禁止在 `_Process` 中新增对象或 LINQ。

## 推荐测试

```bash
dotnet build
# .claude
./.claude/skills/GodotSkill/scripts/run-test.sh --build res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn
./.claude/skills/GodotSkill/scripts/run-test.sh --build res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn
# 或
node .claude/skills/GodotSkill/scripts/godot-scene-runner.mjs list --filter Movement
```
