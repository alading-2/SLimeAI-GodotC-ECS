using System;
using System.Collections.Generic;
using System.Globalization;
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
/// Data 字段变更记录。
/// </summary>
/// <param name="StableKey">发生变更的 stable key。</param>
/// <param name="OldValue">变更前有效值。</param>
/// <param name="NewValue">变更后有效值。</param>
public sealed record DataChangeRecord(string StableKey, object? OldValue, object? NewValue);

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
/// 单个 descriptor 字段的运行时槽位。
/// </summary>
public sealed class DataSlot
{
    private readonly List<DataModifier> _modifiers = new();

    /// <summary>
    /// 创建运行时槽位。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    public DataSlot(DataDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition; // descriptor 定义
    }

    /// <summary>
    /// 字段 descriptor 定义。
    /// </summary>
    public DataDefinition Definition { get; }

    /// <summary>
    /// 是否已有运行时基础值。
    /// </summary>
    public bool HasValue { get; private set; }

    /// <summary>
    /// 当前运行时基础值。
    /// </summary>
    public object? Value { get; private set; }

    /// <summary>
    /// 获取当前有效值；未写入时回退到 descriptor default。
    /// </summary>
    public object? GetEffectiveValue()
    {
        var baseValue = HasValue ? Value : Definition.DefaultValue;
        return _modifiers.Count == 0 ? baseValue : ApplyModifiers(baseValue);
    }

    /// <summary>
    /// 写入运行时基础值。
    /// </summary>
    /// <param name="value">已转换并通过策略校验的值。</param>
    public bool SetValue(object? value)
    {
        if (HasValue && Equals(Value, value))
        {
            return false;
        }

        Value = value;
        HasValue = true;
        return true;
    }

    /// <summary>
    /// 清除运行时基础值。
    /// </summary>
    public bool ClearValue()
    {
        if (!HasValue)
        {
            return false;
        }

        Value = null;
        HasValue = false;
        return true;
    }

    /// <summary>
    /// 添加字段修改器。
    /// </summary>
    /// <param name="modifier">修改器实例。</param>
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

    /// <summary>
    /// 按 id 移除字段修改器。
    /// </summary>
    /// <param name="modifierId">修改器 id。</param>
    public bool RemoveModifier(string modifierId)
    {
        return _modifiers.RemoveAll(modifier => string.Equals(modifier.Id, modifierId, StringComparison.Ordinal)) > 0;
    }

    /// <summary>
    /// 按来源移除字段修改器。
    /// </summary>
    /// <param name="source">修改器来源。</param>
    public int RemoveModifiersBySource(object source)
    {
        return _modifiers.RemoveAll(modifier => Equals(modifier.Source, source));
    }

    /// <summary>
    /// 清除字段所有修改器。
    /// </summary>
    public bool ClearModifiers()
    {
        if (_modifiers.Count == 0)
        {
            return false;
        }

        _modifiers.Clear();
        return true;
    }

    /// <summary>
    /// 获取字段修改器副本。
    /// </summary>
    public List<DataModifier> GetModifiers()
    {
        return new List<DataModifier>(_modifiers);
    }

    private object? ApplyModifiers(object? baseValue)
    {
        if (baseValue == null || !TryGetNumeric(baseValue, out var numericBase))
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

        return ConvertNumericToDefinitionType(effective);
    }

    private object ConvertNumericToDefinitionType(double value)
    {
        return Definition.ValueType switch
        {
            DataValueType.Int => (object)(int)value,
            DataValueType.Float => (object)(float)value,
            DataValueType.Double => (object)value,
            _ => (object)value
        };
    }

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
                numericValue = 0d;
                return false;
        }
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

    private static object ConvertString(object rawValue)
    {
        return rawValue is string text
            ? text
            : throw new InvalidCastException($"期望 string，实际 {rawValue.GetType().Name}");
    }

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

    private static object ConvertInt(object rawValue)
    {
        return rawValue switch
        {
            int intValue => intValue,
            string textValue => int.Parse(textValue, NumberStyles.Integer, CultureInfo.InvariantCulture),
            _ => throw new InvalidCastException($"期望 int，实际 {rawValue.GetType().Name}")
        };
    }

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

    private static object ConvertBool(object rawValue)
    {
        return rawValue switch
        {
            bool boolValue => boolValue,
            string textValue => bool.Parse(textValue),
            _ => throw new InvalidCastException($"期望 bool，实际 {rawValue.GetType().Name}")
        };
    }

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

    private static JsonSerializerOptions CreateModifierListJsonOptions()
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

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

    private static bool RequiresRuntimeObjectReference(DataDefinition definition)
    {
        return definition.ValueType == DataValueType.ObjectRef
               && definition.StoragePolicy == DataStoragePolicy.RuntimeOnly
               && !string.IsNullOrWhiteSpace(definition.RuntimeTypeId);
    }

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

    private static bool IsGodotVector2Type(Type type)
    {
        return string.Equals(type.FullName, "Godot.Vector2", StringComparison.Ordinal);
    }

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
}

/// <summary>
/// descriptor-first Data 运行时存储。
/// </summary>
public sealed class DataRuntimeStorage
{
    private readonly DataDefinitionCatalog _catalog;
    private readonly Dictionary<string, DataSlot> _slots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, object?> _computedCache = new(StringComparer.Ordinal);
    private readonly HashSet<string> _dirtyComputedKeys = new(StringComparer.Ordinal);
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
    /// 字段变更事件。
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

        var value = definition.IsComputed
            ? GetComputedValue(definition)
            : GetOrCreateSlot(definition).GetEffectiveValue();
        return (T)DataValueConverter.ConvertForRead(value, typeof(T), definition.ValueType)!;
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
        return TrySetUntyped(key.StableKey, value, source, out report);
    }

    /// <summary>
    /// 写入字段值。
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
    /// 写入字段值，并输出结构化诊断。
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
    /// 使用已解析 definition 写入字段值。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    public bool SetUntyped(DataDefinition definition, object? value, DataWriteSource source = DataWriteSource.Runtime)
    {
        return TrySetUntyped(definition, value, source, out _);
    }

    /// <summary>
    /// 使用已解析 definition 写入字段值，并输出结构化诊断。
    /// </summary>
    /// <param name="definition">字段 descriptor 定义。</param>
    /// <param name="value">新值。</param>
    /// <param name="source">写入来源。</param>
    /// <param name="report">写入诊断报告。</param>
    public bool TrySetUntyped(DataDefinition definition, object? value, DataWriteSource source, out DataWriteReport report)
    {
        report = new DataWriteReport(definition.StableKey, source);
        if (!DataValueConverter.TryApplyWritePoliciesWithReport(definition, value, source, out var finalValue, out var error))
        {
            if (error != null)
            {
                report.AddError(error);
            }

            return false;
        }

        var slot = GetOrCreateSlot(definition);
        var oldValue = slot.GetEffectiveValue();
        if (!slot.SetValue(finalValue))
        {
            return false;
        }

        MarkDependentComputedDirty(definition.StableKey);
        Changed?.Invoke(new DataChangeRecord(definition.StableKey, oldValue, slot.GetEffectiveValue()));
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

        var slot = GetOrCreateSlot(definition);
        var oldValue = slot.GetEffectiveValue();
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
        Changed?.Invoke(new DataChangeRecord(stableKey, oldValue, slot.GetEffectiveValue()));
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
        var slot = GetOrCreateSlot(definition);
        var oldValue = slot.GetEffectiveValue();
        if (!slot.RemoveModifier(modifierId))
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        Changed?.Invoke(new DataChangeRecord(stableKey, oldValue, slot.GetEffectiveValue()));
        return true;
    }

    /// <summary>
    /// 按来源移除所有字段修改器。
    /// </summary>
    /// <param name="source">修改器来源。</param>
    public int RemoveModifiersBySource(object? source)
    {
        if (source == null)
        {
            return 0;
        }

        var removedTotal = 0;
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValue();
            var removed = pair.Value.RemoveModifiersBySource(source);
            if (removed == 0)
            {
                continue;
            }

            removedTotal += removed;
            MarkDependentComputedDirty(pair.Key);
            Changed?.Invoke(new DataChangeRecord(pair.Key, oldValue, pair.Value.GetEffectiveValue()));
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
            var oldValue = pair.Value.GetEffectiveValue();
            var removed = pair.Value.RemoveModifier(modifierId) ? 1 : 0;
            if (removed == 0)
            {
                continue;
            }

            removedTotal += removed;
            MarkDependentComputedDirty(pair.Key);
            Changed?.Invoke(new DataChangeRecord(pair.Key, oldValue, pair.Value.GetEffectiveValue()));
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
        return GetOrCreateSlot(definition).GetModifiers();
    }

    /// <summary>
    /// 清除字段所有修改器。
    /// </summary>
    /// <param name="stableKey">字段 stable key。</param>
    public bool ClearModifiers(string stableKey)
    {
        var definition = _catalog.GetRequired(stableKey);
        var slot = GetOrCreateSlot(definition);
        var oldValue = slot.GetEffectiveValue();
        if (!slot.ClearModifiers())
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        Changed?.Invoke(new DataChangeRecord(stableKey, oldValue, slot.GetEffectiveValue()));
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
            var oldValue = pair.Value.GetEffectiveValue();
            if (!pair.Value.ClearModifiers())
            {
                continue;
            }

            clearedTotal++;
            MarkDependentComputedDirty(pair.Key);
            Changed?.Invoke(new DataChangeRecord(pair.Key, oldValue, pair.Value.GetEffectiveValue()));
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
        var slot = GetOrCreateSlot(definition);
        var oldValue = slot.GetEffectiveValue();
        var valueChanged = slot.ClearValue();
        var modifiersChanged = slot.ClearModifiers();
        if (!valueChanged && !modifiersChanged)
        {
            return false;
        }

        MarkDependentComputedDirty(stableKey);
        Changed?.Invoke(new DataChangeRecord(stableKey, oldValue, slot.GetEffectiveValue()));
        return true;
    }

    /// <summary>
    /// 清空所有已写入运行时基础值。
    /// </summary>
    public void Clear()
    {
        foreach (var pair in _slots)
        {
            var oldValue = pair.Value.GetEffectiveValue();
            var valueChanged = pair.Value.ClearValue();
            var modifiersChanged = pair.Value.ClearModifiers();
            if (valueChanged || modifiersChanged)
            {
                MarkDependentComputedDirty(pair.Key);
                Changed?.Invoke(new DataChangeRecord(pair.Key, oldValue, pair.Value.GetEffectiveValue()));
            }
        }

        _computedCache.Clear();
        _dirtyComputedKeys.Clear();
    }

    /// <summary>
    /// 获取当前已写入基础值副本。
    /// </summary>
    public Dictionary<string, object?> GetAllValues()
    {
        var values = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var pair in _slots)
        {
            if (pair.Value.HasValue)
            {
                values[pair.Key] = pair.Value.Value;
            }
        }

        return values;
    }

    /// <summary>
    /// 检查 computed 字段是否被依赖写入标脏。
    /// </summary>
    /// <param name="stableKey">computed 字段 stable key。</param>
    public bool IsComputedDirty(string stableKey)
    {
        return _dirtyComputedKeys.Contains(stableKey);
    }

    private DataSlot GetOrCreateSlot(DataDefinition definition)
    {
        if (_slots.TryGetValue(definition.StableKey, out var slot))
        {
            return slot;
        }

        slot = new DataSlot(definition);
        _slots[definition.StableKey] = slot;
        return slot;
    }

    private object? GetComputedValue(DataDefinition definition)
    {
        if (!_dirtyComputedKeys.Contains(definition.StableKey) && _computedCache.TryGetValue(definition.StableKey, out var cached))
        {
            return cached;
        }

        if (_computeContext == null)
        {
            throw new InvalidOperationException($"Data computed resolver 缺少 Data 上下文：{definition.StableKey}");
        }

        var resolver = _catalog.ComputeRegistry.GetRequired(definition.ComputeId);
        var rawValue = resolver.Compute(_computeContext, definition);
        if (!DataValueConverter.TryConvert(rawValue, definition.ValueType, out var computedValue, out var error))
        {
            throw new InvalidOperationException($"Data computed resolver 返回值类型不匹配：{definition.StableKey} ({definition.ValueType}) {error}");
        }

        _computedCache[definition.StableKey] = computedValue;
        _dirtyComputedKeys.Remove(definition.StableKey);
        return computedValue;
    }

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

    private static bool IsNumericValueType(DataValueType valueType)
    {
        return valueType is DataValueType.Int or DataValueType.Float or DataValueType.Double;
    }

    private void MarkDependentComputedDirty(string stableKey)
    {
        var dependents = _catalog.GetDependentComputedKeys(stableKey);
        for (var i = 0; i < dependents.Count; i++)
        {
            MarkComputedDirtyRecursive(dependents[i]);
        }
    }

    private void MarkComputedDirtyRecursive(string stableKey)
    {
        _dirtyComputedKeys.Add(stableKey);
        _computedCache.Remove(stableKey);
        var dependents = _catalog.GetDependentComputedKeys(stableKey);
        for (var i = 0; i < dependents.Count; i++)
        {
            MarkComputedDirtyRecursive(dependents[i]);
        }
    }
}
