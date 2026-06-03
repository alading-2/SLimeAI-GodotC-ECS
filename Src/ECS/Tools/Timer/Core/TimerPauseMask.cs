using System;

/// <summary>
/// Timer 暂停来源。多个来源可叠加，恢复时只清理对应 bit。
/// </summary>
[Flags]
public enum TimerPauseMask
{
    None = 0,
    Manual = 1 << 0,
    Project = 1 << 1,
    Owner = 1 << 2,
    Debug = 1 << 3
}
