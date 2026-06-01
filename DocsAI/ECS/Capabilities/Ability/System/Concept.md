# AbilitySystem 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/Ability/System/
> relatedDocs: DocsAI/ECS/Capabilities/Ability/System/Usage.md, DocsAI/ECS/Capabilities/Ability/Component/TriggerComponent/README.md, DocsAI/ECS/Capabilities/Ability/Component/CostComponent/README.md, DocsAI/ECS/Capabilities/Ability/Component/CooldownComponent/README.md, DocsAI/ECS/Capabilities/Ability/Component/ChargeComponent/README.md
> lastReviewed: 2026-06-01

## 1. 一句话定位

AbilitySystem 是技能系统的三层架构：EntityManager_Ability（CRUD）→ 输入/目标选择层 → AbilitySystem（施法提交）。

## 2. 核心概念

### 三层架构

```
EntityManager_Ability（能力 CRUD）
  ├─ 创建、销毁、查询 Ability Entity
  └─ 管理 Ability 生命周期

输入/目标选择层
  ├─ TriggerComponent（施法意图：Manual/OnEvent/Periodic/Auto）
  ├─ TargetingSystem（异步目标选择会话）
  └─ 索敌、软锁定

AbilitySystem（施法提交）
  ├─ 接收 CastRequest
  ├─ 检查 Cost/Cooldown/Charge
  └─ 执行 Ability 逻辑
```

### 技能类型

- **Instant**：按即触发
- **Unit Target**：自动软锁定
- **Point Target**：方向型

### 组件协作

- **TriggerComponent**：生成施法意图（不处理消耗/冷却）
- **CostComponent**：资源消耗（mana/energy/ammo/HP）
- **CooldownComponent**：毫秒精度 CD 管理
- **ChargeComponent**：多充能系统，自动恢复

## 3. 职责边界

| AbilitySystem 做 | AbilitySystem 不做 |
| ---- | ---- |
| 施法提交与执行 | 目标选择（归 TargetingSystem） |
| 检查 Cost/Cooldown/Charge | 消耗扣除（归 CostComponent） |
| 管理 Ability 生命周期 | 冷却管理（归 CooldownComponent） |

## 4. 依赖关系

- **FeatureSystem**：AbilitySystem 是 FeatureSystem 的施法阶段特化
- **TargetingSystem**：异步目标选择
- **TimerManager**：冷却和充能计时
- **Data**：技能配置和运行时状态
