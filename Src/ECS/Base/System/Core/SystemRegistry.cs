using System;
using System.Collections.Generic;

/// <summary>
/// 系统注册表。
/// <para>进程级全局注册表，保存系统静态描述符（SystemId + Factory）。</para>
/// <para>其他元数据从 runtime snapshot 的 system.config 记录读取。</para>
/// </summary>
public static class SystemRegistry
{
    private static readonly Log _log = new(nameof(SystemRegistry));

    // 使用 Ordinal 避免系统 Id 在不同区域设置下出现比较差异。
    private static readonly Dictionary<string, SystemDescriptor> _descriptors = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册系统（简化签名：只传 SystemId + Factory）。
    /// </summary>
    /// <param name="systemId">系统唯一 Id（必须与 snapshot system.config 记录和资源文件名一致）。</param>
    /// <param name="factory">系统实例工厂。</param>
    public static void Register(string systemId, Func<object> factory)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            _log.Error("SystemId 不能为空");
            return;
        }

        if (factory == null)
        {
            _log.Error($"系统 '{systemId}' 的 Factory 不能为空");
            return;
        }

        if (_descriptors.ContainsKey(systemId))
        {
            _log.Error($"系统 '{systemId}' 重复注册，保留首次注册的描述符");
            return;
        }

        var descriptor = new SystemDescriptor(systemId, factory);
        _descriptors.Add(systemId, descriptor);
    }

    /// <summary>
    /// 获取全部系统描述符。
    /// </summary>
    /// <returns>按注册顺序返回的描述符集合。</returns>
    public static IReadOnlyCollection<SystemDescriptor> GetDescriptorValues()
    {
        return _descriptors.Values;
    }

    /// <summary>
    /// 查找指定系统描述符。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <returns>找到则返回描述符。</returns>
    public static SystemDescriptor? GetDescriptor(string systemId)
    {
        _descriptors.TryGetValue(systemId, out var descriptor);
        return descriptor;
    }

    /// <summary>
    /// 获取指定分组的所有系统描述符。
    /// </summary>
    /// <param name="group">系统挂载分组，单选。</param>
    public static IEnumerable<SystemDescriptor> GetDescriptorsByGroup(SystemGroup group)
    {
        var configs = SystemConfigService.GetConfigsByGroup(group);
        foreach (var config in configs)
        {
            if (_descriptors.TryGetValue(config.SystemId, out var descriptor))
            {
                yield return descriptor;
            }
        }
    }

    /// <summary>
    /// 获取指定标签的所有系统描述符。
    /// </summary>
    /// <param name="tag">系统标签（支持 Flags 组合）。</param>
    public static IEnumerable<SystemDescriptor> GetDescriptorsByTag(SystemTag tag)
    {
        var configs = SystemConfigService.GetConfigsByTag(tag);
        foreach (var config in configs)
        {
            if (_descriptors.TryGetValue(config.SystemId, out var descriptor))
            {
                yield return descriptor;
            }
        }
    }

    /// <summary>
    /// 清空注册表。
    /// </summary>
    public static void Reset()
    {
        // 主要用于测试场景复位，生产流程不建议随意清空。
        _descriptors.Clear();
    }
}
