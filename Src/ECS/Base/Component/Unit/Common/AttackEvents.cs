using Godot;

/// <summary>
/// AttackComponent 的实体级命令和通知事件。
/// </summary>
public static class AttackEvents
{
    public readonly record struct Requested(Node2D Target) : IEntityEvent;

    public readonly record struct CancelRequested() : IEntityEvent;

    public readonly record struct Started(Node2D Target) : IEntityEvent;

    public readonly record struct Finished(Node2D? Target, bool DidHit) : IEntityEvent;

    public readonly record struct Cancelled(AttackCancelReason Reason) : IEntityEvent;
}
