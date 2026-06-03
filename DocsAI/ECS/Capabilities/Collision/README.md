# Collision Capability

> 状态：current
> 定位：碰撞 owner 文档入口。
> 更新：2026-06-02

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Collision概念.md](System/Collision概念.md) | 碰撞桥接层、业务解释层、事件过滤和 ObjectPool 协作边界 |
| [System/Collision使用说明.md](System/Collision使用说明.md) | CollisionComponent、Hurtbox、ContactDamage、Pickup 使用说明 |
| [Concepts/README.md](Concepts/README.md) | 历史问题、对象池兼容、碰撞层和场景结构概念索引 |
| [Concepts/对象池碰撞兼容说明.md](Concepts/对象池碰撞兼容说明.md) | Godot 物理时序、幽灵碰撞和脱树隔离详细说明 |
| [Concepts/碰撞层级说明.md](Concepts/碰撞层级说明.md) | Layer / Mask 约定 |
| [Component/CollisionComponent/CollisionComponent.md](Component/CollisionComponent/CollisionComponent.md) | Area2D 视觉体碰撞桥接组件 |
| [Component/ContactDamageComponent/ContactDamageComponent.md](Component/ContactDamageComponent/ContactDamageComponent.md) | 接触伤害组件 |
| [Component/PickupComponent/PickupComponent.md](Component/PickupComponent/PickupComponent.md) | 拾取组件 |

## 当前边界

- Collision 负责 `Area2D` 信号桥接、Hurtbox / Pickup / ContactDamage 组件约定、layer/mask 说明和业务事件过滤。
- Collision 不负责对象复用、池容量、Node 创建或 Entity 初始化。
- ObjectPool 负责物理根节点池化隔离；根节点是 `CollisionObject2D` 时继续默认脱树。
- Entity Runtime 负责 `Get(false)` 后的 Data / Visual / Transform / Component / Registry 初始化，并在完成后调用 `pool.Activate()`。
- Damage / Movement 负责解释有效碰撞、伤害结算、停止/销毁/穿透等业务语义。

## 源码

```text
Src/ECS/Capabilities/Collision/
├── Component/
├── Events/
└── System/  # 当前文档分层；运行时代码主要在 Component
```

Collision EventType 已迁入 `Src/ECS/Capabilities/Collision/Events/`；`CollisionLayers` 当前仍在 `Data/DataKey/Base/CollisionLayers.cs`，后续是否迁入 Capability 需等待 DataOS generator 决策。
