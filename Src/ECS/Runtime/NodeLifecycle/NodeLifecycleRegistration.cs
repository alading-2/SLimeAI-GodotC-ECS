using Godot;

/// <summary>
/// Node 生命周期注册记录。
/// </summary>
public sealed class NodeLifecycleRegistration
{
    public NodeLifecycleRegistration(
        string nodeId,
        Node node,
        string nodeType,
        NodeLifecycleOwner owner,
        string source)
    {
        NodeId = nodeId;
        Node = node;
        NodeType = nodeType;
        Owner = owner;
        Source = source;
        RegisteredFrame = (ulong)Engine.GetFramesDrawn();
    }

    public string NodeId { get; }
    public Node Node { get; }
    public string NodeType { get; }
    public NodeLifecycleOwner Owner { get; }
    public string Source { get; }
    public ulong RegisteredFrame { get; }
}
