# Data 运行时泛型存储设计

## 当前结论

Data 必须改，而且应按 hard cutover 改。当前 Data 已经在事实源层完成 DataOS descriptor、runtime snapshot、generated `DataKey<T>`，调用侧也已经是 `Data.Get/Set<T>(DataKey<T>)`，但运行时值存储仍回到 `object?`。这会让 typed handle 的 AI-first 收益在热路径被抵消。

用户判断“Data 的 object 问题比较大”成立。Data 是 ECS 运行时状态容器，读写频率远高于大部分工具层 API，不能继续把 `object?` 当主链路。

用户对上一版设计的批评也成立：不应再用一个带 `IntValue/FloatValue/BoolValue/...` 多字段的 `DataRuntimeValue` union 替代 `object?`。那只是把“一个 object 存任意类型”的问题换成“一个结构体携带所有可能类型的字段”，会引入冗余字段、额外分发、调试歧义和未来类型扩张成本。SlimeAI 既然已经有 `DataKey<T>` 和 `Data.Get/Set<T>`，主链路就应继续走泛型。

用户已确认 `DataSlot<T> + IDataSlot` 是当前最优方案。本设计以此作为最终裁决：`DataSlot<T>` 保存真实业务值，`IDataSlot` 只作为跨类型 slot 管理和 diagnostics 边界；不再把 `DataRuntimeValue` union、`object? Value` 或多字典拆分作为同级候选。

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

## 源码证据

| 文件 | 证据 | 风险 |
| --- | --- | --- |
| `Src/ECS/Runtime/Data/DataRuntimeStorage.cs` | `DataChangeRecord(string StableKey, object? OldValue, object? NewValue)` | 每次变更事件都保留 object payload |
| `DataRuntimeStorage.cs` | `DataSlot.Value object?`、`GetEffectiveValue(): object?`、`SetValue(object?)` | 所有值类型字段写入/读取都会穿过 object |
| `DataRuntimeStorage.cs` | `ConvertNumericToDefinitionType` 返回 `(object)(float/int/double)` | modifier 计算后重新装箱 |
| `DataRuntimeStorage.cs` | `DataValueConverter.ConvertInt/Float/Bool/Vector2` 返回 `object` | 写入转换阶段丢失 typed 返回 |
| `DataRuntimeStorage.cs` | `ConvertForRead(object?, Type, DataValueType)` 和 `Activator.CreateInstance` | 读取阶段靠 runtime type 判断 |
| `DataRuntimeStorage.cs` | `_computedCache Dictionary<string, object?>`、`IDataComputeResolver.Compute(): object?` | computed 输出仍是 object |
| `Data.cs` | `SetUntyped(... object?)`、`GetAll(): Dictionary<string, object>` | 宽口 API 仍可被业务误用 |
| `GameEventType_Data.cs` | `PropertyChanged(string Key, object? OldValue, object? NewValue)` | Data changed event 与 Event object 问题耦合 |

## 设计目标

1. Data runtime hot path 不再使用 `object?` 存储数值和常见值类型。
2. `DataKey<T>` 到 runtime storage 的泛型信息不在 `TrySetUntyped` 处丢失。
3. loader / debug / TestSystem 的 untyped 输入只能停留在边界，并明确注释“不推荐业务使用，有装箱/GC 风险”。
4. computed resolver、modifier pipeline 和 changed event 同步 typed 化。
5. 执行后必须能用 grep gate 证明 AI 框架主链路不调用 object API。

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
