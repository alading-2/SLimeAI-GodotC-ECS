using System.Collections.Generic;

/// <summary>
/// 状态实例集合。
/// <para>核心语义：按来源+状态类型区分实例，解决多来源状态互相覆盖问题。</para>
/// </summary>
public sealed class StatusCollection
{
    // key = "{sourceId}::{statusId}"，确保每个来源的同名状态可独立移除。
    private readonly Dictionary<string, StatusInstance> _instances = new();

    /// <summary>
    /// 写入或刷新一个状态实例。
    /// </summary>
    /// <param name="instance">新的状态实例。</param>
    public void Apply(StatusInstance instance)
    {
        var key = BuildKey(instance.SourceId, instance.StatusId);
        // 同 key 写入视为刷新该来源状态（覆盖旧实例）。
        _instances[key] = instance;
    }

    /// <summary>
    /// 按来源和状态 Id 移除状态实例。
    /// </summary>
    /// <param name="sourceId">来源 Id。</param>
    /// <param name="statusId">状态 Id。</param>
    /// <returns>移除成功返回 true。</returns>
    public bool Remove(string sourceId, string statusId)
    {
        return _instances.Remove(BuildKey(sourceId, statusId));
    }

    /// <summary>
    /// 生成当前状态聚合快照。
    /// </summary>
    /// <returns>聚合后的状态快照。</returns>
    public StatusSnapshot BuildSnapshot()
    {
        var flags = StatusEffectFlags.None;
        foreach (var instance in _instances.Values)
        {
            // 多实例按位或聚合，任何来源仍持有标记都应保持生效。
            flags |= instance.Definition.Flags;
        }

        return new StatusSnapshot(flags);
    }

    private static string BuildKey(string sourceId, string statusId)
    {
        return $"{sourceId}::{statusId}";
    }
}
