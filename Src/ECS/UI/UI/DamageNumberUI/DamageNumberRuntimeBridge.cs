using System.Runtime.CompilerServices;

/// <summary>
/// DamageNumberSystem 的运行时桥接服务。
/// </summary>
public sealed class DamageNumberRuntimeBridge : ISystem
{
    [ModuleInitializer]
    public static void Initialize()
    {
        SystemRegistry.Register(new SystemDescriptor(nameof(DamageNumberSystem), SystemKind.PureService, SystemLifetime.Persistent)
        {
            Factory = static () => new DamageNumberRuntimeBridge(),
        });
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        DamageNumberSystem.EnableRuntime();
    }

    /// <inheritdoc />
    public void OnStopped(ProjectStateSnapshot snapshot)
    {
        DamageNumberSystem.DisableRuntime();
    }

    /// <inheritdoc />
    public void OnRemoved()
    {
        DamageNumberSystem.DisableRuntime();
    }
}
