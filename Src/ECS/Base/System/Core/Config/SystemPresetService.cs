using System;
using System.Collections.Generic;
using System.Linq;
using slime.data.Systems;

/// <summary>
/// 系统预设服务。
/// <para>负责加载和解析系统预设，提供预设查询和应用接口。</para>
/// <para>只从 DataOS snapshot-backed DTO 读取配置。</para>
/// </summary>
public static class SystemPresetService
{
    private static readonly Log _log = new(nameof(SystemPresetService));
    private static readonly List<SystemPresetData> _presets = new();
    private static SystemPresetData? _activePreset;
    private static bool _isInitialized;

    /// <summary>
    /// 初始化预设服务，加载所有系统预设。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _presets.Clear();
        _activePreset = null;

        foreach (var data in SystemPresetData.All)
        {
            if (data == null)
            {
                _log.Error("SystemPresetData.All 包含空预设项，请检查静态初始化顺序");
                continue;
            }

            TryAddPreset(data, warnDuplicate: true);
        }

        _isInitialized = true;
        _log.Info($"SystemPresetService 初始化完成，共加载 {_presets.Count} 个预设，激活预设: {_activePreset?.PresetName ?? "无"}");
    }

    /// <summary>
    /// 获取当前激活的预设。
    /// </summary>
    public static SystemPresetData? GetActivePreset()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _activePreset;
    }

    /// <summary>
    /// 获取所有预设。
    /// </summary>
    public static IEnumerable<SystemPresetData> GetAllPresets()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _presets;
    }

    /// <summary>
    /// 根据激活的预设计算应该加载的系统 Id 列表。
    /// <para>规则：</para>
    /// <para>1. Required 系统强制加载</para>
    /// <para>2. 无激活预设时加载 AutoLoad 系统</para>
    /// <para>3. 有激活预设时加载 EnabledTags / EnabledSystemIds 命中的系统</para>
    /// <para>4. 排除 DisabledSystemIds 列表</para>
    /// </summary>
    public static HashSet<string> CalculateEnabledSystems()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        var enabledSystems = new HashSet<string>(StringComparer.Ordinal);

        // 1. Required 系统强制加载
        var requiredConfigs = SystemConfigService.GetRequiredConfigs();
        foreach (var config in requiredConfigs)
        {
            enabledSystems.Add(config.SystemId);
        }

        // 2. 如果没有激活预设，使用 AutoLoad
        if (_activePreset == null)
        {
            var autoLoadConfigs = SystemConfigService.GetAutoLoadConfigs();
            foreach (var config in autoLoadConfigs)
            {
                enabledSystems.Add(config.SystemId);
            }

            return enabledSystems;
        }

        // 3. 应用激活预设的规则
        var preset = _activePreset;

        // 3.1 收集 EnabledTags 对应的系统
        if (preset.EnabledTags != SystemTag.None)
        {
            var tagConfigs = SystemConfigService.GetConfigsByTag(preset.EnabledTags);
            foreach (var config in tagConfigs)
            {
                enabledSystems.Add(config.SystemId);
            }
        }

        // 3.2 收集 EnabledSystemIds 列表
        foreach (var systemId in preset.EnabledSystemIds)
        {
            if (!string.IsNullOrWhiteSpace(systemId))
            {
                enabledSystems.Add(systemId);
            }
        }

        // 3.3 排除 DisabledSystemIds 列表（优先级最高）
        foreach (var systemId in preset.DisabledSystemIds)
        {
            if (!string.IsNullOrWhiteSpace(systemId))
            {
                // Required 系统不允许被禁用
                var config = SystemConfigService.GetConfig(systemId);
                if (config != null && config.Required)
                {
                    _log.Warn($"预设 {preset.PresetName} 尝试禁用必需系统 {systemId}，已忽略");
                    continue;
                }

                enabledSystems.Remove(systemId);
            }
        }

        return enabledSystems;
    }

    private static void TryAddPreset(SystemPresetData preset, bool warnDuplicate)
    {
        if (string.IsNullOrWhiteSpace(preset.PresetName))
        {
            _log.Warn("系统预设名称为空，跳过");
            return;
        }

        if (_presets.Any(existing => string.Equals(existing.PresetName, preset.PresetName, StringComparison.Ordinal)))
        {
            if (warnDuplicate)
            {
                _log.Warn($"重复的系统预设: {preset.PresetName}");
            }
            else
            {
                _log.Debug($"系统预设 {preset.PresetName} 已由 DataOS snapshot 提供，跳过重复项");
            }

            return;
        }

        _presets.Add(preset);
        _log.Debug($"加载系统预设: {preset.PresetName} (Active={preset.IsActive})");

        if (!preset.IsActive)
        {
            return;
        }

        if (_activePreset != null)
        {
            _log.Error($"检测到多个激活的预设: {_activePreset.PresetName} 和 {preset.PresetName}，只允许一个预设激活！");
            throw new InvalidOperationException($"只允许一个 SystemPresetData 的 IsActive = true，但发现多个: {_activePreset.PresetName}, {preset.PresetName}");
        }

        _activePreset = preset;
    }
}
