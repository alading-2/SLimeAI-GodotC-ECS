# Projectile Capability

> 状态：current
> 定位：投射物 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [../Effect/System/Concept.md](../Effect/System/Concept.md) | Projectile / Effect 当前共享命中、归属和视觉生命周期背景 |

## 源码

```text
Src/ECS/Capabilities/Projectile/
├── System/
└── Entity/
```

Projectile 目前通过 `ProjectileTool`、`ProjectileOwnershipService` 和 `ProjectileEntity` 收口生成、owner projection 与生命周期；命中处理依赖 Movement / Collision / Damage owner。
