using System;
using System.Collections.Generic;
using ECS.Base.System.TestSystem.Core;
using slime.data.Systems;

/// <summary>
/// TestSystem 系统信息服务。
/// <para>合并 DataNew SystemData、SystemRegistry 与 SystemManager 运行态，供系统监控模块展示和操作。</para>
/// </summary>
internal sealed class SystemInfoService
{
    /// <summary>
    /// 获取当前全部系统配置对应的展示快照。
    /// </summary>
    /// <param name="manager">当前系统管理器实例。</param>
    public List<SystemInfoSnapshot> GetSnapshots(SystemManager? manager)
    {
        var snapshots = new List<SystemInfoSnapshot>();
        var loadedRuntimeInfos = manager?.GetAllSystems() ?? new List<SystemRuntimeInfo>();

        foreach (var config in SystemConfigService.GetAllConfigs())
        {
            var runtimeInfo = manager?.GetSystemRuntimeInfo(config.SystemId);
            var descriptor = SystemRegistry.GetDescriptor(config.SystemId);
            var dependentSystemId = FindLoadedDependentSystem(config.SystemId, loadedRuntimeInfos);
            snapshots.Add(BuildSnapshot(config, runtimeInfo, descriptor != null, dependentSystemId));
        }

        snapshots.Sort(CompareSnapshots);
        return snapshots;
    }

    /// <summary>
    /// 添加系统。
    /// </summary>
    /// <param name="manager">当前系统管理器实例。</param>
    /// <param name="systemId">系统 Id。</param>
    public TestActionResult AddSystem(SystemManager? manager, string systemId)
    {
        if (manager == null)
        {
            return new TestActionResult(false, "SystemManager 不存在，无法添加系统");
        }

        var success = manager.TryAddSystem(
            systemId, // 目标系统 Id
            out var message);
        return new TestActionResult(success, message);
    }

    /// <summary>
    /// 移除系统。
    /// </summary>
    /// <param name="manager">当前系统管理器实例。</param>
    /// <param name="systemId">系统 Id。</param>
    public TestActionResult RemoveSystem(SystemManager? manager, string systemId)
    {
        if (manager == null)
        {
            return new TestActionResult(false, "SystemManager 不存在，无法移除系统");
        }

        var success = manager.TryRemoveSystem(
            systemId, // 目标系统 Id
            out var message);
        return new TestActionResult(success, message);
    }

    /// <summary>
    /// 设置系统启用状态。
    /// </summary>
    /// <param name="manager">当前系统管理器实例。</param>
    /// <param name="systemId">系统 Id。</param>
    /// <param name="enabled">目标启用状态。</param>
    public TestActionResult SetSystemEnabled(SystemManager? manager, string systemId, bool enabled)
    {
        if (manager == null)
        {
            return new TestActionResult(false, "SystemManager 不存在，无法切换系统启用状态");
        }

        var success = manager.TrySetSystemEnabled(
            systemId, // 目标系统 Id
            enabled, // 目标启用状态
            out var message);
        return new TestActionResult(success, message);
    }

    private static SystemInfoSnapshot BuildSnapshot(
        SystemData config,
        SystemRuntimeInfo? runtimeInfo,
        bool isRegistered,
        string dependentSystemId)
    {
        var status = ResolveStatus(runtimeInfo);
        return new SystemInfoSnapshot
        {
            SystemId = config.SystemId,
            IsRegistered = isRegistered,
            IsLoaded = runtimeInfo != null,
            IsEnabled = runtimeInfo?.IsEnabled ?? false,
            IsRunning = runtimeInfo?.IsRunning ?? false,
            IsStateAllowed = runtimeInfo?.IsStateAllowed ?? false,
            BlockedReason = runtimeInfo?.BlockedReason ?? string.Empty,
            MountGroup = config.MountGroup,
            Tags = config.Tags,
            Required = config.Required,
            AutoLoad = config.AutoLoad,
            StartEnabled = config.StartEnabled,
            Priority = config.Priority,
            Dependencies = config.Dependencies,
            Description = config.Description,
            CustomStats = runtimeInfo?.CustomStats ?? new List<SystemStat>(),
            Status = status,
            DependentSystemId = dependentSystemId
        };
    }

    private static SystemInfoStatus ResolveStatus(SystemRuntimeInfo? runtimeInfo)
    {
        if (runtimeInfo == null)
        {
            return SystemInfoStatus.NotLoaded;
        }

        if (!runtimeInfo.IsEnabled)
        {
            return SystemInfoStatus.Disabled;
        }

        if (!runtimeInfo.IsStateAllowed)
        {
            return SystemInfoStatus.Blocked;
        }

        return runtimeInfo.IsRunning
            ? SystemInfoStatus.Running
            : SystemInfoStatus.Loaded;
    }

    private static string FindLoadedDependentSystem(string targetSystemId, List<SystemRuntimeInfo> loadedRuntimeInfos)
    {
        foreach (var runtimeInfo in loadedRuntimeInfos)
        {
            if (string.Equals(runtimeInfo.SystemId, targetSystemId, StringComparison.Ordinal))
            {
                continue;
            }

            var config = SystemConfigService.GetConfig(runtimeInfo.SystemId);
            if (config == null)
            {
                continue;
            }

            foreach (var dependency in config.Dependencies)
            {
                if (string.Equals(dependency, targetSystemId, StringComparison.Ordinal))
                {
                    return runtimeInfo.SystemId;
                }
            }
        }

        return string.Empty;
    }

    private static int CompareSnapshots(SystemInfoSnapshot left, SystemInfoSnapshot right)
    {
        var priorityCompare = left.Priority.CompareTo(right.Priority);
        return priorityCompare != 0
            ? priorityCompare
            : string.Compare(left.SystemId, right.SystemId, StringComparison.Ordinal);
    }
}

/// <summary>
/// 系统监控模块展示状态。
/// </summary>
internal enum SystemInfoStatus
{
    NotLoaded,
    Loaded,
    Disabled,
    Blocked,
    Running
}

/// <summary>
/// 系统监控模块展示快照。
/// </summary>
internal sealed class SystemInfoSnapshot
{
    public string SystemId { get; init; } = string.Empty;
    public bool IsRegistered { get; init; }
    public bool IsLoaded { get; init; }
    public bool IsEnabled { get; init; }
    public bool IsRunning { get; init; }
    public bool IsStateAllowed { get; init; }
    public string BlockedReason { get; init; } = string.Empty;
    public SystemGroup MountGroup { get; init; }
    public SystemTag Tags { get; init; }
    public bool Required { get; init; }
    public bool AutoLoad { get; init; }
    public bool StartEnabled { get; init; }
    public int Priority { get; init; }
    public string[] Dependencies { get; init; } = Array.Empty<string>();
    public string Description { get; init; } = string.Empty;
    public List<SystemStat> CustomStats { get; init; } = new();
    public SystemInfoStatus Status { get; init; }
    public string DependentSystemId { get; init; } = string.Empty;

    public bool CanAdd => IsRegistered && !IsLoaded;
    public bool CanEnable => IsLoaded && !IsEnabled;
    public bool CanDisable => IsLoaded && IsEnabled && !Required;
    public bool CanRemove => IsLoaded && !Required && string.IsNullOrEmpty(DependentSystemId);
}
