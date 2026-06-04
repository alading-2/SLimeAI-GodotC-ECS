using System.Collections.Generic;
using System.Linq;

/// <summary>
/// System preflight 报告。
/// <para>只读 config / registry / preset，不写回任何运行时配置。</para>
/// </summary>
public sealed class SystemPreflightReport
{
    public int SchemaVersion { get; init; } = 1;
    public int ConfigCount { get; init; }
    public int RegisteredDescriptorCount { get; init; }
    public string ActivePresetName { get; init; } = string.Empty;
    public List<SystemPreflightIssue> Issues { get; init; } = new();

    public int ErrorCount => Issues.Count(static issue => issue.Severity == SystemPreflightSeverity.Error);
    public int WarningCount => Issues.Count(static issue => issue.Severity == SystemPreflightSeverity.Warning);
    public bool HasErrors => ErrorCount > 0;
}
