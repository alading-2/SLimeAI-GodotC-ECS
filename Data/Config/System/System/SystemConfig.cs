using Godot;

/// <summary>
/// 系统配置资源。
/// <para>数据驱动架构：系统元数据从代码迁移到表格资源，支持可视化编辑。</para>
/// <para>代码注册只提供 SystemId + Factory，其他配置在此表格中定义。</para>
/// </summary>
[GlobalClass]
public partial class SystemConfig : Resource
{
    // ============================================================
    // 基础信息
    // ============================================================

    /// <summary>系统唯一 Id（必须与 SystemId 枚举值一致）。</summary>
    [Export]
    public string SystemId { get; set; } = string.Empty;

    /// <summary>系统运行形态。</summary>
    [Export]
    public SystemKind Kind { get; set; } = SystemKind.NodeScript;

    /// <summary>系统分组（挂载位置），多选 Flags。</summary>
    [Export]
    public SystemGroup Groups { get; set; } = SystemGroup.Else;

    /// <summary>系统标签（逻辑分类），多选 Flags。</summary>
    [Export]
    public SystemTag Tags { get; set; } = SystemTag.None;

    // ============================================================
    // 加载配置
    // ============================================================

    /// <summary>默认是否自动装载（Profile 未提供覆盖时回退到此字段）。</summary>
    [Export]
    public bool DefaultAutoAdd { get; set; } = true;

    /// <summary>默认是否启用（首次纳入管理时的人为开关默认值）。</summary>
    [Export]
    public bool DefaultEnabled { get; set; } = true;

    /// <summary>加载优先级（数值越小越优先，用于依赖排序）。</summary>
    [Export]
    public int Priority { get; set; } = 0;

    // ============================================================
    // 运行条件（替代 SystemRunCondition）
    // ============================================================

    /// <summary>允许的应用主阶段（Flags 组合，为 None 表示不限制）。</summary>
    [Export]
    public AppPhase AllowedAppPhases { get; set; } = AppPhase.None;

    /// <summary>允许的会话阶段（Flags 组合，为 None 表示不限制）。</summary>
    [Export]
    public SessionPhase AllowedSessionPhases { get; set; } = SessionPhase.None;

    /// <summary>允许的覆盖层阶段（Flags 组合，为 None 表示不限制）。</summary>
    [Export]
    public OverlayPhase AllowedOverlayPhases { get; set; } = OverlayPhase.None;

    /// <summary>禁止的覆盖层阶段（Flags 组合，为 None 表示不限制）。</summary>
    [Export]
    public OverlayPhase BlockedOverlayPhases { get; set; } = OverlayPhase.None;

    /// <summary>允许的执行阶段（Flags 组合，为 None 表示不限制）。</summary>
    [Export]
    public ExecutionPhase AllowedExecutionPhases { get; set; } = ExecutionPhase.None;

    // ============================================================
    // 依赖关系
    // ============================================================

    /// <summary>依赖系统 Id 列表（使用 SystemId 枚举值的字符串形式）。</summary>
    [Export]
    public string[] Dependencies { get; set; } = System.Array.Empty<string>();

    // ============================================================
    // 说明
    // ============================================================

    /// <summary>系统描述（用于文档和调试）。</summary>
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;
}
