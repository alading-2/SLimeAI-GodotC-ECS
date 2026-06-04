using System;
using System.Collections.Generic;
using System.Linq;
using slime.data.Systems;

/// <summary>
/// SystemManager diagnostics 合同输出。
/// </summary>
public partial class SystemManager
{
    /// <summary>
    /// 构建当前 System Core diagnostics 快照。
    /// </summary>
    public SystemDiagnosticsSnapshot GetDiagnosticsSnapshot(SystemPreflightOptions? preflightOptions = null)
    {
        SystemConfigService.Initialize();
        SystemPresetService.Initialize();

        var configs = SystemConfigService.GetAllConfigs()
            .OrderBy(static config => config.Priority)
            .ThenBy(static config => config.SystemId, StringComparer.Ordinal)
            .ToList();
        var descriptors = SystemRegistry.GetDescriptorValues()
            .OrderBy(static descriptor => descriptor.SystemId, StringComparer.Ordinal)
            .ToList();
        var descriptorIds = descriptors.Select(static descriptor => descriptor.SystemId);
        var allSystemIds = configs.Select(static config => config.SystemId)
            .Concat(descriptorIds)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(systemId => GetPriority(systemId, configs))
            .ThenBy(static systemId => systemId, StringComparer.Ordinal)
            .ToList();

        var entries = new List<SystemDiagnosticsEntry>(allSystemIds.Count);
        foreach (var systemId in allSystemIds)
        {
            entries.Add(BuildDiagnosticsEntry(systemId));
        }

        var preflight = SystemPreflight.Run(preflightOptions);
        return new SystemDiagnosticsSnapshot
        {
            ProjectState = SystemProjectStateDiagnostics.From(ProjectState.Snapshot),
            ActivePreset = SystemPresetService.GetActivePreset()?.PresetName ?? string.Empty,
            ConfigCount = configs.Count,
            RegisteredDescriptorCount = descriptors.Count,
            LoadedCount = entries.Count(static entry => entry.Loaded),
            RunningCount = entries.Count(static entry => entry.Running),
            BlockedCount = entries.Count(static entry => entry.Loaded && entry.Enabled && !entry.StateAllowed),
            DisabledCount = entries.Count(static entry => entry.Loaded && !entry.Enabled),
            TraceCount = _lifecycleTrace.Count,
            Entries = entries,
            PreflightIssues = preflight.Issues,
            RecentTrace = _lifecycleTrace.GetEntries()
        };
    }

    private SystemDiagnosticsEntry BuildDiagnosticsEntry(string systemId)
    {
        var config = SystemConfigService.GetConfig(systemId);
        var descriptor = SystemRegistry.GetDescriptor(systemId);
        _entries.TryGetValue(systemId, out var managedEntry);

        var route = SystemDiagnosticsMetadata.Resolve(systemId);
        var customStats = new List<SystemDiagnosticsStat>();
        if (managedEntry?.Instance is ISystem system)
        {
            var runtimeInfo = system.GetSystemRuntimeInfo();
            if (runtimeInfo?.CustomStats != null)
            {
                customStats.AddRange(runtimeInfo.CustomStats.Select(SystemDiagnosticsStat.From));
            }
        }

        var blockedReasonCode = ResolveBlockedReasonCode(config, descriptor, managedEntry);
        return new SystemDiagnosticsEntry
        {
            SystemId = systemId,
            Owner = route.Owner,
            SourcePath = route.SourcePath,
            ConfigRecordId = route.ConfigRecordId,
            Registered = descriptor != null,
            Configured = config != null,
            Loaded = managedEntry != null,
            Enabled = managedEntry?.IsEnabled ?? false,
            StateAllowed = managedEntry?.IsStateAllowed ?? false,
            Running = managedEntry?.IsRunning ?? false,
            BlockedReasonCode = blockedReasonCode.ToString(),
            BlockedReason = ResolveBlockedReasonMessage(systemId, config, descriptor, managedEntry, blockedReasonCode),
            MountGroup = config?.MountGroup.ToString() ?? string.Empty,
            Tags = config?.Tags.ToString() ?? string.Empty,
            Required = config?.Required ?? false,
            AutoLoad = config?.AutoLoad ?? false,
            StartEnabled = config?.StartEnabled ?? false,
            Priority = config?.Priority ?? 0,
            AllowedFlowStates = config?.AllowedFlowStates.ToString() ?? string.Empty,
            RequiredOverlays = config?.RequiredOverlays.ToString() ?? string.Empty,
            BlockedOverlays = config?.BlockedOverlays.ToString() ?? string.Empty,
            AllowedSimulationStates = config?.AllowedSimulationStates.ToString() ?? string.Empty,
            Dependencies = config?.Dependencies ?? Array.Empty<string>(),
            CustomStats = customStats,
            Description = config?.Description ?? string.Empty
        };
    }

    private static int GetPriority(string systemId, IReadOnlyList<SystemData> configs)
    {
        foreach (var config in configs)
        {
            if (string.Equals(config.SystemId, systemId, StringComparison.Ordinal))
            {
                return config.Priority;
            }
        }

        return int.MaxValue;
    }

    private static SystemBlockedReasonCode ResolveBlockedReasonCode(
        SystemData? config,
        SystemDescriptor? descriptor,
        ManagedSystemEntry? entry)
    {
        if (config == null)
        {
            return SystemBlockedReasonCode.MissingConfig;
        }

        if (descriptor == null)
        {
            return SystemBlockedReasonCode.NotRegistered;
        }

        if (entry == null)
        {
            return SystemBlockedReasonCode.NotLoaded;
        }

        if (!entry.IsEnabled)
        {
            return SystemBlockedReasonCode.Disabled;
        }

        if (!entry.IsStateAllowed)
        {
            return entry.BlockedReasonCode == SystemBlockedReasonCode.None
                ? SystemBlockedReasonCode.Unknown
                : entry.BlockedReasonCode;
        }

        if (!entry.IsRunning)
        {
            return SystemBlockedReasonCode.NotRunning;
        }

        return SystemBlockedReasonCode.None;
    }

    private static string ResolveBlockedReasonMessage(
        string systemId,
        SystemData? config,
        SystemDescriptor? descriptor,
        ManagedSystemEntry? entry,
        SystemBlockedReasonCode reasonCode)
    {
        return reasonCode switch
        {
            SystemBlockedReasonCode.None => string.Empty,
            SystemBlockedReasonCode.MissingConfig => $"系统 {systemId} 缺少配置",
            SystemBlockedReasonCode.NotRegistered => $"系统 {systemId} 未注册",
            SystemBlockedReasonCode.NotLoaded => $"系统 {systemId} 未加载",
            SystemBlockedReasonCode.Disabled => $"系统 {systemId} 已禁用",
            SystemBlockedReasonCode.NotRunning => $"系统 {systemId} 尚未进入运行态",
            _ => !string.IsNullOrEmpty(entry?.BlockedReason)
                ? entry.BlockedReason
                : $"系统 {systemId} 被 {reasonCode} 阻断"
        };
    }
}
