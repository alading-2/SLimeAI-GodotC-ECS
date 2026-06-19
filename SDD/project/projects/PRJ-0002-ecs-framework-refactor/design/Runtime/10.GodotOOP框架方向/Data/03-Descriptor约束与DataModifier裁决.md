# Descriptor 约束、DataMeta 裁决与 DataModifier 边界

> 用户原始问题：[`source-request.md`](./source-request.md)
> 入口：[`README.md`](./README.md)

## 先回答：要不要回退旧 DataMeta

不建议回退。

旧 `DataMeta` 静态方式的优点是真实的：

- C# 里看字段很直观。
- 字段和默认值、范围、描述放在一起。
- 小项目里比数据库链路简单。
- IDE 跳转方便。

但它不适合 SlimeAI 当前目标：

- DataOS 表格驱动会退化成 C# 定义的镜像。
- validator、snapshot、generated `DataKey<T>` 的事实源会分裂。
- AI 需要同时理解 C# meta、数据库、snapshot 和 runtime catalog。
- 多游戏 authoring / 表格编辑 / migration 变得困难。
- 旧 DataMeta 容易重新诱导“字段定义在哪里，运行时值也必须在哪里”的误解。

所以裁决是：

```text
保留数据库 / descriptor 作为定义事实源。
不要回退 DataMeta。
把旧 DataMeta 的直观性用 generated C# contract、字段分组文档和 binder report 补回来。
```

## Descriptor 的新职责

当前 descriptor 已能表达：

- `stableKey`
- `valueType`
- `runtimeTypeId`
- `defaultValue`
- `ownerDomain`
- `ownerCapability`
- `ownerSkill`
- `storagePolicy`
- `writePolicy`
- `rangePolicy`
- `minValue`
- `maxValue`
- `allowedValues`
- `modifierPolicy`
- `computeId`
- `dependencies`
- `displayName`
- `description`
- `uiGroup`
- `unit`
- `format`
- `iconPath`

新 OOP Data 方向需要再补路由和权威字段：

| 字段 | 作用 |
| --- | --- |
| `authority` | `Data` / `Component` / `System`，说明谁是事实源。 |
| `runtimeOwner` | 字段运行时归属，例如 `HealthProfile`、`AttackComponent`、`MovementComponent`、`TargetingSystem`。 |
| `bindingPolicy` | `none` / `initial` / `changed` / `projection` / `diagnostic`。 |
| `writeEntry` | 允许写入的 owner API / command / service，例如 `HealthFeature.ApplyDamage`。 |
| `resetPolicy` | 对象池 acquire/release 时是否重置、从 record 覆盖或跟随 owner 清理。 |
| `projectionSource` | projection 字段来源，例如 `AttackComponent._state`。 |

这些字段不一定全部进入第一版 DB schema，但必须先成为设计事实源。

## Descriptor 不该做什么

Descriptor 不应变成复杂权限系统。

不推荐：

- 让 `writePolicy` 表达完整业务权限。
- 在 descriptor 里写复杂行为逻辑。
- 用 descriptor 替代 Feature owner 的 Command / Service Pipeline。
- 把 Component local 字段硬塞进 descriptor，只为了获得 description。
- 运行时直接读 SQLite 做约束。

Descriptor 的边界是：

```text
字段 contract / authoring validation / generator / routing / diagnostics。
```

业务行为仍由 owner 实现。

## Component local 字段的描述和约束怎么办

如果字段不进 Data，就不享受 Data descriptor 的 `description/range/allowed/modifier/computed`。

这不是缺陷，而是归属不同。

Component local 字段可以通过这些方式治理：

- Component manifest：描述字段意义、生命周期、reset 规则。
- typed options / profile：配置型字段的强类型初始化。
- Component preflight：注册期校验。
- owner validator：Feature 级验证。
- 代码注释：仅对复杂字段写中文注释。

升级为 Data 的条件是：

```text
它需要表格 authoring、跨功能共享、modifier/computed、UI/debug/test/AI 观察或持久化。
```

## DataModifier 保留，但收窄

DataModifier 保留，因为它解决 Buff、装备、Feature 加成、临时效果的核心问题：

```text
source 授予 -> 属性变化 -> 自动重算 -> source 移除 -> 自动回滚
```

但它只能作用于 attribute-like numeric Data 字段。

适合：

- `BaseHp`
- `MoveSpeed`
- `BaseAttack`
- `DamageReduction`
- `CritRate`
- `CooldownReduction`
- `AbilityDamageBonus`
- `AttackRange`

不适合：

- `AttackState`
- `MoveMode`
- `Velocity`
- `AIState`
- `TargetNode`
- 资源路径。
- UI 动画状态。
- Component 私有缓存。
- object_ref / string / enum projection 字段。
- 一帧临时值。

## Modifier lane

保留现有思路：

```text
base
  + Additive
  * Multiplicative
  + FinalAdditive
  -> Override
  -> Cap
  -> range clamp/reject
```

但命名和文档上应叫：

```text
attribute modifier lane
```

而不是“任意 Data 字段都可以 modifier”。

## Feature.Modifiers

`Feature.Modifiers` 可以继续作为 authoring 输入格式之一：

```text
DataOS feature_modifier rows
  -> runtime snapshot modifier list
  -> Feature activation
  -> owner.Data.TryAddModifier(targetKey, modifier, source)
  -> DataChanged<T>
  -> DataBinding / UI / debug / test
```

Feature 不自己计算最终属性；Feature 只授予和移除 modifier source。

后续 validator 必须检查：

- modifier 目标字段存在。
- 目标字段 `modifierPolicy = numeric`。
- 目标字段不是 projection。
- source 可稳定回滚。

## Computed 保留但限制

computed resolver 保留，用于少数共享派生字段：

- `FinalHp = BaseHp + HpBonus`
- `HpPercent = CurrentHp / FinalHp`
- `FinalMoveSpeed = MoveSpeed + MoveSpeedBonus`
- `AttackInterval = FinalAttackSpeed -> seconds`
- `FinalAbilityDamage = AbilityDamage + AbilityDamageBonus`

不适合 computed：

- Component 内部算法中间值。
- 每帧频繁变化但没有共享观察价值的临时值。
- 表现层状态。
- System 查询结果。

## Description / range / allowed values 怎么继续生效

只要字段进入 Data，就继续走：

```text
DataOS descriptor
  -> runtime_snapshot descriptor
  -> DataDefinitionCatalog
  -> Data.Set / ApplyRecord / DataModifier pipeline
  -> diagnostics
```

`description` 的用途：

- 表格作者理解字段。
- AI 判断字段语义。
- debug panel / validation report 展示。
- 生成 owner 文档或 catalog digest。

`range/allowed values` 的用途：

- DB validator 前置检查。
- runtime snapshot 生成期检查。
- Data.Set / ApplyRecord 运行时边界检查。
- modifier 结果 clamp/reject。

## Generated C# contract

为了弥补旧 `DataMeta` 的直观性，后续生成器可以输出更有结构的 C# contract：

```text
GeneratedDataKey.CurrentHp
GeneratedDataDescriptor.CurrentHp.Metadata
GeneratedDataOwner.HealthProfile.Fields
GeneratedDataBindingManifest.Health
```

这不是回退 meta，而是从 DB descriptor 生成只读 C# 视图。

规则：

- C# contract 不持有独立默认值事实源。
- C# contract 不手写范围和 computed。
- C# contract 只帮助 IDE 跳转、AI 阅读和 binder 校验。

## 本页裁决

```text
Data 定义继续集中在 DataOS descriptor。
不回退旧 DataMeta 静态注册。
Descriptor 增加 authority / runtimeOwner / bindingPolicy 等路由能力。
DataModifier 保留但只用于 attribute-like numeric Data。
Computed 保留但只用于共享派生字段。
Component local 字段用 Component manifest / typed options / preflight 管理。
```
