using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 单 parent 生命周期树，只表达销毁和 detach 语义。
/// </summary>
public sealed class LifecycleTree
{
    private readonly Dictionary<EntityId, LifecycleLink> _parentByChild = new();
    private readonly Dictionary<EntityId, List<LifecycleLink>> _childrenByParent = new();

    public bool Attach(
        EntityId parentId,
        EntityId childId,
        ParentDestroyPolicy destroyPolicy,
        int priority = 0)
    {
        if (parentId.IsEmpty || childId.IsEmpty || parentId == childId)
            return false;

        if (_parentByChild.ContainsKey(childId))
            return false;

        if (WouldCreateCycle(parentId, childId))
            return false;

        var link = new LifecycleLink(parentId, childId, destroyPolicy, priority);
        _parentByChild[childId] = link;

        if (!_childrenByParent.TryGetValue(parentId, out var children))
        {
            children = new List<LifecycleLink>();
            _childrenByParent[parentId] = children;
        }

        children.Add(link);
        children.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        return true;
    }

    public bool Detach(EntityId childId)
    {
        if (!_parentByChild.TryGetValue(childId, out var link))
            return false;

        _parentByChild.Remove(childId);

        if (_childrenByParent.TryGetValue(link.ParentId, out var children))
        {
            children.RemoveAll(item => item.ChildId == childId);
            if (children.Count == 0)
                _childrenByParent.Remove(link.ParentId);
        }

        return true;
    }

    /// <summary>
    /// 清理指定实体作为 child 和 parent 的所有链接。
    /// </summary>
    public void DetachAll(EntityId entityId)
    {
        if (entityId.IsEmpty)
            return;

        Detach(entityId);

        if (!_childrenByParent.TryGetValue(entityId, out var children))
            return;

        foreach (var child in children.Select(link => link.ChildId).ToArray())
        {
            Detach(child);
        }
    }

    public EntityId GetParent(EntityId childId)
    {
        return _parentByChild.TryGetValue(childId, out var link)
            ? link.ParentId
            : EntityId.Empty;
    }

    public IReadOnlyList<LifecycleLink> GetChildren(EntityId parentId)
    {
        return _childrenByParent.TryGetValue(parentId, out var children)
            ? children.ToArray()
            : [];
    }

    public LifecycleLink? GetLink(EntityId childId)
    {
        return _parentByChild.TryGetValue(childId, out var link) ? link : null;
    }

    private bool WouldCreateCycle(EntityId parentId, EntityId childId)
    {
        var current = parentId;
        while (!current.IsEmpty)
        {
            if (current == childId)
                return true;

            current = GetParent(current);
        }

        return false;
    }
}
