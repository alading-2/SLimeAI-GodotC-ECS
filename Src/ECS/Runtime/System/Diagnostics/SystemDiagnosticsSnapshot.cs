using System.Collections.Generic;

/// <summary>
/// System Core diagnostics 快照。
/// <para>只读 config / registry / runtime，用于 TestSystem、Godot validation 和 AI debug。</para>
/// </summary>
public sealed class SystemDiagnosticsSnapshot
{
    public int SchemaVersion { get; init; } = 1;
    public SystemProjectStateDiagnostics ProjectState { get; init; } = new();
    public string ActivePreset { get; init; } = string.Empty;
    public int ConfigCount { get; init; }
    public int RegisteredDescriptorCount { get; init; }
    public int LoadedCount { get; init; }
    public int RunningCount { get; init; }
    public int BlockedCount { get; init; }
    public int DisabledCount { get; init; }
    public int TraceCount { get; init; }
    public List<SystemDiagnosticsEntry> Entries { get; init; } = new();
    public List<SystemPreflightIssue> PreflightIssues { get; init; } = new();
    public List<SystemLifecycleTraceEntry> RecentTrace { get; init; } = new();
}
