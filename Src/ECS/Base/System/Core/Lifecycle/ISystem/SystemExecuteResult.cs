/// <summary>
/// 系统命令执行结果。
/// <para>Success 表示命令已通过 SystemCore 门禁并进入系统处理器；领域层是否成功由 Value 自行表达。</para>
/// </summary>
/// <typeparam name="TResult">系统命令的领域结果类型。</typeparam>
public readonly record struct SystemExecuteResult<TResult>(
    bool Success,
    TResult? Value,
    string Message
)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="value">领域结果。</param>
    public static SystemExecuteResult<TResult> Ok(TResult value)
    {
        return new SystemExecuteResult<TResult>(true, value, string.Empty);
    }

    /// <summary>
    /// 创建阻断结果。
    /// </summary>
    /// <param name="message">阻断原因。</param>
    public static SystemExecuteResult<TResult> Blocked(string message)
    {
        return new SystemExecuteResult<TResult>(false, default, message);
    }
}
