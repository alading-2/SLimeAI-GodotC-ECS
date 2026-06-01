/// <summary>
/// 状态定义。
/// <para>定义“一个状态类型”会贡献哪些能力标记，不包含来源和持续时间。</para>
/// </summary>
/// <param name="StatusId">状态唯一 Id。</param>
/// <param name="DisplayName">状态显示名。</param>
/// <param name="Flags">状态贡献的聚合标记。</param>
public sealed record StatusDefinition(
    string StatusId,
    string DisplayName,
    StatusEffectFlags Flags);
