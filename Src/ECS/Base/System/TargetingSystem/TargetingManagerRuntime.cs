using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// TargetingManager 的生命周期桥接系统。
/// </summary>
public sealed partial class TargetingManagerRuntime : Node, ISystem
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SystemRegistry.Register(new SystemDescriptor(nameof(TargetingManager), SystemKind.NodeScript, SystemLifetime.Persistent)
        {
            Factory = static () => new TargetingManagerRuntime(),
        });
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        TargetingManager.EnableRuntime();
    }

    /// <inheritdoc />
    public void OnStopped(ProjectStateSnapshot snapshot)
    {
        TargetingManager.DisableRuntime();
    }

    /// <inheritdoc />
    public void OnRemoved()
    {
        TargetingManager.DisableRuntime();
    }
}
