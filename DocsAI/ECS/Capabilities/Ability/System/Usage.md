<!-- migrated-from: Src/ECS/Base/System/AbilitySystem/README.md -->

> 迁移来源：`Src/ECS/Base/System/AbilitySystem/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# AbilitySystem（技能系统）

## 概述

技能系统拆成三层：

1. `EntityManager_Ability`：技能生命周期管理（增删查、Owner 关系、`TryTrigger` 接线）
2. 输入 / 点选层：根据 `AbilityTargetSelection` 决定是否先进入点选
3. `AbilitySystem`：正式施法提交（检查 -> 消耗 -> 冷却 -> 执行）

当前版本的关键边界：

- `AbilitySystem` 只负责编排正式施法提交，不做通用自动索敌或通用点选决策
- 具体技能如何找目标、无目标时打空还是降级，由 `AbilityFeatureHandler.ExecuteAbility` 内部决定
- `TargetingManager` 只负责异步点选会话；确认点位后重新发起正式 `TryTrigger`

---

## 目录结构

```text
Src/ECS/Capabilities/Ability/System/
├── AbilitySystem.cs          # 正式施法流水线（统一入口）
├── AbilityFeatureHandler.cs  # Ability 子域 Handler 中转层
├── AbilityTool.cs            # Ability 子域薄工具（阵营过滤判断）
├── AbilityImpactTool.cs      # 技能命中工具（Query/Targets -> 特效/伤害）
├── EntityManager_Ability.cs  # 技能 CRUD + 事件接线
├── AbilityCheckPhase.cs      # CheckCanUse 检查优先级
├── TriggerResult.cs          # Success/Failed
└── README.md                 # 本文档
```

---

## 核心流程

### 1) 输入阶段

`ActiveSkillInputComponent` 读取当前主动技能：

- `AbilityTargetSelection.Point`：先调用 `AbilitySystem.CanUseAbility(ability)` 做预检查，通过后直接发 `Targeting.StartTargeting` 进入点选
- 其他类型：直接向技能实体发 `TryTrigger`

点选取消不会扣资源、启动冷却或执行技能。点选确认时，`TargetingManager` 写入 `CastContext.TargetPosition`，再向技能实体发正式 `TryTrigger`。

### 2) Cast（正式提交）

`AbilitySystem.TryTriggerAbilityWithContext(context)` 当前流水线：

1. `CanUseAbility`
2. `ConsumeCharge`
3. `StartCooldown`（周期技能跳过）
4. `ConsumeCost`
5. `FeatureSystem.OnFeatureActivated`

`CanUseAbility` 只做只读检查：启用状态、激活状态、冷却、充能、资源是否足够。正式 `TryTrigger` 会再次检查一次，防止点选期间状态变化。

### 3) Execute（执行）

正式提交成功后：

- 发送 `Ability.Activated`
- 构建 `FeatureContext`，把 `CastContext` 放入 `ActivationData`
- 调用 `FeatureSystem.OnFeatureActivated(...)`
- 由 `AbilityFeatureHandler.OnExecute(...)` 把 `FeatureContext` 转回 `CastContext`
- 调用具体技能的 `ExecuteAbility(context)`
- 发送 `Ability.Executed`
- 同步技能立即调用 `FeatureSystem.OnFeatureEnded(..., FeatureEndReason.Completed)`

其中 `AbilityData.FeatureHandlerId` 必须填写完整唯一 `FeatureId`，例如 `技能.位移.冲刺`；`FeatureGroupId` 只作为展示分组，不参与运行时 Handler 查找。

---

## 目标职责边界

### AbilitySystem 负责

- 接收正式 `TryTrigger`
- 做统一就绪检查
- 成功后做充能、冷却、成本消耗
- 接入 `FeatureSystem` 执行技能

### AbilityFeatureHandler 负责

- 把 `FeatureContext.ActivationData` 转回 `CastContext`
- 只暴露 `ExecuteAbility` 给具体技能实现
- 不承载索敌、点选或 Ability 领域 helper

### 具体技能 Handler 负责

- 在 `ExecuteAbility` 中读取 `CastContext`
- 自行查询目标、读取点位、生成投射物或结算伤害
- 自行决定无目标时返回 0 命中、朝默认方向释放、打空或执行降级逻辑

### TargetingManager 负责

- 接收 `Targeting.StartTargeting`
- 维护单一瞄准会话
- 管理指示器输入与确认/取消
- 确认后发正式 `Ability.TryTrigger`

当前实现已删除 `AbilityTargetSelectionComponent`。实体目标查询直接写在具体 Handler 内部，使用 `EntityTargetSelector.Query(...)`。

---

## 典型模式

### 1) 需要实体目标

例如 `ArcShot`、`ChainLightning`：

- 在 `ExecuteAbility` 开始时查询主目标
- 查到后可写入 `context.Targets` 供本次执行链路使用
- 查不到时返回 `new AbilityExecutedResult { TargetsHit = 0 }`

### 2) 需要玩家点位置

- 输入层识别 `AbilityTargetSelection.Point`
- 输入层调用 `AbilitySystem.CanUseAbility(ability)` 预检查
- 通过后直接发 `Targeting.StartTargeting` 进入点选
- `TargetingManager` 确认后写入 `context.TargetPosition` 并发正式 `TryTrigger`
- 具体 Handler 在 `ExecuteAbility` 中读取 `context.TargetPosition`

### 3) 无目标也可释放

例如按朝向飞行、按默认前方点落地的技能：

- 直接走正式 `TryTrigger`
- 在 `ExecuteAbility` 里自行做“有目标更好、没目标也能继续”的逻辑

---

## 两条时间轴

Ability 子域里有两套不同的时间语义：

1. `TriggerComponent.Periodic`
- 控制技能多久重新执行一次整条正式施法流水线
- 每次都会重新进入 `TryTrigger / ExecuteAbility`

2. `DamageApplyOptions.TickInterval / TotalDuration`
- 控制单次技能执行内部是否继续跳 DoT
- 不会重新进入技能流水线，只会让当前这次执行继续结算伤害

两者可以并存；并存时表示“每次周期触发都会再启动一条新的内部 DoT”。

---

## 返回值与请求-响应

正式 `TryTrigger` 的结果统一写回 `CastContext.ResponseContext`：

```csharp
var result = context.ResponseContext?.HasResult == true
    ? context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;
```

可能值：

- `Success`
- `Failed`

点选等待不是 `AbilitySystem` 的返回状态，而是输入 / Targeting 层的会话状态。

---

## 关键 API

### EntityManager_Ability

| 方法 | 说明 |
| :--- | :--- |
| `AddAbility` | 添加技能、建立 Owner 关系、接线 `TryTrigger -> AbilitySystem.HandleTryTrigger` |
| `RemoveAbility` | 移除技能与关系 |
| `GetAbilities` | 获取单位所有技能 |
| `GetManualAbilities` | 获取可手动施放技能 |
| `GetAbilityByName` | 按名称查询技能 |

### AbilitySystem

| 方法 | 说明 |
| :--- | :--- |
| `HandleTryTrigger` | 正式 `TryTrigger` 事件入口；写入 `ResponseContext` |
| `CanUseAbility` | 仅检查可用性（不消耗），可供输入层点选前预检查 |

### AbilityFeatureHandler

| 方法 | 说明 |
| :--- | :--- |
| `ExecuteAbility` | 具体技能执行逻辑 |

### AbilityTool

| 方法 | 说明 |
| :--- | :--- |
| `MatchesTeamFilter` | 按 Ability 阵营过滤判断能否命中 |

### AbilityImpactTool

| 方法/字段 | 说明 |
| :--- | :--- |
| `Execute` | 统一命中入口 |
| `Query` | 通过范围查询生成目标列表 |
| `Targets` | 已明确目标时直接结算，适合碰撞命中 |

---

## 相关文档

- 架构总览：`DocsAI/ECS/Capabilities/Ability/System/Usage.md`
- 瞄准子系统：`DocsAI/ECS/Capabilities/Ability/System/TargetingSystem/Usage.md`
- 事件总线：`DocsAI/ECS/Runtime/Event/Event系统说明.md`
