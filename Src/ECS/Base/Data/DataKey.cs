using System;

/// <summary>
/// 数据键接口 - 供 SnapshotLoader 等动态场景使用
/// </summary>
public interface IDataKey
{
    /// <summary>键名字符串</summary>
    string Key { get; }
    /// <summary>值的 C# 类型</summary>
    Type ValueType { get; }
    /// <summary>未装箱的默认值</summary>
    object UntypedDefaultValue { get; }
}

/// <summary>
/// 类型安全数据键包装 - 编译期确保 Get/Set 的值类型与键类型一致
/// 用法：data.Get(DataKey.BaseHp) → float，无需显式指定 T
/// </summary>
/// <typeparam name="T">值的 C# 类型</typeparam>
public readonly record struct DataKey<T>(DataMeta Meta) : IDataKey
{
    /// <summary>键名字符串</summary>
    public string Key => Meta.Key;

    /// <summary>值的 C# 类型</summary>
    public Type ValueType => typeof(T);

    /// <summary>未装箱的默认值（从 DataMeta 读取）</summary>
    public object UntypedDefaultValue => Meta.GetDefaultValue();

    /// <summary>
    /// 默认值（透传到 DataMeta.DefaultValue，供旧代码和 Godot SourceGenerator 访问）
    /// </summary>
    public object? DefaultValue => Meta.DefaultValue;

    /// <summary>
    /// 向后兼容：隐式转为 string 返回键名。
    /// 新代码优先使用 data.Get(DataKey.Xxx) 类型安全重载；旧代码/字符串 API 可继续使用此转换。
    /// </summary>
    public static implicit operator string(DataKey<T> key) => key.Key;
}
