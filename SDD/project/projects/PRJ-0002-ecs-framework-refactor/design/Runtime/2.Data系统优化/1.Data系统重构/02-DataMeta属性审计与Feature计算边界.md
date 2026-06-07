# DataMeta 属性审计与 Feature 计算边界

> 更新：2026-05-28
> 状态：设计裁决，供后续执行型 SDD 拆任务使用。
> 范围：旧 ECS `DataMeta` / `DataRegistry` / `Data`、`SlimeAI/Data/Data/`、`SlimeAI/Data/DataNew/`、`FeatureSystem`、`FeatureDefinition`、`FeatureModifierEntry`、DataOS descriptor-first 方案。
> 非目标：本文不要求立即修改源码；不废弃 FeatureSystem；不把旧 ECS 替换成纯 GameOS。

## 1. 本轮重新审查的问题

这轮审查不是重复“DataOS descriptor-first”结论，而是补上两个容易误判的点：

- **Feature 是否可以承载 Data computed 计算函数**：用户指出当前已有 `FeatureSystem`、`FeatureDefinition` 和 `FeatureModifierEntry`，需要判断是否能复用，而不是另建一套计算机制。
- **`DataMeta` 的所有属性是否都应该进数据库**：旧 `DataMeta` 字段很多，其中一部分是运行时必需，一部分是 UI / 调试 / 人类可读信息；不能把旧对象原样搬到 `data_key_descriptor`。

最终裁决：

```text
FeatureSystem 继续保留，用于生命周期、效果执行和 Modifier 授予/回滚。
Data computed 不交给 FeatureSystem，而由独立 DataComputeRegistry / IDataComputeResolver 承载。
DataMeta 不原样数据库化，而是拆成 core definition、runtime policy、compute policy、presentation、migration policy。
SlimeAI/Data/Data 与 SlimeAI/Data/DataNew 不作为新 Data 输入路径保留。
```

## 2. 旧源码事实

### 2.1 当前 `DataMeta` 承载的职责

旧 `DataMeta` 同时包含五类信息：

| 类别 | 字段 | 当前职责 |
| ---- | ---- | ---- |
| 核心定义 | `Key`、`Type`、`DefaultValue` | `Data.Get/Set`、默认值、类型判断 |
| 运行时策略 | `MinValue`、`MaxValue`、`SupportModifiers`、`Options` | clamp、modifier 权限、选项校验 |
| 计算策略 | `Dependencies`、`Compute`、`IsComputed` | computed cache、依赖标脏、lambda 计算 |
| 迁移策略 | `CanMigrate` | Entity 迁移过滤 |
| 展示/工具 | `DisplayName`、`Description`、`Category`、`IsPercentage`、`IconPath` | 调试面板、配置编辑器、Tooltip、格式化 |

这就是旧系统最大的问题：**一个对象同时是 authoring descriptor、runtime contract、UI metadata 和 behavior hook**。

### 2.2 当前定义数量和使用情况

对 `SlimeAI/Data/DataKey/` 粗略统计：

| 字段 | 定义数量 | 主要使用点 | 裁决 |
| ---- | ----: | ---- | ---- |
| `Key` | 170 | `DataRegistry`、`Data.Get/Set` | 核心必需 |
| `Type` | 170 | 类型判断、编辑器控件 | 核心必需，但不能存 C# `Type` |
| `DefaultValue` | 170 | 默认值、Reset、旧 DataNew DTO | 核心必需，但来源迁到 descriptor，旧 DataNew 删除 |
| `DisplayName` | 170 | 调试 UI / DataConfigEditor | 展示元数据 |
| `Category` | 170 | UI 分组、`ResetByCategory` | 需要拆分语义 |
| `Description` | 98 | Tooltip / 搜索 | 展示元数据，但对 AI 有价值 |
| `MinValue` | 70 | clamp / editor range | 运行时策略 |
| `MaxValue` | 13 | clamp / editor range | 运行时策略 |
| `IsPercentage` | 22 | 展示格式 / 调试文本 | 展示语义，不是计算语义 |
| `SupportModifiers` | 63 | modifier 权限 | 运行时策略 |
| `CanMigrate` | 3 | Entity 迁移过滤 | 迁移策略 |
| `IconPath` | 0 | 当前未实际使用 | 展示元数据，可选 |
| `Options` | 0 个业务定义 | 测试和 UI 能力 | 约束能力在新 `allowed_values` 中重建 |
| `Dependencies` | 14 | computed 依赖 | 计算策略必需 |
| `Compute` | 14 | C# lambda 计算 | 删除字段形态，换 resolver |

注意：`DisplayName` / `Description` 不是“只给人用”。AI-first 下它们同样有价值，因为 AI 需要理解字段含义。但它们不应该进入 `Data.Get/Set` 热路径。

### 2.3 当前 `Data` 的职责过重

旧 `Data` 负责：

- 保存 base value。
- 类型转换和 fallback。
- 使用 `DataMeta.Clamp` 做范围限制。
- 管理 modifier 列表。
- 计算 modifier 后最终值。
- 调用 `DataMeta.Compute` 计算 computed。
- 管理 computed cache 和 dirty key。
- 通过 `DataRegistry.GetDependentComputedKeys` 做依赖标脏。
- 发出 `Entity.Events` 数据变更事件。
- 通过反射 `LoadFromConfig` 读取配置对象。

这些行为能力需要在新 Data 系统中重新实现，但不能通过旧 `DataMeta` / `DataRegistry` / `LoadFromConfig` 兼容路径保留：

```text
DataDefinitionCatalog 负责字段定义索引。
DataRuntimePolicy 负责写入、范围、modifier、迁移策略。
DataComputeRegistry 负责 compute_id -> resolver。
DataSlot 负责单字段值、modifier、dirty cache。
Data 负责统一 API、事件通知和 owner 绑定。
```

## 3. FeatureSystem 与 Data computed 的边界

### 3.1 FeatureSystem 当前事实

当前 `FeatureSystem` 职责是管理完整 Feature 生命周期：

```text
Granted
  -> ApplyModifiers
  -> IFeatureHandler.OnGranted
  -> Emit Feature.Granted

Removed
  -> IFeatureHandler.OnRemoved
  -> RemoveModifiersBySource
  -> Emit Feature.Removed

Activated
  -> IFeatureHandler.OnActivated
  -> IFeatureHandler.OnExecute
  -> FeatureActivationCount++
  -> Emit Feature.Executed

Ended
  -> IFeatureHandler.OnEnded
  -> Emit Feature.Ended
```

`FeatureDefinition` 主要保存：

- `Name`
- `FeatureHandlerId`
- `Description`
- `Category`
- `EntityType`
- `Enabled`
- `Modifiers`

`FeatureModifierEntry` 保存：

- `DataKeyName`
- `ModifierType`
- `Value`
- `Priority`

因此，Feature 的核心是：**一个可授予、可移除、可启用、可激活的效果容器**。

### 3.2 为什么 computed 不应该用 Feature 实现

#### 3.2.1 生命周期不匹配

Feature 是生命周期对象：

```text
Granted -> Enabled -> Activated -> Execute -> Ended -> Removed
```

computed 是读值过程：

```text
Data.Get(FinalHp)
  -> 检查缓存
  -> 读取依赖
  -> 调用纯计算 resolver
  -> 返回值
```

如果用 Feature 计算 `FinalHp`，一次普通读值会变成一次生命周期驱动，边界过重。

#### 3.2.2 副作用模型不匹配

Feature 允许副作用：

- 添加 modifier。
- 移除 modifier。
- 发事件。
- 调用系统服务。
- 写入 `FeatureActivationCount`。
- 修改 `FeatureIsActive`。

computed 必须无副作用：

- 不发事件。
- 不写 Data。
- 不创建实体。
- 不访问场景树。
- 不启动 timer。
- 不依赖一次性 `ActivationData`。

否则 `Data.Get` 就不再是安全读操作。

#### 3.2.3 缓存和依赖图不匹配

computed 需要：

- 依赖 key 静态可知。
- 启动时可检测依赖是否存在。
- 可建立反向依赖索引。
- base key 改变时可递归标脏。
- 可检测循环依赖。

Feature handler/action 通常由运行时上下文驱动，不适合作为字段级依赖图节点。

### 3.3 正确关系

正确关系是：

```text
Feature 改变 Data 输入。
Data computed 读取输入并计算派生输出。
```

例如：

```text
Feature Granted
  -> owner.Data.AddModifier(Attribute.BaseAttack, +10)
  -> Attribute.BaseAttack effective value 改变
  -> Attribute.FinalAttack 标脏

Data.Get(Attribute.FinalAttack)
  -> 读取 Attribute.BaseAttack effective value
  -> 读取 Attribute.AttackBonus effective value
  -> 调用 AttributeBonusComputeResolver
  -> 返回最终攻击力
```

所以：

| 场景 | 归属 |
| ---- | ---- |
| 装备给攻击 +10 | Feature + Modifier |
| Buff 给移动速度 +20% | Feature + Modifier |
| 技能释放造成伤害 | Feature / AbilitySystem / DamageSystem |
| 击杀后触发回血 | Feature / Event / DamageSystem |
| `FinalHp = BaseHp + HpBonus` | DataComputeResolver |
| `HpPercent = CurrentHp / FinalHp` | DataComputeResolver |
| `DPS = FinalAttack / AttackInterval` | DataComputeResolver |
| `AttackInterval = 1 / FinalAttackSpeed` | DataComputeResolver |

### 3.4 可以复用 Feature 的思想，但不复用 Feature 生命周期

可以借鉴：

- `FeatureHandlerRegistry` 的注册表模式。
- `FeatureHandlerId` 绑定 C# 处理器的做法。
- “数据中保存 id，C# 中注册行为”的模式。

但 computed 使用独立机制：

```csharp
public interface IDataComputeResolver
{
    string ComputeId { get; }
    object? Compute(Data data, DataDefinition definition);
}
```

Feature 仍然保留，不废弃；只是它不进入 Data computed 纯计算层，也不作为旧 Data 兼容路径。

## 4. `DataMeta` 属性逐项裁决

### 4.1 `Key`

语义保留为核心字段，但来源必须升级为 descriptor stable key。

旧写法：

```text
BaseHp
FinalHp
AbilityDamage
```

推荐写法：

```text
Attribute.BaseHp
Attribute.FinalHp
Ability.Damage
Feature.Enabled
```

原因：AI、validator 和 DataOS 都需要通过 owner/domain 理解字段归属，不能只靠 C# 字段名猜测。

### 4.2 `Type`

类型表达能力必须保留，但不保存 C# `System.Type`。

推荐拆成：

```text
value_type       Data 层基础类型，如 float/int/bool/string/vector2
runtime_type_id  可选 C# / Godot 类型标识，如 Team、Godot.Node2D
storage_policy   决定是否可持久化
```

原因：

- 数据库不能稳定保存 `typeof(float)`。
- Godot `Node`、`Resource`、`Array<FeatureModifierEntry>` 与普通数值字段生命周期不同。
- AI 需要明确知道某字段能否进 snapshot records。

### 4.3 `DefaultValue`

默认值能力必须保留，但来源从旧 `DataMeta` / `DataKey.DefaultValue` / `DataNew` 改为 descriptor，并区分类型默认值和玩法默认值。

| 类型 | 例子 | 来源 |
| ---- | ---- | ---- |
| 类型默认值 | `float -> 0` | 类型系统 |
| 玩法默认值 | `BaseHp -> 10` | descriptor |

`Data.Get` fallback、Reset、snapshot apply 都需要玩法默认值。

### 4.4 `MinValue` / `MaxValue`

范围能力必须保留，但不应该默认表示“运行时静默 clamp”。

推荐新增：

```text
range_policy = none / validate / clamp_runtime / reject_runtime
```

裁决：

- 配置和 snapshot 加载阶段优先 `validate`，错误要报告给 AI。
- 调试 UI 可以用范围限制输入框。
- 战斗运行时是否 clamp 由字段策略决定。
- 不应继续所有字段都静默 `Clamp`，否则会掩盖 authoring 错误。

### 4.5 `IsPercentage`

不作为 Data core 字段。

它当前主要用于展示和调试，不决定真实计算方式。推荐替换为：

```text
unit = percent
format = 0.0%
```

计算中 `/100` 必须由 resolver 或业务系统明确处理，不由 `IsPercentage` 隐式猜测。

### 4.6 `SupportModifiers`

modifier 权限能力必须保留为新运行时策略。

它防止任意字段被 modifier 修改。例如 `Id`、`EntityType`、`CurrentHp`、`FeatureActivationCount` 不应该随便被 Feature modifier 改写。

推荐从 bool 升级为：

```text
modifier_policy = none / numeric / debug_only
```

一次性审计阶段可以读取旧 `supports_modifiers bool` 生成迁移报告；新系统不保留该 bool 作为运行时兼容字段。

### 4.7 `CanMigrate`

迁移策略能力必须保留，但从 bool 升级为迁移策略。

当前只有少数字段设置 `CanMigrate=false`，但 Entity 迁移逻辑确实依赖它。推荐：

```text
migration_policy = default / never / always / profile_only
```

这样比 `CanMigrate` 更适合表达：

- `Id` 永不迁移。
- `SourceEntityId` / `OriginEntityId` 由迁移流程盖章。
- 特殊字段只能由 profile 白名单迁移。

### 4.8 `DisplayName`

不进入 Data 热路径，但保留在 presentation descriptor。

它不仅给人用，也给 AI 用。AI 通过 `DisplayName` 能理解 `BaseHp` 是“基础生命值”，而不是只靠英文缩写猜测。

### 4.9 `Description`

不进入 Data 热路径，但保留在 authoring / AI / UI 层。

它对 AI 很关键，尤其是这些字段：

- `AbilityChainDamageDecay`：`0-100，100=无衰减`。
- `CooldownReduction`：需要知道是否受全局上限限制。
- `AbilityTriggerChance`：概率统一 0-100。

### 4.10 `Category`

不应原样保留为一个字段，因为它混合了 UI 分组和运行时批量操作。

推荐拆成：

```text
owner_domain   Attribute / Ability / Feature
ui_group       Health / Attack / Cooldown
reset_group    RuntimeState / Config / CombatStats
```

旧 `ResetByCategory` 应迁移到 `reset_group` 或 profile，而不是继续依赖 UI 分组。

### 4.11 `IconPath`

当前没有实际业务使用。若后续需要，应作为 presentation 可选字段重建，不进入 core。

### 4.12 `Options`

选项约束能力需要重建，但不要继续只表达为“int index 对应字符串数组”。

推荐：

```json
[
  { "value": "Manual", "label": "手动" },
  { "value": "Periodic", "label": "周期" }
]
```

这样 AI 能直接读懂可选值，不需要猜 index。

### 4.13 `Dependencies`

依赖表达能力必须保留为 computed 必需字段。

validator 必须检查：

- 依赖 key 存在。
- 依赖类型满足 resolver 要求。
- 不存在循环依赖。
- 依赖图可建立反向索引。

### 4.14 `Compute`

删除旧字段形态，行为能力由 `ComputeId + resolver` 重建。

旧方式：

```csharp
Compute = data => ...
```

新方式：

```text
compute_id = AttributeBonus
dependencies = [Attribute.BaseHp, Attribute.HpBonus]
compute_params = {...}
```

C# 中注册：

```text
AttributeBonus -> AttributeBonusComputeResolver
```

### 4.15 `IsComputed`

不作为独立事实源。推荐由以下字段推导：

```text
storage_policy = computed
```

或：

```text
compute_id 非空
```

不要同时维护 `is_computed` 和 `compute_id` 两套可能矛盾的事实源。snapshot 中可以保留只读 mirror 方便调试，但 validator 要以单一规则为准。

## 5. 新 `DataDefinition` 分层模型

不推荐一个大 `DataDefinition` 塞下全部字段。推荐分层：

```text
DataDefinition
  Core
    StableKey
    ValueType
    RuntimeTypeId
    DefaultValue
    StoragePolicy
    WritePolicy
  RuntimePolicy
    RangePolicy
    MinValue
    MaxValue
    ModifierPolicy
    AllowedValues
  Compute
    ComputeId
    Dependencies
    Params
  Migration
    MigrationPolicy
  Presentation
    DisplayName
    Description
    OwnerDomain
    UiGroup
    ResetGroup
    Unit
    Format
    IconPath
```

### 5.1 core definition

核心字段：

| 字段 | 必要性 | 说明 |
| ---- | ---- | ---- |
| `stable_key` | 必需 | 稳定协议 key |
| `value_type` | 必需 | Data 层基础类型 |
| `runtime_type_id` | 可选 | C# / Godot 类型补充 |
| `default_value` | 必需或显式空 | 玩法默认值 |
| `storage_policy` | 必需 | 是否持久化、运行时-only、computed |
| `write_policy` | 必需 | 是否允许普通写入 |

### 5.2 storage policy

推荐枚举：

| policy | 适用例子 | 含义 |
| ---- | ---- | ---- |
| `persisted` | `Attribute.BaseHp` | 可进 snapshot records |
| `runtime_state` | `Attribute.CurrentHp` | 运行时状态，可初始化但局内变化频繁 |
| `runtime_only` | `AI.TargetNode` | 不应持久化运行时对象引用 |
| `computed` | `Attribute.FinalHp` | 只读派生字段 |
| `authoring_blob` | `Feature.Modifiers` | authoring 复杂结构，生成器转换或运行时特殊处理 |

### 5.3 write policy

推荐枚举：

| policy | 含义 |
| ---- | ---- |
| `read_write` | 普通字段 |
| `loader_only` | 只能由 loader 初始化 |
| `system_only` | 只能由指定系统写入 |
| `computed_readonly` | computed 字段，只读 |
| `debug_only` | 只允许调试工具写入 |

### 5.4 runtime policy

推荐字段：

```text
range_policy
min_value
max_value
modifier_policy
allowed_values
```

这些字段进入运行时 catalog，但 UI 字段不需要进入热路径。

### 5.5 presentation

推荐字段：

```text
display_name
description
owner_domain
ui_group
reset_group
unit
format
icon_path
```

这些字段用于 AI authoring、编辑器、调试面板和文档生成，不参与 `Data.Get/Set` 核心路径。

## 6. Feature 相关数据如何保留功能

### 6.1 Feature 不废弃

本设计不是废弃 Feature，而是明确边界：

```text
FeatureSystem = 生命周期 / 效果执行 / Modifier 授予回滚。
DataComputeRegistry = Data 派生值纯计算。
```

### 6.2 `FeatureModifiers` 必须正式进入 Data 定义体系

当前 `DataKey_Feature.cs` 中：

```csharp
public const string FeatureModifiers = "FeatureModifiers";
```

它是裸字符串，并且注释说明“绕过约束系统”。这在新 Data 系统中应该删除裸字符串入口，修正为正式 descriptor：

```text
stable_key = Feature.Modifiers
value_type = modifier_list
runtime_type_id = FeatureModifierList
storage_policy = authoring_blob
write_policy = loader_only
```

这样可以重新实现现有 Feature modifier 功能，同时让 AI 和 validator 能理解这个字段。

### 6.3 `FeatureModifierEntry.DataKeyName` 需要验证

当前字段是裸字符串：

```csharp
DataKeyName
```

新系统 validator 应检查：

- 目标 key 存在。
- 目标 key 是数值字段。
- 目标 key 的 `modifier_policy` 允许 modifier。
- modifier 类型和值符合目标字段范围策略。

这样可以避免 AI 或人类在 Feature 配置里写错目标字段。

### 6.4 Feature 数据迁移不能丢的能力

必须在新系统中重新实现：

- `FeatureHandlerId` 对 handler 的绑定。
- `FeatureEnabled` / `FeatureIsActive` 状态。
- `FeatureActivationCount` 计数。
- `FeatureCategory` 展示分组。
- `FeatureDefinition.Modifiers` 授予时自动应用。
- Feature 移除时按 source 自动回滚 modifier。
- Ability 通过 `FeatureSystem.OnFeatureActivated` 执行 handler 并返回结果。

这些属于 Feature 生命周期能力，不属于 Data computed。

## 7. 新 Data 系统完整性要求

### 7.1 基础 Data 能力

- generated typed `DataKey<T>` handle。
- 内部 string API，仅限 loader、测试 fixture 和一次性审计工具。
- 默认值 fallback。
- untyped snapshot apply。
- 严格类型转换。
- enum / string enum / runtime type 映射。
- 复杂运行时对象的 `runtime_only` 策略。

### 7.2 Modifier 能力

- `Additive`。
- `Multiplicative`。
- `FinalAdditive`。
- `Override`。
- `Cap`。
- `Priority`。
- `Source`。
- `RemoveModifiersBySource`。
- Feature 授予/移除自动应用和回滚。

### 7.3 Computed 能力

- `compute_id`。
- `dependencies`。
- `compute_params`。
- resolver 注册表。
- computed cache。
- transitive dirty。
- 循环依赖检测。
- computed 字段禁止普通 `Set`。
- resolver 缺失 fail fast。

### 7.4 Tooling / Debug 能力

- 显示名。
- 描述。
- UI 分组。
- reset group。
- 范围输入。
- 下拉选项。
- 百分比展示。
- 临时 modifier 调试。
- Feature modifier target key 校验。
- 结构化 validator 报告。

### 7.5 Migration 能力

- 默认安全迁移。
- `migration_policy`。
- profile include / exclude。
- 排除 `Node` / `IEntity` / `IComponent` / `Delegate` / `EventBus` 等危险引用。

## 8. 推荐执行裁决

### 8.1 不做“DataMeta 全量数据库化”

不要把旧 `DataMeta` 字段平铺复制到数据库。这会把旧问题换个位置继续保留。

错误方向：

```text
DataMeta 字段全复制到 data_key_descriptor。
DataDefinition 继续是一个巨型对象。
DisplayName / Category / IsPercentage 全部进入 Data 热路径。
FeatureSystem 被拿来执行 Data computed。
```

### 8.2 做“descriptor-first + 分层 DataDefinition”

正确方向：

```text
DataOS descriptor
  -> runtime_snapshot.json.descriptors
  -> DataDefinitionCatalog
  -> DataSlot / Data
  -> DataComputeRegistry
```

其中：

- `DataKey<T>` 只是 generated typed handle。
- `DataMeta` 只作为一次性审计输入，不保留迁移期 adapter 或运行时 fallback。
- `FeatureSystem` 保持生命周期和 modifier 机制。
- `DataComputeRegistry` 专职 computed 纯计算。
- presentation 字段保留给 AI / UI / debug，但不污染 Data hot path。

### 8.3 第一批执行型 SDD 应补的验证

除 `README.md` 中已有测试外，第一批执行型 SDD 还应增加：

```text
BuildCatalog_ShouldRejectComputedWithoutComputeId
BuildCatalog_ShouldRejectComputeCycle
BuildCatalog_ShouldRejectPresentationOnlyFieldInHotPath
BuildCatalog_ShouldValidateFeatureModifierTargetKey
Data_Set_ShouldRejectComputedReadonlyKey
Data_Set_ShouldRespectWritePolicy
Data_AddModifier_ShouldRejectKeyWithoutModifierPolicy
FeatureModifiers_ShouldBeRepresentedAsAuthoringBlobDefinition
Audit_ShouldReportDataNewReference
Audit_ShouldReportDataDataReference
Audit_ShouldReportOldDataTestSceneReference
```

## 9. 最终一句话

Data 重构不是“DataMeta 换成数据库表”，也不是“Feature 替代计算函数”。

最终目标是：

```text
DataDefinition 描述字段契约。
RuntimePolicy 描述字段如何被写入、限制、修改和迁移。
ComputeResolver 描述字段如何纯计算。
Presentation 描述字段如何被 AI、人类和工具理解。
FeatureSystem 描述能力如何被授予、激活、执行和回滚。
```

这样是在新 Data 系统内重新实现旧 ECS 需要的 Data 能力，同时删除旧 Data 输入路径，避免冗余和职责混乱。
