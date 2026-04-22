using System;
using System.Collections.Generic;

/// <summary>
/// 系统 Profile 解析服务。
/// </summary>
public sealed class SystemProfileService
{
    private static readonly Log _log = new(nameof(SystemProfileService));
    private readonly Dictionary<string, SystemProfileEntry> _entriesBySystemId = new(StringComparer.Ordinal);

    /// <summary>活动中的 Profile；为空时退回描述符默认值。</summary>
    public SystemProfile? ActiveProfile { get; private set; }

    /// <summary>
    /// 设置活动 Profile。
    /// 传入 null 可清除当前 Profile，后续解析将全部退回描述符默认值。
    /// </summary>
    /// <param name="profile">新的活动 Profile。</param>
    public void SetActiveProfile(SystemProfile? profile)
    {
        ActiveProfile = profile;
        RebuildEntryIndex(profile);
    }

    /// <summary>
     /// 解析系统是否应自动装载。
    /// <para>命中 Profile.Systems 条目时使用条目的 AutoAdd；否则回退 descriptor.DefaultAutoAdd。</para>
    /// </summary>
    /// <param name="descriptor">系统描述符。</param>
    public bool ResolveAutoAdd(SystemDescriptor descriptor)
    {
        if (descriptor == null)
        {
            return false;
        }

        if (TryGetEntry(descriptor.SystemId, out var entry))
        {
            return entry.AutoAdd;
        }

        return descriptor.DefaultAutoAdd;
    }

    /// <summary>
     /// 解析系统在 Profile 层是否被允许启用。
    /// <para>命中 Profile.Systems 条目时使用条目的 Enabled；否则回退 descriptor.DefaultEnabled。</para>
    /// </summary>
    /// <param name="descriptor">系统描述符。</param>
    public bool ResolveEnabled(SystemDescriptor descriptor)
    {
        if (descriptor == null)
        {
            return false;
        }

        if (TryGetEntry(descriptor.SystemId, out var entry))
        {
            return entry.Enabled;
        }

        return descriptor.DefaultEnabled;
    }

    private bool TryGetEntry(string systemId, out SystemProfileEntry entry)
    {
        return _entriesBySystemId.TryGetValue(systemId, out entry!);
    }

    private void RebuildEntryIndex(SystemProfile? profile)
    {
        _entriesBySystemId.Clear();

        if (profile == null)
        {
            return;
        }

        for (var i = 0; i < profile.Systems.Count; i++)
        {
            var entry = profile.Systems[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.SystemId))
            {
                continue;
            }

            if (_entriesBySystemId.ContainsKey(entry.SystemId))
            {
                _log.Warn($"SystemProfile 存在重复 SystemId，后者将覆盖前者: {entry.SystemId}");
            }

            _entriesBySystemId[entry.SystemId] = entry;
        }
    }
}
