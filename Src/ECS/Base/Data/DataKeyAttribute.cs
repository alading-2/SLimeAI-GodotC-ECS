using System;

/// <summary>
/// 标记 DataNew 配置属性对应的数据键。
/// <para>用于 Data.LoadFromConfig 时自动映射，避免字符串拼写错误。</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class DataKeyAttribute : Attribute
{
    /// <summary>
    /// 对应的 DataKey 名称。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 构造函数。
    /// </summary>
    /// <param name="key">DataKey 名称。</param>
    public DataKeyAttribute(string key)
    {
        Key = key;
    }
}
