# Descriptor 约束与 DataModifier 裁决

## 结论

Data descriptor 和 DataModifier 都保留，但必须收窄。

保留的原因：

- `description`、`displayName`、`rangePolicy`、`allowedValues` 是 DataOS authoring、UI/debug、AI 理解和 validator 的核心价值。
- DataModifier 是 Buff、装备、Feature 加成和临时效果的成熟表达。
- computed resolver 能表达少量派生属性，避免 Feature 各自重复计算。

收窄的原因：

- 不是所有 Component 字段都需要这些能力。
- descriptor 太重会让 Data 重新变成所有状态的中心系统。
- modifier 泛化到非数值字段会让行为不可预测。

## Descriptor 保留字段

进入 Data 的字段继续保留这些 descriptor 元数据：

| 类别 | 字段 |
| --- | --- |
| 识别 | stableKey、valueType、runtimeTypeId |
| 默认值 | defaultValue |
| 归属 | ownerDomain、ownerFeature/ownerCapability、ownerSkill |
| 写入/存储 | storagePolicy、writePolicy、migrationPolicy |
| 约束 | rangePolicy、minValue、maxValue、allowedValues |
| modifier | modifierPolicy |
| computed | computeId、dependencies、computeParams |
| 展示 | displayName、description、uiGroup、unit、format、iconPath |

但 descriptor 不应继续承载“所有权组织纪律”这类低收益 runtime enforcement。`writePolicy` 可以保留为 loader/system/debug/computed 等真正影响写入路径的约束，不应扩成复杂权限系统。

## Description 怎么用

`description` 不是运行时逻辑，但很有价值：

- DataOS 表格作者理解字段。
- AI 判断字段语义。
- debug panel / validation report 展示字段。
- 生成 owner 文档或 catalog digest。

因此只要字段进入 Data，`description` 应继续保留在 DataOS descriptor。

Component local 字段如果也需要描述，有两种方式：

- 写在 Component options / manifest 中。
- 如果它需要 DataOS authoring 或 AI/validator 追踪，就升级为 Data 字段。

## range / allowed values 怎么实现

保留当前思路：

```text
DataOS descriptor
  -> runtime_snapshot descriptors
  -> DataDefinitionCatalog
  -> Data.Set / ApplyRecord 时执行 range / allowed values
```

区别是：只有 Data 字段有这套约束。Component local 字段通过 typed options、构造参数、Component preflight 或 owner validator 处理，不复用 Data descriptor。

## DataModifier 保留范围

保留 `DataModifier`，但仅用于 attribute-like numeric Data 字段。

适合：

- MaxHp / Shield / DamageReduction。
- MoveSpeed / AttackSpeed。
- Attack / AbilityDamageBonus。
- CritChance / DodgeChance。
- Range / CooldownReduction。

不适合：

- Current animation。
- resource path。
- Entity/Object reference。
- UI state。
- AI internal blackboard cache。
- 一帧内临时值。

## Modifier 管线

现有 `DataModifier` 管线可继续作为基础：

```text
base
  + Additive
  * Multiplicative
  + FinalAdditive
  -> Override
  -> Cap
  -> range clamp/reject
```

后续应把它命名为 attribute modifier lane，而不是任意 Data field modifier。

## Feature.Modifiers 怎么处理

`Feature.Modifiers` 这类 authoring blob 可以保留，但要明确它只是 DataModifier 的输入格式之一：

```text
DataOS feature_modifier rows
  -> runtime snapshot authoring_blob
  -> Feature activation
  -> Data.TryAddModifier(key, modifier, source)
  -> DataChanged<T>
  -> DataBinding updates Component mirror
```

Feature 不应自己计算最终属性；Feature 只授予或移除 modifier source。

## computed 是否保留

保留，但限制在真正共享的派生字段。

适合 computed：

- FinalAttack = BaseAttack + modifiers。
- HpPercent = CurrentHp / MaxHp。
- FinalMoveSpeed = MoveSpeed * speed multipliers。

不适合 computed：

- Component 内部算法中间值。
- 每帧频繁变化但没有跨功能观察需求的临时值。
- 表现层状态。

## 与 Unreal GAS 的类比

Unreal Gameplay Ability System 把 Attribute、AttributeSet、GameplayEffect 和 modifier 作为能力系统的一部分，而不是让所有对象字段都进入属性系统。SlimeAI 可以采纳这个边界：DataModifier 属于玩法属性，不属于所有状态。

## 不推荐方向

- 不推荐删除 DataModifier 后让每个 Buff 自己改字段再回滚。
- 不推荐把 DataModifier 扩展到 string、object_ref 或任意复杂结构。
- 不推荐把 Component local 字段硬塞进 Data descriptor，只为了获得 description。
- 不推荐 runtime 直接读 SQLite 执行约束。
- 不推荐保留旧 SDD-0044 的 registry 小修作为独立执行入口。

