/// <summary>
/// 单个 Timer 的诊断条目。
/// </summary>
public sealed record TimerObservation(
    TimerHandle Handle,
    TimerMode Mode,
    TimerClock Clock,
    TimerOwner Owner,
    TimerPurpose Purpose,
    string? Tag,
    float Duration,
    float Interval,
    float Remaining,
    float Progress,
    int RepeatRemaining,
    TimerPauseMask PauseMask,
    TimerCancelReason CancelReason,
    string? Source);
