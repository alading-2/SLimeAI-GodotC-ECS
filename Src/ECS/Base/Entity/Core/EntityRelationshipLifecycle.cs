using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Entity 关系生命周期工具。
/// <para>职责单一：负责读取 PARENT 关系上的生命周期策略，并输出父实体的直接归属子实体快照。</para>
/// <para>不负责真正执行销毁；真正的销毁顺序仍由 EntityManager 统一编排。</para>
/// </summary>
public static class EntityRelationshipLifecycle
{
    private const string ParentDestroyPolicyDataKey = "parent_destroy_policy";

    private static readonly Log _log = new(nameof(EntityRelationshipLifecycle), LogLevel.Warning);

    /// <summary>
    /// 直接归属子实体快照。
    /// <para>销毁阶段先拍快照，再处理递归，避免遍历期间关系表被改写。</para>
    /// </summary>
    internal readonly record struct OwnedChildSnapshot(
        string ChildId,
        Node ChildNode,
        ParentDestroyPolicy DestroyPolicy
    );

    /// <summary>
    /// 创建 PARENT 关系上的生命周期数据。
    /// </summary>
    /// <param name="parentDestroyPolicy">父销毁策略</param>
    /// <returns>写入关系记录的附加数据</returns>
    public static Dictionary<string, object> CreateParentRelationshipData(
        ParentDestroyPolicy parentDestroyPolicy // 父销毁策略
    )
    {
        return new Dictionary<string, object>(1)
        {
            [ParentDestroyPolicyDataKey] = (int)parentDestroyPolicy // 统一按 int 存储，避免序列化/装箱差异
        };
    }

    /// <summary>
    /// 读取指定 PARENT 关系上的父销毁策略。
    /// <para>缺失或解析失败时，回退为 DestroyRecursively。</para>
    /// </summary>
    /// <param name="parentId">父实体 Id</param>
    /// <param name="childId">子实体 Id</param>
    /// <returns>父销毁策略</returns>
    public static ParentDestroyPolicy ReadParentDestroyPolicy(
        string parentId, // 父实体 Id
        string childId // 子实体 Id
    )
    {
        Dictionary<string, object>? relationData = EntityRelationshipManager.GetRelationshipData(
            parentId, // 父实体 Id
            childId, // 子实体 Id
            EntityRelationshipType.PARENT // 统一从 PARENT 关系读取生命周期策略
        );

        if (relationData == null)
        {
            return ParentDestroyPolicy.DestroyRecursively;
        }

        // 只按标准写入格式读取：int -> ParentDestroyPolicy。
        if (relationData.TryGetValue(ParentDestroyPolicyDataKey, out object? rawValue) &&
            rawValue is int intValue &&
            intValue >= (int)ParentDestroyPolicy.DestroyRecursively &&
            intValue <= (int)ParentDestroyPolicy.Detach)
        {
            return (ParentDestroyPolicy)intValue;
        }

        // 缺键或脏数据直接回退默认值，不做额外兼容解析。
        return ParentDestroyPolicy.DestroyRecursively;
    }

    /// <summary>
    /// 获取父实体当前的直接归属子实体快照。
    /// <para>这里只看 PARENT，业务关系只负责分类，不参与生命周期决策。</para>
    /// </summary>
    /// <param name="parentEntity">父实体</param>
    /// <returns>直接归属子实体快照列表</returns>
    internal static List<OwnedChildSnapshot> GetDirectOwnedChildren(
        Node parentEntity // 父实体
    )
    {
        var result = new List<OwnedChildSnapshot>(4);

        string parentId = EntityRelationshipTraversal.ResolveEntityId(parentEntity); // 父实体 Id
        if (string.IsNullOrEmpty(parentId))
        {
            return result;
        }

        List<string> childIds = EntityRelationshipManager
            .GetChildEntitiesByParentAndType(parentId, EntityRelationshipType.PARENT)
            .ToList();

        foreach (string childId in childIds)
        {
            Node? childNode = EntityManager.GetEntityById(childId);
            if (childNode == null)
            {
                continue;
            }

            ParentDestroyPolicy destroyPolicy = ReadParentDestroyPolicy(
                parentId, // 父实体 Id
                childId // 子实体 Id
            );

            result.Add(new OwnedChildSnapshot(
                childId, // 子实体 Id
                childNode, // 子实体节点
                destroyPolicy // 父销毁策略
            ));
        }

        return result;
    }
}
