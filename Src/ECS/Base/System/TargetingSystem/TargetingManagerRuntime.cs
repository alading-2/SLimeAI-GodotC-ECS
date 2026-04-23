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
        SystemRegistry.Register(nameof(TargetingManager),
            static () => new TargetingManagerRuntime());
    }

    /// <inheritdoc />
    public void OnEnabled(ProjectStateSnapshot snapshot)
    {
        TargetingManager.EnableRuntime();
    }

    /// <inheritdoc />
    public void OnDisabled(ProjectStateSnapshot snapshot)
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
            SystemId = nameof(TargetingManager),
            CustomStats = new List<SystemStat>()
        };
    }
}