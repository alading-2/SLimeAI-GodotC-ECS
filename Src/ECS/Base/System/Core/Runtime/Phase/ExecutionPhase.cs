/// <summary>
/// 逻辑执行阶段。
/// </summary>
public enum ExecutionPhase
{
    /// <summary>正常连续运行。</summary>
    Running,
    /// <summary>暂停运行。</summary>
    Paused,
    /// <summary>单步执行（调试态）。</summary>
    Step,
    /// <summary>执行被外部流程阻塞（过场/等待关键条件）。</summary>
    Blocked,
}
