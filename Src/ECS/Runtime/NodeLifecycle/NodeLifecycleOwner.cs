/// <summary>
/// Node 生命周期注册 owner metadata。
/// </summary>
public readonly record struct NodeLifecycleOwner(
    NodeLifecycleOwnerKind Kind,
    string OwnerId,
    string? ComponentId = null)
{
    public static NodeLifecycleOwner Unknown(string ownerId = "")
        => new(NodeLifecycleOwnerKind.Unknown, ownerId);

    public static NodeLifecycleOwner Entity(string entityId)
        => new(NodeLifecycleOwnerKind.Entity, entityId);

    public static NodeLifecycleOwner Component(string entityId, string componentId)
        => new(NodeLifecycleOwnerKind.Component, entityId, componentId);

    public static NodeLifecycleOwner UI(string uiId)
        => new(NodeLifecycleOwnerKind.UI, uiId);

    public static NodeLifecycleOwner System(string systemId)
        => new(NodeLifecycleOwnerKind.System, systemId);

    public static NodeLifecycleOwner Tool(string toolId)
        => new(NodeLifecycleOwnerKind.Tool, toolId);

    public static NodeLifecycleOwner Test(string testId)
        => new(NodeLifecycleOwnerKind.Test, testId);
}
