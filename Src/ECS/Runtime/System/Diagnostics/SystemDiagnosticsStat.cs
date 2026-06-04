/// <summary>
/// Diagnostics 中使用的系统自定义统计项。
/// </summary>
public sealed class SystemDiagnosticsStat
{
    public string Category { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Value { get; init; } = string.Empty;

    public static SystemDiagnosticsStat From(SystemStat stat)
    {
        return new SystemDiagnosticsStat
        {
            Category = stat.Category,
            Name = stat.Name,
            Value = stat.Value
        };
    }
}
