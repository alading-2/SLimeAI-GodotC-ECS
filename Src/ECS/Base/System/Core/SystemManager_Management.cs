using Godot;
using System;

/// <summary>
/// SystemManager 运行时管理接口。
/// <para>面向 TestSystem 等调试工具，提供受保护的添加、移除、启用和禁用入口。</para>
/// </summary>
public partial class SystemManager
{
    /// <summary>
    /// 按 SystemId 运行时添加系统。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <param name="message">操作结果中文说明。</param>
    public bool TryAddSystem(string systemId, out string message)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            message = "系统 Id 为空，无法添加";
            return false;
        }

        if (_entries.ContainsKey(systemId))
        {
            message = $"系统 {systemId} 已加载";
            return true;
        }

        var descriptor = SystemRegistry.GetDescriptor(systemId);
        if (descriptor == null)
        {
            message = $"系统 {systemId} 未注册，无法添加";
            return false;
        }

        var config = SystemConfigService.GetConfig(systemId);
        if (config == null)
        {
            message = $"系统 {systemId} 缺少配置，无法添加";
            return false;
        }

        try
        {
            Initialize();
            EnsureSystem(descriptor, config);
        }
        catch (Exception ex)
        {
            message = $"系统 {systemId} 添加失败: {ex.Message}";
            return false;
        }

        message = $"系统 {systemId} 已添加";
        return true;
    }

    /// <summary>
    /// 运行时移除系统。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <param name="message">操作结果中文说明。</param>
    public bool TryRemoveSystem(string systemId, out string message)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            message = "系统 Id 为空，无法移除";
            return false;
        }

        if (!_entries.TryGetValue(systemId, out var entry))
        {
            message = $"系统 {systemId} 未加载，无法移除";
            return false;
        }

        if (entry.Config.Required)
        {
            message = $"系统 {systemId} 是必需系统，禁止移除";
            return false;
        }

        var dependentSystemId = FindLoadedDependentSystem(systemId);
        if (!string.IsNullOrEmpty(dependentSystemId))
        {
            message = $"系统 {systemId} 正被 {dependentSystemId} 依赖，无法移除";
            return false;
        }

        try
        {
            if (entry.IsRunning)
            {
                entry.Lifecycle?.OnStopped(ProjectState.Snapshot);
            }

            entry.Lifecycle?.OnUnRegistered();
            _entries.Remove(systemId);

            if (entry.NodeInstance != null && GodotObject.IsInstanceValid(entry.NodeInstance))
            {
                entry.NodeInstance.QueueFree();
            }
        }
        catch (Exception ex)
        {
            message = $"系统 {systemId} 移除失败: {ex.Message}";
            return false;
        }

        message = $"系统 {systemId} 已移除";
        return true;
    }

    /// <summary>
    /// 运行时设置系统人工启用状态。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    /// <param name="enabled">目标启用状态。</param>
    /// <param name="message">操作结果中文说明。</param>
    public bool TrySetSystemEnabled(string systemId, bool enabled, out string message)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            message = "系统 Id 为空，无法切换启用状态";
            return false;
        }

        if (!_entries.TryGetValue(systemId, out var entry))
        {
            message = $"系统 {systemId} 未加载，无法切换启用状态";
            return false;
        }

        if (!enabled && entry.Config.Required)
        {
            message = $"系统 {systemId} 是必需系统，禁止禁用";
            return false;
        }

        if (entry.IsEnabled == enabled)
        {
            message = enabled
                ? $"系统 {systemId} 已是启用状态"
                : $"系统 {systemId} 已是禁用状态";
            return true;
        }

        if (enabled)
        {
            EnableSystem(systemId);
            message = $"系统 {systemId} 已启用";
            return true;
        }

        DisableSystem(systemId);
        message = $"系统 {systemId} 已禁用";
        return true;
    }

    /// <summary>
    /// 查找当前已加载系统中第一个依赖目标系统的系统 Id。
    /// </summary>
    /// <param name="targetSystemId">被检查的目标系统 Id。</param>
    private string FindLoadedDependentSystem(string targetSystemId)
    {
        foreach (var entry in _entries.Values)
        {
            if (string.Equals(entry.Descriptor.SystemId, targetSystemId, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var dependency in entry.Config.Dependencies)
            {
                if (string.Equals(dependency, targetSystemId, StringComparison.Ordinal))
                {
                    return entry.Descriptor.SystemId;
                }
            }
        }

        return string.Empty;
    }
}
