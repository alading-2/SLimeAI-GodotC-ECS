namespace slime.data.Systems;

/// <summary>
/// 系统配置 DTO；数据来源为 runtime snapshot 的 system.config records。
/// </summary>
public class SystemData
{
    /// <summary>系统唯一 Id。</summary>
    public string SystemId { get; set; } = "";

    /// <summary>系统挂载分组（Host 位置，单选）。</summary>
    public SystemGroup MountGroup { get; set; } = SystemGroup.Else;

    /// <summary>系统标签（逻辑分类和预设筛选），多选 Flags。</summary>
    public SystemTag Tags { get; set; } = SystemTag.None;

    /// <summary>是否为必需系统（无论预设如何都装载）。</summary>
    public bool Required { get; set; }

    /// <summary>默认是否自动装载（没有激活预设时使用）。</summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>首次纳入管理时的人工开关默认值。</summary>
    public bool StartEnabled { get; set; } = true;

    /// <summary>加载优先级（数值越小越优先，用于依赖排序）。</summary>
    public int Priority { get; set; }

    /// <summary>允许的流程状态（Flags 组合，为 None 表示不限制）。</summary>
    public GameFlowState AllowedFlowStates { get; set; } = GameFlowState.None;

    /// <summary>要求存在的覆盖层（Flags 组合，为 None 表示不要求覆盖层）。</summary>
    public OverlayFlags RequiredOverlays { get; set; } = OverlayFlags.None;

    /// <summary>禁止的覆盖层（Flags 组合，为 None 表示不屏蔽覆盖层）。</summary>
    public OverlayFlags BlockedOverlays { get; set; } = OverlayFlags.None;

    /// <summary>允许的模拟状态（Flags 组合，为 None 表示不限制）。</summary>
    public SimulationState AllowedSimulationStates { get; set; } = SimulationState.None;

    /// <summary>依赖系统 Id 列表。</summary>
    public string[] Dependencies { get; set; } = System.Array.Empty<string>();

    /// <summary>系统描述（用于文档和调试）。</summary>
    public string Description { get; set; } = "";
}
