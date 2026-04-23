using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// DamageNumberSystem 的运行时桥接服务。
/// </summary>
public sealed class DamageNumberRuntimeBridge : ISystem
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SystemRegistry.Register(nameof(DamageNumberSystem),
            static () => new DamageNumberRuntimeBridge());
    }

    /// <inheritdoc />
    public void OnEnabled(ProjectStateSnapshot snapshot)
    {
        DamageNumberSystem.EnableRuntime();
    }

    /// <inheritdoc />
    public void OnDisabled(ProjectStateSnapshot snapshot)
    {
        DamageNumberSystem.DisableRuntime();
    }

    /// <inheritdoc />
    public void OnUnRegistered()
    {
        DamageNumberSystem.DisableRuntime();
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(DamageNumberSystem),
            CustomStats = new List<SystemStat>()
        };
    }
}