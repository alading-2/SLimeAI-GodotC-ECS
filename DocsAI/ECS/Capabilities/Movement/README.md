# Movement Capability

> 状态：current
> 定位：移动 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | Movement 使用说明和策略流程 |
| [System/Concept.md](System/Concept.md) | Movement 概念和边界 |
| [System/ScalarDriver.md](System/ScalarDriver.md) | 标量参数驱动 |
| [System/Strategies.md](System/Strategies.md) | 运动策略扩展说明 |
| [System/数学与物理概念详解.md](System/数学与物理概念详解.md) | 运动数学背景 |
| [Component/EntityMovementComponent说明.md](Component/EntityMovementComponent说明.md) | Godot 移动调度组件说明 |

## 源码

```text
Src/ECS/Capabilities/Movement/
├── System/
├── Component/
└── Tests/
```

Movement DataKey 当前仍由 `Data/DataKey/Component/Movement/` 和 generated handle 管理；generated 输出路径迁移需等待 DataOS generator 决策。

## Log

Movement owner 使用 `owner=Movement`。当前第一批 hard cutover 覆盖 Movement 验证场景的 `ValidationSession`，运动热路径暂不逐帧接入 `OperationTrace`。

建议 operation 命名：

| operation | phase | 关键字段 |
| --- | --- | --- |
| `MovementStart` | `Movement` | `entityId`、`strategy`、`maxDistance`、`maxDuration` |
| `MovementCollisionResolve` | `Collision` | `entityId`、`targetEntityId`、`accepted`、`reasonCode` |
| `MovementStop` | `Movement` | `entityId`、`stopReason`、`distanceTravelled` |

规则：

- 运动每帧位置更新不写默认日志；只在 start/stop/collision decision 和 diagnostics 场景写 summary。
- 碰撞隔离相关事实优先进入 Validation artifact，避免把 raw callback 当业务命中。
- 查询 Movement 问题时先按 owner 和 operation 筛选，再结合 TargetSelector/ObjectPool/Damage owner flow。

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Movement
```
