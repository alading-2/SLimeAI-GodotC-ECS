/// <summary>
/// 系统生命周期协议。
/// <para>把“实例存在性”“人工开关”“实际运行态”拆成独立语义，避免互相混用。</para>
/// </summary>
public interface ISystemLifecycle
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
    /// 显式调用 EnableSystem 后触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    void OnEnabled(ProjectStateSnapshot snapshot)
    {
    }

    /// <summary>
    /// 显式调用 DisableSystem 后触发。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    void OnDisabled(ProjectStateSnapshot snapshot)
    {
    }
}