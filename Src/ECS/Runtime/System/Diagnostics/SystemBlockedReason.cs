/// <summary>
/// System Core 阻断原因。
/// <para>Code 是机器稳定字段；Message 是面向人类的可读说明。</para>
/// </summary>
public readonly record struct SystemBlockedReason(
    SystemBlockedReasonCode Code,
    string Message)
{
    public bool IsBlocked => Code != SystemBlockedReasonCode.None;

    public static SystemBlockedReason None { get; } = new(SystemBlockedReasonCode.None, string.Empty);
}
