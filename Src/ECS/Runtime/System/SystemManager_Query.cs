using System.Collections.Generic;
using System.Linq;

/// <summary>
/// SystemManager 查询接口扩展。
/// <para>提供系统信息查询、分组筛选、标签筛选等功能。</para>
/// </summary>
public partial class SystemManager
{
    /// <summary>
    /// 获取所有已加载的系统信息。
    /// </summary>
    /// <returns>所有系统的运行时信息列表。</returns>
    public List<SystemRuntimeInfo> GetAllSystems()
    {
        var result = new List<SystemRuntimeInfo>();

        foreach (var entry in _entries.Values)
        {
            var info = GetSystemRuntimeInfoFromEntry(entry);
            if (info != null)
            {
                result.Add(info);
            }
        }

        return result;
    }

    /// <summary>
    /// 按分组查询系统。
    /// </summary>
    /// <param name="group">系统分组（单个分组，不支持组合）。</param>
    /// <returns>匹配该分组的系统信息列表。</returns>
    public List<SystemRuntimeInfo> GetSystemsByGroup(SystemGroup group)
    {
        var result = new List<SystemRuntimeInfo>();

        foreach (var entry in _entries.Values)
        {
            if (entry.Config.MountGroup == group)
            {
                var info = GetSystemRuntimeInfoFromEntry(entry);
                if (info != null)
                {
                    result.Add(info);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 按标签查询系统。
    /// </summary>
    /// <param name="tag">系统标签（单个标签，不支持组合）。</param>
    /// <returns>匹配该标签的系统信息列表。</returns>
    public List<SystemRuntimeInfo> GetSystemsByTag(SystemTag tag)
    {
        var result = new List<SystemRuntimeInfo>();

        foreach (var entry in _entries.Values)
        {
            // 检查系统的 Tags 是否包含指定标签
            if ((entry.Config.Tags & tag) != 0)
            {
                var info = GetSystemRuntimeInfoFromEntry(entry);
                if (info != null)
                {
                    result.Add(info);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取指定系统的运行时信息。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <returns>系统运行时信息，未找到返回 null。</returns>
    public SystemRuntimeInfo? GetSystemRuntimeInfo(string systemId)
    {
        if (!_entries.TryGetValue(systemId, out var entry))
        {
            return null;
        }

        return GetSystemRuntimeInfoFromEntry(entry);
    }

    /// <summary>
    /// 从 ManagedSystemEntry 构建 SystemRuntimeInfo。
    /// </summary>
    private SystemRuntimeInfo GetSystemRuntimeInfoFromEntry(ManagedSystemEntry entry)
    {
        // 基础信息从 entry 和 config 读取
        var info = new SystemRuntimeInfo
        {
            SystemId = entry.Descriptor.SystemId,
            IsAdded = true,
            IsEnabled = entry.IsEnabled,
            IsRunning = entry.IsRunning,
            IsStateAllowed = entry.IsStateAllowed,
            BlockedReason = entry.BlockedReason,
            BlockedReasonCode = entry.BlockedReasonCode,
            MountGroup = entry.Config.MountGroup,
            Tags = entry.Config.Tags,
            CustomStats = new List<SystemStat>()
        };

        // 如果系统实现了 ISystem 接口，调用其 GetSystemRuntimeInfo 方法获取自定义统计
        if (entry.Instance is ISystem system)
        {
            var customInfo = system.GetSystemRuntimeInfo();
            if (customInfo?.CustomStats != null)
            {
                info.CustomStats = customInfo.CustomStats;
            }
        }

        return info;
    }
}
