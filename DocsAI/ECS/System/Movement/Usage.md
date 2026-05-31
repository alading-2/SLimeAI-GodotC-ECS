<!-- migrated-from: Src/ECS/Base/System/Movement/README.md -->

> 迁移来源：`Src/ECS/Base/System/Movement/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# 运动系统 (Movement System)

## 核心定位

让实体按照指定轨迹稳定移动。策略只写 `GeneratedDataKey.Velocity`，调度器统一执行位移、停止、碰撞筛选与生命周期收口。

## 调用方式

所有运动参数通过 `MovementParams` 一次性传入事件，不再分散写 generated handle：

```csharp
entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint, new MovementParams
    {
        Mode = MoveMode.TargetPoint,
        TargetPoint = new Vector2(900, 360),
        MaxDistance = 300f, // 可选，-1 不限制
        DestroyOnComplete = true, // 可选
    }));
```

## MoveMode 与策略速查

| MoveMode | 策略类 | 典型用途 | 关键 MovementParams 字段 |
|----------|--------|----------|--------------------------|
| `FixedDirection` | FixedDirectionStrategy | 直线飞行 | `ActionSpeed` / `MaxDistance`（先写 `GeneratedDataKey.Velocity`） |
| `TargetPoint` | TargetPointStrategy | 冲向指定坐标 | `TargetPoint`, `ReachDistance` |
| `TargetEntity` | TargetEntityStrategy | 追踪实体 | `GeneratedDataKey.TargetNode`, `ReachDistance` |
| `OrbitPoint` | OrbitPointStrategy | 围绕固定点环绕 | `OrbitCenter`, `OrbitRadius`, `OrbitAngularSpeed` |
| `OrbitEntity` | OrbitEntityStrategy | 围绕目标实体 | `GeneratedDataKey.TargetNode`, `OrbitRadius`, `OrbitAngularSpeed` |
| `Spiral` | SpiralStrategy | 螺旋收缩/扩张 | `OrbitCenter`, `OrbitRadius`, `OrbitTargetRadius`, `OrbitAngularSpeed` |
| `SineWave` | SineWaveStrategy | 正弦波弹道 | `WaveAmplitude` / `WaveFrequency` + 可选 `Wave*ScalarDriver` |
| `BezierCurve` | BezierCurveStrategy | 曲线弹道 | `BezierPoints`, `ActionSpeed` 或 `MaxDuration` |
| `Boomerang` | BoomerangStrategy | 双半椭圆回旋弹道 | `TargetPoint`, `GeneratedDataKey.TargetNode`, `ActionSpeed` 或 `MaxDuration`, `Boomerang*`, 可选 `Orientation` |
| `Parabola` | ParabolaStrategy | 抛物线弹道 / 跳跃位移 | `TargetPoint` 或 `GeneratedDataKey.TargetNode`, `ParabolaApexHeight`, `ActionSpeed`, `ReachDistance` |
| `CircularArc` | CircularArcStrategy | 单段圆弧弹道 / 侧切轨迹 | `TargetPoint` 或 `GeneratedDataKey.TargetNode`, `CircularArcRadius`, `CircularArcClockwise`, `ActionSpeed`, `ReachDistance` |
| `AttachToHost` | AttachToHostStrategy | 附着特效 | `GeneratedDataKey.TargetNode`（+ `GeneratedDataKey.EffectOffset`） |
| `PlayerInput` | PlayerInputStrategy | 玩家常驻（DefaultMoveMode） | 无，读 `GeneratedDataKey.MoveSpeed/Acceleration` |
| `AIControlled` | AIControlledStrategy | AI 常驻（DefaultMoveMode） | 无，读 `GeneratedDataKey.AIMoveDirection` 等 |

## 职责分工

- **业务层**：构建 `MovementParams`，触发 `MovementStarted`；命中逻辑优先写在 `MovementCollisionParams.OnCollision` / `MovementParams.OnStop`，只有旁路观察或调试时再监听 `MovementCollision` / `MovementCompleted`
- **策略**：通过 `in MovementParams` 只读本次运动上下文，计算本帧意图写入 `GeneratedDataKey.Velocity`，需要时通过 `MovementUpdateResult` 显式返回 `FacingDirection`
- **组件**：`EntityMovementComponent` 持有 `_params` / `_elapsedTime` / `_traveledDistance`，切换策略，执行位移，消费碰撞候选，统一停止流程；`EntityOrientationComponent` 作为唯一朝向输出层，消费 `MovementFacingDirection` 并按 sink 输出到 `RootRotation` 或 `VisualFlipX`
- **碰撞策略子模块**：`MovementCollisionPolicy` 负责过滤、去重、计数、生成 `MovementCollisionContext`
- **停止协调子模块**：`MovementStopCoordinator` 统一决定是否发完成事件、是否销毁、切到哪个模式

## 结束条件

- `MaxDuration >= 0`：时间限制（`-1` = 不限制）
- `MaxDistance >= 0`：距离限制（`-1` = 不限制）
- 策略返回 `MovementUpdateResult.Complete()`：主动完成
- `DestroyOnComplete = true`：自然完成后销毁；否则按默认逻辑回退 `DefaultMoveMode`
- 外部或内部都可以发送 `MovementStopRequested` 停止当前运动，并用 `EmitCompletedEvent / DestroyEntity / NextMode` 控制收口行为

## 生命周期

- `OnEnter`：策略进入时初始化运行时缓存
- `Update`：每帧计算运动意图
- `OnStop`：统一停止回调，`MovementStopContext` 会携带 `Reason / Params / CollisionTarget / NextMode`
- `MovementStopReason`：当前内置 `Completed / Collision / Requested / Interrupted / ComponentUnregistered`

`MovementCompletedEventData` 直接携带 `ElapsedTime / TraveledDistance / Reason / CollisionTarget`，无需读 generated handle。

## 朝向控制分层

- `Velocity` = “本帧怎么移动”，服务于位移执行与速度分层合成
- `FacingDirection` = “本帧朝哪看”，由策略显式返回，或退回到本帧移动意图
- `GeneratedDataKey.MovementFacingDirection` = `EntityMovementComponent` 对外发布的最终朝向意图，供 `EntityOrientationComponent` 等输出层读取
- `EntityMovementComponent` 不再直接写 `RotationDegrees/FlipH`
- `EntityOrientationComponent.Sink = RootRotation`：最终写 root `RotationDegrees`
- `EntityOrientationComponent.Sink = VisualFlipX`：最终写 `VisualRoot.FlipH`
- `MovementParams.Orientation`：可把当前运动附带的自转/朝向模式一并交给 `EntityOrientationComponent`

## 移动碰撞语义（2026-04 重构）

### 1. 分层语义

移动层现在把碰撞拆成 4 段，而不是“碰到就结束”：

1. 原始碰撞候选：来自 `CollisionComponent` 或 `CharacterBody2D.MoveAndSlide()`
2. `MovementCollisionPolicy` 过滤：判断这次碰撞是否有效
3. 碰撞通知：执行 `MovementCollisionParams.OnCollision`，可选发 `MovementCollision`
4. 停止收口：只有达到 `StopAfterCollisionCount` 阈值时，才发 `MovementStopRequested`

因此 `MovementCollision` 现在表示“有效碰撞通知”，不再等价于“运动已经结束”。

### 2. `MovementParams.Collision`

旧字段 `DestroyOnCollision` 已废弃，改为：

```csharp
Collision = new MovementCollisionParams
{
    TeamFilter = TeamFilter.Enemy, // 阵营过滤
    EntityTypeFilter = EntityType.Unit, // 实体类型过滤
    TargetMatchMode = MovementCollisionTargetMatchMode.Any, // 目标匹配
    StopAfterCollisionCount = 2, // 第 2 个有效碰撞后停止，-1 = 只通知不停止
    DestroyOnStop = true, // 停止后销毁
    EmitCollisionEvent = true, // 是否发 MovementCollision
    OnCollision = ctx => { } // 本地碰撞回调
};
```

### 3. `Area2D` 与 `CharacterBody2D` 分工

- `Area2D` 路径：由 `CollisionComponent` 统一桥接 `CollisionEntered`
- `CharacterBody2D` 路径：仍需在 `MoveAndSlide()` 之后读取 `GetSlideCollisionCount() / GetSlideCollision(i)`

这不是补丁式做法，而是 Godot 物理模型本身的差异。`CharacterBody2D` 没有 `Area2D` 那组 entered/exited 事件，移动组件只能消费 slide collision 作为原始候选。

### 4. 去重与计数

- 同一目标在同一次运动内只计数一次
- 优先用宿主实体实例 ID 去重；没有宿主实体时回退到碰撞节点实例 ID
- 因此可以正确支持“穿透 2 个敌人后结束”“只对锁定目标计数”“只通知不停止”

### 5. 停止请求事件

统一停止入口为：

```csharp
entity.Events.Emit(
    GameEventType.Unit.MovementStopRequested,
    new GameEventType.Unit.MovementStopRequestedEventData
    {
        Reason = MovementStopReason.Requested,
        EmitCompletedEvent = false,
        NextMode = MoveMode.None,
        DestroyEntity = false
    });
```

语义说明：

- `EmitCompletedEvent = false`：只停当前运动，不发 `MovementCompleted`
- `NextMode = MoveMode.None`：表示“沿用默认回退逻辑”，不是“强制停到 None”
- 若确实要强制切到某模式，显式填写 `NextMode`

## 典型用法

### 1. 命中即停的直线子弹

```csharp
projectile.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(
        MoveMode.FixedDirection,
        new MovementParams
        {
            MaxDistance = 800f,
            DestroyOnComplete = true,
            Collision = new MovementCollisionParams
            {
                TeamFilter = TeamFilter.Enemy,
                EntityTypeFilter = EntityType.Unit,
                StopAfterCollisionCount = 1,
                DestroyOnStop = true,
                OnCollision = ctx =>
                {
                    if (ctx.TargetEntity == null) return;
                    // 伤害逻辑
                }
            }
        }));
```

### 2. ArcShot 追踪特定目标

`ArcShot` 不再监听 `MovementCollision`。正确写法是：

- `CircularArc + isTrackTarget + ReachDistance`
- 不配置 `Collision`
- 在 `MovementParams.OnStop` 中只对 `Reason == Completed` 结算伤害

也就是说，ArcShot 的命中语义是“自然到达锁定目标后完成”，不是“物理碰到任意单位就结束”。

## Velocity 分层合成

```text
IsMovementLocked = true  → Zero
VelocityOverride ≠ Zero  → VelocityOverride（击退/硬控）
否则                      → Velocity + VelocityImpulse（单帧冲量，用后清零）
```

## 扩展新策略

1. `MovementEnums.cs` 新增 `MoveMode`
2. `MovementParams` 新增所需 `init` 字段；若是策略内部连续变化的标量，优先复用 `ScalarDriverParams`
3. 新建策略类，私有字段存运行时状态，`[ModuleInitializer]` 注册工厂；`OnEnter / Update` 统一使用 `in MovementParams` 只读接收大参数结构
4. 若视觉朝向不应直接取 `Velocity`，通过 `MovementUpdateResult.Continue(distance, facingDirection)` 显式返回朝向
5. 补全策略类头注释

## 测试

- `Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn`
- `Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn`

第二个场景专门锁定移动碰撞协议默认值、过滤/计数、`TrackedTargetOnly`、`MovementStopRequested` 默认语义与 `MovementStopCoordinator` 行为。

## 阅读顺序

1. `EntityMovementComponent说明.md`：调度器流程与停止收口
2. `ScalarDriver/README.md`：通用标量驱动职责
3. 对应策略类头注释：各模式参数语义
4. `VelocityResolver.cs`：速度是否会被上层覆盖
5. `DocsAI/ECS/System/Movement/Usage.md`：完整设计说明
