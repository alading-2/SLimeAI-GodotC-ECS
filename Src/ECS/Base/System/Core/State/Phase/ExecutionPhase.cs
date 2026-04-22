using System;

/// <summary>
/// 逻辑执行阶段——四维状态坐标第四维：逻辑在跑还是停着？
/// <para>SystemManager 据此裁决系统是否允许运行（ProcessMode / ISystemLifecycle.OnStarted/OnStopped）。</para>
/// <para>改为 Flags 枚举，支持多执行状态组合和预设。</para>
/// </summary>
[Flags]
public enum ExecutionPhase : byte
{
    None    = 0,
    /// <summary>正常连续运行。</summary>
    Running = 1 << 0,
    /// <summary>暂停运行。</summary>
    Paused  = 1 << 1,
    /// <summary>执行被外部流程阻塞（过场/等待关键条件）。</summary>
    Blocked = 1 << 2,

    // 预设组合
    /// <summary>所有执行阶段。</summary>
    All     = Running | Paused | Blocked,
    /// <summary>非运行阶段（暂停和阻塞）。</summary>
    NotRunning = Paused | Blocked,
}
