using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity runtime 注册表，维护 EntityId 与 Godot Node 的双向索引。
/// </summary>
public sealed class EntityRegistry
{
    private readonly Dictionary<EntityId, Node> _nodesById = new();
    private readonly Dictionary<Node, EntityId> _idsByNode = new();

    /// <summary>
    /// 注册实体节点。空 id、空节点、重复 id 或重复节点都会被拒绝。
    /// </summary>
    public bool Register(EntityId id, Node? node)
    {
        if (id.IsEmpty || node == null)
            return false;

        if (_nodesById.ContainsKey(id) || _idsByNode.ContainsKey(node))
            return false;

        _nodesById[id] = node;
        _idsByNode[node] = id;
        return true;
    }

    /// <summary>
    /// 注销指定 id，并同步清理反向索引。
    /// </summary>
    public bool Unregister(EntityId id)
    {
        if (!_nodesById.TryGetValue(id, out var node))
            return false;

        _nodesById.Remove(id);
        _idsByNode.Remove(node);
        return true;
    }

    /// <summary>
    /// 注销指定节点，并同步清理正向索引。
    /// </summary>
    public bool Unregister(Node? node)
    {
        if (node == null || !_idsByNode.TryGetValue(node, out var id))
            return false;

        _idsByNode.Remove(node);
        _nodesById.Remove(id);
        return true;
    }

    public Node? GetNode(EntityId id)
    {
        return _nodesById.TryGetValue(id, out var node) ? node : null;
    }

    public EntityId GetEntityId(Node? node)
    {
        return node != null && _idsByNode.TryGetValue(node, out var id)
            ? id
            : EntityId.Empty;
    }

    /// <summary>
    /// 返回当前注册表快照，避免外部修改内部索引。
    /// </summary>
    public IReadOnlyDictionary<EntityId, Node> Snapshot()
    {
        return _nodesById.ToDictionary(pair => pair.Key, pair => pair.Value);
    }
}
