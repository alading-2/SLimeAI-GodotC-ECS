/// <summary>
/// System Core 对外主接口。
/// <para>默认业务系统直接实现 <see cref="ISystem"/>，由 <see cref="SystemManager"/> 统一托管生命周期、项目状态变化与运行信息。</para>
/// </summary>
public interface ISystem
{
    /// <summary>
    /// 系统实例已创建并纳入管理器。
    /// </summary>
    /// <param name="context">系统注册上下文。</param>
    void OnRegistered(SystemRegistrationContext context)
    {
    }

    /// <summary>
    /// 系统实例即将从管理器中移除。
    /// </summary>
    void OnUnRegistered()
    {
    }

    /// <summary>
    /// 系统进入实际运行态时触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    void OnStarted(ProjectStateSnapshot snapshot)
    {
    }

    /// <summary>
    /// 系统离开实际运行态时触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    void OnStopped(ProjectStateSnapshot snapshot)
    {
    }

    /// <summary>
    /// 项目状态发生切换时触发。
    /// </summary>
    /// <param name="args">状态切换事件参数。</param>
    void OnProjectStateChanged(ProjectStateChangedEventArgs args)
    {
    }

    /// <summary>
    /// 获取系统运行时信息（用于调试和监控）。
    /// </summary>
    SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo();
    }
}
