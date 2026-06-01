using System;
using System.Collections.Generic;
using System.Linq;
using slime.data.Systems;

/// <summary>
/// 系统配置服务。
/// <para>负责加载和解析系统配置，提供系统配置查询接口。</para>
/// <para>只从 runtime snapshot 的 system.config records 读取配置。</para>
/// </summary>
public static class SystemConfigService
{
    private static readonly Log _log = new(nameof(SystemConfigService));
    private static readonly Dictionary<string, SystemData> _configs = new(StringComparer.Ordinal);
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

        var query = new RuntimeDataRecordQuery(DataRuntimeBootstrap.Default);
        foreach (var record in query.GetRecords("system.config"))
        {
            var definition = RuntimeDataRecordProjection.ToSystemConfigDefinition(record);
            var data = ToSystemData(definition);
            TryAddConfig(data, data.SystemId, warnDuplicate: true);
        }

        _isInitialized = true;
        _log.Info($"SystemConfigService 初始化完成，共加载 {_configs.Count} 个系统配置");
    }

    /// <summary>
    /// 获取指定系统的配置。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <returns>系统配置，未找到返回 null。</returns>
    public static SystemData? GetConfig(string systemId)
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
    public static IEnumerable<SystemData> GetAllConfigs()
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
    public static IEnumerable<SystemData> GetConfigsByGroup(SystemGroup group)
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
    public static IEnumerable<SystemData> GetConfigsByTag(SystemTag tag)
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
    public static IEnumerable<SystemData> GetAutoLoadConfigs()
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
    public static IEnumerable<SystemData> GetRequiredConfigs()
    {
        if (!_isInitialized)
        {
            Initialize();
        }

        return _configs.Values.Where(c => c.Required);
    }

    private static void TryAddConfig(SystemData config, string sourceName, bool warnDuplicate)
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
                _log.Debug($"系统配置 {config.SystemId} 已由 runtime snapshot 提供，跳过重复项");
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

    private static SystemData ToSystemData(SystemConfigDefinition definition)
    {
        return new SystemData
        {
            SystemId = definition.SystemId,
            MountGroup = definition.MountGroup,
            Tags = definition.Tags,
            Required = definition.Required,
            AutoLoad = definition.AutoLoad,
            StartEnabled = definition.StartEnabled,
            Priority = definition.Priority,
            AllowedFlowStates = definition.AllowedFlowStates,
            RequiredOverlays = definition.RequiredOverlays,
            BlockedOverlays = definition.BlockedOverlays,
            AllowedSimulationStates = definition.AllowedSimulationStates,
            Dependencies = definition.Dependencies,
            Description = definition.Description
        };
    }
}
