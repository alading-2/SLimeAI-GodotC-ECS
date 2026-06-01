using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Effect host/owner 清单服务。
/// <para>负责 effect -> host/owner 单引用、owner -> effect list 查询和 cleanup，不再使用 EntityRelationshipManager。</para>
/// </summary>
public sealed class EffectOwnershipService
{
    private readonly Func<EntityId, Node?> _resolveNode;
    private readonly Func<OwnedReferenceDescriptor, bool> _registerReference;
    private readonly Func<IEntity, IEntity, OwnedReferenceDescriptor, bool> _addReference;
    private readonly Func<IEntity, OwnedReferenceDescriptor, bool> _removeReference;

    private static EffectOwnershipService? _runtime;

    /// <summary>Effect host/owner Data projection descriptor。</summary>
    public static readonly OwnedReferenceDescriptor OwnerDescriptor = new(
        GeneratedDataKey.EffectHostEntityId,
        GeneratedDataKey.OwnedEffectIds
    );

    public EffectOwnershipService(Func<EntityId, Node?> resolveNode, OwnedReferenceRegistry ownedReferenceRegistry)
        : this(
            resolveNode,
            ownedReferenceRegistry.Register,
            ownedReferenceRegistry.AddReference,
            ownedReferenceRegistry.RemoveReference)
    {
    }

    public EffectOwnershipService(
        Func<EntityId, Node?> resolveNode,
        Func<OwnedReferenceDescriptor, bool> registerReference,
        Func<IEntity, IEntity, OwnedReferenceDescriptor, bool> addReference,
        Func<IEntity, OwnedReferenceDescriptor, bool> removeReference)
    {
        _resolveNode = resolveNode ?? throw new ArgumentNullException(nameof(resolveNode));
        _registerReference = registerReference ?? throw new ArgumentNullException(nameof(registerReference));
        _addReference = addReference ?? throw new ArgumentNullException(nameof(addReference));
        _removeReference = removeReference ?? throw new ArgumentNullException(nameof(removeReference));
        _registerReference(OwnerDescriptor);
    }

    /// <summary>
    /// 默认运行时服务，使用 EntityManager 的 registry 与 owned-reference cleanup hook。
    /// </summary>
    public static EffectOwnershipService Runtime =>
        _runtime ??= new EffectOwnershipService(
            EntityManager.ResolveEntityNode,
            EntityManager.RegisterOwnedReference,
            EntityManager.AddOwnedReference,
            EntityManager.RemoveOwnedReference
        );

    /// <summary>
    /// 建立 host/owner -> effect 引用。
    /// </summary>
    public bool Attach(IEntity? hostOrOwner, EffectEntity? effect)
    {
        if (hostOrOwner == null || effect == null)
            return false;

        return _addReference(hostOrOwner, effect, OwnerDescriptor);
    }

    /// <summary>
    /// 移除 effect 当前 host/owner 引用。
    /// </summary>
    public bool Detach(EffectEntity? effect)
    {
        if (effect == null)
            return false;

        return _removeReference(effect, OwnerDescriptor);
    }

    /// <summary>
    /// 获取 effect 的 host/owner。
    /// </summary>
    public IEntity? GetHost(EffectEntity? effect)
    {
        if (effect == null || !effect.Data.Has(GeneratedDataKey.EffectHostEntityId))
            return null;

        var hostId = EntityId.From(effect.Data.Get(GeneratedDataKey.EffectHostEntityId));
        if (hostId.IsEmpty)
            return null;

        return _resolveNode(hostId) as IEntity;
    }

    /// <summary>
    /// 获取 host/owner 拥有的 effect 列表。
    /// </summary>
    public List<EffectEntity> GetEffects(IEntity? hostOrOwner)
    {
        var effects = new List<EffectEntity>();
        if (hostOrOwner == null || !hostOrOwner.Data.Has(GeneratedDataKey.OwnedEffectIds))
            return effects;

        var ownerId = ResolveEntityId(hostOrOwner);
        if (ownerId.IsEmpty)
            return effects;

        var effectIds = EntityIdList.FromStringArray(hostOrOwner.Data.Get(GeneratedDataKey.OwnedEffectIds));
        foreach (var effectId in effectIds.Values)
        {
            if (_resolveNode(effectId) is not EffectEntity effect)
                continue;

            var currentOwnerId = effect.Data.Has(GeneratedDataKey.EffectHostEntityId)
                ? EntityId.From(effect.Data.Get(GeneratedDataKey.EffectHostEntityId))
                : EntityId.Empty;
            if (currentOwnerId == ownerId)
                effects.Add(effect);
        }

        return effects;
    }

    private static EntityId ResolveEntityId(IEntity entity)
    {
        var id = EntityId.From(entity.Data.Get(GeneratedDataKey.Id));
        if (!id.IsEmpty || entity is not Node node)
            return id;

        return EntityId.From(node.GetInstanceId().ToString());
    }
}
