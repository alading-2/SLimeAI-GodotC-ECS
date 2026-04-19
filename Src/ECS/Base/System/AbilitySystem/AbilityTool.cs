using Godot;

/// <summary>
/// Ability 子域工具。
/// 只保留跨技能确实复用、且不读取 CastContext 的薄逻辑。
/// </summary>
internal static class AbilityTool
{
    /// <summary>
    /// 判断目标是否符合技能阵营过滤。
    /// </summary>
    public static bool MatchesTeamFilter(
        IEntity caster,
        IEntity target,
        TeamFilter filter)
    {
        if (filter == TeamFilter.None || filter == TeamFilter.All)
        {
            return true;
        }

        if (ReferenceEquals(caster, target))
        {
            return filter.HasFlag(TeamFilter.Self);
        }

        var casterId = caster.Data.Get<string>(DataKey.Id);
        var targetId = target.Data.Get<string>(DataKey.Id);
        if (!string.IsNullOrEmpty(casterId) && casterId == targetId)
        {
            return filter.HasFlag(TeamFilter.Self);
        }

        var casterTeam = caster.Data.Get<Team>(DataKey.Team);
        var targetTeam = target.Data.Get<Team>(DataKey.Team);
        if (targetTeam == Team.Neutral)
        {
            return filter.HasFlag(TeamFilter.Neutral);
        }

        if (casterTeam == targetTeam)
        {
            return filter.HasFlag(TeamFilter.Friendly);
        }

        return filter.HasFlag(TeamFilter.Enemy);
    }

    /// <summary>
    /// 将生成点沿给定方向前推一段距离，减少投射物贴在施法者中心导致的首帧重叠。
    /// </summary>
    public static Vector2 ResolveSpawnPosition(
        Vector2 startPos, // 原始生成点
        Vector2 direction, // 出生朝向
        float forwardOffset) // 前推距离
    {
        if (forwardOffset <= 0f)
        {
            return startPos;
        }

        Vector2 normalizedDirection = direction.LengthSquared() >= 0.001f
            ? direction.Normalized()
            : Vector2.Right;
        return startPos + normalizedDirection * forwardOffset;
    }
}
