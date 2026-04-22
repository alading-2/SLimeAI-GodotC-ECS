using System;

/// <summary>
/// 单次会话阶段——四维状态坐标第二维：这一局在干嘛？
/// <para>仅在 AppPhase=InSession 时有意义，描述单局内的准备/进行/结算/结束。</para>
/// <para>改为 Flags 枚举，支持多阶段组合和预设。</para>
/// </summary>
[Flags]
public enum SessionPhase : byte
{
    None      = 0,
    /// <summary>会话准备中。</summary>
    Preparing = 1 << 0,
    /// <summary>会话进行中。</summary>
    Playing   = 1 << 1,
    /// <summary>会话结算中。</summary>
    Resolving = 1 << 2,
    /// <summary>会话已结束。</summary>
    Ended     = 1 << 3,

    // 预设组合
    /// <summary>所有会话阶段。</summary>
    All       = Preparing | Playing | Resolving | Ended,
    /// <summary>活跃游戏阶段（准备和进行）。</summary>
    ActivePhases = Preparing | Playing,
}
