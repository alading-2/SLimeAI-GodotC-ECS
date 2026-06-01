using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// TargetingManager 的生命周期桥接系统。
/// </summary>
public sealed partial class TargetingManagerRuntime : Node, ISystem
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SystemRegistry.Register(nameof(TargetingManagerRuntime),
            static () => new TargetingManagerRuntime());
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
    public void OnUnRegistered()
    {
        TargetingManager.DisableRuntime();
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(TargetingManagerRuntime),
            CustomStats = new List<SystemStat>()
        };
    }
}
