/// <summary>
/// 项目级状态快照——四维状态坐标的不可变瞬时值。
/// <para>四个维度同时存在，描述游戏此刻的完整运行态：</para>
/// <para>  第一维 AppPhase      — 应用在哪个大阶段？</para>
/// <para>  第二维 SessionPhase  — 这一局在干嘛？</para>
/// <para>  第三维 OverlayPhase  — 有没有 UI 覆盖层挡着？</para>
/// <para>  第四维 ExecutionPhase — 逻辑在跑还是停着？</para>
/// <para>使用不可变 record struct，确保系统在同一帧读取到一致状态视图。</para>
/// </summary>
/// <param name="AppPhase">应用主阶段（第一维）。</param>
/// <param name="SessionPhase">当前会话阶段（第二维）。</param>
/// <param name="OverlayPhase">当前覆盖层阶段（第三维）。</param>
/// <param name="ExecutionPhase">当前执行阶段（第四维）。</param>
public readonly record struct ProjectStateSnapshot(
    AppPhase AppPhase,
    SessionPhase SessionPhase,
    OverlayPhase OverlayPhase,
    ExecutionPhase ExecutionPhase);
