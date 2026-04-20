/// <summary>
/// 单次会话阶段。
/// </summary>
public enum SessionPhase
{
    /// <summary>未处于会话。</summary>
    None,
    /// <summary>会话准备中。</summary>
    Preparing,
    /// <summary>会话进行中。</summary>
    Playing,
    /// <summary>会话结算中。</summary>
    Resolving,
    /// <summary>会话已结束。</summary>
    Ended,
}
