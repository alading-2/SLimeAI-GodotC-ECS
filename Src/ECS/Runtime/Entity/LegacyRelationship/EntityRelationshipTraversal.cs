using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Entity 关系追溯工具。
/// <para>职责单一：只负责沿关系链解析实体 Id、查直接父级、查祖先链，不负责关系增删改。</para>
/// <para>所有“归属者是谁”“这个派生实体最终属于哪个单位”这类问题，都应统一从这里进入。</para>
/// </summary>
public static class EntityRelationshipTraversal
{
    private static readonly Log _log = new(nameof(EntityRelationshipTraversal), LogLevel.Warning);

    /// <summary>
    /// 统一解析实体 Id；优先使用 GeneratedDataKey.Id，未初始化时回退到节点实例 Id。
    /// </summary>
    /// <param name="entity">实体节点</param>
    /// <returns>实体 Id；解析失败返回空字符串</returns>
    public static string ResolveEntityId(Node? entity)
    {
        if (entity == null)
        {
            return string.Empty;
        }

        if (entity is IEntity iEntity)
        {
            string entityId = iEntity.Data.Get<string>(GeneratedDataKey.Id); // Data 中记录的实体 Id
            if (!string.IsNullOrEmpty(entityId))
            {
                return entityId;
            }
        }

        return entity.GetInstanceId().ToString();
    }

    /// <summary>
    /// 获取直接父级实体。
    /// </summary>
    /// <param name="entity">起始实体</param>
    /// <param name="relationType">关系类型，默认使用 PARENT 作为归属主链</param>
    /// <returns>直接父级节点；不存在返回 null</returns>
    public static Node? GetDirectParent(Node entity, string relationType = EntityRelationshipType.PARENT)
    {
        if (entity == null)
        {
            _log.Error("GetDirectParent 传入节点为 null");
            return null;
        }

        string entityId = ResolveEntityId(entity); // 起始实体 Id
        if (string.IsNullOrEmpty(entityId))
        {
            return null;
        }

        string? parentId = EntityRelationshipManager
            .GetParentEntitiesByChildAndType(entityId, relationType)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(parentId))
        {
            return null;
        }

        return EntityManager.GetEntityById(parentId);
    }

    /// <summary>
    /// 获取直接父级实体并尝试转为指定类型。
    /// </summary>
    /// <typeparam name="T">目标父级类型</typeparam>
    /// <param name="entity">起始实体</param>
    /// <param name="relationType">关系类型，默认使用 PARENT</param>
    /// <returns>匹配的直接父级；不匹配返回 null</returns>
    public static T? GetDirectParentOfType<T>(Node entity, string relationType = EntityRelationshipType.PARENT) where T : class
    {
        return GetDirectParent(entity, relationType) as T;
    }

    /// <summary>
    /// 查找第一个符合类型的祖先实体。
    /// <para>会先检查自身，再沿 PARENT 关系逐级向上查找。</para>
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="entity">起始实体</param>
    /// <param name="maxDepth">最大查找深度，防止关系异常时无限循环</param>
    /// <returns>命中的祖先实体；未找到返回 null</returns>
    public static T? FindAncestorOfType<T>(Node entity, int maxDepth = 10) where T : class
    {
        if (entity == null)
        {
            _log.Error($"FindAncestorOfType 传入节点为 null，无法查找 {typeof(T).Name}");
            return null;
        }

        if (entity is T typedEntity)
        {
            return typedEntity;
        }

        string currentId = ResolveEntityId(entity); // 当前遍历实体 Id
        if (string.IsNullOrEmpty(currentId))
        {
            return null;
        }

        int depth = 0;
        while (depth < maxDepth)
        {
            string? parentId = EntityRelationshipManager
                .GetParentEntitiesByChildAndType(currentId, EntityRelationshipType.PARENT)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(parentId))
            {
                break;
            }

            Node? parentEntity = EntityManager.GetEntityById(parentId);
            if (parentEntity == null)
            {
                _log.Warn($"父级实体 {parentId} 已不存在，终止向上查找");
                break;
            }

            if (parentEntity is T matchedParent)
            {
                return matchedParent;
            }

            currentId = parentId;
            depth++;
        }

        _log.Warn($"未能在实体 {ResolveEntityId(entity)}({entity.GetType().Name}) 的层级链上找到类型 {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 获取从起始实体到最顶层的祖先链。
    /// <para>自身如果实现了 IEntity，也会作为第一个元素返回。</para>
    /// </summary>
    /// <param name="startNode">起始节点</param>
    /// <param name="maxDepth">最大查找深度</param>
    /// <returns>沿 PARENT 关系向上的 IEntity 序列</returns>
    public static IEnumerable<IEntity> GetAncestorChain(Node startNode, int maxDepth = 10)
    {
        if (startNode == null)
        {
            yield break;
        }

        if (startNode is IEntity startEntity)
        {
            yield return startEntity;
        }

        string currentId = ResolveEntityId(startNode); // 当前遍历实体 Id
        if (string.IsNullOrEmpty(currentId))
        {
            yield break;
        }

        int depth = 0;
        while (depth < maxDepth)
        {
            string? parentId = EntityRelationshipManager
                .GetParentEntitiesByChildAndType(currentId, EntityRelationshipType.PARENT)
                .FirstOrDefault();

            if (string.IsNullOrEmpty(parentId))
            {
                yield break;
            }

            Node? parentEntity = EntityManager.GetEntityById(parentId);
            if (parentEntity == null)
            {
                yield break;
            }

            if (parentEntity is IEntity iEntity)
            {
                yield return iEntity;
            }

            currentId = parentId;
            depth++;
        }
    }
}
