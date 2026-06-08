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

伤害型投射物的常用语义：

- 沿途命中：通过 `MovementCollisionParams.OnCollision` 接收有效碰撞，再调用 `AbilityImpactTool` / `DamageTool` 结算伤害。
- 锁定目标兜底：如果技能已经通过 `TargetQueryEngine.QueryEntities(...)` 锁定目标，但弹道自然完成前没有触发任何有效碰撞，可以在 `MovementParams.OnStop` 且 `Reason=Completed` 时对锁定目标结算一次。锁定目标只用于瞄准和无碰撞兜底，不得限制弹道距离、生命周期或 `StopAfterCollisionCount=-1` 的多目标命中语义。该兜底只用于避免“技能释放成功但完全无伤害”，不替代沿途碰撞命中。
- 执行结果：`AbilityExecutedResult.TargetsHit` 在异步投射物 Handler 中通常只是生成数量或同步结果，不代表后续碰撞伤害上限。
- 无目标降级：只有查不到敌方目标时，才使用随机点、默认方向或打空逻辑。
