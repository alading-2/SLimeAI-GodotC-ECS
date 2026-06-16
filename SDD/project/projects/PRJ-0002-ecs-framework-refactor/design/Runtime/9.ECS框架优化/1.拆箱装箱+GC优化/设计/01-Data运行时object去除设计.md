# Data 运行时泛型存储设计

## 原始结论与当前状态

Data 必须改，而且应按 hard cutover 改。这是本设计在 `SDD-0031` 实施前的原始裁决。

`SDD-0031` 前，Data 已经在事实源层完成 DataOS descriptor、runtime snapshot、generated `DataKey<T>`，调用侧也已经是 `Data.Get/Set<T>(DataKey<T>)`，但运行时值存储仍回到 `object?`。这会让 typed handle 的 AI-first 收益在热路径被抵消。

`SDD-0031` 后，Data runtime 主存储已经切到 `DataSlot<T> + IDataSlot`，因此旧的 `DataSlot.Value object?` 和 `_computedCache Dictionary<string, object?>` 不再是当前主链路问题。当前剩余问题变为：哪些 object 属于合理边界，哪些 object 仍会破坏 typed contract，哪些业务 string/untyped 调用必须优先收口。

用户判断“Data 的 object 问题比较大”成立。Data 是 ECS 运行时状态容器，读写频率远高于大部分工具层 API，不能继续把 `object?` 当主链路。

用户对上一版设计的批评也成立：不应再用一个带 `IntValue/FloatValue/BoolValue/...` 多字段的 `DataRuntimeValue` union 替代 `object?`。那只是把“一个 object 存任意类型”的问题换成“一个结构体携带所有可能类型的字段”，会引入冗余字段、额外分发、调试歧义和未来类型扩张成本。SlimeAI 既然已经有 `DataKey<T>` 和 `Data.Get/Set<T>`，主链路就应继续走泛型。

用户已确认 `DataSlot<T> + IDataSlot` 是当前最优方案。本设计以此作为最终裁决：`DataSlot<T>` 保存真实业务值，`IDataSlot` 只作为跨类型 slot 管理和 diagnostics 边界；不再把 `DataRuntimeValue` union、`object? Value` 或多字典拆分作为同级候选。

## SDD-0031 后复查：object 是否还存在

存在。`SDD-0031` 去掉的是 Data runtime 业务值主链路中的 object slot，不是把所有 `object?` 从仓库里删除。

当前应按性质区分：

1. **已解决的问题**
   - `DataSlot.Value object?` 不再作为基础值存储。
   - `Data.Get/Set<T>(DataKey<T>)` 不再回落到 `TrySetUntyped(key.StableKey, value, ...)`。
   - computed cache 不再是 `_computedCache Dictionary<string, object?>`，而是进入 typed slot。
   - modifier 有效值写回 typed slot，不再把最终业务值保存在 object 槽里。

2. **允许存在的边界 object**
   - `RuntimeDataSnapshot` / descriptor DTO / record DTO 的 `object?`：这是 JSON、SQLite、snapshot 输入边界。
   - `SetUntyped(... object?)`：这是 loader、debug、TestSystem 边界。
   - `GetAll()` / diagnostics 读取：这是跨类型 dump 边界。
   - `DataValueConverter.TryConvert(object?)`：这是边界转换器。

3. **需要后续收口的协议债务**
   - `DataChangeRecord(string, object?, object?)` 和 `GameEventType.Data.PropertyChanged(string, object?, object?)`。
   - `IDataComputeResolver.Compute(): object?`。
   - `DataModifier.Source object?` / `RemoveModifiersBySource(object)`。
   - `DataDefinition.DefaultValue object?` 在 runtime 读取默认值时仍可能反复转换，应投影或缓存为 typed default。

4. **需要优先修复的业务误用**
   - 业务 Capability 中继续调用 `Data.Get<T>(string)`、`Data.Add(string, ...)`、`TrySetUntyped(stableKey, ...)`。
   - 这类调用比 DTO 中的 object 更危险，因为它让 AI 和人重新绕过 generated `DataKey<T>`。

因此，`object 还存在` 不等于方案失败；真正的判断标准是 object 是否仍作为业务状态主协议。

## 是否真的有必要继续修改

从框架开发者角度，继续修改是有必要的，但不能按“看到 object 就删”的方式推进。

### 必须继续改的部分

#### 1. 业务 string/untyped Data API 调用

这类调用必须优先处理。原因不是单纯装箱，而是契约破坏：

- 它绕过 DataOS descriptor 生成的 `DataKey<T>`。
- 它允许 `"CurrentEnergy"`、`"CurrentAmmo"` 这类未定义字段潜入业务。
- 它给 AI 提供了错误示例，后续会继续复制裸字符串访问。
- 它让 unknown key / wrong type 从编译期或生成期推迟到运行时。

推荐裁决：

```text
业务 Capability 禁止新增 Data.Get<T>(string)、Data.Set<T>(string)、Data.Add(string)、TrySetUntyped(string)。
现有调用按 owner 清理；TestSystem、loader、migration 可保留边界例外。
```

能力资源消耗这类逻辑应改为 typed resolver：

```csharp
private static bool TryGetResourceKey(AbilityCostType type, out DataKey<float> key)
{
    switch (type)
    {
        case AbilityCostType.Mana:
            key = GeneratedDataKey.CurrentMana;
            return true;
        case AbilityCostType.Health:
            key = GeneratedDataKey.CurrentHp;
            return true;
        default:
            key = default;
            return false;
    }
}
```

如果 Energy / Ammo 是未来能力，应先进入 DataOS descriptor，再生成 `GeneratedDataKey.CurrentEnergy` / `CurrentAmmo`；在字段未定义前，不应通过裸字符串临时支持。

#### 2. Data changed 事件 typed 化

`PropertyChanged(object?)` 可以保留给 diagnostics，但不能作为业务主事件。

需要改的原因：

- 每次值类型 old/new 进入 object event 都会装箱。
- 监听方需要 runtime cast，AI 不知道 payload 类型。
- UI、Damage、Recovery、Feature 如果订阅这个事件，会把 Data 的 typed contract 重新打散。

推荐终局：

```text
DataChanged<T>(DataKey<T> key, T oldValue, T newValue)
HealthChanged(float oldHp, float newHp)
DataDiagnosticChange(stableKey, valueType, oldText, newText)
```

短期裁决：

```text
PropertyChanged(object?) 是 Event/diagnostic 边界，不作为 Data runtime typed 化完成证明。
```

#### 3. Computed resolver typed 化

`IDataComputeResolver.Compute(): object?` 当前作为 SDD-0031 范围控制可以接受，但它不是终局。

需要改的原因：

- computed 是 Data 核心运行时能力，不是 debug 边界。
- resolver 输出类型错误会推迟到运行时转换。
- 高频 computed 会继续产生 object 返回值和转换成本。

推荐后续：

```csharp
public interface IDataComputeResolver<T>
{
    string ComputeId { get; }
    T Compute(Data data, DataFieldDefinition<T> definition);
}
```

`DataComputeRegistry` 可以继续用非泛型 dictionary 管 metadata，但 bind / catalog build 时必须验证 output T。

#### 4. typed default cache / typed runtime field

`DataDefinition.DefaultValue object?` 在 DTO 层可以保留，但 runtime 读取默认值不应每次从 object 转换。

推荐拆法：

```text
P0.5：DataSlot<T> 创建时缓存 typed default。
P1：Catalog build 投影 DataFieldDefinition<T>。
P2：generated field id / array-backed storage。
```

这项值得做，因为默认值读取会发生在很多未显式写入字段上；它不是最危险的契约问题，但属于 Data runtime 完整性问题。

#### 5. DataModifier.Source typed 化

`DataModifier.Source object?` 不直接存 Data 值，但它是 Feature / Buff 回滚的身份协议。长期用 object 会让来源语义不稳定。

推荐后续改为稳定 source id：

```text
FeatureInstanceId
EntityId
string sourceId
```

短期可以保留，因为它不是数值热读写主链路；但 Feature owner 需要把它列入后续 typed context 工作。

### 不建议急着改的部分

#### 1. Snapshot / DTO object

不建议把 `RuntimeDataRecordDto.Value object?`、`RuntimeDataDescriptorDto.DefaultValue object?` 急着泛型化。

原因：

- JSON / SQLite 输入天然没有 C# 泛型类型。
- 强行泛型化 DTO 会让 loader 复杂度上升。
- 只要 apply 阶段按 descriptor fail-fast 并写入 typed slot，边界 object 是合理的。

#### 2. Diagnostic dump object

`GetAll()` 返回 `Dictionary<string, object>` 不适合作为业务 API，但 debug/TestSystem 需要跨类型展示。

更好的方向不是立即删除，而是改语义和命名：

```text
GetAll() -> GetDiagnosticSnapshot()
DataDiagnosticValue { stableKey, valueType, text, boxedValue? }
```

业务 grep gate 禁止调用即可。

#### 3. Runtime object_ref 的特殊边界

`TargetNode` 这类 runtime-only Godot Node 引用不是普通持久化值。它可以有受控边界，但必须满足：

- generated handle 是 `DataKey<Godot.Node2D>`。
- descriptor 标明 `runtime_only` / `system_only`。
- 写入入口说明来源，例如 System 写入。
- 生命周期失效由 owner capability 负责校验。

如果因为 write policy 需要 `DataWriteSource.System` 而临时走 `TrySetUntyped(stableKey, node, System)`，它应被记录为 system boundary，不应扩散成普通业务写法。更好的后续 API 是：

```csharp
Data.TrySetSystem(GeneratedDataKey.TargetNode, node2D, out report);
```

这样既保留 system 写入来源，又不丢 `DataKey<T>`。

## 推荐后续优先级

| 优先级 | 工作 | 原因 |
| --- | --- | --- |
| P0 | 清理业务 `Data.Get/Set/Add(string)` 和 untyped 调用 | 这是当前最容易让 object/string 回流业务主链路的问题。 |
| P0 | 给 Data API 加 grep gate / analyzer-style 文档门禁 | 防止 AI 后续新增裸字符串访问。 |
| P1 | typed Data changed / domain event 分层 | Event object 是 Data typed 化后的最大协议债务。 |
| P1 | `IDataComputeResolver<T>` | computed 是 Data 核心能力，应 typed。 |
| P1 | typed default cache / `DataFieldDefinition<T>` | 减少默认值热读转换，提升 runtime 完整性。 |
| P1 | `DataModifier.Source` typed source id | 与 Feature typed context 一起处理。 |
| P2 | field id / array-backed generated storage | 性能优化，需 benchmark 驱动。 |
| 不做 | 全仓零 object | 过度设计；边界 object 合理存在。 |

## 当前完成度裁决

`SDD-0031` 可以判定为完成 `Data runtime generic slot hard cutover`，但不能描述为“Data 系统已完全无 object”。

更准确的完成态：

```text
Data 业务值主存储已从 object slot 切到 DataSlot<T>；
loader/debug/diagnostic/Event/computed registry/source identity 仍保留 object 边界；
其中 Event、computed、source identity 和业务 string/untyped 调用是后续 typed contract 工作。
```

## 当初为什么这么设计

从现有 DocsAI 和 PRJ-0002 记录看，Data 先解决的是事实源漂移：

```text
DataOS descriptor -> runtime_snapshot.json -> DataDefinitionCatalog -> generated DataKey<T> -> Data.Get/Set
```

这一步的重点是删除旧 `DataMeta` / `DataRegistry` / 手写 key，避免 AI 不知道字段定义在哪里。`object?` 在当时承担了三类便利：

- loader 可以把 JSON / SQLite / Godot 输入统一送进 `DataValueConverter`。
- 一个 `DataSlot` 可以存所有类型，避免短期内重写 modifier / computed / changed event。
- 人工调试和 TestSystem 可以用 `SetUntyped` / `GetAll` 快速检查数据。

这些便利帮助 Data 完成第一轮 descriptor-first hard cutover，但它不是最终形态。当前 AI-first 目标已经从“字段定义统一”推进到“运行时契约也要 typed、热路径也要可验证”。

## 原始源码证据（SDD-0031 前）

下表是本设计创建时推动 hard cutover 的历史证据。`SDD-0031` 完成后，`DataSlot.Value object?`、`GetEffectiveValue(): object?`、`_computedCache Dictionary<string, object?>` 已不应再作为当前 runtime 主链路问题描述；它们保留在这里用于解释为什么当初必须 hard cutover。

| 文件 | 证据 | 风险 |
| --- | --- | --- |
| `Src/ECS/Runtime/Data/DataRuntimeStorage.cs` | `DataChangeRecord(string StableKey, object? OldValue, object? NewValue)` | 每次变更事件都保留 object payload |
| `DataRuntimeStorage.cs` | `DataSlot.Value object?`、`GetEffectiveValue(): object?`、`SetValue(object?)` | 所有值类型字段写入/读取都会穿过 object |
| `DataRuntimeStorage.cs` | `ConvertNumericToDefinitionType` 返回 `(object)(float/int/double)` | modifier 计算后重新装箱 |
| `DataRuntimeStorage.cs` | `DataValueConverter.ConvertInt/Float/Bool/Vector2` 返回 `object` | 写入转换阶段丢失 typed 返回 |
| `DataRuntimeStorage.cs` | `ConvertForRead(object?, Type, DataValueType)` 和 `Activator.CreateInstance` | 读取阶段靠 runtime type 判断 |
| `DataRuntimeStorage.cs` | `_computedCache Dictionary<string, object?>`、`IDataComputeResolver.Compute(): object?` | computed 输出仍是 object |
| `Data.cs` | `SetUntyped(... object?)`、`GetAll(): Dictionary<string, object>` | 宽口 API 仍可被业务误用；SDD-0031 后仍需作为边界收口 |
| `GameEventType_Data.cs` | `PropertyChanged(string Key, object? OldValue, object? NewValue)` | Data changed event 与 Event object 问题耦合 |

## 终局设计目标

1. Data runtime hot path 不再使用 `object?` 存储数值和常见值类型。
2. `DataKey<T>` 到 runtime storage 的泛型信息不在 `TrySetUntyped` 处丢失。
3. loader / debug / TestSystem 的 untyped 输入只能停留在边界，并明确注释“不推荐业务使用，有装箱/GC 风险”。
4. computed resolver、modifier pipeline 和 changed event 同步 typed 化。
5. 执行后必须能用 grep gate 证明 AI 框架主链路不调用 object API。

这些是终局目标，不表示 `SDD-0031` 必须一次完成所有项。`SDD-0031` 已完成第 1、2 项的主链路，并完成 computed cache / modifier 存储层 typed 化；第 3 项需要继续靠 API 命名、注释和 grep gate 约束；第 4 项里的 typed computed resolver 与 typed changed event 属于后续 SDD 范围。

## 最终架构

### 1. 用 `DataSlot<T>` 替代 `DataSlot.Value object?`

最终裁决不是新增通用 value union，而是让每个 slot 自己持有真实类型：

目标形状：

```csharp
internal interface IDataSlot
{
    DataDefinition Definition { get; }
    Type ValueClrType { get; }
    bool HasValue { get; }
    DataDiagnosticValue ToDiagnosticValue();
}

internal class DataSlot<T> : IDataSlot
{
    private readonly DataValuePolicy<T> _policy;
    private T _value;
    private bool _hasValue;

    public virtual T GetEffectiveValue();
    public virtual bool SetValue(T value);
    public bool TrySetFromBoundary(object? rawValue, DataWriteSource source, out DataWriteReport report);
}
```

裁决边界：

- `DataRuntimeStorage` 可以继续用 `Dictionary<string, IDataSlot>` 管不同类型的 slot；这是类型擦除的管理边界，不存业务值。
- 热路径 `Data.Set<T>(DataKey<T>, T)` 取到 `DataSlot<T>` 后直接写 `T`，不经过 `object?`。
- 热路径 `Data.Get<T>(DataKey<T>)` 取到 `DataSlot<T>` 后直接读 `T`，不经过 `ConvertForRead(object?)`。
- `IDataSlot` 只暴露 metadata、diagnostic snapshot 和边界方法，不能提供 `object? Value`。
- 实现 SDD 不再比较 `DataRuntimeValue`、多字典存储和 `DataSlot<T>` 作为平级方案；只能在 `DataSlot<T> + IDataSlot` 内部细化 policy、resolver、change event 和 diagnostics。

### 2. Catalog 建 typed runtime definition

`DataDefinition.DefaultValue object?` 可以作为 snapshot/DTO 层的历史输入，但进入 runtime catalog 后必须投影为 typed definition：

```csharp
internal interface IDataFieldDefinition
{
    string StableKey { get; }
    Type ValueClrType { get; }
    DataValueType ValueType { get; }
    IDataSlot CreateSlot();
}

internal sealed class DataFieldDefinition<T> : IDataFieldDefinition
{
    public DataKey<T> Key { get; }
    public T DefaultValue { get; }
    public DataValuePolicy<T> Policy { get; }

    public IDataSlot CreateSlot() => new DataSlot<T>(this);
}
```

`DataDefinitionCatalog.GetField<T>(DataKey<T>)` 必须校验 generated handle 的 `T` 与 descriptor `valueType/runtimeTypeId` 一致。不一致时 fail fast，而不是读写阶段再 `Convert.ChangeType`。

### 3. Converter 分层：泛型热路径 + 边界 untyped

上一版设计提出 `TryConvert<T>(..., out DataRuntimeValue)`，问题在于最终又回到统一 value 容器。新方案应直接转换到 `T`：

```csharp
public sealed class DataValuePolicy<T>
{
    public bool TryConvertTyped(T value, DataWriteSource source, out T finalValue, out DataWriteReport report);
    public bool TryConvertBoundary(object? rawValue, DataWriteSource source, out T finalValue, out DataWriteReport report);
    public bool AreEqual(T left, T right);
}
```

| 层 | API | 用途 |
| --- | --- | --- |
| Typed hot path | `Data.Set<T>(DataKey<T>, T)` -> `DataSlot<T>.SetValue(T)` | 业务读写主链路 |
| Runtime read | `Data.Get<T>(DataKey<T>)` -> `DataSlot<T>.GetEffectiveValue()` | 业务读取主链路 |
| Boundary untyped | `TrySetFromBoundary(object? raw, ...)` | snapshot loader、debug tool、TestSystem |
| Diagnostics | `ToDiagnosticValue()` | UI/Test dump，允许格式化或复制 |

边界入口必须写清楚：

```csharp
// 仅用于 loader/debug/TestSystem 边界。业务代码不要调用该入口；
// 值类型传入 object 会产生装箱，且会绕过 DataKey<T> 编译期契约。
```

### 4. Modifier pipeline 类型化

`DataModifier.Value` 当前是 `float`，数值修饰管线只应存在于数值 slot。不要让所有字段都走 `object? -> double -> object?`。

推荐做法：

```csharp
internal interface INumericDataSlot
{
    bool AddModifier(DataModifier modifier);
    bool RemoveModifier(string modifierId);
}

internal sealed class NumericDataSlot<T> : DataSlot<T>, INumericDataSlot
{
    private readonly NumericDataValuePolicy<T> _numericPolicy;
    private readonly List<DataModifier> _modifiers = new();

    public override T GetEffectiveValue()
    {
        var baseValue = GetBaseValue();
        return _numericPolicy.ApplyModifiers(baseValue, _modifiers, Definition);
    }
}
```

实现时可以用 `FloatDataValuePolicy`、`IntDataValuePolicy`、`DoubleDataValuePolicy` 三个显式策略，先不强依赖 C# generic math。重点是 slot 存的是 `T`，modifier 计算返回的也是 `T`。

### 5. Computed resolver 类型化

当前 `IDataComputeResolver.Compute()` 返回 `object?`。目标改为泛型 resolver：

```csharp
public interface IDataComputeResolver<T>
{
    string ComputeId { get; }
    T Compute(Data data, DataFieldDefinition<T> definition);
}

public abstract class FloatComputeResolver : IDataComputeResolver<float>
{
    public abstract string ComputeId { get; }
    public abstract float Compute(Data data, DataFieldDefinition<float> definition);
}
```

`DataComputeRegistry` 可以用非泛型 metadata 管注册表，但执行时必须按 field 的 `T` 获取 `IDataComputeResolver<T>`。类型不匹配是 catalog build error，不是运行时转换。

computed cache 不再是 `Dictionary<string, object?>`。computed 字段本身就是 `ComputedDataSlot<T>`，slot 内缓存 `T _cachedValue` 和 dirty flag。

### 6. Data changed event 泛型化

`PropertyChanged(string, object?, object?)` 不应继续是运行时通用事件。推荐由 typed slot 负责发 typed change：

```csharp
public readonly record struct DataChanged<T>(DataKey<T> Key, T OldValue, T NewValue);

internal interface IDataChangeDispatcher
{
    void Emit<T>(DataKey<T> key, T oldValue, T newValue);
}
```

对外分层：

- 高频业务监听：Capability 自己定义领域事件，例如 `HealthChanged(float oldHp, float newHp)`，或订阅 `DataChanged<float>`。
- Runtime 内部：slot 调 `IDataChangeDispatcher.Emit<T>`，不把 old/new 先装成 object。
- Debug/TestSystem：单独生成 `DataDiagnosticChange`，可包含字符串、stable key、类型名和格式化值。

### 7. `DataRuntimeValue` 裁决

不推荐引入上一版 `DataRuntimeValue` 多字段 union，原因：

- 它复制了 descriptor 已经知道的类型分发信息。
- 它让每个值都携带所有候选类型字段，结构冗余明显。
- 新增 `EntityId`、`ResourceRef`、`EntityIdList`、自定义 runtime ref 时会继续膨胀。
- 它不能像 `DataSlot<T>` 一样让编译器直接检查 `DataKey<T>` 与 storage 的一致性。
- 它仍鼓励写“通用 runtime value 处理器”，AI 容易继续绕过 `Data.Get/Set<T>`。

只有一种例外：如果未来 profiler 证明 `Dictionary<string, IDataSlot>` 的虚调用/类型检查成为瓶颈，可以在非常热的固定字段集合上做 source-generated typed storage。但那应是 benchmark 驱动的 P2 优化，不是本轮 object 去除的默认架构。

## API 裁决

| API | 裁决 |
| --- | --- |
| `Data.Get<T>(DataKey<T>)` | 保留，主入口 |
| `Data.Set<T>(DataKey<T>, T)` | 保留，主入口 |
| `Data.Get<T>(string)` | 降为 internal 或删除；AI 框架不应业务调用 |
| `Data.Set<T>(string, T)` | 降为 internal 或删除 |
| `Data.SetUntyped(... object?)` | 仅 loader/debug internal；注释明确装箱/GC 风险 |
| `Data.GetAll(): Dictionary<string, object>` | 改为 diagnostic snapshot API，不作为业务工具 |
| `DataModifier.Source object?` | 改为 typed source id，优先 `EntityId?` / `FeatureInstanceId` / `string sourceId` |

## 迁移步骤

1. 建 `DataFieldDefinition<T>`、`DataSlot<T>`、`DataValuePolicy<T>`，先覆盖 `int/float/double/bool/string/string[]/ResourceRef/EntityId?`。
2. 让 catalog build 阶段把 descriptor 投影为 typed runtime field，generated `DataKey<T>` 与 field `T` 不一致时报错。
3. 改 `Data.Get/Set<T>(DataKey<T>)` 直接走 `DataSlot<T>`，不再进入 `SetUntyped(... object?)`。
4. 改 modifier 为数值 slot/policy，移除 `object? -> double -> object?` 路径。
5. 改 computed resolver 为 `IDataComputeResolver<T>`，computed cache 进入 `ComputedDataSlot<T>`。
6. 改 `DataChangeRecord` 和 `GameEventType.Data.PropertyChanged` 为 typed change 或 domain event + diagnostic snapshot。
7. 改 snapshot loader 和 debug/test untyped API 到边界 converter，保留注释和 grep gate。
8. 迁移业务调用点，清理 `Get<T>(string)`、`SetUntyped(string, object?)`、`GetAll()` 非测试调用。
9. 更新 DocsAI Runtime/Data 和 `ecs-data` skill。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
rg -n "SetUntyped\\(|GetAll\\(|Dictionary<string, object>|object\\? OldValue|object\\? NewValue" Src/ECS/Runtime/Data Src/ECS/Capabilities Data/DataKey
```

需要新增：

- Data generic slot unit / scene test：`float/int/bool/string/string_array/resource_ref/entity_id`。
- Catalog type mismatch test：descriptor / generated handle / runtime field 不一致时 fail fast。
- Modifier test：Additive/Multiplicative/FinalAdditive/Override/Cap 返回 typed `T`，不经 `object?`。
- Computed resolver test：`IDataComputeResolver<T>` 返回 typed `T`，cache dirty 正确。
- PropertyChanged test：业务 typed event 和 debug snapshot 分离。
- 分配基线：至少用 benchmark 或 Godot scene artifact 记录改前/改后 `Get/Set/Modifier/Computed` 分配。

## Must Confirm

- 是否接受删除或 internal 化业务层 `Data.Get<T>(string)` / `Data.Set<T>(string)`。
- 是否接受 `PropertyChanged(object?)` 改为 typed/domain event + debug snapshot，而不是继续给 UI/TestSystem 监听 object。

## Confirmed Decisions

- `DataSlot<T> + IDataSlot` 是 Data 去 object 的最终架构方向。
- `DataRuntimeValue` 多字段 union 不作为实现方案。
- `Dictionary<string, IDataSlot>` 可作为 slot 管理边界；业务值不通过 `object?` 存储或读取。
