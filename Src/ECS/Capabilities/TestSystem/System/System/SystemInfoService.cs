using System;
using System.Collections.Generic;
using ECS.Base.System.TestSystem.Core;
using slime.data.Systems;

/// <summary>
/// TestSystem 系统信息服务。
/// <para>合并 snapshot system.config 投影、SystemRegistry 与 SystemManager 运行态，供系统监控模块展示和操作。</para>
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
        if (manager != null)
        {
            var diagnostics = manager.GetDiagnosticsSnapshot();
            foreach (var entry in diagnostics.Entries)
            {
                var dependentSystemId = FindLoadedDependentSystem(entry.SystemId, diagnostics.Entries);
                snapshots.Add(BuildSnapshot(entry, dependentSystemId));
            }

            snapshots.Sort(CompareSnapshots);
            return snapshots;
        }

        foreach (var config in SystemConfigService.GetAllConfigs())
        {
            var descriptor = SystemRegistry.GetDescriptor(config.SystemId);
            snapshots.Add(BuildSnapshot(config, null, descriptor != null, string.Empty));
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
            BlockedReasonCode = runtimeInfo?.BlockedReasonCode.ToString() ?? SystemBlockedReasonCode.None.ToString(),
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

    private static SystemInfoSnapshot BuildSnapshot(
        SystemDiagnosticsEntry entry,
        string dependentSystemId)
    {
        var status = ResolveStatus(entry);
        return new SystemInfoSnapshot
        {
            SystemId = entry.SystemId,
            IsRegistered = entry.Registered,
            IsLoaded = entry.Loaded,
            IsEnabled = entry.Enabled,
            IsRunning = entry.Running,
            IsStateAllowed = entry.StateAllowed,
            BlockedReasonCode = entry.BlockedReasonCode,
            BlockedReason = entry.BlockedReason,
            MountGroup = Enum.TryParse<SystemGroup>(entry.MountGroup, out var group) ? group : SystemGroup.Else,
            Tags = Enum.TryParse<SystemTag>(entry.Tags, out var tags) ? tags : SystemTag.None,
            Required = entry.Required,
            AutoLoad = entry.AutoLoad,
            StartEnabled = entry.StartEnabled,
            Priority = entry.Priority,
            Dependencies = entry.Dependencies,
            Description = entry.Description,
            CustomStats = entry.CustomStats.ConvertAll(static stat => new SystemStat
            {
                Category = stat.Category,
                Name = stat.Name,
                Value = stat.Value
            }),
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

    private static SystemInfoStatus ResolveStatus(SystemDiagnosticsEntry entry)
    {
        if (!entry.Loaded)
        {
            return SystemInfoStatus.NotLoaded;
        }

        if (!entry.Enabled)
        {
            return SystemInfoStatus.Disabled;
        }

        if (!entry.StateAllowed)
        {
            return SystemInfoStatus.Blocked;
        }

        return entry.Running
            ? SystemInfoStatus.Running
            : SystemInfoStatus.Loaded;
    }

    private static string FindLoadedDependentSystem(string targetSystemId, List<SystemDiagnosticsEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (!entry.Loaded || string.Equals(entry.SystemId, targetSystemId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var dependency in entry.Dependencies)
            {
                if (string.Equals(dependency, targetSystemId, StringComparison.Ordinal))
                {
                    return entry.SystemId;
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
    public string BlockedReasonCode { get; init; } = SystemBlockedReasonCode.None.ToString();
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
