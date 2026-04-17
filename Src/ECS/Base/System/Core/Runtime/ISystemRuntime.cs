/// <summary>
/// 可被 <c>SystemManager</c> 治理的系统运行时接口。
/// <para>默认方法全部为空，实现方可按需覆写。</para>
/// </summary>
public interface ISystemRuntime
{
    /// <summary>
    /// 系统实例创建并接入管理器后触发。
    /// </summary>
    /// <param name="context">注册上下文。</param>
    /// <remarks>通常在此缓存依赖、完成一次性初始化。</remarks>
    void OnSystemRegistered(SystemRegistrationContext context)
    {
    }

    /// <summary>
    /// 系统被启用时触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    /// <remarks>适合在这里订阅事件、恢复处理流程。</remarks>
    void OnSystemEnabled(ProjectStateSnapshot snapshot)
    {
    }

    /// <summary>
    /// 系统被禁用时触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    /// <remarks>适合在这里退订事件、清理短生命周期资源。</remarks>
    void OnSystemDisabled(ProjectStateSnapshot snapshot)
    {
    }

    /// <summary>
    /// 项目状态切换时触发。
    /// </summary>
    /// <param name="args">状态切换事件参数。</param>
    void OnProjectStateChanged(ProjectStateChangedEventArgs args)
    {
    }

    /// <summary>
    /// 系统从管理器中移除前触发。
    /// </summary>
    void OnSystemUnregistered()
    {
    }
}
