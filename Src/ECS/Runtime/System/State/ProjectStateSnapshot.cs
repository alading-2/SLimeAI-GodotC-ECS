/// <summary>
/// 项目级状态快照。
/// <para>三个状态域共同描述当前项目流程、覆盖层和模拟推进状态。</para>
/// <para>使用不可变 record struct，确保系统在同一帧读取到一致状态视图。</para>
/// </summary>
/// <param name="FlowState">当前游戏流程状态；运行时快照只允许单一流程状态。</param>
/// <param name="Overlays">当前激活的覆盖层标记。</param>
/// <param name="SimulationState">当前模拟推进状态；运行时快照只允许单一模拟状态。</param>
public readonly record struct ProjectStateSnapshot(
    GameFlowState FlowState,
    OverlayFlags Overlays,
    SimulationState SimulationState);
