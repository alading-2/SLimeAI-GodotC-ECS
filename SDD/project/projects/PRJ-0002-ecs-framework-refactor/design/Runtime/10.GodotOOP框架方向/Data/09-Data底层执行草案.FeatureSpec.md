# Data 底层执行草案 FeatureSpec

> 状态：draft / candidate
> Source Design：[`README.md`](./README.md)、[`07-OOP中数据定义与运行时管理方案.md`](./07-OOP中数据定义与运行时管理方案.md)、[`08-Command与数据修改入口.md`](./08-Command与数据修改入口.md)
> 用户原始问题：[`source-request.md`](./source-request.md)

## 这份执行文档先回答什么

用户现在的问题不是“Data 方向对不对”，而是：

```text
你大概要怎么改？
代码会落在哪里？
哪些东西先不做？
```

本草案只写第一阶段执行方向：**先确定框架 Data 底层**。

明确不做：

- 不做数据库怎么传到 Data。
- 不做 DataOS record / snapshot 怎么加载到 Data。
- 不做 RuntimeRecordBinder。
- 不做全部字段 authority 审计。
- 不全仓迁移所有 `Data.Set`。
- 不把 Health/Damage/Recovery 全部重构完。

也就是说，本阶段先让 Data runtime 自己具备“字段权威、写入口、projection、owner guard、modifier 限制”的底层形状。后面数据库到 Data 的链路只能调用这个底层形状，不能再绕开。

## 当前代码里的问题点

### 1. `Data` 现在仍像一个万能可写容器

当前入口在：

```text
Src/ECS/Runtime/Data/Data.cs
Src/ECS/Runtime/Data/DataRuntimeStorage.cs
```

`Data.Set(DataKey<T>, T)` 默认就是 `DataWriteSource.Runtime`。这能执行 `writePolicy`，但它回答不了：

```text
谁有资格写 CurrentHp？
谁有资格写 AttackState projection？
这个字段是 Data 事实源，还是 Component/System 投影？
```

所以只靠 `DataWritePolicy.ReadWrite / SystemOnly / LoaderOnly` 不够。

### 2. 旧注释还在诱导错误方向

`Data.cs` 顶部仍写着：

```text
Data 是唯一数据源，所有数据统一从 Data 容器访问。
```

这和当前裁决冲突。第一阶段应先改这个事实源注释，避免后续 AI 继续照旧理解。

### 3. `DataWriteSource` 只有来源类型，没有 owner 身份

当前：

```csharp
public enum DataWriteSource
{
    Runtime,
    Loader,
    System,
    Debug
}
```

它能区分“普通运行时 / loader / system / debug”，但不能区分：

```text
Health owner 写 CurrentHp
RecoverySystem 写 CurrentMana
AttackComponent 投影 AttackState
UI/debug/test 试图写 gameplay state
```

所以第一刀不应该直接删 `Data.Set`，而是加一个更细的写入上下文。

### 4. projection 没有专门写入口

当前 `Data.TrySetSystem(TargetNode, ...)` 已经表达“系统写入”，但没有表达：

```text
这是 projection。
只有 projectionSource owner 可以写。
外部不能通过普通 Data.Set 改它。
modifier 不允许作用于 projection。
```

这就是 `AttackState / MoveMode / AIState` 容易被误写成 Data authoritative 的根因。

### 5. Health 样板仍混合了旧入口

当前例子：

```text
HealthComponent.ApplyHeal -> _data.Set(CurrentHp)
HealthComponent.ApplyDamage -> _data.Set(CurrentHp)
RecoverySystem -> data.Set(CurrentMana)
LifecycleComponent -> _data.Set(CurrentHp)
Ability CostComponent -> CurrentMana / CurrentHp cost
```

这些不一定都错，但它们需要被分类：

- owner 内部写，可以保留但应显式。
- 非 owner 写，要改为 Command / owner API。
- test/debug 写，要走 debug/test 边界。

第一阶段先定义底层 API 和一个 Health 样板，不马上全仓改完。

## 总体改法

第一阶段建议加一层很小的底层 contract：

```text
DataMutationContext
DataFieldAuthority
DataBindingPolicy
DataWriteEntry
DataProjectionSource
DataMutationResult / DataWriteReport 扩展
DataOwnerWrite API
DataProjectionWrite API
```

它们先可以只存在 C# runtime，不急着进数据库 schema。

原因：

```text
先让框架 Data 底层能表达正确语义。
再让 DataOS descriptor / snapshot / binder 去喂这些语义。
```

如果反过来先改数据库，会把还没想清楚的底层概念固化进 schema。

## Feature List

| ID | Feature | Priority | Status | Notes |
| --- | --- | --- | --- | --- |
| FS-1 | Data 底层权威模型 | P0 | draft | 先在 C# runtime 表达 authority / bindingPolicy，不先改 DB。 |
| FS-2 | Owner 写入口 | P0 | draft | 普通 `Data.Set` 保留但降级，新增 owner/context 写入口。 |
| FS-3 | Projection 写入口 | P0 | draft | Component/System authoritative 字段只允许 owner 投影。 |
| FS-4 | Modifier 限制收窄 | P0 | draft | 只允许 Data authoritative + numeric modifier 字段。 |
| FS-5 | Health 样板切片 | P1 | draft | 只做 CurrentHp/CurrentMana 的写入口分类和最小测试。 |

## FS-1: Data 底层权威模型

### Goal

让 `DataDefinition` 能在 runtime 层表达：

```text
这个字段谁是事实源？
它在 Data 中是存储值、mirror、projection 还是 diagnostic？
谁能写？
对象池 reset 时怎么处理？
```

### Behavior

- Data authoritative 字段可以通过 owner/context API 修改。
- Component/System authoritative 字段默认不能通过普通 Runtime 写入。
- projection 字段只能通过 projection owner 写入。
- modifier 只作用于 Data authoritative numeric 字段。

### Implementation Guidance

Owner:

- Runtime Data owner。

Key files / areas:

- `Src/ECS/Runtime/Data/DataDefinition.cs`
- `Src/ECS/Runtime/Data/DataValueType.cs`
- `Src/ECS/Runtime/Data/DataRuntimeStorage.cs`
- `Src/ECS/Runtime/Data/Data.cs`
- `Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.cs`

建议新增 enum / value object：

```csharp
public enum DataFieldAuthority
{
    Data,
    Component,
    System,
}

public enum DataBindingPolicy
{
    None,
    Initial,
    Changed,
    Projection,
    Diagnostic,
}

public readonly record struct DataOwnerId(string Value)
{
    public static DataOwnerId RuntimeData = new("RuntimeData");
    public static DataOwnerId Loader = new("Loader");
    public static DataOwnerId Debug = new("Debug");
}
```

建议先扩展 `DataDefinition`：

```csharp
public DataFieldAuthority Authority { get; init; } = DataFieldAuthority.Data;
public string RuntimeOwner { get; init; } = string.Empty;
public DataBindingPolicy BindingPolicy { get; init; } = DataBindingPolicy.None;
public string WriteEntry { get; init; } = string.Empty;
public string ResetPolicy { get; init; } = string.Empty;
public string ProjectionSource { get; init; } = string.Empty;
```

这些字段第一阶段由测试手写 catalog 填充，不改 DataOS 生成器。

Forbidden:

- 不在第一阶段改 SQLite schema。
- 不要求 runtime snapshot descriptor 立刻带这些字段。
- 不把 enum 文本字符串散落在业务代码。

### TDD Handoff

expectedInputs:

- 手写 `DataDefinitionCatalog`，包含：
  - `CurrentHp`: `Authority=Data`, `BindingPolicy=Changed`
  - `AttackState`: `Authority=Component`, `BindingPolicy=Projection`, `ProjectionSource=AttackComponent`
  - `Unit.Name`: `Authority=Data`, `ModifierPolicy=None`

expectedObservations:

- `CurrentHp` 可通过 owner write 成功。
- `AttackState` 普通 runtime write 被拒绝。
- `AttackState` projection owner write 成功。
- `Unit.Name` modifier 被拒绝。

passCriteria:

- `DataRuntimeTestScene` 或新增 runtime test scene 输出 PASS。
- 错误 report 包含 stableKey、authority、bindingPolicy、writerOwner。

## FS-2: Owner 写入口

### Goal

不再让所有业务都通过裸 `Data.Set` 表达写操作。第一阶段先新增显式 owner 写入口，后续再迁移调用点。

### Behavior

推荐目标调用形态：

```csharp
data.TrySetOwned(
    GeneratedDataKey.CurrentHp,
    newHp,
    DataOwnerId.From("Health"),
    out var report);
```

或封装到 owner API：

```csharp
HealthDataWriter.TrySetCurrentHp(entity, newHp, HealthMutationReason.Damage, out result);
```

第一阶段不要强制所有调用点立刻迁移。先做到：

- 新 owner API 存在。
- 新测试使用新 API。
- 文档禁止新增裸 `Data.Set(CurrentHp)`。

### Implementation Guidance

建议新增：

```csharp
public readonly record struct DataMutationContext(
    DataWriteSource Source,
    DataOwnerId Owner,
    string Entry,
    bool IsProjection = false);
```

`DataRuntimeStorage.TrySet` 增加 overload：

```csharp
public bool TrySet<T>(
    DataKey<T> key,
    T value,
    DataMutationContext context,
    out DataWriteReport report)
```

原有 API 保留：

```csharp
public bool Set<T>(DataKey<T> key, T value)
```

但其语义变成 legacy/runtime shorthand。后续 grep gate 逐步限制业务字段。

DataWriteReport 增加：

```csharp
public DataOwnerId Owner { get; }
public string Entry { get; }
```

或先加 optional fields，避免大面积构造函数改动。

Forbidden:

- 不让 UI / AI / unrelated system 直接拿 `Data.Set(CurrentHp)` 改血。
- 不用 Event 代替 owner write。Event 是事实或广播，Command/owner API 才是写入口。

## FS-3: Projection 写入口

### Goal

解决 `AttackState / MoveMode / AIState` 这类字段“Data 里有，但 Data 不是事实源”的问题。

### Behavior

目标调用形态：

```csharp
data.TryProject(
    GeneratedDataKey.AttackState,
    AttackState.WindUp,
    DataOwnerId.From("AttackComponent"),
    out var report);
```

规则：

- `BindingPolicy=Projection` 的字段，普通 `TrySetOwned` 拒绝。
- `TryProject` 只有 owner 等于 `ProjectionSource` 或 `RuntimeOwner` 时成功。
- projection 字段不允许 modifier。
- projection 字段对象池 reset 时跟随 owner 清理，不由 DataOS record 初始化为长期真值。

### Implementation Guidance

建议新增：

```csharp
public bool TryProject<T>(
    DataKey<T> key,
    T value,
    DataOwnerId projectionOwner,
    out DataWriteReport report)
```

内部转换为：

```csharp
new DataMutationContext(
    DataWriteSource.System,
    projectionOwner,
    "projection",
    IsProjection: true)
```

DataRuntimeStorage 校验：

```text
definition.BindingPolicy == Projection
context.IsProjection == true
context.Owner matches definition.ProjectionSource or definition.RuntimeOwner
```

如果字段不是 projection，`TryProject` 返回 `projection_policy_rejected`。

## FS-4: Modifier 限制收窄

### Goal

让 `DataModifier` 只作用于 attribute-like numeric Data authoritative 字段。

### Behavior

允许：

- `MoveSpeed`
- `BaseAttack`
- `DamageReduction`
- `AttackRange`

拒绝：

- `AttackState`
- `MoveMode`
- `Velocity`
- `AIState`
- `TargetNode`
- `Unit.Name`
- `object_ref`
- `string`
- `enum projection`

### Implementation Guidance

当前 `CanApplyModifier(definition, source)` 已经检查 `ModifierPolicy` 和数值类型。第一阶段要补：

```text
definition.Authority == Data
definition.BindingPolicy != Projection
```

也就是即使某个 projection 是 enum/int，也不能 modifier。

TDD：

- projection numeric 字段 `Movement.SpeedProjection` 即使 `ModifierPolicy=Numeric` 也应拒绝。
- Data authoritative numeric 字段 `MoveSpeed` 成功。

## FS-5: Health 样板切片

### Goal

用 `CurrentHp / CurrentMana` 证明底层写入口能工作，但不在第一阶段完成完整 Health 重构。

### 当前代码落点

```text
Src/ECS/Capabilities/Unit/Component/Common/HealthComponent/HealthComponent.cs
Src/ECS/Capabilities/Unit/System/RecoverySystem/RecoverySystem.cs
Src/ECS/Capabilities/Damage/System/Processors/HealthExecutionProcessor.cs
Src/ECS/Capabilities/Ability/Component/CostComponent/CostComponent.cs
Src/ECS/Capabilities/Unit/Component/Common/LifecycleComponent/LifecycleComponent.cs
```

### 第一刀建议

新增一个很薄的 owner API，不急着设计完整 Profile：

```csharp
public static class HealthDataWriter
{
    public static bool TrySetCurrentHp(
        IEntity entity,
        float newHp,
        HealthMutationReason reason,
        out HealthMutationResult result);

    public static bool TrySetCurrentMana(
        IEntity entity,
        float newMana,
        HealthMutationReason reason,
        out HealthMutationResult result);
}
```

`HealthMutationReason` 先简单：

```csharp
public enum HealthMutationReason
{
    Damage,
    Heal,
    Recovery,
    AbilityCost,
    Revive,
    Debug,
    Test,
}
```

`HealthMutationResult`：

```csharp
public readonly record struct HealthMutationResult(
    bool Applied,
    float OldValue,
    float NewValue,
    string ReasonCode);
```

第一阶段迁移样板：

- `HealthComponent.ApplyDamage` 内部改用 `HealthDataWriter.TrySetCurrentHp`。
- `HealthComponent.ApplyHeal` 内部改用 `HealthDataWriter.TrySetCurrentHp`。
- `RecoverySystem` 的 `CurrentMana` 写入改用 `HealthDataWriter.TrySetCurrentMana` 或先标 TODO，不强行一次改完。

暂不改：

- 数据库字段。
- DataInitComponent 的初始化。
- LifecycleComponent 全部复活逻辑。
- Ability CostComponent 全部资源消耗逻辑。

原因：这些牵涉更多 owner 边界，第一阶段先证明底层写入口，不把业务重构混进去。

### TDD Handoff

expectedInputs:

- 创建测试 entity，绑定 Data catalog。
- 初始 `CurrentHp=100`、`FinalHp=100`、`CurrentMana=20`、`FinalMana=50`。

expectedObservations:

- owner write 把 `CurrentHp` 从 100 改到 70，触发 typed `DataChanged< float >`。
- 普通 projection write 尝试改 `AttackState` 被拒绝。
- HealthDataWriter 返回 old/new/result。

passCriteria:

- Data runtime test PASS。
- grep 可以列出 `Data.Set(CurrentHp|CurrentMana)` 调用点，并分类 owner/test/debug/待迁移。

## 第一阶段文件改动草案

可能改动：

```text
Src/ECS/Runtime/Data/DataDefinition.cs
Src/ECS/Runtime/Data/DataValueType.cs
Src/ECS/Runtime/Data/DataRuntimeStorage.cs
Src/ECS/Runtime/Data/Data.cs
Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.cs
Src/ECS/Capabilities/Unit/System/HealthDataWriter.cs
Src/ECS/Capabilities/Unit/Component/Common/HealthComponent/HealthComponent.cs
DocsAI/ECS/Runtime/Data/Data系统说明.md
```

暂不改：

```text
Data/DataOS/Schema/
Data/DataOS/Migrations/
Data/DataOS/Generators/
Data/DataOS/Snapshots/runtime_snapshot.json
Data/DataKey/Generated/DataKey_Generated.cs
RuntimeRecordBinder
ObjectFactory / ObjectAssembler
```

## 第一阶段 grep gate

先分类，不要求清零：

```bash
rg -n "Data\\.Set\\(GeneratedDataKey\\.(CurrentHp|CurrentMana)" Src/ECS Data -g '*.cs'
rg -n "Data\\.Set\\(GeneratedDataKey\\.(AttackState|MoveMode|AIState|Velocity)" Src/ECS Data -g '*.cs'
rg -n "TryAddModifier|AddModifier\\(" Src/ECS Data -g '*.cs'
```

分类目标：

- Health owner 内部写：迁到 `HealthDataWriter`。
- Test/debug 写：保留，但命名成 test/debug 边界。
- projection 写：改 `TryProject`。
- 非 owner gameplay 写：后续 SDD 迁移。

## 需要确认

1. 第一阶段是否接受“先在 C# runtime 手写 `authority/bindingPolicy` 字段，不改数据库 schema”？

为什么问：这能先定 Data 底层，不会把未稳定的方案固化进 DataOS。

默认值：接受。

2. Health 样板是否先叫 `HealthDataWriter`，还是更希望叫 `UnitResourceWriter`？

为什么问：`CurrentMana` 不完全属于 Health，但首切片放一起能验证 HP/Mana 两类共享资源写入口。

默认值：先叫 `HealthDataWriter`，后续如果 Mana/Energy/Ammo 扩大，再拆 `UnitResourceWriter`。

3. 第一阶段是否允许旧 `Data.Set(CurrentHp|CurrentMana)` 调用点暂时存在，只做 grep 分类和新增禁用规则？

为什么问：一次全仓迁移会把底层设计、Health、Lifecycle、Ability cost、Recovery 混成大任务。

默认值：允许暂存，首切片只迁 HealthComponent 内部样板。
