using Godot;

/// <summary>
/// 有效移动碰撞上下文。
/// </summary>
public readonly struct MovementCollisionContext
{
    public MovementCollisionContext(
        MoveMode mode,
        Node2D targetNode,
        IEntity? targetEntity,
        int collisionCount,
        bool willStop,
        MovementParams @params)
    {
        Mode = mode;
        TargetNode = targetNode;
        TargetEntity = targetEntity;
        CollisionCount = collisionCount;
        WillStop = willStop;
        Params = @params;
    }

    /// <summary>发生碰撞时的移动模式。</summary>
    public MoveMode Mode { get; }

    /// <summary>命中的节点。</summary>
    public Node2D TargetNode { get; }

    /// <summary>命中节点所属实体；不存在则为 null。</summary>
    public IEntity? TargetEntity { get; }

    /// <summary>本次运动内第几个有效碰撞。</summary>
    public int CollisionCount { get; }

    /// <summary>该次有效碰撞是否会触发自动停止。</summary>
    public bool WillStop { get; }

    /// <summary>本次运动参数快照。</summary>
    public MovementParams Params { get; }
}
