/// <summary>
/// System Core 稳定阻断原因码。
/// <para>用于 diagnostics、BDD 和自动验证；中文 message 仍保留给日志和 UI。</para>
/// </summary>
public enum SystemBlockedReasonCode
{
    None = 0,
    Disabled = 1,
    FlowStateMismatch = 2,
    MissingRequiredOverlay = 3,
    BlockedOverlay = 4,
    SimulationStateMismatch = 5,
    NotLoaded = 6,
    NotRunning = 7,
    NotRegistered = 8,
    MissingConfig = 9,
    DependencyMissing = 10,
    CommandTargetMissing = 11,
    Unknown = 99
}
