using System;

/// <summary>
/// 系统描述符。
/// <para>描述“系统是什么”而不是“系统当前状态”。当前状态由 ManagedSystemEntry 维护。</para>
/// </summary>
public sealed class SystemDescriptor
{
    /// <summary>
    /// 构造系统描述符。
    /// </summary>
    /// <param name="systemId">系统唯一 Id。</param>
    /// <param name="kind">系统运行形态。</param>
    /// <param name="lifetime">系统生命周期域。</param>
    public SystemDescriptor(string systemId, SystemKind kind, SystemLifetime lifetime)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            throw new ArgumentException("SystemId 不能为空", nameof(systemId));
        }

        SystemId = systemId;
        Kind = kind;
        Lifetime = lifetime;
    }

    /// <summary>系统唯一 Id。</summary>
    public string SystemId { get; }

    /// <summary>系统运行形态。</summary>
    public SystemKind Kind { get; }

    /// <summary>系统生命周期域。</summary>
    public SystemLifetime Lifetime { get; }

    /// <summary>默认是否启用。</summary>
    /// <remarks>仅表示首次接管时的初始开关，不会覆盖运行中人工启停状态。</remarks>
    public bool DefaultEnabled { get; init; } = true;

    /// <summary>系统运行条件。</summary>
    public SystemRunCondition RunCondition { get; init; } = SystemRunCondition.Always;

    /// <summary>挂载父路径；纯服务为空即可。</summary>
    /// <remarks>当前语义为“相对对应 Lifetime Host 的路径”。</remarks>
    public string ParentPath { get; init; } = string.Empty;

    /// <summary>依赖系统 Id 列表。</summary>
    /// <remarks>依赖会在 EnsureSystem 中递归确保先创建。</remarks>
    public string[] Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 系统实例工厂。
    /// <para>NodeScript / PureService 统一由工厂创建；NodeScene 可返回 PackedScene.Instantiate() 的结果。</para>
    /// </summary>
    public Func<object>? Factory { get; init; }
}
