using Godot;
using slime.data.Systems;

/// <summary>
/// SystemManager 的运行时条目。
/// <para>保存系统实例、配置和当前运行状态。</para>
/// </summary>
internal sealed class ManagedSystemEntry
{
    /// <summary>静态描述符（SystemId + Factory）。</summary>
    public required SystemDescriptor Descriptor { get; init; }

    /// <summary>系统配置（从 DataNew SystemData 读取的元数据）。</summary>
    public required SystemData Config { get; init; }

    /// <summary>系统运行条件（从配置构建后缓存）。</summary>
    public required SystemRunCondition RunCondition { get; init; }

    /// <summary>系统实例本体（Node 或纯服务对象）。</summary>
    public required object Instance { get; init; }

    /// <summary>如果实例是 Node，则保存对应节点引用；纯服务场景为 null。</summary>
    public Node? NodeInstance { get; init; }

    /// <summary>ISystemLifecycle 接口（生命周期协议）。</summary>
    public ISystemLifecycle? Lifecycle { get; init; }

    /// <summary>系统当前是否被显式启用（人工开关）。</summary>
    public bool IsEnabled { get; set; }

    /// <summary>系统是否满足项目状态运行条件（Phase 条件门禁）。</summary>
    public bool IsStateAllowed { get; set; }

    /// <summary>状态门禁未通过时的原因；为空表示通过。</summary>
    public string BlockedReason { get; set; } = string.Empty;

    /// <summary>系统当前是否处于运行态（IsEnabled &amp;&amp; IsStateAllowed）。</summary>
    public bool IsRunning { get; set; }
}
