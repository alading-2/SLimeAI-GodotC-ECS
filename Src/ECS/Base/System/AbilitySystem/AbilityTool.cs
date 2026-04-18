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
}