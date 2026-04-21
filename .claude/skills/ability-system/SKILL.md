---
name: ability-system
description: 使用技能系统、实现或修改技能功能时使用。适用于：新建技能、配置冷却/充能/目标选择、读取触发结果、实现技能效果处理器，以及判断逻辑应写在 ExecuteAbility 还是 OnGranted。
---

# AbilitySystem 使用入口

## 什么时候看这个 Skill

以下场景应优先看本 Skill：

- 新建一个技能 Handler
- 修改已有技能执行逻辑
- 调整冷却、充能、触发方式、目标选择
- 判断技能逻辑应该写在 `ExecuteAbility` 还是 `OnGranted`
- 想确认 `CastContext`、`AbilityImpactTool`、`DamageApplyOptions` 该怎么组合
- 看到这些关键词：`AbilitySystem`、`TryTrigger`、`CastContext`、`CooldownComponent`、`ChargeComponent`、`TriggerComponent`、`AbilityEntity`、`IFeatureHandler`

如果你要处理的是：

- 伤害系统本身 → 看 `@damage-system`
- Data 运行时读写规则 → 看 `@ecs-data`
- Data 目录配置与资源映射 → 看 `@data-authoring`
- Feature 生命周期模板化问题 → 看 `@feature-system`

## 阅读顺序

按这个顺序看：

1. 先看本文件，确认任务属于哪一类
2. 参数、写法、时间轴、AbilityImpactTool 用法 → 看 [`references/ability-logic-parameters.md`](./references/ability-logic-parameters.md)
3. 需要查系统全局入口或模块位置 → 看 [项目索引](/mnt/e/Godot/Games/MyGames/复刻土豆兄弟/brotato-my/Docs/框架/项目索引.md)
4. 真正落代码时，再去打开对应 Handler / Tool / 组件源码

## 当前原则

- `AbilitySystem` 只编排流水线，不做统一自动索敌或点选决策
- 实体目标查询通常写在具体 Handler 的 `ExecuteAbility`
- `Point` 点选由输入层在正式 `TryTrigger` 前发起，确认后再提交正式 `TryTrigger`
- `TargetingManager` / `TargetingIndicatorControlComponent` 只负责异步点选会话
- 技能逻辑参数写法不要继续堆在 Skill 入口里，统一下沉到 `references/`
- 技能通用发射辅助统一放 `AbilityTool`；贝塞尔模板/选点模式统一放 `Src/ECS/Tools/Math/Bezier/`，不要再回填到技能目录里的临时工具桶

## 常见任务入口

- “技能逻辑参数怎么写” → [`references/ability-logic-parameters.md`](./references/ability-logic-parameters.md)
- “为什么这个技能每隔 N 秒自动放一次” → 先查 `TriggerComponent + AbilityCooldown`
- “为什么这次命中后还在持续掉血” → 先查 `DamageApplyOptions.TickInterval / TotalDuration`
- “为什么这个逻辑该写在 OnGranted 还是 ExecuteAbility” → 先查 `references/ability-logic-parameters.md` 里的生命周期边界
- “为什么不该在 AbilitySystem 里做统一索敌” → 先看本文件的当前原则

## 文档结构约定

`ability-system` 目录后续按两层维护：

- `SKILL.md`
  - 只保留入口、边界、阅读顺序、任务导航
- `references/*.md`
  - 放领域知识、参数语义、代码模板、踩坑说明

不要再把整套参数写法直接回填到 `SKILL.md`。

## 当前参考文档

- [`references/ability-logic-parameters.md`](./references/ability-logic-parameters.md)
