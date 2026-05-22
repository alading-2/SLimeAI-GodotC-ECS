using System;
using System.Collections.Generic;

/// <summary>
/// Runtime Data typed key 契约。
/// <para>stable string 只用于 catalog/snapshot/debug 边界，业务代码应持有 DataKey&lt;T&gt;。</para>
/// </summary>
public interface IDataKey
{
    string Key { get; }
    Type ValueType { get; }
    object? UntypedDefaultValue { get; }
    Enum? Category { get; }
    bool SupportsModifiers { get; }
    bool IsComputed { get; }
}

/// <summary>
/// 强类型 Runtime Data key。
/// </summary>
public sealed class DataKey<T> : DataMeta, IDataKey
{
    public DataKey(DataMeta meta)
    {
        Key = meta.Key;
        DisplayName = meta.DisplayName;
        Description = meta.Description;
        Category = meta.Category;
        Type = typeof(T);
        DefaultValue = meta.DefaultValue ?? DataMeta.GetTypeDefaultValue(typeof(T));
        MinValue = meta.MinValue;
        MaxValue = meta.MaxValue;
        IsPercentage = meta.IsPercentage;
        SupportModifiers = meta.SupportModifiers;
        CanMigrate = meta.CanMigrate;
        IconPath = meta.IconPath;
        Options = meta.Options;
        Dependencies = meta.Dependencies;
        Compute = meta.Compute;
    }

    public Type ValueType => typeof(T);

    public object? UntypedDefaultValue => GetDefaultValue();

    public T TypedDefaultValue => (T)GetDefaultValue();

    public bool SupportsModifiers => SupportModifiers ?? false;

    bool IDataKey.IsComputed => IsComputed;
}

/// <summary>
/// Data typed slot 基类。
/// </summary>
public interface IDataSlot
{
    IDataKey Key { get; }
    object? UntypedValue { get; set; }
}

/// <summary>
/// Data typed slot。
/// </summary>
public sealed class DataSlot<T> : IDataSlot
{
    public DataSlot(DataKey<T> key, T value)
    {
        Key = key;
        Value = value;
    }

    public DataKey<T> Key { get; }

    IDataKey IDataSlot.Key => Key;

    public T Value { get; set; }

    public object? UntypedValue
    {
        get => Value;
        set => Value = value is T typed ? typed : (T)DataMeta.GetTypeDefaultValue(typeof(T));
    }
}

/// <summary>
/// Data apply 结构化错误。
/// </summary>
public sealed record DataApplyError(string Code, string Message, string? Key = null);

/// <summary>
/// Data apply 结构化结果。
/// </summary>
public sealed class DataApplyReport
{
    private readonly List<DataApplyError> _errors = [];

    public List<DataApplyError> Errors => _errors;

    public bool Success => _errors.Count == 0;

    public void AddError(string code, string message, string? key = null)
    {
        _errors.Add(new DataApplyError(code, message, key));
    }
}
