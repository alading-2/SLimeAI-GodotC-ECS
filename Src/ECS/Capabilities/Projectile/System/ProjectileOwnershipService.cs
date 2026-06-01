using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Projectile owner 清单服务。
/// <para>负责 projectile -> owner 单引用、owner -> projectile list 查询和 cleanup，不再使用 EntityRelationshipManager。</para>
/// </summary>
public sealed class ProjectileOwnershipService
{
    private readonly Func<EntityId, Node?> _resolveNode;
    private readonly Func<OwnedReferenceDescriptor, bool> _registerReference;
    private readonly Func<IEntity, IEntity, OwnedReferenceDescriptor, bool> _addReference;
    private readonly Func<IEntity, OwnedReferenceDescriptor, bool> _removeReference;

    private static ProjectileOwnershipService? _runtime;

    /// <summary>Projectile owner Data projection descriptor。</summary>
    public static readonly OwnedReferenceDescriptor OwnerDescriptor = new(
        GeneratedDataKey.ProjectileOwnerEntityId,
        GeneratedDataKey.OwnedProjectileIds
    );

    public ProjectileOwnershipService(Func<EntityId, Node?> resolveNode, OwnedReferenceRegistry ownedReferenceRegistry)
        : this(
            resolveNode,
            ownedReferenceRegistry.Register,
            ownedReferenceRegistry.AddReference,
            ownedReferenceRegistry.RemoveReference)
    {
    }

    public ProjectileOwnershipService(
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
    public static ProjectileOwnershipService Runtime =>
        _runtime ??= new ProjectileOwnershipService(
            EntityManager.ResolveEntityNode,
            EntityManager.RegisterOwnedReference,
            EntityManager.AddOwnedReference,
            EntityManager.RemoveOwnedReference
        );

    /// <summary>
    /// 建立 owner -> projectile 引用。
    /// </summary>
    public bool Attach(IEntity? owner, IEntity? projectile)
    {
        if (owner == null || projectile == null)
            return false;

        return _addReference(owner, projectile, OwnerDescriptor);
    }

    /// <summary>
    /// 移除 projectile 当前 owner 引用。
    /// </summary>
    public bool Detach(IEntity? projectile)
    {
        if (projectile == null)
            return false;

        return _removeReference(projectile, OwnerDescriptor);
    }

    /// <summary>
    /// 获取 projectile owner。
    /// </summary>
    public IEntity? GetOwner(IEntity? projectile)
    {
        if (projectile == null || !projectile.Data.Has(GeneratedDataKey.ProjectileOwnerEntityId))
            return null;

        var ownerId = EntityId.From(projectile.Data.Get(GeneratedDataKey.ProjectileOwnerEntityId));
        if (ownerId.IsEmpty)
            return null;

        return _resolveNode(ownerId) as IEntity;
    }

    /// <summary>
    /// 获取 owner 拥有的 projectile 列表。
    /// </summary>
    public List<IEntity> GetProjectiles(IEntity? owner)
    {
        var projectiles = new List<IEntity>();
        if (owner == null || !owner.Data.Has(GeneratedDataKey.OwnedProjectileIds))
            return projectiles;

        var ownerId = ResolveEntityId(owner);
        if (ownerId.IsEmpty)
            return projectiles;

        var projectileIds = EntityIdList.FromStringArray(owner.Data.Get(GeneratedDataKey.OwnedProjectileIds));
        foreach (var projectileId in projectileIds.Values)
        {
            if (_resolveNode(projectileId) is not IEntity projectile)
                continue;

            var currentOwnerId = projectile.Data.Has(GeneratedDataKey.ProjectileOwnerEntityId)
                ? EntityId.From(projectile.Data.Get(GeneratedDataKey.ProjectileOwnerEntityId))
                : EntityId.Empty;
            if (currentOwnerId == ownerId)
                projectiles.Add(projectile);
        }

        return projectiles;
    }

    private static EntityId ResolveEntityId(IEntity entity)
    {
        var id = EntityId.From(entity.Data.Get(GeneratedDataKey.Id));
        if (!id.IsEmpty || entity is not Node node)
            return id;

        return EntityId.From(node.GetInstanceId().ToString());
    }
}
