using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Slime.ConfigNew.Systems;

/// <summary>
/// 系统配置服务。
/// <para>负责加载和解析系统配置，提供系统配置查询接口。</para>
/// <para>优先使用 DataNew 纯 C# 数据，ResourceManagement 的 .tres 资源作为回退。</para>
/// </summary>
public static class SystemConfigService
{
    private static readonly Log _log = new(nameof(SystemConfigService));
    private static readonly Dictionary<string, SystemConfig> _configs = new(StringComparer.Ordinal);
    private static bool _isInitialized;

    /// <summary>
    /// 初始化配置服务，加载所有系统配置。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _configs.Clear();

        // DataNew 是优先数据源；同名 .tres 只作为兼容回退，不覆盖纯 C# 配置。
        foreach (var data in SystemConfigData.All)
        {
            if (data == null)
            {
                _log.Error("SystemConfigData.All 包含空配置项，请检查静态初始化顺序");
                continue;
            }

            TryAddConfig(data.ToResource(), data.SystemId, warnDuplicate: true);
        }

        // 使用 ResourceManagement 加载仍未迁移到 DataNew 的系统配置资源。
        var configs = ResourceManagement.LoadAll<SystemConfig>(ResourceCategory.ConfigSystem);
        foreach (var config in configs)
        {
            var resourceName = Path.GetFileNameWithoutExtension(config.ResourcePath);
            TryAddConfig(config, resourceName, warnDuplicate: false);
        }

        _isInitialized = true;
        _log.Info($"SystemConfigService 初始化完成，共加载 {_configs.Count} 个系统配置");
    }

    /// <summary>
    /// 获取指定系统的配置。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <returns>系统配置，未找到返回 null。</returns>
    public static SystemConfig? GetConfig(string systemId)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.TryGetValue(systemId, out var config) ? config : null;
    }

    /// <summary>
    /// 获取所有系统配置。
    /// </summary>
    public static IEnumerable<SystemConfig> GetAllConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values;
    }

    /// <summary>
    /// 获取指定分组的所有系统配置。
    /// </summary>
    /// <param name="group">系统挂载分组，单选。</param>
    public static IEnumerable<SystemConfig> GetConfigsByGroup(SystemGroup group)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => c.MountGroup == group);
    }

    /// <summary>
    /// 获取指定标签的所有系统配置。
    /// </summary>
    /// <param name="tag">系统标签（支持 Flags 组合）。</param>
    public static IEnumerable<SystemConfig> GetConfigsByTag(SystemTag tag)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => (c.Tags & tag) != 0);
    }

    /// <summary>
    /// 获取所有 AutoLoad = true 的系统配置。
    /// </summary>
    public static IEnumerable<SystemConfig> GetAutoLoadConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => c.AutoLoad);
    }

    /// <summary>
    /// 获取所有 Required = true 的系统配置（强制加载）。
    /// </summary>
    public static IEnumerable<SystemConfig> GetRequiredConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => c.Required);
    }

    private static void TryAddConfig(SystemConfig config, string sourceName, bool warnDuplicate)
    {
        if (string.IsNullOrWhiteSpace(config.SystemId))
        {
            _log.Warn("配置的 SystemId 为空，跳过");
            return;
        }

        if (_configs.ContainsKey(config.SystemId))
        {
            if (warnDuplicate)
            {
                _log.Warn($"重复的 SystemId: {config.SystemId}");
            }
            else
            {
                _log.Debug($"系统配置 {config.SystemId} 已由 DataNew 提供，跳过资源回退");
            }

            return;
        }

        if (!string.Equals(config.SystemId, sourceName, StringComparison.Ordinal))
        {
            _log.Warn($"系统配置来源名与 SystemId 不一致: source={sourceName}, SystemId={config.SystemId}");
        }

        _configs.Add(config.SystemId, config);
        _log.Debug($"加载系统配置: {config.SystemId}");
    }
}
