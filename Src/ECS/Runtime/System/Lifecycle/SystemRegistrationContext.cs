/// <summary>
/// 系统注册上下文。
/// <para>把 SystemDescriptor 与 ProjectStateService 一起传给运行时，避免系统到处找全局单例。</para>
/// </summary>
public sealed class SystemRegistrationContext
{
    /// <summary>
    /// 构造系统注册上下文。
    /// </summary>
    /// <param name="descriptor">当前系统描述符。</param>
    /// <param name="projectState">项目状态服务。</param>
    public SystemRegistrationContext(SystemDescriptor descriptor, ProjectStateService projectState)
    {
        Descriptor = descriptor;
        ProjectState = projectState;
    }

    /// <summary>当前系统描述符。</summary>
    public SystemDescriptor Descriptor { get; }

    /// <summary>项目状态服务。</summary>
    public ProjectStateService ProjectState { get; }
}
