using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime Node 生命周期注册表。
/// </summary>
public sealed class NodeLifecycleRegistry
{
    private readonly Dictionary<string, NodeLifecycleRegistration> _registrations = new();
    private readonly Dictionary<string, HashSet<string>> _idsByType = new();

    public bool Register(Node node, NodeLifecycleOwner owner, string source)
    {
        var id = GetNodeId(node);
        if (_registrations.ContainsKey(id))
            return false;

        var type = node.GetType().Name;
        _registrations[id] = new NodeLifecycleRegistration(id, node, type, owner, source);

        if (!_idsByType.TryGetValue(type, out var ids))
        {
            ids = new HashSet<string>();
            _idsByType[type] = ids;
        }

        ids.Add(id);
        return true;
    }

    public bool Unregister(Node node)
    {
        return Unregister(GetNodeId(node));
    }

    public bool Unregister(string nodeId)
    {
        if (!_registrations.Remove(nodeId, out var registration))
            return false;

        if (_idsByType.TryGetValue(registration.NodeType, out var ids))
        {
            ids.Remove(nodeId);
            if (ids.Count == 0)
                _idsByType.Remove(registration.NodeType);
        }

        return true;
    }

    public bool IsRegistered(string nodeId)
    {
        return _registrations.ContainsKey(nodeId);
    }

    public bool IsRegistered(Node node)
    {
        return IsRegistered(GetNodeId(node));
    }

    public Node? GetNodeById(string nodeId)
    {
        return _registrations.TryGetValue(nodeId, out var registration)
            ? registration.Node
            : null;
    }

    public IReadOnlyList<T> GetNodesByType<T>() where T : Node
    {
        var type = typeof(T).Name;
        if (!_idsByType.TryGetValue(type, out var ids))
            return System.Array.Empty<T>();

        return ids
            .Select(GetNodeById)
            .OfType<T>()
            .Where(IsValid)
            .ToArray();
    }

    public IReadOnlyList<Node> GetAllNodes()
    {
        return _registrations.Values
            .Select(registration => registration.Node)
            .Where(IsValid)
            .ToArray();
    }

    public IReadOnlyList<T> GetNodesByInterface<T>() where T : class
    {
        return _registrations.Values
            .Select(registration => registration.Node)
            .Where(IsValid)
            .OfType<T>()
            .ToArray();
    }

    public NodeLifecycleSnapshot GetSnapshot()
    {
        return new NodeLifecycleSnapshot(
            _registrations.Values
                .Select(registration => new NodeLifecycleSnapshotEntry(registration))
                .ToArray());
    }

    public int CleanupInvalid()
    {
        var invalidIds = GetSnapshot()
            .Entries
            .Where(entry => !entry.IsValid)
            .Select(entry => entry.NodeId)
            .ToArray();

        foreach (var id in invalidIds)
        {
            Unregister(id);
        }

        return invalidIds.Length;
    }

    public (int TotalNodes, int TypeCount) GetStats()
    {
        return (_registrations.Count, _idsByType.Count);
    }

    public void Clear()
    {
        _registrations.Clear();
        _idsByType.Clear();
    }

    private static string GetNodeId(Node node)
    {
        return node.GetInstanceId().ToString();
    }

    private static bool IsValid(Node? node)
    {
        return node != null && GodotObject.IsInstanceValid(node) && !node.IsQueuedForDeletion();
    }
}
