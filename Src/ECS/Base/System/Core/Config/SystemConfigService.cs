using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 系统配置服务。
/// <para>负责加载和解析 SystemConfig 资源，提供系统配置查询接口。</para>
/// <para>使用 ResourceManagement 统一加载资源。</para>
/// </summary>
public static class SystemConfigService
{
    private static readonly Log _log = new(nameof(SystemConfigService));
    private static readonly Dictionary<string, SystemConfig> _configs = new(StringComparer.Ordinal);
    private static bool _isInitialized;

    /// <summary>
    /// 初始化配置服务，加载所有 SystemConfig 资源。
    /// </summary>
    public static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _configs.Clear();

        // 使用 ResourceManagement 加载所有系统配置
        var configs = ResourceManagement.LoadAll<SystemConfig>(ResourceCategory.ConfigSystem);
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.SystemId))
            {
                _log.Warn($"配置的 SystemId 为空，跳过");
            }
            else if (_configs.ContainsKey(config.SystemId))
            {
                _log.Warn($"重复的 SystemId: {config.SystemId}");
            }
            else
            {
                _configs.Add(config.SystemId, config);
                _log.Debug($"加载系统配置: {config.SystemId}");
            }
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
    /// <param name="group">系统分组（支持 Flags 组合）。</param>
    public static IEnumerable<SystemConfig> GetConfigsByGroup(SystemGroup group)
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => (c.Groups & group) != 0);
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
    /// 获取所有 DefaultAutoAdd = true 的系统配置。
    /// </summary>
    public static IEnumerable<SystemConfig> GetAutoAddConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => c.DefaultAutoAdd);
    }

    /// <summary>
    /// 获取所有 Base 分组的系统配置（强制加载）。
    /// </summary>
    public static IEnumerable<SystemConfig> GetBaseConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => (c.Groups & SystemGroup.Base) != 0);
    }
}
