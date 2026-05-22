using Godot;

/// <summary>
/// TargetingSystem 的 world 级瞄准流程事件。
/// </summary>
public static class TargetingEvents
{
    public readonly record struct StartTargeting(CastContext Context) : IGlobalEvent;

    public readonly record struct TargetConfirmed(Vector2 TargetPosition) : IGlobalEvent;

    public readonly record struct TargetCancelled() : IGlobalEvent;

    public readonly record struct TargetingEnded(bool WasConfirmed) : IGlobalEvent;
}
