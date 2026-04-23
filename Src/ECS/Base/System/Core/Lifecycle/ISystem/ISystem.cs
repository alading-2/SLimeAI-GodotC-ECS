/// <summary>
/// System Core 对外主接口。
/// <para>默认业务系统直接实现 <see cref="ISystem"/>，同时获得生命周期与项目状态观察能力。</para>
/// <para>若极少数内部实现只需要部分能力，仍可只实现 <see cref="ISystemLifecycle"/> 或 <see cref="IProjectStateAwareSystem"/>。</para>
/// </summary>
public interface ISystem : ISystemLifecycle, IProjectStateAwareSystem
{
    /// <summary>
    /// 获取系统运行时信息（用于调试和监控）。
    /// </summary>
    SystemRuntimeInfo GetSystemRuntimeInfo();
}