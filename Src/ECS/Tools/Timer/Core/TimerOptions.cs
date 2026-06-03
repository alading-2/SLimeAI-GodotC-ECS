using System;

/// <summary>
/// Timer 创建参数。owner/purpose/clock 是新 gameplay timer 的核心契约。
/// </summary>
public sealed record TimerOptions(
    TimerOwner Owner,
    TimerPurpose Purpose,
    TimerClock Clock = TimerClock.Game,
    string? Tag = null,
    Action<float>? OnUpdate = null,
    string? Source = null)
{
    public static TimerOptions Test(string ownerId, TimerPurpose purpose = TimerPurpose.Test, TimerClock clock = TimerClock.Game)
    {
        return new TimerOptions(new TimerOwner(TimerOwnerType.Test, ownerId), purpose, clock);
    }
}
