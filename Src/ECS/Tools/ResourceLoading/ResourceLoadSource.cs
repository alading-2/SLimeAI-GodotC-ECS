/// <summary>
/// 资源加载来源类型。
/// </summary>
public enum ResourceLoadSourceKind
{
    None = 0,
    Runtime = 1,
    DataOS = 2,
    Debug = 3,
    Test = 4
}

/// <summary>
/// 资源加载来源诊断信息。
/// </summary>
public readonly record struct ResourceLoadSource(
    ResourceLoadSourceKind Kind,
    string Owner,
    string Usage)
{
    public static ResourceLoadSource None => new(ResourceLoadSourceKind.None, string.Empty, string.Empty);

    public static ResourceLoadSource Runtime(string owner, string usage)
        => new(ResourceLoadSourceKind.Runtime, owner, usage);

    public static ResourceLoadSource DataOS(string owner, string usage)
        => new(ResourceLoadSourceKind.DataOS, owner, usage);

    public static ResourceLoadSource Debug(string owner, string usage)
        => new(ResourceLoadSourceKind.Debug, owner, usage);

    public static ResourceLoadSource Test(string owner, string usage)
        => new(ResourceLoadSourceKind.Test, owner, usage);

    public bool IsSpecified =>
        Kind != ResourceLoadSourceKind.None
        && !string.IsNullOrWhiteSpace(Owner)
        && !string.IsNullOrWhiteSpace(Usage);
}
