/// <summary>
/// 系统命令执行结果。
/// <para>Success 表示命令已通过 SystemCore 门禁并进入系统处理器；领域层是否成功由 Value 自行表达。</para>
/// </summary>
/// <typeparam name="TResult">系统命令的领域结果类型。</typeparam>
public readonly record struct SystemExecuteResult<TResult>(
    bool Success,
    TResult? Value,
    string Message,
    SystemBlockedReasonCode ReasonCode = SystemBlockedReasonCode.None
)
{
    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="value">领域结果。</param>
    public static SystemExecuteResult<TResult> Ok(TResult value)
    {
        return new SystemExecuteResult<TResult>(true, value, string.Empty, SystemBlockedReasonCode.None);
    }

    /// <summary>
    /// 创建阻断结果。
    /// </summary>
    /// <param name="message">阻断原因。</param>
    public static SystemExecuteResult<TResult> Blocked(string message)
    {
        return new SystemExecuteResult<TResult>(false, default, message, SystemBlockedReasonCode.Unknown);
    }

    /// <summary>
    /// 创建带稳定原因码的阻断结果。
    /// </summary>
    /// <param name="reasonCode">稳定阻断原因码。</param>
    /// <param name="message">中文阻断原因。</param>
    public static SystemExecuteResult<TResult> Blocked(SystemBlockedReasonCode reasonCode, string message)
    {
        return new SystemExecuteResult<TResult>(false, default, message, reasonCode);
    }
}
