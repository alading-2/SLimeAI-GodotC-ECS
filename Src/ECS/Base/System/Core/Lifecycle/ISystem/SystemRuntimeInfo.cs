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

    /// <summary>系统分组（挂载位置）。</summary>
    public SystemGroup Groups { get; set; }

    /// <summary>系统标签（逻辑分类）。</summary>
    public SystemTag Tags { get; set; }

    /// <summary>自定义统计信息列表。</summary>
    public List<SystemStat> CustomStats { get; set; } = new();
}
