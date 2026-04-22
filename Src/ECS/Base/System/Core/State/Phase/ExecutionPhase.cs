/// <summary>
/// 逻辑执行阶段——四维状态坐标第四维：逻辑在跑还是停着？
/// <para>SystemManager 据此裁决系统是否允许运行（ProcessMode / ISystemLifecycle.OnStarted/OnStopped）。</para>
/// </summary>
public enum ExecutionPhase
{
    /// <summary>正常连续运行。</summary>
    Running,
    /// <summary>暂停运行。</summary>
    Paused,
    /// <summary>执行被外部流程阻塞（过场/等待关键条件）。</summary>
    Blocked,
}
