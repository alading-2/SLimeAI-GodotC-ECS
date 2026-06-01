using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 销毁管线。
/// <para>职责单一：按生命周期树、owner cleanup、component unregister、Data/Events 清理和 registry 注销的固定顺序销毁实体。</para>
/// </summary>
public sealed class EntityDestroyPipeline
{
    private readonly EntityRegistry _registry;
    private readonly LifecycleTree _lifecycleTree;
    private readonly Action<EntityId>? _ownerCleanup;
    private readonly HashSet<EntityId> _destroyedIds = new();
    private readonly HashSet<ulong> _destroyedInstanceIds = new();

    public EntityDestroyPipeline(
        EntityRegistry registry,
        LifecycleTree lifecycleTree,
        Action<EntityId>? ownerCleanup = null)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _lifecycleTree = lifecycleTree ?? throw new ArgumentNullException(nameof(lifecycleTree));
        _ownerCleanup = ownerCleanup;
    }

    public EntityDestroyResult Destroy(Node? entity)
    {
        if (entity == null)
        {
            return EntityDestroyResult.Missing;
        }

        var instanceId = entity.GetInstanceId();
        if (_destroyedInstanceIds.Contains(instanceId))
        {
            return EntityDestroyResult.Repeat(ResolveEntityId(entity));
        }

        var entityId = _registry.GetEntityId(entity);
        if (entityId.IsEmpty)
        {
            entityId = ResolveEntityId(entity);
        }

        if (entityId.IsEmpty)
        {
            return EntityDestroyResult.Missing;
        }

        if (_destroyedIds.Contains(entityId))
        {
            _destroyedInstanceIds.Add(instanceId);
            return EntityDestroyResult.Repeat(entityId);
        }

        _destroyedIds.Add(entityId);
        _destroyedInstanceIds.Add(instanceId);

        var childSnapshot = _lifecycleTree.GetChildren(entityId).ToArray();
        foreach (var childLink in childSnapshot.Where(link => link.DestroyPolicy == ParentDestroyPolicy.DestroyRecursively))
        {
            var childNode = _registry.GetNode(childLink.ChildId);
            if (childNode != null)
            {
                Destroy(childNode);
            }
        }

        foreach (var childLink in childSnapshot)
        {
            _lifecycleTree.Detach(childLink.ChildId);
        }

        _lifecycleTree.DetachAll(entityId);
        _ownerCleanup?.Invoke(entityId);
        UnregisterComponents(entity);

        if (entity is IEntity iEntityForClear)
        {
            iEntityForClear.Events.Clear();
            iEntityForClear.Data.Clear();
        }

        _registry.Unregister(entity);

        if (entity is IPoolable)
        {
            ObjectPoolManager.ReturnToPool(entity);
        }
        else if (GodotObject.IsInstanceValid(entity))
        {
            entity.QueueFree();
        }

        return EntityDestroyResult.Success(entityId);
    }

    private static void UnregisterComponents(Node entity)
    {
        var components = entity
            .FindChildren("*", "Node", true, false)
            .OfType<Node>()
            .Where(node => node is IComponent)
            .ToArray();

        foreach (var componentNode in components)
        {
            if (componentNode is IComponent component)
            {
                component.OnComponentUnregistered();
            }
        }
    }

    private static EntityId ResolveEntityId(Node entity)
    {
        if (entity is IEntity iEntity)
        {
            return EntityId.From(iEntity.Data.Get<string>(GeneratedDataKey.Id));
        }

        return EntityId.From(entity.GetInstanceId().ToString());
    }
}

/// <summary>
/// Entity 销毁结果。
/// </summary>
public readonly record struct EntityDestroyResult(bool Destroyed, bool AlreadyDestroyed, EntityId EntityId)
{
    public static EntityDestroyResult Missing => new(false, false, EntityId.Empty);

    public static EntityDestroyResult Repeat(EntityId entityId)
        => new(false, true, entityId);

    public static EntityDestroyResult Success(EntityId entityId)
        => new(true, false, entityId);
}
