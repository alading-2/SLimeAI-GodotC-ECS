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
        AbilityTargetTeamFilter filter)
    {
        if (filter == AbilityTargetTeamFilter.None || filter == AbilityTargetTeamFilter.All)
        {
            return true;
        }

        if (ReferenceEquals(caster, target))
        {
            return filter.HasFlag(AbilityTargetTeamFilter.Self);
        }

        var casterId = caster.Data.Get<string>(DataKey.Id);
        var targetId = target.Data.Get<string>(DataKey.Id);
        if (!string.IsNullOrEmpty(casterId) && casterId == targetId)
        {
            return filter.HasFlag(AbilityTargetTeamFilter.Self);
        }

        var casterTeam = caster.Data.Get<Team>(DataKey.Team);
        var targetTeam = target.Data.Get<Team>(DataKey.Team);
        if (targetTeam == Team.Neutral)
        {
            return filter.HasFlag(AbilityTargetTeamFilter.Neutral);
        }

        if (casterTeam == targetTeam)
        {
            return filter.HasFlag(AbilityTargetTeamFilter.Friendly);
        }

        return filter.HasFlag(AbilityTargetTeamFilter.Enemy);
    }
}
