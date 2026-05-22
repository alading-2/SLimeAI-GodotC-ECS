using Godot;

/// <summary>
/// Unit 领域事件。跨系统观察的战斗结果使用广播事件，其余保持实体局部。
/// </summary>
public static class UnitEvents
{
    public readonly record struct Damaged(
        IEntity Victim,
        float Amount,
        IEntity? Attacker = null,
        DamageType Type = DamageType.True,
        bool IsCritical = false) : IBroadcastEvent;

    public readonly record struct Dodged(IEntity Victim, IEntity? Attacker = null) : IBroadcastEvent;

    public readonly record struct HealRequest(float Amount, HealSource Source = HealSource.Unknown) : IEntityEvent;

    public readonly record struct HealApplied(
        IEntity Victim,
        float RequestedAmount,
        float ActualAmount,
        HealSource Source) : IBroadcastEvent;

    public readonly record struct Killed(
        IEntity? Victim,
        IEntity? Killer,
        DeathType DeathType = DeathType.Normal,
        DamageType DamageType = DamageType.True) : IBroadcastEvent;

    public readonly record struct LevelUp(IEntity Entity, int OldLevel, int NewLevel) : IBroadcastEvent;

    public readonly record struct StateChanged(string Key, string OldValue, string NewValue) : IEntityEvent;

    public readonly record struct Reviving(float Duration) : IEntityEvent;

    public readonly record struct Revived() : IEntityEvent;

    public readonly record struct AnimationFinished(string AnimName) : IEntityEvent;

    public readonly record struct StopAnimationRequested() : IEntityEvent;

    public readonly record struct PlayAnimationRequested(
        string AnimName,
        bool ForceRestart = false,
        float Duration = -1f) : IEntityEvent;

    public readonly record struct MovementStarted(MoveMode Mode, MovementParams Params) : IEntityEvent;

    public readonly record struct MovementCompleted(
        MoveMode Mode,
        float ElapsedTime,
        float TraveledDistance,
        MovementStopReason Reason,
        Node2D? CollisionTarget = null) : IEntityEvent;

    public readonly record struct MovementCollision(
        MoveMode Mode,
        Node2D? Target,
        IEntity? TargetEntity = null,
        int CollisionCount = 0,
        bool WillStop = false) : IEntityEvent;

    public readonly record struct OrientationStarted : IEntityEvent
    {
        public OrientationStarted()
        {
        }

        public OrientationSource Source { get; init; } = OrientationSource.Standalone;

        public OrientationParams Params { get; init; } = new();

        public bool StopWithMovement { get; init; }
    }

    public readonly record struct OrientationStopped : IEntityEvent
    {
        public OrientationStopped()
        {
        }

        public OrientationSource Source { get; init; } = OrientationSource.Standalone;

        public MovementStopReason Reason { get; init; } = MovementStopReason.Requested;
    }

    public readonly record struct MovementStopRequested : IEntityEvent
    {
        public MovementStopRequested()
        {
        }

        public MovementStopReason Reason { get; init; } = MovementStopReason.Requested;

        public bool EmitCompletedEvent { get; init; } = true;

        public MoveMode NextMode { get; init; } = MoveMode.None;

        public Node2D? CollisionTarget { get; init; }

        public bool DestroyEntity { get; init; }
    }
}
