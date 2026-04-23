using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统预设服务。
/// <para>负责加载和解析 SystemPreset 资源，提供预设查询和应用接口。</para>
/// <para>使用 ResourceManagement 统一加载资源。</para>
/// </summary>
public static class SystemPresetService
{
    private static readonly Log _log = new(nameof(SystemPresetService));
    private static readonly List<SystemPreset> _presets = new();
    private static SystemPreset? _activePreset;
    private static bool _isInitialized;

    /// <summary>
    /// 初始化预设服务，加载所有 SystemPreset 资源。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _presets.Clear();
        _activePreset = null;

        // 使用 ResourceManagement 加载所有系统预设
        var presets = ResourceManagement.LoadAll<SystemPreset>(ResourceCategory.ConfigSystemPreset);
        foreach (var preset in presets)
        {
            _presets.Add(preset);
            _log.Debug($"加载系统预设: {preset.PresetName} (Active={preset.IsActive})");

            if (preset.IsActive)
            {
                if (_activePreset != null)
                {
                    _log.Error($"检测到多个激活的预设: {_activePreset.PresetName} 和 {preset.PresetName}，只允许一个预设激活！");
                    throw new InvalidOperationException($"只允许一个 SystemPreset 的 IsActive = true，但发现多个: {_activePreset.PresetName}, {preset.PresetName}");
                }

                _activePreset = preset;
            }
        }

        _isInitialized = true;
        _log.Info($"SystemPresetService 初始化完成，共加载 {_presets.Count} 个预设，激活预设: {_activePreset?.PresetName ?? "无"}");
    }

    /// <summary>
    /// 获取当前激活的预设。
    /// </summary>
    public static SystemPreset? GetActivePreset()
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
    public static IEnumerable<SystemPreset> GetAllPresets()
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
    /// <para>1. 收集 EnabledGroups 对应的所有系统</para>
    /// <para>2. 收集 EnabledTags 对应的所有系统</para>
    /// <para>3. 收集 EnabledSystemIds 列表</para>
    /// <para>4. 排除 DisabledSystemIds 列表</para>
    /// <para>5. Base 分组的系统强制加载（不受预设影响）</para>
    /// </summary>
    public static HashSet<string> CalculateEnabledSystems()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        var enabledSystems = new HashSet<string>(StringComparer.Ordinal);

        // 1. Base 分组强制加载
        var baseConfigs = SystemConfigService.GetBaseConfigs();
        foreach (var config in baseConfigs)
        {
            enabledSystems.Add(config.SystemId);
        }

        // 2. 如果没有激活预设，使用 DefaultAutoAdd
        if (_activePreset == null)
        {
            var autoAddConfigs = SystemConfigService.GetAutoAddConfigs();
            foreach (var config in autoAddConfigs)
            {
                enabledSystems.Add(config.SystemId);
            }

            return enabledSystems;
        }

        // 3. 应用激活预设的规则
        var preset = _activePreset;

        // 3.1 收集 EnabledGroups 对应的系统
        if (preset.EnabledGroups != SystemGroup.None)
        {
            var groupConfigs = SystemConfigService.GetConfigsByGroup(preset.EnabledGroups);
            foreach (var config in groupConfigs)
            {
                enabledSystems.Add(config.SystemId);
            }
        }

        // 3.2 收集 EnabledTags 对应的系统
        if (preset.EnabledTags != SystemTag.None)
        {
            var tagConfigs = SystemConfigService.GetConfigsByTag(preset.EnabledTags);
            foreach (var config in tagConfigs)
            {
                enabledSystems.Add(config.SystemId);
            }
        }

        // 3.3 收集 EnabledSystemIds 列表
        foreach (var systemId in preset.EnabledSystemIds)
        {
            if (!string.IsNullOrWhiteSpace(systemId))
            {
                enabledSystems.Add(systemId);
            }
        }

        // 3.4 排除 DisabledSystemIds 列表（优先级最高）
        foreach (var systemId in preset.DisabledSystemIds)
        {
            if (!string.IsNullOrWhiteSpace(systemId))
            {
                // Base 分组的系统不允许被禁用
                var config = SystemConfigService.GetConfig(systemId);
                if (config != null && (config.Groups & SystemGroup.Base) != 0)
                {
                    _log.Warn($"预设 {preset.PresetName} 尝试禁用 Base 分组系统 {systemId}，已忽略");
                    continue;
                }

                enabledSystems.Remove(systemId);
            }
        }

        return enabledSystems;
    }
}
