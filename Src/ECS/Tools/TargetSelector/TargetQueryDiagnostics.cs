using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 目标查询诊断信息。
/// </summary>
public sealed class TargetQueryDiagnostics
{
    public TargetQueryDiagnostics(
        Vector2 resolvedOrigin,
        Vector2 resolvedForward,
        int candidateCount,
        int geometryHitCount,
        int filteredByTeamCount,
        int filteredByTypeCount,
        int filteredByLifecycleCount,
        int returnedCount,
        int maxTargets,
        bool limitApplied,
        bool truncated,
        string sortApplied,
        IReadOnlyList<string>? warnings = null,
        IReadOnlyList<string>? errors = null)
    {
        ResolvedOrigin = resolvedOrigin;
        ResolvedForward = resolvedForward;
        CandidateCount = candidateCount;
        GeometryHitCount = geometryHitCount;
        FilteredByTeamCount = filteredByTeamCount;
        FilteredByTypeCount = filteredByTypeCount;
        FilteredByLifecycleCount = filteredByLifecycleCount;
        ReturnedCount = returnedCount;
        MaxTargets = maxTargets;
        LimitApplied = limitApplied;
        Truncated = truncated;
        SortApplied = sortApplied;
        Warnings = warnings ?? Array.Empty<string>();
        Errors = errors ?? Array.Empty<string>();
    }

    public Vector2 ResolvedOrigin { get; }
    public Vector2 ResolvedForward { get; }
    public int CandidateCount { get; }
    public int GeometryHitCount { get; }
    public int FilteredByTeamCount { get; }
    public int FilteredByTypeCount { get; }
    public int FilteredByLifecycleCount { get; }
    public int ReturnedCount { get; }
    public int MaxTargets { get; }
    public bool LimitApplied { get; }
    public bool Truncated { get; }
    public string SortApplied { get; }
    public IReadOnlyList<string> Warnings { get; }
    public IReadOnlyList<string> Errors { get; }
    public bool HasErrors => Errors.Count > 0;
}
