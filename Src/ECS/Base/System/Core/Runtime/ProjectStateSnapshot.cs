/// <summary>
/// 项目级状态快照。
/// <para>使用不可变 record struct，确保系统在同一帧读取到一致状态视图。</para>
/// </summary>
/// <param name="AppPhase">应用主阶段。</param>
/// <param name="SessionPhase">当前会话阶段。</param>
/// <param name="OverlayPhase">当前覆盖层阶段。</param>
/// <param name="ExecutionPhase">当前执行阶段。</param>
public readonly record struct ProjectStateSnapshot(
    AppPhase AppPhase,
    SessionPhase SessionPhase,
    OverlayPhase OverlayPhase,
    ExecutionPhase ExecutionPhase);
