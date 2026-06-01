using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Component 注册器。
/// <para>只维护 Entity 与 Component 的内部 owner 索引，不再通过 EntityRelationshipManager 暴露组件归属关系。</para>
/// </summary>
public sealed class ComponentRegistrar
{
    private static readonly Log _log = new(nameof(ComponentRegistrar), LogLevel.Debug);

    private readonly EntityRegistry? _entityRegistry;
    private readonly Dictionary<Node, HashSet<Node>> _componentsByEntity = new();
    private readonly Dictionary<Node, Node> _entityByComponent = new();
    private readonly Dictionary<Type, HashSet<Node>> _componentsByType = new();

    public ComponentRegistrar(EntityRegistry? entityRegistry = null)
    {
        _entityRegistry = entityRegistry;
    }

    /// <summary>
    /// 递归扫描并注册指定 Entity 下的所有 Component。
    /// </summary>
    public int RegisterComponents(Node? entity)
    {
        if (entity == null)
            return 0;

        return RegisterComponents(entity, FindComponentNodes(entity));
    }

    /// <summary>
    /// 使用外部已扫描的 Component 列表注册，供 EntityManager 复用预热缓存。
    /// </summary>
    public int RegisterComponents(Node? entity, IEnumerable<Node> components)
    {
        if (entity == null)
            return 0;

        var registeredCount = 0;
        foreach (var component in components.Distinct().ToArray())
        {
            if (RegisterComponent(entity, component))
                registeredCount++;
        }

        return registeredCount;
    }

    /// <summary>
    /// 注册单个 Component，并建立内部 owner 反查索引。
    /// </summary>
    public bool RegisterComponent(Node? entity, Node? component)
    {
        if (entity == null || component == null || entity == component)
            return false;

        if (_entityRegistry != null && _entityRegistry.GetEntityId(entity).IsEmpty)
        {
            _log.Warn($"Entity 未注册到 EntityRegistry，跳过 Component 注册: {entity.Name}");
            return false;
        }

        if (!IsComponentNode(component))
            return false;

        if (_entityByComponent.TryGetValue(component, out var existingOwner))
        {
            if (existingOwner != entity)
                _log.Warn($"Component 已归属其他 Entity，跳过重复注册: {component.Name}");

            return false;
        }

        NodeLifecycleManager.Register(component);

        if (!_componentsByEntity.TryGetValue(entity, out var components))
        {
            components = new HashSet<Node>();
            _componentsByEntity[entity] = components;
        }

        components.Add(component);
        _entityByComponent[component] = entity;
        AddTypeIndex(component);

        if (component is IComponent iComponent)
        {
            try
            {
                iComponent.OnComponentRegistered(entity);
            }
            catch (Exception ex)
            {
                _log.Error($"Component 注册回调失败: {component.GetType().Name}, error={ex.Message}");
            }
        }

        return true;
    }

    /// <summary>
    /// 注销指定 Entity 的全部 Component，不销毁节点本身。
    /// </summary>
    public int UnregisterComponents(Node? entity)
    {
        if (entity == null)
            return 0;

        var components = GetComponents(entity).ToArray();
        var unregisteredCount = 0;

        foreach (var component in components)
        {
            if (UnregisterComponent(entity, component))
                unregisteredCount++;
        }

        return unregisteredCount;
    }

    /// <summary>
    /// 移除 Component：注销索引和生命周期注册，并释放节点。
    /// </summary>
    public bool RemoveComponent(Node? entity, Node? component)
    {
        if (!UnregisterComponent(entity, component))
            return false;

        if (component != null && GodotObject.IsInstanceValid(component))
            component.QueueFree();

        return true;
    }

    /// <summary>
    /// 注销单个 Component，不释放节点。
    /// </summary>
    public bool UnregisterComponent(Node? entity, Node? component)
    {
        if (entity == null || component == null)
            return false;

        if (!_entityByComponent.TryGetValue(component, out var owner) || owner != entity)
            return false;

        if (component is IComponent iComponent)
        {
            try
            {
                iComponent.OnComponentUnregistered();
            }
            catch (Exception ex)
            {
                _log.Error($"Component 注销回调失败: {component.GetType().Name}, error={ex.Message}");
            }
        }

        RemoveIndexes(entity, component);
        NodeLifecycleManager.Unregister(component);
        return true;
    }

    public IReadOnlyList<Node> GetComponents(Node? entity)
    {
        if (entity == null || !_componentsByEntity.TryGetValue(entity, out var components))
            return Array.Empty<Node>();

        return components.ToArray();
    }

    public T? GetComponent<T>(Node? entity) where T : Node
    {
        return GetComponents(entity).OfType<T>().FirstOrDefault();
    }

    public IEnumerable<T> GetComponentsByType<T>() where T : Node
    {
        return _entityByComponent.Keys.OfType<T>().ToArray();
    }

    public Node? GetEntityByComponent(Node? component)
    {
        return component != null && _entityByComponent.TryGetValue(component, out var entity)
            ? entity
            : null;
    }

    public void Clear()
    {
        _componentsByEntity.Clear();
        _entityByComponent.Clear();
        _componentsByType.Clear();
    }

    private static IEnumerable<Node> FindComponentNodes(Node entity)
    {
        return entity
            .FindChildren("*", "Node", true, false)
            .OfType<Node>()
            .Where(IsComponentNode)
            .ToArray();
    }

    private static bool IsComponentNode(Node node)
    {
        return node is IComponent || node.GetType().Name.EndsWith("Component", StringComparison.Ordinal);
    }

    private void AddTypeIndex(Node component)
    {
        var type = component.GetType();
        if (!_componentsByType.TryGetValue(type, out var components))
        {
            components = new HashSet<Node>();
            _componentsByType[type] = components;
        }

        components.Add(component);
    }

    private void RemoveIndexes(Node entity, Node component)
    {
        _entityByComponent.Remove(component);

        if (_componentsByEntity.TryGetValue(entity, out var ownerComponents))
        {
            ownerComponents.Remove(component);
            if (ownerComponents.Count == 0)
                _componentsByEntity.Remove(entity);
        }

        foreach (var pair in _componentsByType.ToArray())
        {
            pair.Value.Remove(component);
            if (pair.Value.Count == 0)
                _componentsByType.Remove(pair.Key);
        }
    }
}
