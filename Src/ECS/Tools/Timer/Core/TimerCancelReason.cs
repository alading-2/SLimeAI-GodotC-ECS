/// <summary>
/// Timer 取消原因，用于 diagnostics 和生命周期追踪。
/// </summary>
public enum TimerCancelReason
{
    None,
    Manual,
    OwnerDestroyed,
    ComponentUnregistered,
    SystemStopped,
    AbilityCancelled,
    TargetInvalid,
    SceneExit,
    Replaced,
    TestCleanup,
    Completed
}
