using System.Collections.Generic;

/// <summary>
/// 系统运行时信息。
/// <para>用于调试和监控，支持自定义统计信息扩展。</para>
/// </summary>
public class SystemRuntimeInfo
{
    /// <summary>系统 Id。</summary>
    public string SystemId { get; set; } = string.Empty;

    /// <summary>系统是否已加载。</summary>
    public bool IsAdded { get; set; }

    /// <summary>系统是否启用（人工开关）。</summary>
    public bool IsEnabled { get; set; }

    /// <summary>系统是否运行中（IsEnabled &amp;&amp; IsStateAllowed）。</summary>
    public bool IsRunning { get; set; }

    /// <summary>系统是否满足项目状态运行条件。</summary>
    public bool IsStateAllowed { get; set; }

    /// <summary>状态门禁未通过时的原因；为空表示通过。</summary>
    public string BlockedReason { get; set; } = string.Empty;

    /// <summary>状态门禁未通过时的稳定原因码。</summary>
    public SystemBlockedReasonCode BlockedReasonCode { get; set; } = SystemBlockedReasonCode.None;

    /// <summary>系统挂载分组。</summary>
    public SystemGroup MountGroup { get; set; }

    /// <summary>系统标签（逻辑分类）。</summary>
    public SystemTag Tags { get; set; }

    /// <summary>自定义统计信息列表。</summary>
    public List<SystemStat> CustomStats { get; set; } = new();
}
