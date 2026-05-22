using Godot;

/// <summary>
/// Collision 组件的实体级局部事件。
/// </summary>
public static class CollisionEvents
{
    public readonly record struct CollisionEntered(IEntity Source, Node2D Target) : IEntityEvent;

    public readonly record struct CollisionExited(IEntity Source, Node2D Target) : IEntityEvent;

    public readonly record struct HurtboxEntered(
        IEntity Source,
        Area2D Hurtbox,
        Node2D Target,
        IEntity? TargetEntity = null) : IEntityEvent;

    public readonly record struct HurtboxExited(
        IEntity Source,
        Area2D Hurtbox,
        Node2D Target,
        IEntity? TargetEntity = null) : IEntityEvent;
}
