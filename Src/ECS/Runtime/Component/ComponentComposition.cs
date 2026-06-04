using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 提供 Entity 默认 Component 组合的接口。
/// <para>具体 Entity 可以实现该接口，把旧 Component Preset 的组合事实源迁到代码 profile。</para>
/// </summary>
public interface IComponentCompositionProvider
{
    ComponentCompositionProfile GetComponentCompositionProfile();
}

/// <summary>
/// Component 代码化组合 profile。
/// <para>Profile 只描述要创建哪些节点以及注册前如何注入结构参数，不承载业务状态。</para>
/// </summary>
public sealed class ComponentCompositionProfile
{
    public ComponentCompositionProfile(IEnumerable<ComponentCompositionEntry> entries)
    {
        Entries = new List<ComponentCompositionEntry>(entries ?? Array.Empty<ComponentCompositionEntry>());
    }

    public IReadOnlyList<ComponentCompositionEntry> Entries { get; }

    public static ComponentCompositionProfile Empty { get; } = new(Array.Empty<ComponentCompositionEntry>());
}

/// <summary>
/// 单个 Component 组合条目。
/// </summary>
public sealed class ComponentCompositionEntry
{
    public ComponentCompositionEntry(
        string nodeName,
        Func<Node> createNode,
        Action<Node>? configure = null)
    {
        if (string.IsNullOrWhiteSpace(nodeName))
            throw new ArgumentException("Component node name cannot be empty.", nameof(nodeName));

        NodeName = nodeName;
        CreateNode = createNode ?? throw new ArgumentNullException(nameof(createNode));
        Configure = configure;
    }

    public string NodeName { get; }
    public Func<Node> CreateNode { get; }
    public Action<Node>? Configure { get; }
}

/// <summary>
/// Component 代码化组合器。
/// <para>调用顺序：new component -> Configure(options) -> AddChild -> ComponentRegistrar.RegisterComponents。</para>
/// </summary>
public static class ComponentComposer
{
    public const string ComponentContainerName = "Component";

    /// <summary>
    /// 根据 Entity 自身提供的 profile 创建默认 Component。
    /// </summary>
    public static int Compose(Node? entity)
    {
        if (entity is not IComponentCompositionProvider provider)
            return 0;

        return Compose(entity, provider.GetComponentCompositionProfile());
    }

    /// <summary>
    /// 根据 profile 创建默认 Component。已有同名节点时跳过，避免覆盖 scene 中显式挂载的组件。
    /// </summary>
    public static int Compose(Node? entity, ComponentCompositionProfile? profile)
    {
        if (entity == null || profile == null || profile.Entries.Count == 0)
            return 0;

        var container = GetOrCreateComponentContainer(entity);
        var composedCount = 0;

        foreach (var entry in profile.Entries)
        {
            if (container.GetNodeOrNull(entry.NodeName) != null)
                continue;

            var component = entry.CreateNode();
            component.Name = entry.NodeName;
            entry.Configure?.Invoke(component);
            container.AddChild(component);
            composedCount++;
        }

        return composedCount;
    }

    private static Node GetOrCreateComponentContainer(Node entity)
    {
        var container = entity.GetNodeOrNull(ComponentContainerName);
        if (container != null)
            return container;

        container = new Node2D
        {
            Name = ComponentContainerName
        };
        entity.AddChild(container);
        return container;
    }
}
