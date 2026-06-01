<!-- migrated-from: Src/ECS/Base/Component/Movement/EntityMovementComponent说明.md -->

> 迁移来源：`Src/ECS/Base/Component/Movement/EntityMovementComponent说明.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# EntityMovementComponent 说明

## 1. 组件定位

运动系统的调度器，不决定“为什么移动”，只负责把当前运动策略稳定跑起来，并把移动碰撞、停止事件、生命周期收口统一在组件内编排。

- 业务层构建 `MovementParams` 并通过事件传入
- 策略负责计算本帧位移意图，写入 `GeneratedDataKey.Velocity`
- 组件持有 `MovementParams`、统计字段和碰撞策略状态，执行位移、检查结束、发事件
- 组件会把最终解析出的朝向意图写入 `GeneratedDataKey.MovementFacingDirection`，供 `EntityOrientationComponent` 这类朝向输出层消费
- 适用于 `Node2D + IEntity`、`Area2D + IEntity`、`CharacterBody2D + IEntity`

## 2. 私有状态

```csharp
private MovementParams _params;              // 本次运动输入参数（由事件传入）
private bool _moveCompleted;                 // 停止标志（防止重复收口）
private Vector2 _facingDirection;            // 当前帧显式朝向意图
private IMovementStrategy? _currentStrategy; // 当前策略实例（每次切换新建）
private readonly MovementCollisionPolicy _collisionPolicy = new(); // 碰撞过滤/计数状态
```

说明：

- `_params.ElapsedTime / _params.TraveledDistance` 由组件每帧累加
- `_collisionPolicy` 不是全局系统，只是“本实体这次运动”的局部状态容器
- 旧 `_hasCollided` 语义已移除，不再用“本次运动只碰一次”这种粗粒度锁

## 3. 执行路径

```text
_Process / _PhysicsProcess
  -> RunMovementLogic()
     -> 策略 Update(...) 写 GeneratedDataKey.Velocity
     -> 策略可选返回 FacingDirection
     -> 若策略主动完成 -> StopMovement(Completed)
     -> AccumulateTravel()
     -> CheckEndConditions()
  -> ApplyMovement()
     -> VelocityResolver.Resolve()
     -> 写 GeneratedDataKey.MovementFacingDirection
     -> CharacterBody2D: MoveAndSlide() + slide collision 采样
     -> 其他 Node2D/Area2D: GlobalPosition += velocity * delta
```

帧率路径由策略 `UsePhysicsProcess` 决定，与节点类型无关。

## 4. 参数传递方式

所有运动参数通过 `MovementParams` 一次性传入，不再分散写 generated handle：

```csharp
entity.Events.Emit(
    GameEventType.Unit.MovementStarted,
    new GameEventType.Unit.MovementStartedEventData(MoveMode.TargetPoint, new MovementParams
    {
        Mode = MoveMode.TargetPoint,
        TargetPoint = new Vector2(900, 360),
        MaxDuration = -1f, // 可选，-1 不限制
        DestroyOnComplete = false, // 可选
    }));
```

`MovementParams` 是 `record struct`，输入字段均为 `init`，策略只能只读访问。

### `Orientation`

- `MovementParams.Orientation` 用于把本次运动附带的朝向控制参数交给 `EntityOrientationComponent`
- `EntityMovementComponent` 不再直接写 `root RotationDegrees` 或 `VisualRoot.FlipH`
- `EntityOrientationComponent` 是唯一最终朝向输出层，会消费 `GeneratedDataKey.MovementFacingDirection`
- `EntityOrientationComponent.Sink = RootRotation`：适用于投射物/特效等整体旋转实体
- `EntityOrientationComponent.Sink = VisualFlipX`：适用于角色类单位，兼容现有 `VisualRoot.FlipH` 语义
- 默认情况下朝向组件会持续跟随 `MovementFacingDirection` 输出；`RotateToVelocity = false` 现在表示“当前 movement 临时冻结朝向输出并保持现有朝向”，而不是彻底关闭朝向系统

## 5. DefaultMoveMode 与临时模式

实体初始化阶段必须由 snapshot record 提前写入 `GeneratedDataKey.DefaultMoveMode`，组件注册后再进入该模式。

临时运动结束后：

- 若停止解析结果给出 `NextMode`，则切回对应模式
- 若未销毁且 `NextMode == MoveMode.None`，组件会把 `GeneratedDataKey.MoveMode` 置为 `None`
- 默认场景下 `MovementStopCoordinator` 会在“未销毁且当前模式不是默认模式”时自动回退 `DefaultMoveMode`

### 打断规则

- 当前是默认模式：允许直接切换
- 当前是临时模式且 `CanBeInterrupted = false`：拒绝新的 `MovementStarted`

## 6. SwitchStrategy 重置

每次切换会：

1. 对旧策略发送一次 `OnStop(Interrupted)`
2. 重置 `Velocity / VelocityOverride / VelocityImpulse`
3. 清空 `_moveCompleted / _facingDirection`
4. 用新 `MovementParams` 重置 `_collisionPolicy`
5. 新建策略实例并调用 `OnEnter`

## 7. 统一停止流程

停止入口统一为 `StopMovement(...)`，来源包括：

- 策略主动完成
- 时间/距离达到阈值
- 碰撞策略命中自动停止阈值后发出的 `MovementStopRequested`
- 外部系统主动发送 `MovementStopRequested`

### `MovementStopRequested`

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

### `StopMovement` 内部顺序

```text
MovementStopCoordinator.Resolve(...)
  -> _moveCompleted = true
  -> StopCurrentStrategy(reason, resolution.NextMode, collisionTarget)
  -> 可选发 MovementCompleted(Mode / ElapsedTime / TraveledDistance / Reason / CollisionTarget)
  -> 可选销毁实体
  -> 否则按 resolution.NextMode 切换，或把 MoveMode 置为 None
```

## 8. 移动碰撞处理

### 8.1 触发路径

| 实体类型 | 原始候选来源 | 说明 |
|----------|--------------|------|
| `Area2D` | `CollisionComponent -> CollisionEntered` | 只有实体根节点是 `Area2D` 才走这条 |
| `CharacterBody2D` | `MoveAndSlide()` 后 `GetSlideCollision(i)` | Godot 标准做法，不是“人工补事件” |

### 8.2 为什么 `CharacterBody2D` 还要手动采样

因为 `CharacterBody2D` 没有 `Area2D` 那组 entered/exited 信号。`MoveAndSlide()` 给的是“这一帧撞到了谁”，移动组件只能把它当作原始碰撞候选再交给策略层过滤。

问题从来不在“要不要采样 slide collision”，而在旧实现把“采样到碰撞”直接等同于“停止并销毁”。这次重构改掉的是后半段硬编码，不是删掉采样入口。

### 8.3 新碰撞链路

```text
原始碰撞候选
  -> TryHandleRawCollision(target)
  -> MovementCollisionPolicy.TryAccept(...)
  -> OnCollision 回调（可选）
  -> MovementCollision 事件（可选）
  -> 若达到 StopAfterCollisionCount
       -> 发 MovementStopRequested(Reason=Collision, DestroyEntity=DestroyOnStop)
```

### 8.4 `MovementCollisionPolicy`

负责：

- 阵营过滤：`TeamFilter`
- 实体类型过滤：`EntityTypeFilter`
- 目标匹配：`Any / TrackedTargetOnly / SpecificNode`
- 同一次运动内去重计数
- 判断本次是否会停止，并生成 `MovementCollisionContext`

### 8.5 `MovementCollisionEventData`

有效碰撞事件现在包含：

- `Mode`
- `Target`
- `TargetEntity`
- `CollisionCount`
- `WillStop`

所以技能层不需要再自己从 `evt.Target` 手动回溯宿主实体，除非有特殊需求。

## 9. ArcShot 语义

`ArcShot` 不再依赖 `MovementCollision`。

正确写法：

- `MoveMode.CircularArc`
- `isTrackTarget = true`
- `ReachDistance > 0`
- `OnStop` 中判断 `Reason == Completed`

这样它只会在“自然追到锁定目标”时结算，而不会被沿路碰到的任意敌人/玩家打断。

## 10. BezierCurve 语义

`BezierCurve` 现在有两套输入方式：

- `BezierPoints`
- `BezierTemplate`

推荐：

- 静态曲线演出或已有世界坐标控制点：继续传 `BezierPoints`
- 追踪目标、随机多发样式、想复用统一选点方案：优先传 `BezierTemplate`

追踪模式的关键变化：

- 旧逻辑：只更新终点，中间控制点世界坐标锁死
- 新逻辑：如果存在模板，则按“当前实体位置 → 当前目标位置”重建剩余曲线

这样可以保证：

- 已经飞出来的历史段不会被整体重写
- 剩余段会继续保持原本的样式关系，并随剩余时长逐步收束
- 长距离目标不会再把横向偏移按距离无限放大
- 3~5 阶随机模板在追踪目标时不会中段抽搐或被硬拽变形

## 11. `MovementStopReason` / `MovementStopContext`

当前停止原因：

- `Completed`
- `Collision`
- `Requested`
- `Interrupted`
- `ComponentUnregistered`

`MovementStopContext` 常用字段：

- `Reason`
- `Params`
- `CollisionTarget`
- `NextMode`
- `IsCompleted / IsCollision / IsRequested / IsInterrupted`

注意：`IsCompleted` 仍只对 `Completed / Collision` 为 `true`。若业务要精确分支，应直接判断 `Reason`。

## 12. Velocity 分层合成

```text
IsMovementLocked = true  → Zero
VelocityOverride ≠ Zero  → VelocityOverride
否则                      → Velocity + VelocityImpulse（用后清零）
```

## 13. 测试场景

- `Src/ECS/Capabilities/Movement/Tests/MovementComponentTestScene.tscn`
- `Src/ECS/Capabilities/Movement/Tests/MovementCollisionRuntimeTest.tscn`

后者专门锁定移动碰撞协议、停止请求默认值、停止协调器行为和 `TrackedTargetOnly` 过滤语义。
