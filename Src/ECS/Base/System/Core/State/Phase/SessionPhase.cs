/// <summary>
/// 单次会话阶段——四维状态坐标第二维：这一局在干嘛？
/// <para>仅在 AppPhase=InSession 时有意义，描述单局内的准备/进行/结算/结束。</para>
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
