using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 业务 owner 引用注册表。
/// <para>只同步 typed EntityId 与 Data string/string_array projection，不参与生命周期销毁决策。</para>
/// </summary>
public sealed class OwnedReferenceRegistry
{
    private static readonly Log _log = new(nameof(OwnedReferenceRegistry), LogLevel.Warning);

    private readonly Func<EntityId, Node?>? _resolveNode;
    private readonly List<OwnedReferenceDescriptor> _descriptors = new();

    public OwnedReferenceRegistry(Func<EntityId, Node?>? resolveNode = null)
    {
        _resolveNode = resolveNode;
    }

    /// <summary>已注册 descriptor 快照。</summary>
    public IReadOnlyList<OwnedReferenceDescriptor> Descriptors => _descriptors.ToArray();

    /// <summary>
    /// 注册 owner 引用 descriptor；重复注册返回 false。
    /// </summary>
    public bool Register(OwnedReferenceDescriptor descriptor)
    {
        if (!descriptor.IsValid)
            return false;

        if (_descriptors.Contains(descriptor))
            return false;

        _descriptors.Add(descriptor);
        return true;
    }

    /// <summary>
    /// 建立 owner -> child 业务引用，并写入双方 Data projection。
    /// </summary>
    public bool AddReference(IEntity? owner, IEntity? child, OwnedReferenceDescriptor descriptor)
    {
        if (owner == null || child == null || !descriptor.IsValid)
            return false;

        Register(descriptor);

        if (!TryResolveEntityId(owner, out var ownerId) || !TryResolveEntityId(child, out var childId))
            return false;

        if (!CanUseDescriptor(owner.Data, child.Data, descriptor))
            return false;

        EnsureProjectionDefaults(owner.Data, child.Data, descriptor);

        var currentOwnerId = EntityId.From(child.Data.Get(descriptor.ChildToOwnerKey));
        if (!currentOwnerId.IsEmpty && currentOwnerId != ownerId)
        {
            _log.Warn($"Owned reference child 已归属其他 owner: child={childId.Value}, owner={currentOwnerId.Value}");
            return false;
        }

        var currentList = EntityIdList.FromStringArray(owner.Data.Get(descriptor.OwnerListKey));
        var nextList = currentList.Add(childId);
        var changed = currentOwnerId != ownerId || !currentList.Contains(childId);

        if (!changed)
            return false;

        child.Data.Set(descriptor.ChildToOwnerKey, ownerId.Value);
        owner.Data.Set(descriptor.OwnerListKey, nextList.ToStringArray());
        return true;
    }

    /// <summary>
    /// 移除 child 当前 owner 中的业务引用。
    /// </summary>
    public bool RemoveReference(IEntity? child, OwnedReferenceDescriptor descriptor)
    {
        if (child == null || !descriptor.IsValid)
            return false;

        Register(descriptor);

        if (!TryResolveEntityId(child, out var childId))
            return false;

        return RemoveReference(childId, child, descriptor);
    }

    /// <summary>
    /// Entity 销毁前 cleanup hook：自动从 owner list 中移除被销毁 child。
    /// </summary>
    public void CleanupDestroyedChild(EntityId childId)
    {
        if (childId.IsEmpty || _resolveNode == null)
            return;

        var childNode = _resolveNode(childId);
        if (childNode is not IEntity child)
            return;

        foreach (var descriptor in _descriptors.ToArray())
        {
            RemoveReference(childId, child, descriptor);
        }
    }

    private bool RemoveReference(EntityId childId, IEntity child, OwnedReferenceDescriptor descriptor)
    {
        if (!child.Data.Has(descriptor.ChildToOwnerKey))
            return false;

        var ownerId = EntityId.From(child.Data.Get(descriptor.ChildToOwnerKey));
        if (ownerId.IsEmpty)
            return false;

        var changed = child.Data.Set(descriptor.ChildToOwnerKey, string.Empty);
        if (_resolveNode == null)
            return changed;

        var ownerNode = _resolveNode(ownerId);
        if (ownerNode is not IEntity owner || !owner.Data.Has(descriptor.OwnerListKey))
            return changed;

        var currentList = EntityIdList.FromStringArray(owner.Data.Get(descriptor.OwnerListKey));
        var nextList = currentList.Remove(childId);
        if (nextList.Count == currentList.Count)
            return changed;

        owner.Data.Set(descriptor.OwnerListKey, nextList.ToStringArray());
        return true;
    }

    private static bool CanUseDescriptor(Data ownerData, Data childData, OwnedReferenceDescriptor descriptor)
    {
        return ownerData.Has(descriptor.OwnerListKey)
            && childData.Has(descriptor.ChildToOwnerKey);
    }

    private static void EnsureProjectionDefaults(Data ownerData, Data childData, OwnedReferenceDescriptor descriptor)
    {
        // 手工测试实体或旧路径可能只绑定了 catalog，没有显式写入运行时投影值。
        if (!ownerData.TryGetValue<string[]>(descriptor.OwnerListKey.StableKey, out _))
            ownerData.Set(descriptor.OwnerListKey, Array.Empty<string>());

        if (!childData.TryGetValue<string>(descriptor.ChildToOwnerKey.StableKey, out _))
            childData.Set(descriptor.ChildToOwnerKey, string.Empty);
    }

    private static bool TryResolveEntityId(IEntity entity, out EntityId entityId)
    {
        entityId = EntityId.From(entity.Data.Get(GeneratedDataKey.Id));
        if (!entityId.IsEmpty)
            return true;

        if (entity is Node node)
        {
            entityId = EntityId.From(node.GetInstanceId().ToString());
            return !entityId.IsEmpty;
        }

        return false;
    }
}
