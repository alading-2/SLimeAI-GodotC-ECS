# AbilitySystem（技能系统）

## 概述

技能系统拆成两层：

1. `EntityManager_Ability`：技能生命周期管理（增删查、Owner 关系、`TryTrigger` 接线）
2. `AbilitySystem`：施法流水线编排（检查 -> Handler 前置准备 -> 消耗 -> 执行）

当前版本的关键边界：

- `AbilitySystem` 只负责编排，不负责通用自动索敌或通用点选决策
- 具体技能如何找目标、是否直接失败、是否进入点选，都由 `AbilityFeatureHandler.PrepareCast` 决定
- `TargetingManager` 只负责异步点选会话，不负责决定“何时开始瞄准”

---

## 目录结构

```text
Src/ECS/Base/System/AbilitySystem/
├── AbilitySystem.cs          # 施法流水线（统一入口）
├── AbilityFeatureHandler.cs  # Ability 子域 Handler 中转层 + PrepareCast 钩子
├── AbilityTargetingTool.cs   # Ability 子域点选请求工具
├── EntityManager_Ability.cs  # 技能 CRUD + 事件接线
├── AbilityCheckPhase.cs      # CheckCanUse 检查优先级
├── TriggerResult.cs          # Success/Failed/WaitingForTarget
└── README.md                 # 本文档
```

---

## 核心流程（Trigger -> Cast -> Execute）

### 1) Trigger（触发）

触发源通常来自：

- `ActiveSkillInputComponent`（玩家手动）
- `TriggerComponent`（事件触发 / 周期触发）

统一方式：向技能实体发 `TryTrigger` 事件，并携带 `CastContext`。

```csharp
var context = new CastContext
{
    Ability = ability,
    Caster = caster,
    ResponseContext = new EventContext()
};

ability.Events.Emit(
    GameEventType.Ability.TryTrigger,
    new GameEventType.Ability.TryTriggerEventData(context)
);
```

### 2) Cast（施法）

`AbilitySystem.TryTriggerAbilityWithContext(context)` 当前流水线：

1. `CanUseAbility`
2. `PrepareAbilityCast`
3. `ConsumeCharge`
4. `StartCooldown`（周期技能跳过）
5. `ConsumeCost`

`PrepareAbilityCast` 会读取当前 `AbilityConfig.FeatureHandlerId` 对应的处理器，并在进入任何消耗前调用：

- `AbilityFeatureHandler.PrepareCast(context)`

这个钩子只允许做三件事：

- 补齐 `context.Targets`
- 请求点选并返回 `WaitingForTarget`
- 直接返回 `Failed`

不允许在这里做任何资源消耗或冷却启动。

### 3) Execute（执行）

施法前置准备成功后：

- 发送 `Ability.Activated`
- 构建 `FeatureContext`，把 `CastContext` 放入 `ActivationData`
- 调用 `FeatureSystem.OnFeatureActivated(...)`
- 由 `AbilityFeatureHandler.OnExecute(...)` 把 `FeatureContext` 转回 `CastContext`
- 调用具体技能的 `ExecuteAbility(context)`
- 发送 `Ability.Executed`
- 同步技能立即调用 `FeatureSystem.OnFeatureEnded(..., FeatureEndReason.Completed)`

其中 `AbilityConfig.FeatureHandlerId` 必须填写完整唯一 `FeatureId`，例如 `技能.位移.冲刺`；`FeatureGroupId` 只作为展示分组，不参与运行时 Handler 查找。

---

## 目标职责边界

### AbilitySystem 负责

- 接收 `TryTrigger`
- 做统一就绪检查
- 在消耗前调用 Handler 的 `PrepareCast`
- 成功后再做充能、冷却、成本消耗
- 接入 `FeatureSystem` 执行技能

### AbilityFeatureHandler 负责

- 决定这次施法是否需要自动索敌
- 决定无目标时是 `Failed` 还是进入点选
- 决定如何把结果写入 `context.Targets` / `context.TargetPosition`

### TargetingManager 负责

- 接收 `Targeting.StartTargeting`
- 维护单一瞄准会话
- 管理指示器输入与确认/取消
- 在确认后回调 `AbilitySystem.ResumeAfterTargeting(context)`

当前实现已删除 `AbilityTargetSelectionComponent`。实体目标查询直接写在具体 Handler 内部，使用 `EntityTargetSelector.Query(...)`。

---

## 典型模式

### 1) 需要实体目标，否则失败

例如 `ArcShot`、`ChainLightning`：

- 在 `PrepareCast` 中查询主目标
- 查到后写入 `context.Targets`
- 查不到直接返回 `TriggerResult.Failed`

### 2) 需要玩家点位置

- 在 `PrepareCast` 中检查 `context.TargetPosition`
- 如果没有，则调用 `RequestPointTarget(context)` 并返回 `WaitingForTarget`
- 玩家确认后由 `TargetingManager` 调 `ResumeAfterTargeting(context)` 重跑流水线

### 3) 无目标也可释放

例如按朝向飞行、按默认前方点落地的技能：

- `PrepareCast` 直接返回 `Success`
- 在 `ExecuteAbility` 里自行做“有目标更好、没目标也能继续”的逻辑

---

## 两条时间轴

Ability 子域里有两套不同的时间语义：

1. `TriggerComponent.Periodic`
- 控制技能多久重新执行一次整条流水线
- 每次都会重新进入 `TryTrigger / PrepareCast / ExecuteAbility`

2. `DamageApplyOptions.TickInterval / TotalDuration`
- 控制单次技能执行内部是否继续跳 DoT
- 不会重新进入技能流水线，只会让当前这次执行继续结算伤害

两者可以并存；并存时表示“每次周期触发都会再启动一条新的内部 DoT”。

---

## 返回值与请求-响应

`TryTrigger` 的结果统一写回 `CastContext.ResponseContext`：

```csharp
var result = context.ResponseContext?.HasResult == true
    ? (TriggerResult)context.ResponseContext.GetResult<TriggerResult>()
    : TriggerResult.Failed;
```

可能值：

- `Success`
- `Failed`
- `WaitingForTarget`

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
| `HandleTryTrigger` | `TryTrigger` 事件入口；写入 `ResponseContext` |
| `CanUseAbility` | 仅检查可用性（不消耗） |
| `ResumeAfterTargeting` | 点选确认后重跑整条流水线 |

### AbilityFeatureHandler

| 方法 | 说明 |
| :--- | :--- |
| `PrepareCast` | 消耗前前置钩子；决定成功/失败/等待点选 |
| `ExecuteAbility` | 具体技能执行逻辑 |
| `RequestPointTarget` | 发起统一点选请求 |

---

## 相关文档

- 架构总览：`Docs/框架/ECS/Ability/技能系统架构设计理念.md`
- 瞄准子系统：`Src/ECS/Base/System/TargetingSystem/README.md`
- 事件总线：`Src/ECS/Base/Event/README_EventBus.md`
