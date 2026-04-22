using Godot;
using System.Collections.Generic;

/// <summary>
/// SystemManager 的运行时条目。
/// <para>这是 SystemDescriptor 在运行期的展开结果，用于保存实例、依赖与状态。</para>
/// </summary>
internal sealed class ManagedSystemEntry
{
    /// <summary>静态描述符（注册时定义的元数据）。</summary>
    public required SystemDescriptor Descriptor { get; init; }

    /// <summary>系统实例本体（Node 或纯服务对象）。</summary>
    public required object Instance { get; init; }

    /// <summary>如果实例是 Node，则保存对应节点引用；纯服务场景为 null。</summary>
    public Node? NodeInstance { get; init; }

    /// <summary>生命周期入口。</summary>
    public ISystemLifecycle? Lifecycle { get; init; }

    /// <summary>项目状态观察入口。</summary>
    public IProjectStateAwareSystem? StateAware { get; init; }

    /// <summary>系统当前是否已经加入管理器。</summary>
    public bool IsAdded { get; set; }

    /// <summary>系统当前是否被显式启用。</summary>
    public bool DesiredEnabled { get; set; }

    /// <summary>Profile 层是否允许启用。</summary>
    public bool ProfileEnabled { get; set; }

    /// <summary>系统是否满足项目状态运行条件。</summary>
    public bool IsConditionAllowed { get; set; }

    /// <summary>系统当前是否处于运行态。</summary>
    public bool IsRunning { get; set; }

    /// <summary>依赖当前系统的上游系统 Id 集合。</summary>
    public HashSet<string> DependentIds { get; } = new();
}
