/// <summary>
/// 系统运行条件。
/// <para>用于把业务状态判断前置到调度层，避免系统内部散落条件分支。</para>
/// </summary>
public sealed class SystemRunCondition
{
    /// <summary>无条件运行。</summary>
    public static SystemRunCondition Always { get; } = new();

    /// <summary>
    /// 局内主玩法运行条件。
    /// <para>仅在会话进行中且执行态为 Running 时通过。</para>
    /// </summary>
    public static SystemRunCondition GameplayRunning()
    {
        return new SystemRunCondition
        {
            AllowedFlowStates = GameFlowState.Gameplay,
            BlockedOverlays = OverlayFlags.Blocking,
            AllowedSimulationStates = SimulationState.Running
        };
    }

    /// <summary>
    /// 覆盖层系统运行条件。
    /// <para>默认允许 PauseMenu / ModalUi / Cutscene 三类覆盖层。</para>
    /// </summary>
    /// <param name="requiredOverlays">要求存在的覆盖层；为 None 时使用默认覆盖层集合。</param>
    public static SystemRunCondition OverlayActive(OverlayFlags requiredOverlays = OverlayFlags.None)
    {
        var overlays = requiredOverlays == OverlayFlags.None
            ? OverlayFlags.PauseMenu | OverlayFlags.ModalUi | OverlayFlags.Cutscene
            : requiredOverlays;

        return new SystemRunCondition
        {
            RequiredOverlays = overlays
        };
    }

    /// <summary>允许的流程状态；为 None 表示不限制。</summary>
    public GameFlowState AllowedFlowStates { get; set; } = GameFlowState.None;

    /// <summary>要求存在的覆盖层；为 None 表示不要求覆盖层。</summary>
    public OverlayFlags RequiredOverlays { get; set; } = OverlayFlags.None;

    /// <summary>禁止的覆盖层；为 None 表示不屏蔽任何覆盖层。</summary>
    public OverlayFlags BlockedOverlays { get; set; } = OverlayFlags.None;

    /// <summary>允许的模拟状态；为 None 表示不限制。</summary>
    public SimulationState AllowedSimulationStates { get; set; } = SimulationState.None;

    /// <summary>
    /// 判断指定快照是否满足当前运行条件。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    /// <returns>满足条件返回 true。</returns>
    public bool Evaluate(ProjectStateSnapshot snapshot)
    {
        return !GetBlockedReason(snapshot).IsBlocked;
    }

    /// <summary>
    /// 返回运行条件未通过的状态和原因；IsBlocked=false 表示通过。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    public (bool IsBlocked, string Reason) GetBlockedReason(ProjectStateSnapshot snapshot)
    {
        if (!MatchesFlowState(AllowedFlowStates, snapshot.FlowState))
        {
            return (true, $"FlowState={snapshot.FlowState} 不在允许范围 {AllowedFlowStates}");
        }

        if (!ContainsRequiredOverlays(RequiredOverlays, snapshot.Overlays))
        {
            return (true, $"Overlays={snapshot.Overlays} 缺少要求覆盖层 {RequiredOverlays}");
        }

        if (HasBlockedOverlay(BlockedOverlays, snapshot.Overlays))
        {
            return (true, $"Overlays={snapshot.Overlays} 命中禁止覆盖层 {BlockedOverlays}");
        }

        if (!MatchesSimulationState(AllowedSimulationStates, snapshot.SimulationState))
        {
            return (true, $"SimulationState={snapshot.SimulationState} 不在允许范围 {AllowedSimulationStates}");
        }

        return (false, string.Empty);
    }

    /// <summary>
    /// 判断流程状态是否匹配允许的 Flags。
    /// </summary>
    /// <param name="allowedStates">允许的流程状态（为 None 表示不限制）。</param>
    /// <param name="currentState">当前流程状态。</param>
    private static bool MatchesFlowState(GameFlowState allowedStates, GameFlowState currentState)
    {
        if (allowedStates == GameFlowState.None)
        {
            return true;
        }

        return (allowedStates & currentState) != 0;
    }

    /// <summary>
    /// 判断模拟状态是否匹配允许的 Flags。
    /// </summary>
    /// <param name="allowedStates">允许的模拟状态（为 None 表示不限制）。</param>
    /// <param name="currentState">当前模拟状态。</param>
    private static bool MatchesSimulationState(SimulationState allowedStates, SimulationState currentState)
    {
        if (allowedStates == SimulationState.None)
        {
            return true;
        }

        return (allowedStates & currentState) != 0;
    }

    private static bool ContainsRequiredOverlays(OverlayFlags requiredOverlays, OverlayFlags currentOverlays)
    {
        return requiredOverlays == OverlayFlags.None || (currentOverlays & requiredOverlays) == requiredOverlays;
    }

    private static bool HasBlockedOverlay(OverlayFlags blockedOverlays, OverlayFlags currentOverlays)
    {
        return blockedOverlays != OverlayFlags.None && (currentOverlays & blockedOverlays) != 0;
    }

}
