# Collision 概念

> status: current
> sourcePaths: Src/ECS/Base/Component/Collision/
> relatedDocs: ./Collision使用说明.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

碰撞系统拆成两层：碰撞桥接层（Godot 物理接触 → ECS 事件）和业务解释层（移动、接触伤害、拾取、技能命中分别消费事件）。

## 2. 核心概念

### 两层架构

**碰撞桥接层**：只负责把 Godot 物理接触转换成 ECS 事件。
- `CollisionComponent`：Entity 根节点为 `Area2D` 时的视觉体碰撞桥接
- `HurtboxComponent`：受击区碰撞（自身就是 `Area2D`）

**业务解释层**：各系统按自己的语义消费事件。
- `EntityMovementComponent`：消费碰撞事件决定是否停止
- `ContactDamageComponent`：消费 HurtboxEntered/Exited 造成接触伤害
- `PickupComponent`：消费碰撞事件实现拾取

### 三种碰撞路径

| 路径 | 候选来源 | 说明 |
|------|----------|------|
| Area2D 视觉体 | `CollisionComponent → CollisionEntered` | 进入一次收到一次候选 |
| 受击区 | `HurtboxComponent → HurtboxEntered/Exited` | 专用事件链路 |
| CharacterBody2D 移动 | `MoveAndSlide()` 后 slide collision | 每帧可能多个候选 |

### 碰撞层级

> **Layer = 我是谁，Mask = 我关心谁**

```text
Layer 1 (值=1)    → Terrain
Layer 2 (值=2)    → Player
Layer 3 (值=4)    → Enemy
Layer 4 (值=8)    → PlayerHurtbox
Layer 5 (值=16)   → PlayerPickup
Layer 6 (值=32)   → Projectile
Layer 7 (值=64)   → EnemyHurtbox
Layer 8 (值=128)  → WeaponHitbox
Layer 9 (值=256)  → SelectionPickable
```

所有常量统一定义在 `Data/DataKey/Base/CollisionLayers.cs`。

### 对象池碰撞兼容

**核心问题**：`CollisionShape2D.disabled` 的切换让 Godot 宽相先销毁再重建碰撞对，重建时可能在旧位置触发 ENTER（幽灵碰撞）。

**解决方案**：泊车位 + 脱树 + 挂树后同步禁用 + Activate 后延迟 MoveAndSlide。

## 3. 职责边界

| 碰撞系统做 | 碰撞系统不做 |
| ---- | ---- |
| 桥接 Godot 物理事件到 ECS | 决定"命中后要不要停/销毁"（归移动系统） |
| 提供碰撞层级定义 | `EntityMovementComponent` 采样全局物理 |
| 对象池碰撞隔离 | 业务伤害计算（归 DamageSystem） |

## 4. 依赖关系

- **CollisionComponent**：Area2D 视觉体碰撞桥接
- **HurtboxComponent**：受击区碰撞桥接（Area2D）
- **ContactDamageComponent**：接触伤害（消费 HurtboxEntered/Exited）
- **EntityMovementComponent**：移动碰撞（消费 CollisionEntered 或 slide collision）
- **MovementCollisionPolicy**：有效碰撞判定
- **ObjectPool**：碰撞型实体的脱树/挂树时序

具体组件迁移全文见 [CollisionComponent.md](../Component/Collision/CollisionComponent/CollisionComponent.md)、[ContactDamageComponent.md](../Component/Collision/ContactDamageComponent/ContactDamageComponent.md)、[PickupComponent.md](../Component/Collision/PickupComponent/PickupComponent.md)。

## 5. 历史备注

- 核心教训：Godot 4 中 `Node` 节点阻断 Transform 继承，导致子节点锁定在 (0,0)
- 碰撞容器节点链不可夹杂 `Node`，必须全部是 `Node2D`
- 对象池不应越权接管物理，业务状态必须闭环封装
