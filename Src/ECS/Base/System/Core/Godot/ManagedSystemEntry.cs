using Godot;

/// <summary>
/// SystemManager 的运行时条目。
/// <para>这是 SystemDescriptor 在运行期的展开结果，用于保存实例与状态。</para>
/// </summary>
internal sealed class ManagedSystemEntry
{
    /// <summary>静态描述符（注册时定义的元数据）。</summary>
    public required SystemDescriptor Descriptor { get; init; }

    /// <summary>系统实例本体（Node 或纯服务对象）。</summary>
    public required object Instance { get; init; }

    /// <summary>如果实例是 Node，则保存对应节点引用；纯服务场景为 null。</summary>
    public Node? NodeInstance { get; init; }

    /// <summary>如果实例实现了 ISystemRuntime，则保存运行时回调入口。</summary>
    public ISystemRuntime? Runtime { get; init; }

    /// <summary>系统是否被显式启用（人工开关层）。</summary>
    public bool IsEnabled { get; set; }

    /// <summary>系统是否满足项目状态运行条件（状态门禁层）。</summary>
    public bool IsStateAllowed { get; set; }
}
