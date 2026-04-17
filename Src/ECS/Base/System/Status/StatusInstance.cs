/// <summary>
/// 单次状态实例。
/// <para>同一个 StatusDefinition 可由多个来源并存实例化。</para>
/// </summary>
/// <param name="SourceId">来源唯一 Id。</param>
/// <param name="StatusId">状态 Id。</param>
/// <param name="Definition">状态定义。</param>
/// <param name="DurationSeconds">持续时间；-1 表示不限时。</param>
public sealed record StatusInstance(
    string SourceId,
    string StatusId,
    StatusDefinition Definition,
    float DurationSeconds);
