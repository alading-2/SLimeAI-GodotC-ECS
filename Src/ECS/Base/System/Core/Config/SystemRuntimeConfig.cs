using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统运行时配置，由 DataOS runtime_snapshot 的 system.config 表构建。
/// </summary>
public sealed class SystemRuntimeConfig
{
    /// <summary>系统唯一 Id。</summary>
    public string SystemId { get; init; } = string.Empty;

    /// <summary>系统挂载分组。</summary>
    public SystemGroup MountGroup { get; init; } = SystemGroup.Else;

    /// <summary>系统标签。</summary>
    public SystemTag Tags { get; init; } = SystemTag.None;

    /// <summary>是否为必需系统。</summary>
    public bool Required { get; init; }

    /// <summary>默认是否自动装载。</summary>
    public bool AutoLoad { get; init; } = true;

    /// <summary>首次纳入管理时的人工开关默认值。</summary>
    public bool StartEnabled { get; init; } = true;

    /// <summary>加载优先级，数值越小越优先。</summary>
    public int Priority { get; init; }

    /// <summary>允许的流程状态。</summary>
    public GameFlowState AllowedFlowStates { get; init; } = GameFlowState.None;

    /// <summary>要求存在的覆盖层。</summary>
    public OverlayFlags RequiredOverlays { get; init; } = OverlayFlags.None;

    /// <summary>禁止的覆盖层。</summary>
    public OverlayFlags BlockedOverlays { get; init; } = OverlayFlags.None;

    /// <summary>允许的模拟状态。</summary>
    public SimulationState AllowedSimulationStates { get; init; } = SimulationState.None;

    /// <summary>依赖系统 Id 列表。</summary>
    public string[] Dependencies { get; init; } = Array.Empty<string>();

    /// <summary>系统描述。</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// 系统预设运行时配置，由 DataOS runtime_snapshot 的 system.preset 表构建。
/// </summary>
public sealed class SystemRuntimePreset
{
    /// <summary>预设名称。</summary>
    public string PresetName { get; init; } = string.Empty;

    /// <summary>是否激活此预设。</summary>
    public bool IsActive { get; init; }

    /// <summary>启用的系统标签。</summary>
    public SystemTag EnabledTags { get; init; } = SystemTag.None;

    /// <summary>显式启用的系统 Id 列表。</summary>
    public string[] EnabledSystemIds { get; init; } = Array.Empty<string>();

    /// <summary>显式禁用的系统 Id 列表。</summary>
    public string[] DisabledSystemIds { get; init; } = Array.Empty<string>();

    /// <summary>预设描述。</summary>
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// DataOS snapshot 运行时配置解析工具。
/// </summary>
internal static class RuntimeSnapshotConfigReader
{
    public static IReadOnlyList<SystemRuntimeConfig> LoadSystemConfigs()
    {
        const string table = "system.config";
        var ids = SnapshotLoader.GetRecordIds(SnapshotLoader.DefaultSnapshotPath, table);
        var result = new List<SystemRuntimeConfig>(ids.Count);
        foreach (var id in ids)
        {
            var f = SnapshotLoader.GetRecordFields(SnapshotLoader.DefaultSnapshotPath, table, id);
            result.Add(new SystemRuntimeConfig
            {
                SystemId = Str(f, "SystemId"),
                MountGroup = EnumValue<SystemGroup>(Str(f, "MountGroup")),
                Tags = Flags<SystemTag>(Str(f, "Tags")),
                Required = Bool(f, "Required"),
                AutoLoad = Bool(f, "AutoLoad", true),
                StartEnabled = Bool(f, "StartEnabled", true),
                Priority = Int(f, "Priority"),
                AllowedFlowStates = Flags<GameFlowState>(Str(f, "AllowedFlowStates")),
                RequiredOverlays = Flags<OverlayFlags>(Str(f, "RequiredOverlays")),
                BlockedOverlays = Flags<OverlayFlags>(Str(f, "BlockedOverlays")),
                AllowedSimulationStates = Flags<SimulationState>(Str(f, "AllowedSimulationStates")),
                Dependencies = StringArray(Str(f, "Dependencies")),
                Description = Str(f, "Description"),
            });
        }
        return result;
    }

    public static IReadOnlyList<SystemRuntimePreset> LoadSystemPresets()
    {
        const string table = "system.preset";
        var ids = SnapshotLoader.GetRecordIds(SnapshotLoader.DefaultSnapshotPath, table);
        var result = new List<SystemRuntimePreset>(ids.Count);
        foreach (var id in ids)
        {
            var f = SnapshotLoader.GetRecordFields(SnapshotLoader.DefaultSnapshotPath, table, id);
            result.Add(new SystemRuntimePreset
            {
                PresetName = Str(f, "PresetName"),
                IsActive = Bool(f, "IsActive"),
                EnabledTags = Flags<SystemTag>(Str(f, "EnabledTags")),
                EnabledSystemIds = StringArray(Str(f, "EnabledSystemIds")),
                DisabledSystemIds = StringArray(Str(f, "DisabledSystemIds")),
                Description = Str(f, "Description"),
            });
        }
        return result;
    }

    private static string Str(IReadOnlyDictionary<string, (string TypeName, string StringValue)> f, string key)
    {
        return f.TryGetValue(key, out var value) ? value.StringValue : string.Empty;
    }

    private static bool Bool(IReadOnlyDictionary<string, (string TypeName, string StringValue)> f, string key, bool defaultValue = false)
    {
        return f.TryGetValue(key, out var value) ? value.StringValue.Equals("true", StringComparison.OrdinalIgnoreCase) : defaultValue;
    }

    private static int Int(IReadOnlyDictionary<string, (string TypeName, string StringValue)> f, string key)
    {
        return f.TryGetValue(key, out var value) && int.TryParse(value.StringValue, out var parsed) ? parsed : 0;
    }

    private static T EnumValue<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, ignoreCase: true, out var parsed) ? parsed : default;
    }

    private static T Flags<T>(string value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return default;
        var result = 0;
        foreach (var part in value.Split(','))
        {
            if (Enum.TryParse<T>(part.Trim(), ignoreCase: true, out var parsed))
            {
                result |= Convert.ToInt32(parsed);
            }
        }
        return (T)(object)result;
    }

    private static string[] StringArray(string value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Array.Empty<string>()
            : value.Split(',').Select(static s => s.Trim()).Where(static s => s.Length > 0).ToArray();
    }
}
