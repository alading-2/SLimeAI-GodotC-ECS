/// <summary>
/// Timer diagnostics 显式过滤条件，避免默认 dump 输出过大。
/// </summary>
public sealed record TimerDiagnosticsFilter(
    TimerOwner? Owner = null,
    TimerPurpose? Purpose = null,
    TimerClock? Clock = null,
    string? Tag = null,
    int MaxEntries = 100);
