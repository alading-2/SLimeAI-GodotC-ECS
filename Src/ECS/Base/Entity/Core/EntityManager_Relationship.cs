using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// EntityManager 的关系绑定扩展。
/// <para>职责：在实体生成阶段统一建立“父实体 -> 子实体”的归属关系，避免各系统重复手写 AddRelationship。</para>
/// </summary>
public static partial class EntityManager
{
    /// <summary>
    /// 统一绑定子实体的父级关系。
    /// <para>适用于投射物、特效、技能等“由某个实体生成并归属”的派生实体。</para>
    /// </summary>
    /// <param name="childEntity">子实体</param>
    /// <param name="parentEntity">父实体/归属者</param>
    /// <param name="autoAddParentRelation">是否自动补一条 PARENT 关系，供统一溯源使用</param>
    /// <param name="parentDestroyPolicy">父实体销毁时对子实体的处理策略，只写入 PARENT 关系</param>
    /// <param name="relationTypes">额外业务关系类型，如 ENTITY_TO_PROJECTILE</param>
    /// <returns>全部关系绑定成功返回 true；否则返回 false</returns>
    public static bool BindParentRelationships(
        IEntity childEntity, // 子实体
        IEntity? parentEntity, // 父实体/归属者
        bool autoAddParentRelation = true, // 是否自动补 PARENT
        ParentDestroyPolicy parentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively, // 父销毁策略
        params string[] relationTypes)
    {
        if (childEntity is not Node childNode)
        {
            _log.Error("BindParentRelationships 失败：childEntity 不是 Node");
            return false;
        }

        if (parentEntity is not Node parentNode)
        {
            _log.Warn($"子实体 {childNode.Name} 未提供 ParentEntity，跳过关系绑定");
            return false;
        }

        string childId = EntityRelationshipTraversal.ResolveEntityId(childNode); // 子实体 Id
        string parentId = EntityRelationshipTraversal.ResolveEntityId(parentNode); // 父实体 Id
        if (string.IsNullOrEmpty(childId) || string.IsNullOrEmpty(parentId))
        {
            _log.Error($"BindParentRelationships 失败：无法解析实体 Id，parent={parentNode.Name}, child={childNode.Name}");
            return false;
        }

        var normalizedRelationTypes = NormalizeOwnedRelationTypes(autoAddParentRelation, relationTypes); // 统一整理需要绑定的关系类型
        if (normalizedRelationTypes.Count == 0)
        {
            _log.Warn($"子实体 {childNode.Name} 未提供任何关系类型，跳过关系绑定");
            return false;
        }

        if (normalizedRelationTypes.Contains(EntityRelationshipType.PARENT) && WouldCreateParentCycle(childNode, parentNode))
        {
            _log.Error($"拒绝建立循环 PARENT 关系：{parentNode.Name} -> {childNode.Name}");
            return false;
        }

        // 先整体校验，避免只绑成功一部分关系导致状态半成品。
        foreach (string relationType in normalizedRelationTypes)
        {
            if (EntityRelationshipManager.GetParentEntitiesByChildAndType(childId, relationType).Any())
            {
                _log.Warn($"子实体 {childNode.Name} 已存在 {relationType} 关系，拒绝重复绑定");
                return false;
            }
        }

        foreach (string relationType in normalizedRelationTypes)
        {
            if (!EntityRelationshipManager.AddRelationship(
                    parentId, // 父实体 Id
                    childId, // 子实体 Id
                    relationType, // 关系类型
                    data: relationType == EntityRelationshipType.PARENT
                        ? EntityRelationshipLifecycle.CreateParentRelationshipData(parentDestroyPolicy) // 仅 PARENT 关系记录生命周期策略
                        : null,
                    constraint: RelationshipConstraint.OneToMany // 单子归属：一个子实体只能有一个该类型父级
                ))
            {
                _log.Error($"绑定关系失败：{parentNode.Name} -> {childNode.Name} ({relationType})");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// 规范化拥有型关系列表，自动去重并补齐 PARENT。
    /// </summary>
    private static List<string> NormalizeOwnedRelationTypes(bool autoAddParentRelation, IEnumerable<string>? relationTypes)
    {
        var result = new List<string>(4);

        if (autoAddParentRelation)
        {
            result.Add(EntityRelationshipType.PARENT);
        }

        if (relationTypes == null)
        {
            return result;
        }

        foreach (string relationType in relationTypes)
        {
            if (string.IsNullOrWhiteSpace(relationType))
            {
                continue;
            }

            if (result.Contains(relationType))
            {
                continue;
            }

            result.Add(relationType);
        }

        return result;
    }

    /// <summary>
    /// 判断新的 PARENT 绑定是否会形成环。
    /// <para>约束规则：child 不能把自己或自己的后代再挂回自己名下。</para>
    /// </summary>
    private static bool WouldCreateParentCycle(Node childNode, Node parentNode)
    {
        string childId = EntityRelationshipTraversal.ResolveEntityId(childNode); // 待绑定子实体 Id
        string parentId = EntityRelationshipTraversal.ResolveEntityId(parentNode); // 待绑定父实体 Id

        if (string.IsNullOrEmpty(childId) || string.IsNullOrEmpty(parentId))
        {
            return false;
        }

        if (childId == parentId)
        {
            return true;
        }

        foreach (IEntity ancestor in EntityRelationshipTraversal.GetAncestorChain(parentNode))
        {
            if (EntityRelationshipTraversal.ResolveEntityId((Node)ancestor) == childId)
            {
                return true;
            }
        }

        return false;
    }
}
