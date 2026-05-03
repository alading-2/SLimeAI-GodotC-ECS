# ContactDamageComponent 源码入口

“实体碰到敌人时自身持续受到接触伤害”的逻辑组件。AI 执行契约见 `DocsAI/Modules/Collision.md`。

## 职责边界

只消费 `HurtboxEntered/HurtboxExited` 事件（由 `HurtboxComponent` 转发），对敌对实体通过 `DamageService` 结算持续接触伤害。不直接监听 Godot 碰撞信号，不做空间查询。

## 标准链路

```
HurtboxComponent → HurtboxEntered/HurtboxExited → ContactDamageComponent → DamageService
```

前置条件：实体挂有 `HurtboxComponent` 且配置了有效 `CollisionShape2D`。

## 工作流程概要

- **进入**：收到 `HurtboxEntered` → 判断敌对阵营 → 立即结算一次伤害 → 为每个目标维护独立 `TimerManager` 循环计时器
- **持续**：计时器按 `AttackInterval` 触发 → 校验自身和目标有效性 → 通过 `DamageService` 结算
- **离开**：收到 `HurtboxExited` → 移除目标计时器
- **死亡/复活**：死亡时暂停计时器，复活后若仍重叠则恢复

伤害值读取攻击者 `DataKey.FinalAttack`，伤害间隔读取 `DataKey.AttackInterval`。受害者为当前挂有本组件的实体，攻击者为碰到的敌对实体。

## 应用方式

在实体 `.tscn` 添加 `HurtboxComponent`（下挂 `CollisionShape2D`）+ `ContactDamageComponent`。

完整规则、禁止事项见 `DocsAI/Modules/Collision.md`。

## 关键文件

- 组件实现：`ContactDamageComponent.cs`
- 受击区桥接：`../HurtboxComponent/HurtboxComponent.cs`
- 碰撞总览：`Docs/框架/ECS/Collision/碰撞系统说明.md`
