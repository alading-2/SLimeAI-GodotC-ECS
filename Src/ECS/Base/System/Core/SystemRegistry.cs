using System;
using System.Collections.Generic;

/// <summary>
/// 系统注册表。
/// <para>进程级全局注册表，保存系统静态描述符。</para>
/// </summary>
public static class SystemRegistry
{
    private static readonly Log _log = new(nameof(SystemRegistry));

    // 使用 Ordinal 避免系统 Id 在不同区域设置下出现比较差异。
    private static readonly Dictionary<string, SystemDescriptor> _descriptors = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册系统描述符。
    /// </summary>
    /// <param name="descriptor">系统描述符。</param>
    public static void Register(SystemDescriptor descriptor)
    {
        if (descriptor == null)
        {
            _log.Error("忽略空系统描述符注册请求");
            return;
        }

        if (_descriptors.ContainsKey(descriptor.SystemId))
        {
            _log.Error($"系统 '{descriptor.SystemId}' 重复注册，保留首次注册的描述符");
            return;
        }

        // 注册顺序会影响 Bootstrap 接管顺序。
        _descriptors.Add(descriptor.SystemId, descriptor);
    }

    /// <summary>
    /// 获取全部系统描述符。
    /// </summary>
    /// <returns>按注册顺序返回的描述符集合。</returns>
    public static IReadOnlyCollection<SystemDescriptor> GetDescriptorValues()
    {
        // 直接返回 Values 只读视图，调用方不应持久化并依赖可变顺序行为。
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
    /// 清空注册表。
    /// </summary>
    public static void Reset()
    {
        // 主要用于测试场景复位，生产流程不建议随意清空。
        _descriptors.Clear();
    }
}
