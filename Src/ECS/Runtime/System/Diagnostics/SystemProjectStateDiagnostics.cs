/// <summary>
/// Diagnostics 中使用的项目状态快照。
/// </summary>
public sealed class SystemProjectStateDiagnostics
{
    public string FlowState { get; init; } = string.Empty;
    public string Overlays { get; init; } = string.Empty;
    public string SimulationState { get; init; } = string.Empty;

    public static SystemProjectStateDiagnostics From(ProjectStateSnapshot snapshot)
    {
        return new SystemProjectStateDiagnostics
        {
            FlowState = snapshot.FlowState.ToString(),
            Overlays = snapshot.Overlays.ToString(),
            SimulationState = snapshot.SimulationState.ToString()
        };
    }
}
