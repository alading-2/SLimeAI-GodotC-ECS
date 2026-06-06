# Data 运行时 object 去除设计

## 当前结论

Data 必须改，而且应按 hard cutover 改。当前 Data 已经在事实源层完成 DataOS descriptor、runtime snapshot、generated `DataKey<T>`，但运行时值存储仍回到 `object?`。这会让 typed handle 的 AI-first 收益在热路径被抵消。

用户判断“Data 的 object 问题比较大”成立。Data 是 ECS 运行时状态容器，读写频率远高于大部分工具层 API，不能继续把 `object?` 当主链路。

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

## 推荐架构

### 1. 引入 `DataRuntimeValue`

用 descriptor `DataValueType` 做稳定分发，而不是把 public API 泛型化到所有内部类型。

目标形状：

```csharp
internal readonly struct DataRuntimeValue
{
    public DataValueType ValueType { get; }
    public int IntValue { get; }
    public float FloatValue { get; }
    public double DoubleValue { get; }
    public bool BoolValue { get; }
    public System.Numerics.Vector2 Vector2Value { get; }
    public string? StringValue { get; }
    public string[]? StringArrayValue { get; }
    public ResourceRef ResourceRefValue { get; }
    public object? DebugObjectValue { get; }
}
```

说明：

- 数值、bool、Vector2、enum string、ResourceRef 走 typed 字段。
- 引用型复杂数据仍可通过受约束字段保存，但必须由 descriptor 声明 `object_ref/runtime_only/runtimeTypeId`。
- `DebugObjectValue` 只服务 debug/test/runtime object reference，不作为普通 DataKey 入口。

### 2. `DataSlot` 改为 typed value

当前：

```csharp
public object? Value { get; private set; }
public object? GetEffectiveValue()
public bool SetValue(object? value)
```

目标：

```csharp
private DataRuntimeValue _value;
private bool _hasValue;

public DataRuntimeValue GetEffectiveValue();
public bool SetValue(DataRuntimeValue value);
```

modifier 不再接收 `object? baseValue`，而是只允许数值型 `DataRuntimeValue`：

```csharp
private DataRuntimeValue ApplyNumericModifiers(DataRuntimeValue baseValue)
```

这样能避免“拆箱 -> double -> 重新装箱”的默认路径。

### 3. Converter 分层

保留 untyped converter，但只在 loader/debug 边界调用：

| 层 | API | 用途 |
| --- | --- | --- |
| Typed hot path | `TryConvert<T>(DataKey<T>, T value, out DataRuntimeValue value)` | 业务 `Data.Set<T>` |
| Runtime read | `Read<T>(DataRuntimeValue value, DataDefinition definition)` | 业务 `Data.Get<T>` |
| Boundary untyped | `TryConvertUntyped(object? raw, DataDefinition definition, DataWriteSource source, out DataRuntimeValue value)` | snapshot loader、debug tool |

注释要求：

```csharp
// 仅用于 loader/debug 边界。业务代码不要调用该入口；
// 值类型传入 object 会产生装箱，且绕过 DataKey<T> 编译期契约。
```

### 4. Computed resolver 类型化

当前 `IDataComputeResolver.Compute()` 返回 `object?`。目标至少分两步：

第一步：resolver 返回 `DataRuntimeValue`：

```csharp
public interface IDataComputeResolver
{
    string ComputeId { get; }
    DataRuntimeValue Compute(Data data, DataDefinition definition);
}
```

第二步：常见数值 resolver 提供泛型基类，避免每个 resolver 手写转换：

```csharp
public abstract class FloatComputeResolver : IDataComputeResolver
{
    protected abstract float ComputeFloat(Data data, DataDefinition definition);
}
```

### 5. Data changed event 分层

`PropertyChanged(string, object?, object?)` 不应继续是运行时通用事件。

推荐：

- Runtime 内部：`DataChangeRecord` 保存 `DataRuntimeValue OldValue/NewValue`。
- 高频业务监听：提供领域事件，例如 `HealthChanged(float oldHp, float newHp)`，或由对应 Capability 发 typed event。
- Debug/TestSystem：可以有 `DataChangedSnapshot`，在订阅时按需格式化为字符串或 diagnostic object。

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

1. 建 `DataRuntimeValue` 和 typed converter，先不删除旧 API。
2. 改 `DataSlot`、modifier、computed cache 为 `DataRuntimeValue`。
3. 改 `Data.Get/Set<T>(DataKey<T>)` 走 typed converter，不再进入 `object?`。
4. 改 snapshot loader 和 debug/test untyped API 到边界 converter。
5. 改 `DataChangeRecord` 和 `GameEventType.Data.PropertyChanged`。
6. 迁移业务调用点，清理 `Get<T>(string)`、`SetUntyped(string, object?)`、`GetAll()` 非测试调用。
7. 更新 DocsAI Runtime/Data 和 `ecs-data` skill。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
rg -n "SetUntyped\\(|GetAll\\(|Dictionary<string, object>|object\\? OldValue|object\\? NewValue" Src/ECS/Runtime/Data Src/ECS/Capabilities Data/DataKey
```

需要新增：

- Data typed value unit / scene test：`float/int/bool/enum/vector2/string_array/object_ref`。
- Modifier test：Additive/Multiplicative/FinalAdditive/Override/Cap 不装箱路径。
- Computed resolver test：resolver 返回 typed value，cache dirty 正确。
- PropertyChanged test：业务 typed event 和 debug snapshot 分离。
- 分配基线：至少用 benchmark 或 Godot scene artifact 记录改前/改后 `Get/Set/Modifier/Computed` 分配。

## Must Confirm

- 是否接受删除或 internal 化业务层 `Data.Get<T>(string)` / `Data.Set<T>(string)`。
- 是否接受 `PropertyChanged(object?)` 改为 typed/domain event + debug snapshot，而不是继续给 UI/TestSystem 监听 object。
