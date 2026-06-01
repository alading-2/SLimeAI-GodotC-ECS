using System;

/// <summary>
/// 项目级状态服务。
/// <para>统一维护流程、覆盖层和模拟状态，并通过实例级 C# event 广播切换前/中/后事件。</para>
/// </summary>
public sealed class ProjectStateService
{
    private const ushort KnownFlowStateBits =
        (ushort)(GameFlowState.Boot
            | GameFlowState.FrontEnd
            | GameFlowState.SessionPreparing
            | GameFlowState.SessionPlaying
            | GameFlowState.SessionResolving
            | GameFlowState.SessionEnded
            | GameFlowState.ShuttingDown);

    private const byte KnownSimulationStateBits =
        (byte)(SimulationState.Running | SimulationState.Suspended);

    // 默认值代表框架启动后、尚未进入任何会话的初始状态。
    private ProjectStateSnapshot _snapshot = new(
        GameFlowState.Boot,
        OverlayFlags.None,
        SimulationState.Running);

    /// <summary>当前游戏流程状态。</summary>
    public GameFlowState FlowState => _snapshot.FlowState;

    /// <summary>当前激活的覆盖层标记。</summary>
    public OverlayFlags Overlays => _snapshot.Overlays;

    /// <summary>当前模拟推进状态。</summary>
    public SimulationState SimulationState => _snapshot.SimulationState;

    /// <summary>当前完整快照。</summary>
    public ProjectStateSnapshot Snapshot => _snapshot;

    /// <summary>
    /// 状态切换前事件。
    /// <para>这里不用 EventBus：ProjectStateService 是 SystemManager 持有的实例状态源，实例级事件能避免临时测试服务污染全局运行时。</para>
    /// </summary>
    public event EventHandler<ProjectStateChangedEventArgs>? BeforeStateChanged;

    /// <summary>
    /// 状态切换事件。
    /// <para>这里不用 EventBus：状态门禁重算只属于当前 ProjectStateService 实例，SystemManager 订阅本实例后再分发给托管系统。</para>
    /// </summary>
    public event EventHandler<ProjectStateChangedEventArgs>? StateChanged;

    /// <summary>
    /// 状态切换后事件。
    /// <para>这里不用 EventBus：切换完成通知仍应跟随当前状态源实例，避免全局事件被外部误当成业务协议订阅。</para>
    /// </summary>
    public event EventHandler<ProjectStateChangedEventArgs>? AfterStateChanged;

    /// <summary>
    /// 用新的快照替换当前项目状态。
    /// </summary>
    /// <param name="next">新的项目状态。</param>
    public void Apply(ProjectStateSnapshot next)
    {
        ValidateSnapshot(next);

        // 避免重复广播同一状态。
        if (_snapshot.Equals(next))
        {
            return;
        }

        var previous = _snapshot;
        var args = new ProjectStateChangedEventArgs(previous, next);

        BeforeStateChanged?.Invoke(this, args); // 切换前：允许系统做收尾或保存现场
        _snapshot = next;
        StateChanged?.Invoke(this, args); // 切换中：主要用于系统运行资格重算
        AfterStateChanged?.Invoke(this, args); // 切换后：用于补充逻辑/触发后续流程
    }

    /// <summary>
    /// 单独切换游戏流程状态。
    /// </summary>
    /// <param name="state">目标流程状态。</param>
    public void SetFlowState(GameFlowState state)
    {
        Apply(_snapshot with { FlowState = state });
    }

    /// <summary>
    /// 单独切换覆盖层标记。
    /// </summary>
    /// <param name="overlays">目标覆盖层标记。</param>
    public void SetOverlays(OverlayFlags overlays)
    {
        Apply(_snapshot with { Overlays = overlays });
    }

    /// <summary>
    /// 单独切换模拟推进状态。
    /// </summary>
    /// <param name="state">目标模拟状态。</param>
    public void SetSimulationState(SimulationState state)
    {
        Apply(_snapshot with { SimulationState = state });
    }

    /// <summary>
    /// 进入前台流程。
    /// <para>用于主菜单、配置页等非局内场景。</para>
    /// </summary>
    public void EnterFrontEnd()
    {
        Apply(new ProjectStateSnapshot(
            GameFlowState.FrontEnd,
            OverlayFlags.None,
            SimulationState.Running));
    }

    /// <summary>
    /// 进入局内主流程。
    /// <para>默认直接落到 Playing，用于现有主场景直接开局链路。</para>
    /// </summary>
    public void BeginGameplaySession()
    {
        Apply(new ProjectStateSnapshot(
            GameFlowState.SessionPlaying,
            OverlayFlags.None,
            SimulationState.Running));
    }

    /// <summary>
    /// 打开暂停菜单并暂停主逻辑。
    /// </summary>
    public void OpenPauseMenu()
    {
        Apply(_snapshot with
        {
            Overlays = _snapshot.Overlays | OverlayFlags.PauseMenu,
            SimulationState = SimulationState.Suspended
        });
    }

    /// <summary>
    /// 关闭暂停菜单并恢复主逻辑。
    /// </summary>
    public void ClosePauseMenu()
    {
        Apply(_snapshot with
        {
            Overlays = _snapshot.Overlays & ~OverlayFlags.PauseMenu,
            SimulationState = SimulationState.Running
        });
    }

    /// <summary>
    /// 进入阻塞态。
    /// <para>典型场景是过场或强制等待外部流程。</para>
    /// </summary>
    /// <param name="reason">阻塞对应的覆盖层标记。</param>
    public void SetBlocked(OverlayFlags reason)
    {
        Apply(_snapshot with
        {
            Overlays = reason,
            SimulationState = SimulationState.Suspended
        });
    }

    /// <summary>
    /// 清理阻塞态并恢复主逻辑。
    /// </summary>
    public void ClearBlocked()
    {
        Apply(_snapshot with
        {
            Overlays = OverlayFlags.None,
            SimulationState = SimulationState.Running
        });
    }

    /// <summary>
    /// 结束当前会话。
    /// <para>保留当前主阶段，统一清空覆盖层并阻塞执行。</para>
    /// </summary>
    public void EndSession()
    {
        Apply(_snapshot with
        {
            FlowState = GameFlowState.SessionEnded,
            Overlays = OverlayFlags.None,
            SimulationState = SimulationState.Suspended
        });
    }

    /// <summary>
    /// 校验快照合法性：FlowState 和 SimulationState 必须各仅含一个合法位。
    /// </summary>
    private static void ValidateSnapshot(ProjectStateSnapshot snapshot)
    {
        if (!IsSingleFlowState(snapshot.FlowState))
        {
            throw new System.ArgumentException($"ProjectStateSnapshot.FlowState 必须是单一流程状态: {snapshot.FlowState}");
        }

        if (!IsSingleSimulationState(snapshot.SimulationState))
        {
            throw new System.ArgumentException($"ProjectStateSnapshot.SimulationState 必须是单一模拟状态: {snapshot.SimulationState}");
        }
    }

    /// <summary>
    /// 判断是否为单一合法流程状态（恰好一个已知位被置位）。
    /// </summary>
    /// <remarks>
    /// 三重校验：
    /// 1) value != 0 — 不允许空值；
    /// 2) (value &amp; ~KnownFlowStateBits) == 0 — 不允许未知位；
    /// 3) (value &amp; (value - 1)) == 0 — 经典单比特检测，不允许多位组合。
    /// </remarks>
    private static bool IsSingleFlowState(GameFlowState state)
    {
        var value = (ushort)state;
        return value != 0 && (value & ~KnownFlowStateBits) == 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    /// 判断是否为单一合法模拟状态（恰好一个已知位被置位）。
    /// </summary>
    /// <remarks>
    /// 三重校验：
    /// 1) value != 0 — 不允许空值；
    /// 2) (value &amp; ~KnownSimulationStateBits) == 0 — 不允许未知位；
    /// 3) (value &amp; (value - 1)) == 0 — 经典单比特检测，不允许多位组合。
    /// </remarks>
    private static bool IsSingleSimulationState(SimulationState state)
    {
        var value = (byte)state;
        return value != 0 && (value & ~KnownSimulationStateBits) == 0 && (value & (value - 1)) == 0;
    }
}
