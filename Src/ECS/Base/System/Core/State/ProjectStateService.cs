using System;

/// <summary>
/// 项目级状态服务。
/// <para>统一维护四个状态域，并提供切换前/中/后事件。</para>
/// </summary>
public sealed class ProjectStateService
{
    // 默认值代表框架启动后、尚未进入任何会话的初始状态。
    private ProjectStateSnapshot _snapshot = new(
        AppPhase.Boot,
        SessionPhase.None,
        OverlayPhase.None,
        ExecutionPhase.Running);

    /// <summary>当前应用主阶段。</summary>
    public AppPhase AppPhase => _snapshot.AppPhase;

    /// <summary>当前会话阶段。</summary>
    public SessionPhase SessionPhase => _snapshot.SessionPhase;

    /// <summary>当前覆盖层阶段。</summary>
    public OverlayPhase OverlayPhase => _snapshot.OverlayPhase;

    /// <summary>当前执行阶段。</summary>
    public ExecutionPhase ExecutionPhase => _snapshot.ExecutionPhase;

    /// <summary>当前完整快照。</summary>
    public ProjectStateSnapshot Snapshot => _snapshot;

    /// <summary>状态切换前事件。</summary>
    public event EventHandler<ProjectStateChangedEventArgs>? BeforeStateChanged;

    /// <summary>状态切换事件。</summary>
    public event EventHandler<ProjectStateChangedEventArgs>? StateChanged;

    /// <summary>状态切换后事件。</summary>
    public event EventHandler<ProjectStateChangedEventArgs>? AfterStateChanged;

    /// <summary>
    /// 用新的快照替换当前项目状态。
    /// </summary>
    /// <param name="next">新的项目状态。</param>
    public void Apply(ProjectStateSnapshot next)
    {
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
    /// 单独切换应用主阶段。
    /// </summary>
    /// <param name="phase">目标主阶段。</param>
    public void SetAppPhase(AppPhase phase)
    {
        Apply(_snapshot with { AppPhase = phase });
    }

    /// <summary>
    /// 单独切换会话阶段。
    /// </summary>
    /// <param name="phase">目标会话阶段。</param>
    public void SetSessionPhase(SessionPhase phase)
    {
        Apply(_snapshot with { SessionPhase = phase });
    }

    /// <summary>
    /// 单独切换覆盖层阶段。
    /// </summary>
    /// <param name="phase">目标覆盖层阶段。</param>
    public void SetOverlayPhase(OverlayPhase phase)
    {
        Apply(_snapshot with { OverlayPhase = phase });
    }

    /// <summary>
    /// 单独切换执行阶段。
    /// </summary>
    /// <param name="phase">目标执行阶段。</param>
    public void SetExecutionPhase(ExecutionPhase phase)
    {
        Apply(_snapshot with { ExecutionPhase = phase });
    }
}
