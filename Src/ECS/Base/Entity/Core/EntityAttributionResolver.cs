using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 归因解析器。
/// <para>用于 Damage / Movement 等系统解析直接来源背后的 owner/source 链，避免沿旧 Relationship parent-chain 猜业务归属。</para>
/// </summary>
public static class EntityAttributionResolver
{
    private const int MaxDepth = 10;

    /// <summary>
    /// 解析用于阵营、暴击、吸血等规则的归属单位。
    /// </summary>
    public static IUnit? ResolveUnit(Node? source)
    {
        if (source is not IEntity entity)
            return null;

        foreach (var item in ResolveChainFromEntity(entity))
        {
            if (item is IUnit unit)
                return unit;
        }

        return null;
    }

    /// <summary>
    /// 解析直接来源到归属来源的链路。第一个元素总是传入实体本身。
    /// </summary>
    public static IReadOnlyList<IEntity> ResolveChain(Node? source)
    {
        if (source is not IEntity entity)
            return System.Array.Empty<IEntity>();

        return ResolveChainFromEntity(entity);
    }

    /// <summary>
    /// 解析直接来源到归属来源的链路。第一个元素总是传入实体本身。
    /// </summary>
    private static IReadOnlyList<IEntity> ResolveChainFromEntity(IEntity? source)
    {
        var result = new List<IEntity>();
        var visited = new HashSet<EntityId>();
        IEntity? current = source;

        for (var depth = 0; current != null && depth < MaxDepth; depth++)
        {
            var currentId = ResolveEntityId(current);
            if (!currentId.IsEmpty && !visited.Add(currentId))
                break;

            result.Add(current);

            var nextId = ResolveNextSourceId(current);
            if (nextId.IsEmpty)
                break;

            current = EntityManager.ResolveEntityNode(nextId) as IEntity;
        }

        return result;
    }

    private static EntityId ResolveNextSourceId(IEntity entity)
    {
        var projectileOwnerId = ReadEntityId(entity, GeneratedDataKey.ProjectileOwnerEntityId);
        if (!projectileOwnerId.IsEmpty)
            return projectileOwnerId;

        var effectHostId = ReadEntityId(entity, GeneratedDataKey.EffectHostEntityId);
        if (!effectHostId.IsEmpty)
            return effectHostId;

        var sourceId = ReadEntityId(entity, GeneratedDataKey.SourceEntityId);
        if (!sourceId.IsEmpty)
            return sourceId;

        var originId = ReadEntityId(entity, GeneratedDataKey.OriginEntityId);
        if (!originId.IsEmpty)
            return originId;

        return EntityId.Empty;
    }

    private static EntityId ReadEntityId(IEntity entity, DataKey<string> key)
    {
        return entity.Data.Has(key)
            ? EntityId.From(entity.Data.Get(key))
            : EntityId.Empty;
    }

    private static EntityId ResolveEntityId(IEntity entity)
    {
        var id = entity.Data.Has(GeneratedDataKey.Id)
            ? EntityId.From(entity.Data.Get(GeneratedDataKey.Id))
            : EntityId.Empty;
        if (!id.IsEmpty || entity is not Node node)
            return id;

        return EntityId.From(node.GetInstanceId().ToString());
    }
}
