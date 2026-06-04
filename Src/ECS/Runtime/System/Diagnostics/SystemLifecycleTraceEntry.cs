/// <summary>
/// System 生命周期 trace 条目。
/// </summary>
public sealed class SystemLifecycleTraceEntry
{
    public long Sequence { get; init; }
    public string EventName { get; init; } = string.Empty;
    public string SystemId { get; init; } = string.Empty;
    public string ReasonCode { get; init; } = SystemBlockedReasonCode.None.ToString();
    public string Message { get; init; } = string.Empty;
    public string FlowState { get; init; } = string.Empty;
    public string Overlays { get; init; } = string.Empty;
    public string SimulationState { get; init; } = string.Empty;
}
