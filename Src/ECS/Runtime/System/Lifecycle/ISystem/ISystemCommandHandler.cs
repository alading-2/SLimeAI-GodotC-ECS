/// <summary>
/// 系统命令处理接口。
/// <para>外部请求系统执行运行态能力时，由 <see cref="SystemManager"/> 先判断系统是否可执行，再转发到实现者。</para>
/// </summary>
/// <typeparam name="TRequest">命令请求类型。</typeparam>
/// <typeparam name="TResult">命令结果类型。</typeparam>
public interface ISystemCommandHandler<in TRequest, out TResult>
{
    /// <summary>
    /// 执行系统命令。
    /// </summary>
    /// <param name="request">命令请求。</param>
    TResult Execute(TRequest request);
}
