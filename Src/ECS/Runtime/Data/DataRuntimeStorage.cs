using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using slime.data.Features;

/// <summary>
/// Data 写入来源，用于执行 descriptor write policy。
/// </summary>
public enum DataWriteSource
{
    Runtime,
    Loader,
    System,
    Debug
}

/// <summary>
/// Data diagnostic 字段变更记录。
/// 仅用于 TestSystem、debug 和兼容监听；业务代码应订阅 typed DataChangeRecord&lt;T&gt; / GameEventType.Data.Changed&lt;T&gt;。
/// </summary>
/// <param name="StableKey">发生变更的 stable key。</param>
/// <param name="OldValue">变更前有效值；值类型会装箱。</param>
/// <param name="NewValue">变更后有效值；值类型会装箱。</param>
public sealed record DataChangeRecord(string StableKey, object? OldValue, object? NewValue);

/// <summary>
/// Data typed 字段变更记录。
/// </summary>
public interface IDataChangeRecord
{
    /// <summary>发生变更的 stable key。</summary>
    string StableKey { get; }

    /// <summary>typed value 的 CLR 类型。</summary>
    Type ValueClrType { get; }

    /// <summary>边界诊断用旧值；值类型会装箱。</summary>
    object? OldValueForDiagnostics { get; }

    /// <summary>边界诊断用新值；值类型会装箱。</summary>
    object? NewValueForDiagnostics { get; }

    /// <summary>向 Entity.Events 发出对应 typed payload。</summary>
    void EmitTyped(EventBus events);
}

/// <summary>
/// Data typed 字段变更记录。
/// </summary>
/// <typeparam name="T">字段值类型。</typeparam>
/// <param name="Key">descriptor generated DataKey。</param>
/// <param name="OldValue">变更前 typed 值。</param>
/// <param name="NewValue">变更后 typed 值。</param>
public sealed record DataChangeRecord<T>(DataKey<T> Key, T OldValue, T NewValue) : IDataChangeRecord
{
    /// <inheritdoc />
    public string StableKey => Key.StableKey;

    /// <inheritdoc />
    public Type ValueClrType => typeof(T);

    /// <inheritdoc />
    public object? OldValueForDiagnostics => DataValueConverter.CloneForBoundary(OldValue);

    /// <inheritdoc />
    public object? NewValueForDiagnostics => DataValueConverter.CloneForBoundary(NewValue);

    /// <inheritdoc />
    public void EmitTyped(EventBus events)
    {
        events.Emit(new GameEventType.Data.Changed<T>(Key, OldValue, NewValue));
    }
}

/// <summary>
/// Data 写入诊断报告。
/// </summary>
public sealed class DataWriteReport
{
    /// <summary>
    /// 创建写入诊断报告。
    /// </summary>
    /// <param name="stableKey">写入目标 stable key。</param>
    /// <param name="source">写入来源。</param>
    public DataWriteReport(string stableKey, DataWriteSource source)
    {
        StableKey = stableKey;
        Source = source;
    }

    /// <summary>
    /// 写入目标 stable key。
    /// </summary>
    public string StableKey { get; }

    /// <summary>
    /// 写入来源。
    /// </summary>
    public DataWriteSource Source { get; }

    /// <summary>
    /// 结构化错误列表。
    /// </summary>
    public List<DataWriteError> Errors { get; } = new();

    /// <summary>
    /// 是否存在错误。
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// 追加结构化错误。
    /// </summary>
    /// <param name="error">写入错误。</param>
    public void AddError(DataWriteError error)
    {
        Errors.Add(error);
    }
}

/// <summary>
/// Data 写入结构化错误。
/// </summary>
/// <param name="Code">错误码。</param>
/// <param name="StableKey">字段 stable key。</param>
/// <param name="Message">错误信息。</param>
/// <param name="Source">写入来源。</param>
/// <param name="ExpectedType">期望 descriptor 值类型。</param>
/// <param name="ActualType">实际 CLR 类型。</param>
/// <param name="Policy">拒绝写入的策略。</param>
/// <param name="RawValue">原始值摘要。</param>
public sealed record DataWriteError(
    string Code,
    string StableKey,
    string Message,
    DataWriteSource Source,
    string ExpectedType,
    string? ActualType,
    string? Policy,
    string? RawValue);

/// <summary>
/// 跨类型槽位管理边界；不暴露业务值的 object 存储入口。
/// </summary>
public interface IDataSlot
{
    /// <summary>
    /// 字段 descriptor 定义。
    /// </summary>
    DataDefinition Definition { get; }

    /// <summary>
    /// 槽位实际保存的 CLR 类型。
    /// </summary>
    Type ValueClrType { get; }

    /// <summary>
    /// 是否已有运行时基础值。
    /// </summary>
    bool HasValue { get; }

    /// <summary>
    /// 边界诊断用：读取有效值，会在值类型字段上发生装箱。
    /// </summary>
    object? GetEffectiveValueForDiagnostics();

    /// <summary>
    /// 边界诊断用：读取已写入基础值，会在值类型字段上发生装箱。
    /// </summary>
    object? GetStoredValueForDiagnostics();

    /// <summary>
    /// 从 loader/debug/TestSystem 边界写入已通过策略校验的值。
    /// </summary>
    bool SetValueFromBoundary(object? value);

    bool ClearValue();

    bool AddModifier(DataModifier modifier);

    bool RemoveModifier(string modifierId);

    int RemoveModifiersBySource(DataModifierSource source);

    bool ClearModifiers();

    List<DataModifier> GetModifiers();

    /// <summary>
    /// 发出当前槽位的 typed changed 事件。
    /// </summary>
    void PublishChange(DataRuntimeStorage storage, object? oldValueForDiagnostics);
}

/// <summary>
/// 单个 descriptor 字段的泛型运行时槽位。
/// </summary>
/// <typeparam name="T">槽位保存的 CLR 值类型。</typeparam>
public sealed class DataSlot<T> : IDataSlot
{
    private readonly List<DataModifier> _modifiers = new();
    private readonly T _defaultValue;
    private readonly bool _hasDefaultValue;
    private T _value = default!;

    /// <summary>
    /// 创建泛型运行时槽位。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    public DataSlot(DataDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition; // descriptor 定义
        _defaultValue = ConvertToSlotValue(definition.DefaultValue);
        _hasDefaultValue = definition.DefaultValue != null || typeof(T).IsValueType;
    }

    /// <inheritdoc />
    public DataDefinition Definition { get; }

    /// <inheritdoc />
    public Type ValueClrType => typeof(T);

    /// <inheritdoc />
    public bool HasValue { get; private set; }

    /// <summary>
    /// 获取当前 typed 有效值；未写入时回退到 descriptor default。
    /// </summary>
    public T GetEffectiveValue()
    {
        var baseValue = HasValue ? _value : _defaultValue;
        var effective = _modifiers.Count == 0 ? baseValue : ApplyModifiers(baseValue);
        return CloneIfMutable(effective);
    }

    /// <summary>
    /// 写入 typed 运行时基础值。
    /// </summary>
    /// <param name="value">已转换并通过策略校验的值。</param>
    public bool SetValue(T value)
    {
        if (HasValue && EqualityComparer<T>.Default.Equals(_value, value))
        {
            return false;
        }

        _value = CloneIfMutable(value);
        HasValue = true;
        return true;
    }

    /// <inheritdoc />
    public bool SetValueFromBoundary(object? value)
    {
        return SetValue(ConvertToSlotValue(value));
    }

    /// <inheritdoc />
    public bool ClearValue()
    {
        if (!HasValue)
        {
            return false;
        }

        _value = default!;
        HasValue = false;
        return true;
    }

    /// <inheritdoc />
    public bool AddModifier(DataModifier modifier)
    {
        ArgumentNullException.ThrowIfNull(modifier);
        for (var i = 0; i < _modifiers.Count; i++)
        {
            if (string.Equals(_modifiers[i].Id, modifier.Id, StringComparison.Ordinal))
            {
                return false;
            }
        }

        var insertIndex = _modifiers.BinarySearch(modifier, ModifierPriorityComparer.Instance);
        if (insertIndex < 0)
        {
            insertIndex = ~insertIndex;
        }

        _modifiers.Insert(insertIndex, modifier);
        return true;
    }

    /// <inheritdoc />
    public bool RemoveModifier(string modifierId)
    {
        return _modifiers.RemoveAll(modifier => string.Equals(modifier.Id, modifierId, StringComparison.Ordinal)) > 0;
    }

    /// <inheritdoc />
    public int RemoveModifiersBySource(DataModifierSource source)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        return _modifiers.RemoveAll(modifier => modifier.SourceId == source);
    }

    /// <inheritdoc />
    public bool ClearModifiers()
    {
        if (_modifiers.Count == 0)
        {
            return false;
        }

        _modifiers.Clear();
        return true;
    }

    /// <inheritdoc />
    public List<DataModifier> GetModifiers()
    {
        return new List<DataModifier>(_modifiers);
    }

    /// <inheritdoc />
    public object? GetEffectiveValueForDiagnostics()
    {
        return DataValueConverter.CloneForBoundary(GetEffectiveValue());
    }

    /// <inheritdoc />
    public object? GetStoredValueForDiagnostics()
    {
        return HasValue ? DataValueConverter.CloneForBoundary(_value) : null;
    }

    /// <inheritdoc />
    public void PublishChange(DataRuntimeStorage storage, object? oldValueForDiagnostics)
    {
        var oldValue = ConvertToSlotValue(oldValueForDiagnostics);
        storage.PublishTypedChange(Definition, oldValue, GetEffectiveValue());
    }

    private T ApplyModifiers(T baseValue)
    {
        if (!TryGetNumeric(baseValue, out var numericBase))
        {
            return baseValue;
        }

        var additive = 0d;
        var multiplicative = 1d;
        var finalAdditive = 0d;
        double? overrideValue = null;
        double? cap = null;
        for (var i = 0; i < _modifiers.Count; i++)
        {
            var modifier = _modifiers[i];
            switch (modifier.Type)
            {
                case ModifierType.Additive:
                    additive += modifier.Value;
                    break;
                case ModifierType.Multiplicative:
                    multiplicative *= modifier.Value;
                    break;
                case ModifierType.FinalAdditive:
                    finalAdditive += modifier.Value;
                    break;
                case ModifierType.Override:
                    overrideValue ??= modifier.Value;
                    break;
                case ModifierType.Cap:
                    cap = cap.HasValue ? Math.Min(cap.Value, modifier.Value) : modifier.Value;
                    break;
            }
        }

        var effective = overrideValue ?? ((numericBase + additive) * multiplicative + finalAdditive);
        if (cap.HasValue)
        {
            effective = Math.Min(effective, cap.Value);
        }

        if (Definition.MinValue.HasValue && effective < Definition.MinValue.Value)
        {
            effective = Definition.MinValue.Value;
        }

        if (Definition.MaxValue.HasValue && effective > Definition.MaxValue.Value)
        {
            effective = Definition.MaxValue.Value;
        }

        return ConvertNumericToSlotType(effective);
    }

    private T ConvertToSlotValue(object? value)
    {
        if (value == null && !_hasDefaultValue)
        {
            return default!;
        }

        var converted = DataValueConverter.ConvertForRead(value, typeof(T), Definition.ValueType);
        return converted == null ? default! : CloneIfMutable((T)converted);
    }

    private static T CloneIfMutable(T value)
    {
        if (value is string[] stringArray)
        {
            var clone = (string[])stringArray.Clone();
            return Unsafe.As<string[], T>(ref clone);
        }

        if (value is FeatureModifierEntryData[] modifierArray)
        {
            var clone = (FeatureModifierEntryData[])modifierArray.Clone();
            return Unsafe.As<FeatureModifierEntryData[], T>(ref clone);
        }

        return value;
    }

    private static bool TryGetNumeric(T value, out double numericValue)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (targetType == typeof(int))
        {
            numericValue = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(float))
        {
            numericValue = Convert.ToSingle(value, CultureInfo.InvariantCulture);
            return true;
        }

        if (targetType == typeof(double))
        {
            numericValue = Convert.ToDouble(value, CultureInfo.InvariantCulture);
            return true;
        }

        numericValue = 0d;
        return false;
    }

    private static T ConvertNumericToSlotType(double value)
    {
        if (typeof(T) == typeof(int))
        {
            var typedValue = (int)value;
            return Unsafe.As<int, T>(ref typedValue);
        }

        if (typeof(T) == typeof(int?))
        {
            int? typedValue = (int)value;
            return Unsafe.As<int?, T>(ref typedValue);
        }

        if (typeof(T) == typeof(float))
        {
            var typedValue = (float)value;
            return Unsafe.As<float, T>(ref typedValue);
        }

        if (typeof(T) == typeof(float?))
        {
            float? typedValue = (float)value;
            return Unsafe.As<float?, T>(ref typedValue);
        }

        if (typeof(T) == typeof(double))
        {
            return Unsafe.As<double, T>(ref value);
        }

        if (typeof(T) == typeof(double?))
        {
            double? typedValue = value;
            return Unsafe.As<double?, T>(ref typedValue);
        }

        return default!;
    }

    private sealed class ModifierPriorityComparer : IComparer<DataModifier>
    {
        public static readonly ModifierPriorityComparer Instance = new();

        public int Compare(DataModifier? x, DataModifier? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return x.Priority.CompareTo(y.Priority);
        }
    }
}

/// <summary>
/// descriptor-first Data 值转换与 runtime policy 校验工具。
/// </summary>
public static class DataValueConverter
{
    private static readonly JsonSerializerOptions ModifierListJsonOptions = CreateModifierListJsonOptions();

    /// <summary>
    /// 边界诊断用浅拷贝：避免可变数组默认值或存储值被 dump 调用方修改。
    /// </summary>
    /// <param name="value">要返回给边界调用方的值。</param>
    public static object? CloneForBoundary(object? value)
    {
        return value switch
        {
            string[] stringArray => (string[])stringArray.Clone(),
            FeatureModifierEntryData[] modifierArray => (FeatureModifierEntryData[])modifierArray.Clone(),
            _ => value
        };
    }

    /// <summary>
    /// 检查泛型读取类型是否兼容 descriptor 值类型。
    /// </summary>
    /// <typeparam name="T">调用方期望的 CLR 类型。</typeparam>
    /// <param name="valueType">descriptor 值类型。</param>
    public static bool IsCompatible<T>(DataValueType valueType)
    {
        return IsCompatible(typeof(T), valueType);
    }

    /// <summary>
    /// 检查 CLR 类型是否兼容 descriptor 值类型。
    /// </summary>
    /// <param name="clrType">调用方期望的 CLR 类型。</param>
    /// <param name="valueType">descriptor 值类型。</param>
    public static bool IsCompatible(Type clrType, DataValueType valueType)
    {
        var targetType = Nullable.GetUnderlyingType(clrType) ?? clrType;
        return valueType switch
        {
            DataValueType.String => targetType == typeof(string),
            DataValueType.StringArray => targetType == typeof(string[]),
            DataValueType.Int => targetType == typeof(int),
            DataValueType.Float => targetType == typeof(float),
            DataValueType.Double => targetType == typeof(double),
            DataValueType.Bool => targetType == typeof(bool),
            DataValueType.Vector2 => targetType == typeof(System.Numerics.Vector2) || IsGodotVector2Type(targetType),
            DataValueType.Enum => targetType == typeof(string) || targetType.IsEnum,
            DataValueType.ModifierList => targetType == typeof(FeatureModifierEntryData[]),
            DataValueType.ObjectRef => targetType == typeof(ResourceRef) || (!targetType.IsValueType && targetType != typeof(string)),
            _ => false
        };
    }

    /// <summary>
    /// 按 descriptor 值类型严格转换输入值。
    /// </summary>
    /// <param name="rawValue">原始输入值。</param>
    /// <param name="valueType">descriptor 值类型。</param>
    /// <param name="convertedValue">转换后的值。</param>
    /// <param name="error">失败原因。</param>
    public static bool TryConvert(object? rawValue, DataValueType valueType, out object? convertedValue, out string error)
    {
        convertedValue = null;
        error = string.Empty;
        if (rawValue == null)
        {
            return true;
        }

        try
        {
            convertedValue = valueType switch
            {
                DataValueType.String => ConvertString(rawValue),
                DataValueType.StringArray => ConvertStringArray(rawValue),
                DataValueType.Int => ConvertInt(rawValue),
                DataValueType.Float => ConvertFloat(rawValue),
                DataValueType.Double => ConvertDouble(rawValue),
                DataValueType.Bool => ConvertBool(rawValue),
                DataValueType.Vector2 => ConvertVector2(rawValue),
                DataValueType.Enum => ConvertEnum(rawValue),
                DataValueType.ModifierList => ConvertModifierList(rawValue),
                DataValueType.ObjectRef => ConvertObjectRef(rawValue),
                _ => throw new InvalidOperationException($"未知 DataValueType：{valueType}")
            };
            return true;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException or InvalidOperationException)
        {
            error = ex.Message;
            return false;
        }
    }

    /// <summary>
    /// 执行 write/range/allowed_values 策略并输出最终写入值。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="rawValue">原始输入值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="finalValue">最终写入值。</param>
    /// <param name="error">失败原因。</param>
    public static bool TryApplyWritePolicies(
        DataDefinition definition,
        object? rawValue,
        DataWriteSource source,
        out object? finalValue,
        out string error)
    {
        var success = TryApplyWritePoliciesWithReport(definition, rawValue, source, out finalValue, out var writeError);
        error = writeError?.Message ?? string.Empty;
        return success;
    }

    /// <summary>
    /// 执行 write/range/allowed_values 策略，并输出结构化错误。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="rawValue">原始输入值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="finalValue">最终写入值。</param>
    /// <param name="writeError">结构化错误。</param>
    public static bool TryApplyWritePoliciesWithReport(
        DataDefinition definition,
        object? rawValue,
        DataWriteSource source,
        out object? finalValue,
        out DataWriteError? writeError)
    {
        ArgumentNullException.ThrowIfNull(definition);
        finalValue = null;
        writeError = null;
        if (!CanWrite(definition.WritePolicy, source))
        {
            writeError = CreateWriteError(
                definition,
                "write_policy_rejected",
                $"Data write policy 拒绝写入：{definition.StableKey} ({definition.WritePolicy}, source={source})",
                source,
                rawValue,
                definition.WritePolicy.ToString());
            return false;
        }

        if (RequiresRuntimeObjectReference(definition) && rawValue is string or ResourceRef)
        {
            writeError = CreateWriteError(
                definition,
                "wrong_clr_type",
                $"Data object_ref 运行时对象字段拒绝资源引用：{definition.StableKey} expected={definition.RuntimeTypeId}",
                source,
                rawValue,
                null,
                definition.RuntimeTypeId);
            return false;
        }

        if (!TryConvert(rawValue, definition.ValueType, out var convertedValue, out var error))
        {
            writeError = CreateWriteError(
                definition,
                "wrong_clr_type",
                $"Data value type 不匹配：{definition.StableKey} ({definition.ValueType}) {error}",
                source,
                rawValue,
                null);
            return false;
        }

        if (RequiresRuntimeObjectReference(definition) && !MatchesRuntimeObjectReference(definition, convertedValue))
        {
            writeError = CreateWriteError(
                definition,
                "wrong_clr_type",
                $"Data object_ref 运行时对象类型不匹配：{definition.StableKey} expected={definition.RuntimeTypeId}",
                source,
                rawValue,
                null,
                definition.RuntimeTypeId);
            return false;
        }

        if (!IsAllowedValue(definition, convertedValue))
        {
            writeError = CreateWriteError(
                definition,
                "allowed_values_rejected",
                $"Data allowed_values 拒绝写入：{definition.StableKey} = {ToStableText(convertedValue)}",
                source,
                rawValue,
                "allowed_values");
            return false;
        }

        if (!TryApplyRangePolicy(definition, convertedValue, source, out finalValue, out var rangeError))
        {
            writeError = CreateWriteError(
                definition,
                "range_policy_rejected",
                rangeError,
                source,
                rawValue,
                definition.RangePolicy.ToString());
            return false;
        }

        return true;
    }

    /// <summary>
    /// 执行 typed 热路径写入策略；成功路径直接返回 T，避免回到 untyped boundary 写槽位。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="rawValue">调用方传入的 typed 值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="finalValue">最终写入 typed 值。</param>
    /// <param name="writeError">结构化错误。</param>
    public static bool TryApplyTypedWritePoliciesWithReport<T>(
        DataDefinition definition,
        T rawValue,
        DataWriteSource source,
        out T finalValue,
        out DataWriteError? writeError)
    {
        ArgumentNullException.ThrowIfNull(definition);
        finalValue = rawValue;
        writeError = null;
        if (!CanWrite(definition.WritePolicy, source))
        {
            writeError = CreateWriteError(
                definition,
                "write_policy_rejected",
                $"Data write policy 拒绝写入：{definition.StableKey} ({definition.WritePolicy}, source={source})",
                source,
                rawValue,
                definition.WritePolicy.ToString());
            return false;
        }

        if (RequiresRuntimeObjectReference(definition) && rawValue is string or ResourceRef)
        {
            writeError = CreateWriteError(
                definition,
                "wrong_clr_type",
                $"Data object_ref 运行时对象字段拒绝资源引用：{definition.StableKey} expected={definition.RuntimeTypeId}",
                source,
                rawValue,
                null,
                definition.RuntimeTypeId);
            return false;
        }

        if (RequiresRuntimeObjectReference(definition) && !MatchesRuntimeObjectReference(definition, rawValue))
        {
            writeError = CreateWriteError(
                definition,
                "wrong_clr_type",
                $"Data object_ref 运行时对象类型不匹配：{definition.StableKey} expected={definition.RuntimeTypeId}",
                source,
                rawValue,
                null,
                definition.RuntimeTypeId);
            return false;
        }

        if (!IsAllowedValue(definition, rawValue))
        {
            writeError = CreateWriteError(
                definition,
                "allowed_values_rejected",
                $"Data allowed_values 拒绝写入：{definition.StableKey} = {ToStableText(rawValue)}",
                source,
                rawValue,
                "allowed_values");
            return false;
        }

        if (!TryApplyRangePolicy(definition, rawValue, source, out finalValue, out var rangeError))
        {
            writeError = CreateWriteError(
                definition,
                "range_policy_rejected",
                rangeError,
                source,
                rawValue,
                definition.RangePolicy.ToString());
            return false;
        }

        return true;
    }

    private static DataWriteError CreateWriteError(
        DataDefinition definition,
        string code,
        string message,
        DataWriteSource source,
        object? rawValue,
        string? policy,
        string? expectedTypeOverride = null)
    {
        return new DataWriteError(
            code,
            definition.StableKey,
            message,
            source,
            expectedTypeOverride ?? definition.ValueType.ToString(),
            rawValue?.GetType().Name,
            policy,
            ToStableText(rawValue));
    }

    /// <summary>
    /// 根据 write policy 和写入来源判断是否允许写入。
    /// ReadWrite 全放行；LoaderOnly 仅 Loader；SystemOnly 仅 System + Loader；ComputedReadonly 全拒绝；DebugOnly 仅 Debug。
    /// </summary>
    private static bool CanWrite(DataWritePolicy policy, DataWriteSource source)
    {
        return policy switch
        {
            DataWritePolicy.ReadWrite => true,
            DataWritePolicy.LoaderOnly => source == DataWriteSource.Loader,
            DataWritePolicy.SystemOnly => source == DataWriteSource.System || source == DataWriteSource.Loader,
            DataWritePolicy.ComputedReadonly => false,
            DataWritePolicy.DebugOnly => source == DataWriteSource.Debug,
            _ => false
        };
    }

    /// <summary>
    /// 校验范围策略：None 不检查；ClampRuntime 对 Runtime 来源自动 clamp；Validate/RejectRuntime 超出则拒绝。
    /// </summary>
    private static bool TryApplyRangePolicy(
        DataDefinition definition,
        object? convertedValue,
        DataWriteSource source,
        out object? finalValue,
        out string error)
    {
        finalValue = convertedValue;
        error = string.Empty;
        if (definition.RangePolicy == DataRangePolicy.None || convertedValue == null)
        {
            return true;
        }

        if (!TryGetNumeric(convertedValue, out var numericValue))
        {
            error = $"Data range policy 仅支持数值字段：{definition.StableKey}";
            return false;
        }

        var hasMin = definition.MinValue.HasValue;
        var hasMax = definition.MaxValue.HasValue;
        var min = definition.MinValue ?? numericValue;
        var max = definition.MaxValue ?? numericValue;
        var outOfRange = (hasMin && numericValue < min) || (hasMax && numericValue > max);
        if (!outOfRange)
        {
            return true;
        }

        if (definition.RangePolicy == DataRangePolicy.ClampRuntime && source == DataWriteSource.Runtime)
        {
            var clamped = Math.Min(Math.Max(numericValue, min), max);
            finalValue = ConvertNumericToOriginalType(clamped, convertedValue.GetType());
            return true;
        }

        error = $"Data range policy 拒绝写入：{definition.StableKey} = {numericValue.ToString(CultureInfo.InvariantCulture)}";
        return false;
    }

    /// <summary>
    /// typed 版本范围策略校验，避免装箱。
    /// </summary>
    private static bool TryApplyRangePolicy<T>(
        DataDefinition definition,
        T convertedValue,
        DataWriteSource source,
        out T finalValue,
        out string error)
    {
        finalValue = convertedValue;
        error = string.Empty;
        if (definition.RangePolicy == DataRangePolicy.None || convertedValue is null)
        {
            return true;
        }

        if (!TryGetNumeric(convertedValue, out var numericValue))
        {
            error = $"Data range policy 仅支持数值字段：{definition.StableKey}";
            return false;
        }

        var hasMin = definition.MinValue.HasValue;
        var hasMax = definition.MaxValue.HasValue;
        var min = definition.MinValue ?? numericValue;
        var max = definition.MaxValue ?? numericValue;
        var outOfRange = (hasMin && numericValue < min) || (hasMax && numericValue > max);
        if (!outOfRange)
        {
            return true;
        }

        if (definition.RangePolicy == DataRangePolicy.ClampRuntime && source == DataWriteSource.Runtime)
        {
            var clamped = Math.Min(Math.Max(numericValue, min), max);
            finalValue = ConvertNumericToTyped<T>(clamped);
            return true;
        }

        error = $"Data range policy 拒绝写入：{definition.StableKey} = {numericValue.ToString(CultureInfo.InvariantCulture)}";
        return false;
    }

    /// <summary>
    /// 校验值是否在 descriptor AllowedValues 白名单内。AllowedValues 为空则放行。
    /// </summary>
    private static bool IsAllowedValue(DataDefinition definition, object? convertedValue)
    {
        if (definition.AllowedValues.Count == 0)
        {
            return true;
        }

        var stableValue = ToStableText(convertedValue);
        for (var i = 0; i < definition.AllowedValues.Count; i++)
        {
            if (string.Equals(definition.AllowedValues[i].Value, stableValue, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// typed 版本 allowed values 校验，避免装箱。
    /// </summary>
    private static bool IsAllowedValue<T>(DataDefinition definition, T convertedValue)
    {
        if (definition.AllowedValues.Count == 0)
        {
            return true;
        }

        var stableValue = ToStableText(convertedValue);
        for (var i = 0; i < definition.AllowedValues.Count; i++)
        {
            if (string.Equals(definition.AllowedValues[i].Value, stableValue, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 将值转为稳定文本表示，用于 allowed values 白名单比较和错误信息。
    /// bool 用小写，数值用 InvariantCulture，string[] 用逗号连接。
    /// </summary>
    private static string ToStableText(object? value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolValue => boolValue ? "true" : "false",
            float floatValue => floatValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString(CultureInfo.InvariantCulture),
            int intValue => intValue.ToString(CultureInfo.InvariantCulture),
            string[] arrayValue => string.Join(",", arrayValue),
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
        };
    }

    /// <summary>将原始值转为 string，类型不匹配则抛异常。</summary>
    private static object ConvertString(object rawValue)
    {
        return rawValue is string text
            ? text
            : throw new InvalidCastException($"期望 string，实际 {rawValue.GetType().Name}");
    }

    /// <summary>将原始值转为 string[]。支持直接传入数组或逗号分隔的文本。</summary>
    private static object ConvertStringArray(object rawValue)
    {
        if (rawValue is string[] arrayValue)
        {
            return arrayValue;
        }

        if (rawValue is string textValue)
        {
            return ParseStringArrayText(textValue);
        }

        throw new InvalidCastException($"期望 string_array，实际 {rawValue.GetType().Name}");
    }

    /// <summary>将原始值转为 FeatureModifierEntryData[]。支持直接传入数组或 JSON array 文本。</summary>
    private static object? ConvertModifierList(object rawValue)
    {
        if (rawValue is FeatureModifierEntryData[] arrayValue)
        {
            return arrayValue;
        }

        if (rawValue is string textValue)
        {
            return ParseModifierListText(textValue);
        }

        throw new InvalidCastException($"期望 modifier_list，实际 {rawValue.GetType().Name}");
    }

    /// <summary>将原始值转为 ResourceRef 或运行时对象引用。空字符串返回 null。</summary>
    private static object? ConvertObjectRef(object rawValue)
    {
        if (rawValue is ResourceRef resourceRef)
        {
            return resourceRef.HasValue ? resourceRef : null;
        }

        if (rawValue is string textValue)
        {
            return string.IsNullOrWhiteSpace(textValue) ? null : new ResourceRef(textValue);
        }

        if (!rawValue.GetType().IsValueType)
        {
            return rawValue;
        }

        throw new InvalidCastException($"期望 object_ref，实际 {rawValue.GetType().Name}");
    }

    /// <summary>将原始值转为 int。支持直接传入 int 或数字文本。</summary>
    private static object ConvertInt(object rawValue)
    {
        return rawValue switch
        {
            int intValue => intValue,
            string textValue => int.Parse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"期望 int，实际 {rawValue.GetType().Name}")
        };
    }

    /// <summary>将原始值转为 float。支持 int→float 提升和数字文本解析。</summary>
    private static object ConvertFloat(object rawValue)
    {
        return rawValue switch
        {
            float floatValue => floatValue,
            int intValue => (float)intValue,
            string textValue => float.Parse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"期望 float，实际 {rawValue.GetType().Name}")
        };
    }

    /// <summary>将原始值转为 double。支持 float/int→double 提升和数字文本解析。</summary>
    private static object ConvertDouble(object rawValue)
    {
        return rawValue switch
        {
            double doubleValue => doubleValue,
            float floatValue => (double)floatValue,
            int intValue => (double)intValue,
            string textValue => double.Parse(textValue, NumberStyles.Float, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"期望 double，实际 {rawValue.GetType().Name}")
        };
    }

    /// <summary>将原始值转为 bool。支持直接传入 bool 或 "true"/"false" 文本。</summary>
    private static object ConvertBool(object rawValue)
    {
        return rawValue switch
        {
            bool boolValue => boolValue,
            string textValue => bool.Parse(textValue),
            _ => throw new InvalidCastException($"期望 bool，实际 {rawValue.GetType().Name}")
        };
    }

    /// <summary>将原始值转为 System.Numerics.Vector2。支持 Godot.Vector2 反射读取和 "x,y" 文本解析。</summary>
    private static object ConvertVector2(object rawValue)
    {
        if (TryReadVector2(rawValue, out var x, out var y))
        {
            return new System.Numerics.Vector2(x, y);
        }

        if (rawValue is System.Numerics.Vector2 vectorValue)
        {
            return vectorValue;
        }

        if (rawValue is string textValue)
        {
            var parts = textValue.Split(',', StringSplitOptions.TrimEntries);
            if (parts.Length == 2)
            {
                return new System.Numerics.Vector2(
                    float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                    float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture));
            }
        }

        throw new InvalidCastException($"期望 vector2，实际 {rawValue.GetType().Name}");
    }

    /// <summary>将原始值转为 enum 文本。enum 值统一存储为字符串，不存 int。</summary>
    private static object ConvertEnum(object rawValue)
    {
        if (rawValue is string textValue)
        {
            return textValue;
        }

        if (rawValue.GetType().IsEnum)
        {
            return rawValue.ToString() ?? string.Empty;
        }

        if (rawValue is int intValue)
        {
            return intValue.ToString(CultureInfo.InvariantCulture);
        }

        throw new InvalidCastException($"期望 enum string，实际 {rawValue.GetType().Name}");
    }

    /// <summary>
    /// 将存储值转换为调用方读取类型。
    /// </summary>
    public static object? ConvertForRead(object? rawValue, Type clrType, DataValueType valueType)
    {
        if (rawValue == null)
        {
            return clrType.IsValueType ? Activator.CreateInstance(clrType) : null;
        }

        var targetType = Nullable.GetUnderlyingType(clrType) ?? clrType;
        if (targetType.IsInstanceOfType(rawValue))
        {
            return rawValue;
        }

        if (valueType == DataValueType.Enum)
        {
            if (targetType == typeof(string))
            {
                return Convert.ToString(rawValue, CultureInfo.InvariantCulture) ?? string.Empty;
            }

            if (targetType.IsEnum)
            {
                if (rawValue is string textValue)
                {
                    if (int.TryParse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var numeric))
                    {
                        return Enum.ToObject(targetType, numeric);
                    }

                    return Enum.Parse(targetType, textValue, ignoreCase: false);
                }

                return Enum.ToObject(targetType, Convert.ToInt32(rawValue, CultureInfo.InvariantCulture));
            }
        }

        if (valueType == DataValueType.StringArray && targetType == typeof(string[]))
        {
            return rawValue switch
            {
                string[] arrayValue => arrayValue,
                string textValue => ParseStringArrayText(textValue),
                _ => rawValue
            };
        }

        if (valueType == DataValueType.ModifierList && targetType == typeof(FeatureModifierEntryData[]))
        {
            return rawValue switch
            {
                FeatureModifierEntryData[] arrayValue => arrayValue,
                string textValue => ParseModifierListText(textValue),
                _ => rawValue
            };
        }

        if (valueType == DataValueType.ObjectRef)
        {
            if (targetType == typeof(ResourceRef))
            {
                return rawValue switch
                {
                    ResourceRef resourceRef => resourceRef,
                    string textValue => new ResourceRef(textValue),
                    _ => throw new InvalidCastException($"期望 ResourceRef，实际 {rawValue.GetType().Name}")
                };
            }

            if (targetType.IsInstanceOfType(rawValue))
            {
                return rawValue;
            }
        }

        if (valueType == DataValueType.Vector2 && IsGodotVector2Type(targetType))
        {
            if (rawValue is System.Numerics.Vector2 systemVectorValue)
            {
                return Activator.CreateInstance(targetType, systemVectorValue.X, systemVectorValue.Y);
            }

            if (rawValue is string textValue)
            {
                var parts = textValue.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    return Activator.CreateInstance(
                        targetType,
                        float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                        float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture));
                }
            }
        }

        if (valueType == DataValueType.Vector2 && targetType == typeof(System.Numerics.Vector2))
        {
            if (TryReadVector2(rawValue, out var x, out var y))
            {
                return new System.Numerics.Vector2(x, y);
            }

            if (rawValue is string textValue)
            {
                var parts = textValue.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length == 2)
                {
                    return new System.Numerics.Vector2(
                        float.Parse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture),
                        float.Parse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture));
                }
            }
        }

        return Convert.ChangeType(rawValue, targetType, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// 为 modifier_list JSON 反序列化创建序列化选项，启用 StringEnumConverter 支持枚举文本。
    /// </summary>
    private static JsonSerializerOptions CreateModifierListJsonOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    /// <summary>
    /// 解析 string_array 文本：支持 JSON array 格式 ["a","b"] 和逗号分隔格式 "a,b"。
    /// </summary>
    private static string[] ParseStringArrayText(string textValue)
    {
        if (string.IsNullOrWhiteSpace(textValue))
        {
            return Array.Empty<string>();
        }

        var trimmed = textValue.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                return JsonSerializer.Deserialize<string[]>(trimmed) ?? Array.Empty<string>();
            }
            catch (JsonException ex)
            {
                throw new FormatException($"string_array JSON 解析失败：{textValue}", ex);
            }
        }

        return textValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    /// <summary>
    /// 解析 modifier_list 文本：仅接受 JSON array 格式 [{"Type":"Additive","Value":10}]。
    /// </summary>
    private static FeatureModifierEntryData[] ParseModifierListText(string textValue)
    {
        if (string.IsNullOrWhiteSpace(textValue))
        {
            return Array.Empty<FeatureModifierEntryData>();
        }

        var trimmed = textValue.Trim();
        if (trimmed == "[]")
        {
            return Array.Empty<FeatureModifierEntryData>();
        }

        if (!trimmed.StartsWith("[", StringComparison.Ordinal))
        {
            throw new InvalidCastException("modifier_list 必须是 JSON array。");
        }

        try
        {
            return JsonSerializer.Deserialize<FeatureModifierEntryData[]>(trimmed, ModifierListJsonOptions)
                   ?? Array.Empty<FeatureModifierEntryData>();
        }
        catch (JsonException ex)
        {
            throw new FormatException($"modifier_list JSON 解析失败：{textValue}", ex);
        }
    }

    /// <summary>
    /// 判断是否为运行时对象引用字段：ObjectRef + RuntimeOnly + 有 RuntimeTypeId。
    /// 此类字段拒绝 string/ResourceRef，只接受运行时对象实例。
    /// </summary>
    private static bool RequiresRuntimeObjectReference(DataDefinition definition)
    {
        return definition.ValueType == DataValueType.ObjectRef
               && definition.StoragePolicy == DataStoragePolicy.RuntimeOnly
               && !string.IsNullOrWhiteSpace(definition.RuntimeTypeId);
    }

    /// <summary>
    /// 校验运行时对象值是否匹配 descriptor 声明的 RuntimeTypeId。
    /// null 视为合法（允许清空引用）；ResourceRef 不合法（应使用对象实例）。
    /// </summary>
    private static bool MatchesRuntimeObjectReference(DataDefinition definition, object? value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is ResourceRef)
        {
            return false;
        }

        return MatchesRuntimeType(value.GetType(), definition.RuntimeTypeId);
    }

    /// <summary>
    /// 通过反射匹配类型：检查实际类型的 FullName/Name 及其实现的接口是否与 runtimeTypeId 匹配。
    /// </summary>
    private static bool MatchesRuntimeType(Type actualType, string runtimeTypeId)
    {
        var expected = runtimeTypeId.Trim();
        for (var type = actualType; type != null; type = type.BaseType)
        {
            if (string.Equals(type.FullName, expected, StringComparison.Ordinal)
                || string.Equals(type.Name, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        foreach (var interfaceType in actualType.GetInterfaces())
        {
            if (string.Equals(interfaceType.FullName, expected, StringComparison.Ordinal)
                || string.Equals(interfaceType.Name, expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 检查类型是否为 Godot.Vector2（通过 FullName 字符串匹配，避免直接引用 Godot 程序集）。
    /// </summary>
    private static bool IsGodotVector2Type(Type type)
    {
        return string.Equals(type.FullName, "Godot.Vector2", StringComparison.Ordinal);
    }

    /// <summary>
    /// 通过反射读取 Godot.Vector2 的 X/Y 字段值。
    /// 避免直接引用 Godot 程序集，通过 Property/Field 反射获取。
    /// </summary>
    private static bool TryReadVector2(object rawValue, out float x, out float y)
    {
        x = 0f;
        y = 0f;
        if (!IsGodotVector2Type(rawValue.GetType()))
        {
            return false;
        }

        var type = rawValue.GetType();
        var xMember = type.GetProperty("X") ?? (object?)type.GetField("X");
        var yMember = type.GetProperty("Y") ?? (object?)type.GetField("Y");
        object? rawX = xMember switch
        {
            System.Reflection.PropertyInfo property => property.GetValue(rawValue),
            System.Reflection.FieldInfo field => field.GetValue(rawValue),
            _ => null
        };
        object? rawY = yMember switch
        {
            System.Reflection.PropertyInfo property => property.GetValue(rawValue),
            System.Reflection.FieldInfo field => field.GetValue(rawValue),
            _ => null
        };

        if (rawX == null || rawY == null)
        {
            return false;
        }

        x = Convert.ToSingle(rawX, CultureInfo.InvariantCulture);
        y = Convert.ToSingle(rawY, CultureInfo.InvariantCulture);
        return true;
    }

    /// <summary>
    /// 尝试将 object 值转为 double。支持 int/float/double 三种数值类型。
    /// </summary>
    private static bool TryGetNumeric(object value, out double numericValue)
    {
        switch (value)
        {
            case int intValue:
                numericValue = intValue;
                return true;
            case float floatValue:
                numericValue = floatValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            default:
                numericValue = 0;
                return false;
        }
    }

    /// <summary>
    /// 泛型版本：尝试将 T 值转为 double，用于 typed modifier 计算路径避免装箱。
    /// </summary>
    private static bool TryGetNumeric<T>(T value, out double numericValue)
    {
        switch (value)
        {
            case int intValue:
                numericValue = intValue;
                return true;
            case float floatValue:
                numericValue = floatValue;
                return true;
            case double doubleValue:
                numericValue = doubleValue;
                return true;
            default:
                numericValue = 0;
                return false;
        }
    }

    /// <summary>
    /// 将 double 值转回原始 CLR 类型（int/float/double），用于 untyped 范围策略 clamp 后恢复原类型。
    /// </summary>
    private static object ConvertNumericToOriginalType(double value, Type originalType)
    {
        if (originalType == typeof(int))
        {
            return (int)value;
        }

        if (originalType == typeof(float))
        {
            return (float)value;
        }

        return value;
    }

    /// <summary>
    /// 泛型版本：将 double 值转回 T 类型，使用 Unsafe.As 避免装箱。
    /// </summary>
    private static T ConvertNumericToTyped<T>(double value)
    {
        if (typeof(T) == typeof(int))
        {
            var typedValue = (int)value;
            return Unsafe.As<int, T>(ref typedValue);
        }

        if (typeof(T) == typeof(int?))
        {
            int? typedValue = (int)value;
            return Unsafe.As<int?, T>(ref typedValue);
        }

        if (typeof(T) == typeof(float))
        {
            var typedValue = (float)value;
            return Unsafe.As<float, T>(ref typedValue);
        }

        if (typeof(T) == typeof(float?))
        {
            float? typedValue = (float)value;
            return Unsafe.As<float?, T>(ref typedValue);
        }

        if (typeof(T) == typeof(double))
        {
            return Unsafe.As<double, T>(ref value);
        }

        if (typeof(T) == typeof(double?))
        {
            double? typedValue = value;
            return Unsafe.As<double?, T>(ref typedValue);
        }

        return default!;
    }
}

/// <summary>
/// descriptor-first Data 运行时存储。
/// </summary>
public sealed class DataRuntimeStorage
{
    /// <summary>字段定义 catalog，提供 descriptor 查询和 computed 依赖索引。</summary>
    private readonly DataDefinitionCatalog _catalog;
    /// <summary>stable key → 运行时槽位，按需创建。</summary>
    private readonly Dictionary<string, IDataSlot> _slots = new(StringComparer.Ordinal);
    /// <summary>标记为脏的 computed key 集合，下次读取时重新计算。</summary>
    private readonly HashSet<string> _dirtyComputedKeys = new(StringComparer.Ordinal);
    /// <summary>resolver 读取当前 Data 容器的上下文引用。</summary>
    private readonly Data? _computeContext;

    /// <summary>
    /// 创建 Data 运行时存储。
    /// </summary>
    /// <param name="catalog">字段定义 catalog。</param>
    /// <param name="computeContext">resolver 读取当前 Data 的上下文。</param>
    public DataRuntimeStorage(DataDefinitionCatalog catalog, Data? computeContext = null)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog; // 字段定义 catalog
        _computeContext = computeContext; // resolver 读取上下文
    }

    /// <summary>
    /// typed 字段变更事件，业务桥接优先使用。
    /// </summary>
    public event Action<IDataChangeRecord>? TypedChanged;

    /// <summary>
    /// diagnostic 字段变更事件；值类型会装箱，仅用于 TestSystem/debug 兼容。
    /// </summary>
    public event Action<DataChangeRecord>? Changed;

    /// <summary>
    /// 是否存在已写入基础值。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public bool HasValue(string stableKey)
    {
        return _slots.TryGetValue(stableKey, out var slot) && slot.HasValue;
    }

    /// <summary>
    /// 检查 stable key 是否存在于 catalog。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public bool HasDefinition(string stableKey)
    {
        return _catalog.TryGet(stableKey, out _);
    }

    /// <summary>
    /// 读取类型安全字段值。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">字段 stable key 句柄。</param>
    public T Get<T>(DataKey<T> key)
    {
        return Get<T>(key.StableKey);
    }

    /// <summary>
    /// 读取字段值；未写入时返回 descriptor default。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="stableKey">字段 stable key。</param>
    public T Get<T>(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        if (!DataValueConverter.IsCompatible<T>(definition.ValueType))
        {
            throw new InvalidOperationException($"Data.Get 类型不匹配：{stableKey} expected={definition.ValueType}, actual={typeof(T).Name}");
        }

        return definition.IsComputed
            ? GetComputedValue<T>(definition)
            : GetOrCreateTypedSlot<T>(definition).GetEffectiveValue();
    }

    /// <summary>
    /// 写入类型安全字段值。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">字段 stable key 句柄。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    public bool Set<T>(DataKey<T> key, T value, DataWriteSource source = DataWriteSource.Runtime)
    {
        return TrySet(key, value, out _, source);
    }

    /// <summary>
    /// 写入类型安全字段值，并输出结构化诊断。
    /// </summary>
    /// <typeparam name="T">字段值类型。</typeparam>
    /// <param name="key">字段 stable key 句柄。</param>
    /// <param name="value">新值。</param>
    /// <param name="report">写入诊断报告。</param>
    /// <param name="source">写入来源。</param>
    public bool TrySet<T>(DataKey<T> key, T value, out DataWriteReport report, DataWriteSource source = DataWriteSource.Runtime)
    {
        report = new DataWriteReport(key.StableKey, source);
        if (!_catalog.TryGet(key.StableKey, out var definition))
        {
            report.AddError(new DataWriteError(
                "unknown_key",
                key.StableKey,
                $"未注册 DataDefinition：{key.StableKey}",
                source,
                string.Empty,
                typeof(T).Name,
                null,
                value?.ToString()));
            return false;
        }

        if (!DataValueConverter.IsCompatible<T>(definition.ValueType))
        {
            report.AddError(new DataWriteError(
                "wrong_clr_type",
                key.StableKey,
                $"Data value type 不匹配：{key.StableKey} ({definition.ValueType})",
                source,
                definition.ValueType.ToString(),
                typeof(T).Name,
                null,
                value?.ToString()));
            return false;
        }

        if (!DataValueConverter.TryApplyTypedWritePoliciesWithReport(definition, value, source, out var finalValue, out var error))
        {
            if (error != null)
            {
                report.AddError(error);
            }

            return false;
        }

        var slot = GetOrCreateTypedSlot<T>(definition);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        if (!slot.SetValue(finalValue))
        {
            return false;
        }

        MarkDependentComputedDirty(definition.StableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 写入字段值；仅用于 loader/debug/TestSystem 边界，业务热路径应使用 DataKey&lt;T&gt;。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    public bool SetUntyped(string stableKey, object? value, DataWriteSource source = DataWriteSource.Runtime)
    {
        var definition = _catalog.GetRequired(stableKey);
        return SetUntyped(definition, value, source);
    }

    /// <summary>
    /// 写入字段值并输出结构化诊断；仅用于 loader/debug/TestSystem 边界，值类型进入这里会装箱。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TrySetUntyped(string stableKey, object? value, DataWriteSource source, out DataWriteReport report)
    {
        report = new DataWriteReport(stableKey, source);
        if (!_catalog.TryGet(stableKey, out var definition))
        {
            report.AddError(new DataWriteError(
                "unknown_key",
                stableKey,
                $"未注册 DataDefinition：{stableKey}",
                source,
                string.Empty,
                value?.GetType().Name,
                null,
                value?.ToString()));
            return false;
        }

        return TrySetUntyped(definition, value, source, out report);
    }

    /// <summary>
    /// 使用已解析 definition 写入字段值；仅用于 loader/debug/TestSystem 边界。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    public bool SetUntyped(DataDefinition definition, object? value, DataWriteSource source = DataWriteSource.Runtime)
    {
        return TrySetUntyped(definition, value, source, out _);
    }

    /// <summary>
    /// 使用已解析 definition 写入字段值并输出结构化诊断；值类型进入这里会装箱，不作为业务热路径。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TrySetUntyped(DataDefinition definition, object? value, DataWriteSource source, out DataWriteReport report)
    {
        report = new DataWriteReport(definition.StableKey, source);
        // 仅用于 loader/debug/TestSystem 边界；业务热路径应使用 DataKey<T>，值类型进入这里会装箱。
        if (!DataValueConverter.TryApplyWritePoliciesWithReport(definition, value, source, out var finalValue, out var error))
        {
            if (error != null)
            {
                report.AddError(error);
            }

            return false;
        }

        var slot = GetOrCreateBoundarySlot(definition, finalValue);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        if (!slot.SetValueFromBoundary(finalValue))
        {
            return false;
        }

        MarkDependentComputedDirty(definition.StableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 添加字段修改器。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="modifier">修改器实例。</param>
    /// <param name="source">写入来源。</param>
    public bool AddModifier(string stableKey, DataModifier modifier, DataWriteSource source = DataWriteSource.Runtime)
    {
        var definition = _catalog.GetRequired(stableKey);
        return TryAddModifierResolved(definition, stableKey, modifier, source, out _);
    }

    /// <summary>
    /// 添加字段修改器，并输出结构化诊断。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="modifier">修改器实例。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TryAddModifier(string stableKey, DataModifier modifier, DataWriteSource source, out DataWriteReport report)
    {
        report = new DataWriteReport(stableKey, source);
        if (!_catalog.TryGet(stableKey, out var definition))
        {
            report.AddError(new DataWriteError(
                "unknown_key",
                stableKey,
                $"未注册 DataDefinition：{stableKey}",
                source,
                string.Empty,
                modifier.GetType().Name,
                null,
                modifier.ToString()));
            return false;
        }

        return TryAddModifierResolved(definition, stableKey, modifier, source, out report);
    }

    private bool TryAddModifierResolved(
        DataDefinition definition,
        string stableKey,
        DataModifier modifier,
        DataWriteSource source,
        out DataWriteReport report)
    {
        report = new DataWriteReport(stableKey, source);
        if (!CanApplyModifier(definition, source))
        {
            report.AddError(new DataWriteError(
                "modifier_policy_rejected",
                stableKey,
                $"Data modifier policy 拒绝写入：{stableKey} ({definition.ModifierPolicy}, source={source})",
                source,
                definition.ValueType.ToString(),
                modifier.GetType().Name,
                definition.ModifierPolicy.ToString(),
                modifier.Value.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        var slot = GetOrCreateBoundarySlot(definition);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        if (!slot.AddModifier(modifier))
        {
            report.AddError(new DataWriteError(
                "duplicate_modifier",
                stableKey,
                $"Data modifier id 重复：{stableKey} id={modifier.Id}",
                source,
                definition.ValueType.ToString(),
                modifier.GetType().Name,
                definition.ModifierPolicy.ToString(),
                modifier.Value.ToString(CultureInfo.InvariantCulture)));
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 移除字段修改器。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    /// <param name="modifierId">修改器 id。</param>
    public bool RemoveModifier(string stableKey, string modifierId)
    {
        var definition = _catalog.GetRequired(stableKey);
        var slot = GetOrCreateBoundarySlot(definition);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        if (!slot.RemoveModifier(modifierId))
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 按来源移除所有字段修改器。
    /// </summary>
    /// <param name="source">修改器来源。</param>
    public int RemoveModifiersBySource(DataModifierSource source)
    {
        if (source.IsEmpty)
        {
            return 0;
        }

        var removedTotal = 0;
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValueForDiagnostics();
            var removed = pair.Value.RemoveModifiersBySource(source);
            if (removed == 0)
            {
                continue;
            }

            removedTotal += removed;
            MarkDependentComputedDirty(pair.Key);
            pair.Value.PublishChange(this, oldValue);
        }

        return removedTotal;
    }

    /// <summary>
    /// 按 id 移除所有字段修改器。
    /// </summary>
    /// <param name="modifierId">修改器 id。</param>
    public int RemoveModifierById(string modifierId)
    {
        if (string.IsNullOrWhiteSpace(modifierId))
        {
            return 0;
        }

        var removedTotal = 0;
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValueForDiagnostics();
            var removed = pair.Value.RemoveModifier(modifierId) ? 1 : 0;
            if (removed == 0)
            {
                continue;
            }

            removedTotal += removed;
            MarkDependentComputedDirty(pair.Key);
            pair.Value.PublishChange(this, oldValue);
        }

        return removedTotal;
    }

    /// <summary>
    /// 获取字段修改器副本。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public List<DataModifier> GetModifiers(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        return GetOrCreateBoundarySlot(definition).GetModifiers();
    }

    /// <summary>
    /// 清除字段所有修改器。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public bool ClearModifiers(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        var slot = GetOrCreateBoundarySlot(definition);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        if (!slot.ClearModifiers())
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 清除所有字段修改器。
    /// </summary>
    public int ClearAllModifiers()
    {
        var clearedTotal = 0;
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValueForDiagnostics();
            if (!pair.Value.ClearModifiers())
            {
                continue;
            }

            clearedTotal++;
            MarkDependentComputedDirty(pair.Key);
            pair.Value.PublishChange(this, oldValue);
        }

        return clearedTotal;
    }

    /// <summary>
    /// 移除字段运行时基础值，后续读取回退 descriptor default。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public bool Remove(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        var slot = GetOrCreateBoundarySlot(definition);
        var oldValue = slot.GetEffectiveValueForDiagnostics();
        var valueChanged = slot.ClearValue();
        var modifiersChanged = slot.ClearModifiers();
        if (!valueChanged && !modifiersChanged)
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        slot.PublishChange(this, oldValue);
        return true;
    }

    /// <summary>
    /// 清空所有已写入运行时基础值。
    /// </summary>
    public void Clear()
    {
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValueForDiagnostics();
            var valueChanged = pair.Value.ClearValue();
            var modifiersChanged = pair.Value.ClearModifiers();
            if (valueChanged || modifiersChanged)
            {
                MarkDependentComputedDirty(pair.Key);
                pair.Value.PublishChange(this, oldValue);
            }
        }

        _dirtyComputedKeys.Clear();
    }

    /// <summary>
    /// 获取当前已写入基础值副本；仅供 diagnostics/TestSystem dump 使用，值类型会在返回字典中装箱。
    /// </summary>
    public Dictionary<string, object?> GetAllValuesForDiagnostics()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in _slots)
        {
            if (pair.Value.HasValue)
            {
                values[pair.Key] = pair.Value.GetStoredValueForDiagnostics();
            }
        }

        return values;
    }

    /// <summary>
    /// 兼容边界：获取当前已写入基础值副本。
    /// 新调用应使用 GetAllValuesForDiagnostics，明确这是 boxed diagnostic dump。
    /// </summary>
    [Obsolete("GetAllValues 是 diagnostic 兼容包装；请使用 GetAllValuesForDiagnostics。")]
    public Dictionary<string, object?> GetAllValues()
    {
        return GetAllValuesForDiagnostics();
    }

    /// <summary>
    /// 检查 computed 字段是否被依赖写入标脏。
    /// </summary>
    /// <param name="stableKey">computed 字段 stable key。</param>
    public bool IsComputedDirty(string stableKey)
    {
        return _dirtyComputedKeys.Contains(stableKey);
    }

    /// <summary>
    /// 获取或创建 typed 槽位。如果已有槽位但类型不匹配（理论上不应发生），则替换并迁移基础值和修改器。
    /// </summary>
    private DataSlot<T> GetOrCreateTypedSlot<T>(DataDefinition definition)
    {
        if (_slots.TryGetValue(definition.StableKey, out var existing))
        {
            if (existing is DataSlot<T> typedSlot)
            {
                return typedSlot;
            }

            return ReplaceSlot<T>(definition, existing);
        }

        var slot = new DataSlot<T>(definition);
        _slots[definition.StableKey] = slot;
        return slot;
    }

    /// <summary>
    /// 获取或创建边界写入槽位。对于 object_ref + runtime_only 字段，使用实际运行时对象类型创建槽位。
    /// </summary>
    private IDataSlot GetOrCreateBoundarySlot(DataDefinition definition, object? value = null)
    {
        if (_slots.TryGetValue(definition.StableKey, out var slot))
        {
            return slot;
        }

        slot = CreateSlot(ResolveBoundarySlotType(definition, value), definition);
        _slots[definition.StableKey] = slot;
        return slot;
    }

    /// <summary>
    /// 读取 computed 字段值。使用 dirty cache：未标脏且有缓存值时直接返回；否则调用 resolver 重新计算并缓存。
    /// </summary>
    private T GetComputedValue<T>(DataDefinition definition)
    {
        var slot = GetOrCreateTypedSlot<T>(definition);
        if (!_dirtyComputedKeys.Contains(definition.StableKey) && slot.HasValue)
        {
            return slot.GetEffectiveValue();
        }

        if (_computeContext == null)
        {
            throw new InvalidOperationException($"Data computed resolver 缺少 Data 上下文：{definition.StableKey}");
        }

        var resolver = _catalog.ComputeRegistry.GetRequired<T>(definition.StableKey, definition.ComputeId);
        var computedValue = resolver.Compute(_computeContext, definition);
        slot.SetValue(computedValue);
        _dirtyComputedKeys.Remove(definition.StableKey);
        return slot.GetEffectiveValue();
    }

    /// <summary>
    /// 从 DataSlot<T> 发布 typed 与 diagnostic 双层变更事件。
    /// </summary>
    internal void PublishTypedChange<T>(DataDefinition definition, T oldValue, T newValue)
    {
        var record = new DataChangeRecord<T>(new DataKey<T>(definition.StableKey), oldValue, newValue);
        TypedChanged?.Invoke(record);
        Changed?.Invoke(new DataChangeRecord(
            definition.StableKey,
            record.OldValueForDiagnostics,
            record.NewValueForDiagnostics));
    }

    /// <summary>
    /// 替换已有槽位：创建新 typed 槽位，迁移旧槽位的基础值和修改器。
    /// </summary>
    private DataSlot<T> ReplaceSlot<T>(DataDefinition definition, IDataSlot existing)
    {
        var replacement = new DataSlot<T>(definition);
        if (existing.HasValue)
        {
            replacement.SetValueFromBoundary(existing.GetStoredValueForDiagnostics());
        }

        var modifiers = existing.GetModifiers();
        for (var i = 0; i < modifiers.Count; i++)
        {
            replacement.AddModifier(modifiers[i]);
        }

        _slots[definition.StableKey] = replacement;
        return replacement;
    }

    /// <summary>
    /// 通过反射创建 DataSlot&lt;T&gt; 实例，用于边界写入时无法在编译期确定 T 的场景。
    /// </summary>
    private static IDataSlot CreateSlot(Type valueType, DataDefinition definition)
    {
        return (IDataSlot)Activator.CreateInstance(typeof(DataSlot<>).MakeGenericType(valueType), definition)!;
    }

    /// <summary>
    /// 解析边界写入的槽位 CLR 类型。
    /// runtime_only object_ref 优先使用实际运行时对象类型；否则按 descriptor ValueType 映射。
    /// </summary>
    private static Type ResolveBoundarySlotType(DataDefinition definition, object? value)
    {
        if (definition.ValueType == DataValueType.ObjectRef
            && definition.StoragePolicy == DataStoragePolicy.RuntimeOnly
            && !string.IsNullOrWhiteSpace(definition.RuntimeTypeId))
        {
            return value != null && value is not ResourceRef && !value.GetType().IsValueType
                ? value.GetType()
                : typeof(object);
        }

        if (definition.ValueType == DataValueType.ObjectRef
            && value != null
            && value is not ResourceRef
            && !value.GetType().IsValueType)
        {
            return value.GetType();
        }

        return definition.ValueType switch
        {
            DataValueType.String => typeof(string),
            DataValueType.StringArray => typeof(string[]),
            DataValueType.Int => typeof(int),
            DataValueType.Float => typeof(float),
            DataValueType.Double => typeof(double),
            DataValueType.Bool => typeof(bool),
            DataValueType.Vector2 => typeof(System.Numerics.Vector2),
            DataValueType.Enum => typeof(string),
            DataValueType.ModifierList => typeof(FeatureModifierEntryData[]),
            DataValueType.ObjectRef => typeof(ResourceRef),
            _ => typeof(object)
        };
    }

    /// <summary>
    /// 校验 modifier 策略：仅数值类型（int/float/double）且 modifierPolicy 允许时可添加。
    /// Numeric 全放行；DebugOnly 仅 Debug 来源。
    /// </summary>
    private static bool CanApplyModifier(DataDefinition definition, DataWriteSource source)
    {
        if (!IsNumericValueType(definition.ValueType))
        {
            return false;
        }

        return definition.ModifierPolicy switch
        {
            DataModifierPolicy.Numeric => true,
            DataModifierPolicy.DebugOnly => source == DataWriteSource.Debug,
            _ => false
        };
    }

    /// <summary>
    /// 判断 descriptor 值类型是否为数值类型（int/float/double），modifier 仅对数值类型有效。
    /// </summary>
    private static bool IsNumericValueType(DataValueType valueType)
    {
        return valueType is DataValueType.Int or DataValueType.Float or DataValueType.Double;
    }

    /// <summary>
    /// 当基础值或 modifier 变化时，传递标脏所有直接和间接依赖该 key 的 computed 字段。
    /// </summary>
    private void MarkDependentComputedDirty(string stableKey)
    {
        var dependents = _catalog.GetDependentComputedKeys(stableKey);
        for (var i = 0; i < dependents.Count; i++)
        {
            MarkComputedDirtyRecursive(dependents[i]);
        }
    }

    /// <summary>
    /// 递归标脏：将当前 computed key 加入脏集，然后递归标脏依赖它的上层 computed。
    /// </summary>
    private void MarkComputedDirtyRecursive(string stableKey)
    {
        _dirtyComputedKeys.Add(stableKey);
        var dependents = _catalog.GetDependentComputedKeys(stableKey);
        for (var i = 0; i < dependents.Count; i++)
        {
            MarkComputedDirtyRecursive(dependents[i]);
        }
    }
}
