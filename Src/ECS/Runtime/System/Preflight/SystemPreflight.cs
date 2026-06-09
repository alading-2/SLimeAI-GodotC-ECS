using System;
using System.Collections.Generic;
using System.Linq;
using slime.data.Systems;

/// <summary>
/// System Core 启动前检查。
/// <para>用于把 DataOS system.config、system.preset 与 SystemRegistry 对齐成可验证 gate。</para>
/// </summary>
public static class SystemPreflight
{
    public static SystemPreflightReport Run(SystemPreflightOptions? options = null)
    {
        using var trace = Log.BeginTrace("System", nameof(SystemPreflight), "SystemPreflight", "Preflight");
        options ??= SystemPreflightOptions.Default();
        SystemConfigService.Initialize();
        SystemPresetService.Initialize();

        var configs = SystemConfigService.GetAllConfigs()
            .OrderBy(static config => config.Priority)
            .ThenBy(static config => config.SystemId, StringComparer.Ordinal)
            .ToList();
        var configById = configs.ToDictionary(static config => config.SystemId, StringComparer.Ordinal);
        var descriptors = SystemRegistry.GetDescriptorValues()
            .OrderBy(static descriptor => descriptor.SystemId, StringComparer.Ordinal)
            .ToList();
        var descriptorById = descriptors.ToDictionary(static descriptor => descriptor.SystemId, StringComparer.Ordinal);
        var activePreset = SystemPresetService.GetActivePreset();

        var issues = new List<SystemPreflightIssue>();
        CheckConfigBasics(configs, issues);
        CheckRequiredDescriptors(configs, descriptorById, issues);
        CheckActivePreset(activePreset, configById, issues);
        CheckDependencies(configs, configById, descriptorById, issues);
        CheckDependencyCycles(configs, configById, issues);
        CheckDescriptorOnly(descriptors, configById, options, issues);
        CheckPriorityCollisions(configs, issues);

        var report = new SystemPreflightReport
        {
            ConfigCount = configs.Count,
            RegisteredDescriptorCount = descriptors.Count,
            ActivePresetName = activePreset?.PresetName ?? string.Empty,
            Issues = issues
        };
        trace.Complete(report.HasErrors ? LogOutcome.Failed : LogOutcome.Completed, "System preflight completed", new LogFields
        {
            ["configCount"] = report.ConfigCount,
            ["registeredDescriptorCount"] = report.RegisteredDescriptorCount,
            ["activePresetName"] = report.ActivePresetName,
            ["errorCount"] = report.ErrorCount,
            ["warningCount"] = report.WarningCount
        });
        return report;
    }

    private static void CheckConfigBasics(
        IEnumerable<SystemData> configs,
        List<SystemPreflightIssue> issues)
    {
        foreach (var config in configs)
        {
            if (string.IsNullOrWhiteSpace(config.SystemId))
            {
                issues.Add(Error("SYS-PF-001", string.Empty, "system.config 的 SystemId 为空"));
            }

            if (string.IsNullOrWhiteSpace(config.Description))
            {
                issues.Add(Error("SYS-PF-001", config.SystemId, "system.config 缺少 Description"));
            }
        }
    }

    private static void CheckRequiredDescriptors(
        IEnumerable<SystemData> configs,
        IReadOnlyDictionary<string, SystemDescriptor> descriptorById,
        List<SystemPreflightIssue> issues)
    {
        foreach (var config in configs)
        {
            if (config.Required && !descriptorById.ContainsKey(config.SystemId))
            {
                issues.Add(Error("SYS-PF-002", config.SystemId, "Required system 缺少 SystemRegistry descriptor"));
            }
        }
    }

    private static void CheckActivePreset(
        SystemPresetData? activePreset,
        IReadOnlyDictionary<string, SystemData> configById,
        List<SystemPreflightIssue> issues)
    {
        if (activePreset == null)
        {
            return;
        }

        foreach (var systemId in activePreset.EnabledSystemIds)
        {
            if (!string.IsNullOrWhiteSpace(systemId) && !configById.ContainsKey(systemId))
            {
                issues.Add(Error("SYS-PF-003", systemId, $"active preset {activePreset.PresetName} 引用了不存在的 EnabledSystemId"));
            }
        }

        foreach (var systemId in activePreset.DisabledSystemIds)
        {
            if (string.IsNullOrWhiteSpace(systemId) || !configById.TryGetValue(systemId, out var config))
            {
                continue;
            }

            if (config.Required)
            {
                issues.Add(Error("SYS-PF-004", systemId, $"active preset {activePreset.PresetName} 不能禁用 Required system"));
            }
        }
    }

    private static void CheckDependencies(
        IEnumerable<SystemData> configs,
        IReadOnlyDictionary<string, SystemData> configById,
        IReadOnlyDictionary<string, SystemDescriptor> descriptorById,
        List<SystemPreflightIssue> issues)
    {
        foreach (var config in configs)
        {
            foreach (var dependency in config.Dependencies)
            {
                if (string.IsNullOrWhiteSpace(dependency))
                {
                    continue;
                }

                if (!configById.ContainsKey(dependency))
                {
                    issues.Add(Error("SYS-PF-005", config.SystemId, $"依赖系统 {dependency} 缺少 system.config"));
                }

                if (!descriptorById.ContainsKey(dependency))
                {
                    issues.Add(Error("SYS-PF-005", config.SystemId, $"依赖系统 {dependency} 缺少 SystemRegistry descriptor"));
                }
            }
        }
    }

    private static void CheckDependencyCycles(
        IReadOnlyList<SystemData> configs,
        IReadOnlyDictionary<string, SystemData> configById,
        List<SystemPreflightIssue> issues)
    {
        var state = new Dictionary<string, VisitState>(StringComparer.Ordinal);
        var stack = new Stack<string>();

        foreach (var config in configs)
        {
            Visit(config.SystemId, configById, state, stack, issues);
        }
    }

    private static void Visit(
        string systemId,
        IReadOnlyDictionary<string, SystemData> configById,
        Dictionary<string, VisitState> state,
        Stack<string> stack,
        List<SystemPreflightIssue> issues)
    {
        if (!configById.TryGetValue(systemId, out var config))
        {
            return;
        }

        if (state.TryGetValue(systemId, out var currentState))
        {
            if (currentState == VisitState.Visiting)
            {
                var cycle = string.Join(" -> ", stack.Reverse().Concat(new[] { systemId }));
                issues.Add(Error("SYS-PF-006", systemId, $"检测到 system dependency cycle: {cycle}"));
            }

            return;
        }

        state[systemId] = VisitState.Visiting;
        stack.Push(systemId);
        foreach (var dependency in config.Dependencies)
        {
            if (!string.IsNullOrWhiteSpace(dependency))
            {
                Visit(dependency, configById, state, stack, issues);
            }
        }

        stack.Pop();
        state[systemId] = VisitState.Visited;
    }

    private static void CheckDescriptorOnly(
        IEnumerable<SystemDescriptor> descriptors,
        IReadOnlyDictionary<string, SystemData> configById,
        SystemPreflightOptions options,
        List<SystemPreflightIssue> issues)
    {
        foreach (var descriptor in descriptors)
        {
            if (configById.ContainsKey(descriptor.SystemId) || IsAllowedDescriptorOnly(descriptor.SystemId, options))
            {
                continue;
            }

            var severity = options.TreatDescriptorOnlyAsError
                ? SystemPreflightSeverity.Error
                : SystemPreflightSeverity.Warning;
            issues.Add(new SystemPreflightIssue(
                "SYS-PF-007",
                severity,
                descriptor.SystemId,
                "SystemRegistry descriptor 缺少 system.config；测试专用 descriptor 必须进入 allow-list"));
        }
    }

    private static void CheckPriorityCollisions(
        IEnumerable<SystemData> configs,
        List<SystemPreflightIssue> issues)
    {
        foreach (var group in configs.GroupBy(static config => config.Priority))
        {
            var items = group.OrderBy(static config => config.SystemId, StringComparer.Ordinal).ToList();
            if (items.Count <= 1)
            {
                continue;
            }

            issues.Add(new SystemPreflightIssue(
                "SYS-PF-010",
                SystemPreflightSeverity.Warning,
                string.Join(",", items.Select(static config => config.SystemId)),
                $"多个 system.config 共用 Priority={group.Key}；运行时将用 SystemId 作为稳定排序补充"));
        }
    }

    private static bool IsAllowedDescriptorOnly(string systemId, SystemPreflightOptions options)
    {
        if (options.AllowedDescriptorOnlySystemIds.Contains(systemId))
        {
            return true;
        }

        foreach (var prefix in options.AllowedDescriptorOnlySystemIdPrefixes)
        {
            if (!string.IsNullOrEmpty(prefix) && systemId.StartsWith(prefix, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static SystemPreflightIssue Error(string ruleId, string systemId, string message)
    {
        return new SystemPreflightIssue(ruleId, SystemPreflightSeverity.Error, systemId, message);
    }

    private enum VisitState
    {
        Visiting,
        Visited
    }
}
