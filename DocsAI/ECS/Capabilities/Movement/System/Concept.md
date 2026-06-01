# Movement 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/Movement/System/
> relatedDocs: DocsAI/ECS/Capabilities/Movement/System/Usage.md, DocsAI/ECS/Capabilities/Movement/Component/EntityMovementComponent说明.md
> lastReviewed: 2026-06-01

## 1. 一句话定位

Movement System 采用策略模式，EntityMovementComponent 作为调度器，通过 IMovementStrategy 接口支持多种运动模式。

## 2. 核心概念

### 策略模式

```
EntityMovementComponent（调度器）
  ├─ FixedDirection（固定方向）
  ├─ TargetPoint（目标点）
  ├─ TargetEntity（目标实体）
  ├─ OrbitPoint（绕点旋转）
  ├─ Wave（波浪运动）
  └─ ... 更多策略
```

### MovementParams 事件驱动

运动参数通过事件驱动 API 设置，不直接修改组件状态：

```csharp
_entity.Events.Emit(GameEventType.Unit.MovementParams, new MovementParamsEventData(...));
```

### ScalarDriver

通用标量参数驱动器，处理运动策略中的数值参数：
- 初始值覆盖
- 速度/加速度
- 最小/最大边界
- 边界响应

### 移动碰撞

移动碰撞不等于"碰到就停"。通过 `MovementCollisionPolicy` 决定哪些碰撞算有效：
- TeamFilter（阵营过滤）
- EntityTypeFilter（实体类型过滤）
- StopAfterCollisionCount（累积有效碰撞后停止）
- DestroyOnStop（停止后是否销毁）

## 3. 职责边界

| Movement 做 | Movement 不做 |
| ---- | ---- |
| 运动策略注册与执行 | 碰撞检测（归 CollisionSystem） |
| 移动碰撞策略 | 伤害计算（归 DamageSystem） |
| 运动参数管理 | 物理引擎调用（归 Godot） |

## 4. 依赖关系

- **EntityMovementComponent**：移动调度器
- **IMovementStrategy**：运动策略接口
- **MovementCollisionPolicy**：碰撞策略
- **ScalarDriver**：标量参数驱动
- **TimerManager**：运动计时

移动调度器原文迁移见 [EntityMovementComponent说明.md](../Component/EntityMovementComponent说明.md)。
