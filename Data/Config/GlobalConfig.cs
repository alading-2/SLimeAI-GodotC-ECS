
public static partial class GlobalConfig
{
    /// <summary>当前配置数据源模式，默认使用 DataNew 纯 C#。</summary>
    public static DataSourceMode DataSourceMode { get; set; } = DataSourceMode.PureCSharp;
}

/// <summary>配置数据源模式。</summary>
public enum DataSourceMode
{
    /// <summary>纯 C# DataNew 数据。</summary>
    PureCSharp,

    /// <summary>旧 C# Resource + .tres 数据。</summary>
    GodotResource,
}