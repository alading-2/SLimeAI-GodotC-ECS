using System;
using System.Collections.Generic;

/// <summary>
/// 单个 System 的 diagnostics 合同条目。
/// </summary>
public sealed class SystemDiagnosticsEntry
{
    public string SystemId { get; init; } = string.Empty;
    public string Owner { get; init; } = string.Empty;
    public string SourcePath { get; init; } = string.Empty;
    public string ConfigRecordId { get; init; } = string.Empty;
    public bool Registered { get; init; }
    public bool Configured { get; init; }
    public bool Loaded { get; init; }
    public bool Enabled { get; init; }
    public bool StateAllowed { get; init; }
    public bool Running { get; init; }
    public string BlockedReasonCode { get; init; } = SystemBlockedReasonCode.None.ToString();
    public string BlockedReason { get; init; } = string.Empty;
    public string MountGroup { get; init; } = string.Empty;
    public string Tags { get; init; } = string.Empty;
    public bool Required { get; init; }
    public bool AutoLoad { get; init; }
    public bool StartEnabled { get; init; }
    public int Priority { get; init; }
    public string AllowedFlowStates { get; init; } = string.Empty;
    public string RequiredOverlays { get; init; } = string.Empty;
    public string BlockedOverlays { get; init; } = string.Empty;
    public string AllowedSimulationStates { get; init; } = string.Empty;
    public string[] Dependencies { get; init; } = Array.Empty<string>();
    public List<SystemDiagnosticsStat> CustomStats { get; init; } = new();
    public string Description { get; init; } = string.Empty;
}
