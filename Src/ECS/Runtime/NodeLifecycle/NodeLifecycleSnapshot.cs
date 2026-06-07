using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Node 生命周期注册表诊断快照。
/// </summary>
public sealed class NodeLifecycleSnapshot
{
    public NodeLifecycleSnapshot(IReadOnlyList<NodeLifecycleSnapshotEntry> entries)
    {
        Entries = entries;
    }

    public IReadOnlyList<NodeLifecycleSnapshotEntry> Entries { get; }
    public int TotalCount => Entries.Count;
    public int InvalidCount => Entries.Count(entry => !entry.IsValid);

    public int GetOwnerCount(NodeLifecycleOwnerKind ownerKind)
    {
        return Entries.Count(entry => entry.Owner.Kind == ownerKind);
    }

    public int GetTypeCount(string nodeType)
    {
        return Entries.Count(entry => entry.NodeType == nodeType);
    }

    public NodeLifecycleSnapshotEntry? GetEntry(string nodeId)
    {
        return Entries.FirstOrDefault(entry => entry.NodeId == nodeId);
    }
}

/// <summary>
/// 单个 Node 生命周期诊断条目。
/// </summary>
public sealed class NodeLifecycleSnapshotEntry
{
    public NodeLifecycleSnapshotEntry(NodeLifecycleRegistration registration)
    {
        NodeId = registration.NodeId;
        Node = registration.Node;
        NodeType = registration.NodeType;
        Owner = registration.Owner;
        Source = registration.Source;
        RegisteredFrame = registration.RegisteredFrame;
        IsValid = GodotObject.IsInstanceValid(Node) && !Node.IsQueuedForDeletion();
        IsInsideTree = IsValid && Node.IsInsideTree();
        NodePath = IsInsideTree ? Node.GetPath().ToString() : string.Empty;
    }

    public string NodeId { get; }
    public Node Node { get; }
    public string NodeType { get; }
    public NodeLifecycleOwner Owner { get; }
    public string Source { get; }
    public ulong RegisteredFrame { get; }
    public bool IsValid { get; }
    public bool IsInsideTree { get; }
    public string NodePath { get; }
}
