# Collision Capability

> 状态：current
> 定位：碰撞 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Collision概念.md](System/Collision概念.md) | 碰撞桥接层、业务解释层和对象池碰撞兼容边界 |
| [System/Collision使用说明.md](System/Collision使用说明.md) | CollisionComponent、Hurtbox、ContactDamage、Pickup 使用说明 |
| [Component/CollisionComponent/CollisionComponent.md](Component/CollisionComponent/CollisionComponent.md) | Area2D 视觉体碰撞桥接组件 |
| [Component/ContactDamageComponent/ContactDamageComponent.md](Component/ContactDamageComponent/ContactDamageComponent.md) | 接触伤害组件 |
| [Component/PickupComponent/PickupComponent.md](Component/PickupComponent/PickupComponent.md) | 拾取组件 |

## 源码

```text
Src/ECS/Capabilities/Collision/
├── Component/
├── Events/
└── System/  # 当前文档分层；运行时代码主要在 Component
```

Collision EventType 已迁入 `Src/ECS/Capabilities/Collision/Events/`；`CollisionLayers` 当前仍在 `Data/DataKey/Base/CollisionLayers.cs`，后续是否迁入 Capability 需等待 DataOS generator 决策。
