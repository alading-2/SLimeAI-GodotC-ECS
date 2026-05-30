namespace slime.data.Systems;

/// <summary>
/// 系统预设 DTO；数据来源为 runtime snapshot 的 system.preset records。
/// </summary>
public class SystemPresetData
{
    /// <summary>预设名称。</summary>
    public string PresetName { get; set; } = "DefaultPreset";

    /// <summary>是否激活此预设（只允许一个 Preset 为 true）。</summary>
    public bool IsActive { get; set; }

    /// <summary>启用的系统标签（Flags 组合）。</summary>
    public SystemTag EnabledTags { get; set; } = SystemTag.None;

    /// <summary>显式启用的系统 Id 列表。</summary>
    public string[] EnabledSystemIds { get; set; } = System.Array.Empty<string>();

    /// <summary>显式禁用的系统 Id 列表（Required 系统不能被禁用）。</summary>
    public string[] DisabledSystemIds { get; set; } = System.Array.Empty<string>();

    /// <summary>预设描述（用于文档和调试）。</summary>
    public string Description { get; set; } = "";
}
