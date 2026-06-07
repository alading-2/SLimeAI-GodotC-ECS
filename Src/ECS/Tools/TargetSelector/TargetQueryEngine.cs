using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 目标查询主入口。
/// 负责生成带 ownership 和 diagnostics 的查询结果；旧 TargetSelector facade 只做兼容转发。
/// </summary>
public static class TargetQueryEngine
{
    private static readonly Random _random = new();

    /// <summary>查询实体目标。</summary>
    public static TargetQueryResult<IEntity> QueryEntities(TargetSelectorQuery query)
    {
        var candidates = CollectEntityCandidates(query);

        if (candidates.Count > 1)
        {
            SortTargets(candidates, query.ResolveOrigin(), query.Sorting);
        }

        var candidateCount = candidates.Count;
        var maxTargets = query.MaxTargets;
        var truncated = maxTargets > 0 && candidateCount > maxTargets;
        var returned = truncated ? candidates.GetRange(0, maxTargets) : candidates;

        var diagnostics = new TargetQueryDiagnostics(
            CandidateCount: candidateCount,
            ReturnedCount: returned.Count,
            MaxTargets: maxTargets,
            Truncated: truncated);

        return new TargetQueryResult<IEntity>(returned, diagnostics);
    }

    /// <summary>查询或生成位置目标。</summary>
    public static TargetQueryResult<Vector2> QueryPositions(TargetSelectorQuery query)
    {
        var results = new List<Vector2>();
        var count = query.MaxTargets > 0 ? query.MaxTargets : 1;

        // 位置采样仍走 Godot RNG；ownership 先由 TargetQueryResult 表达，后续再决定是否 lease buffer。
        var rng = new RandomNumberGenerator();
        rng.Seed = (ulong)Time.GetTicksMsec();

        for (var i = 0; i < count; i++)
        {
            results.Add(GeometryCalculator.GetRandomPointInGeometry(query, rng));
        }

        var diagnostics = new TargetQueryDiagnostics(
            CandidateCount: results.Count,
            ReturnedCount: results.Count,
            MaxTargets: query.MaxTargets,
            Truncated: false);

        return new TargetQueryResult<Vector2>(results, diagnostics);
    }

    private static List<IEntity> CollectEntityCandidates(TargetSelectorQuery query)
    {
        var candidates = new List<IEntity>();

        if (query.Geometry == GeometryType.Single)
        {
            return candidates;
        }

        foreach (var entity in GetAllNode2DEntities())
        {
            if (entity is not Node2D node2D) continue;
            if (GeometryCalculator.IsPointInGeometry(node2D.GlobalPosition, query))
            {
                candidates.Add(entity);
            }
        }

        return FilterTargets(candidates, query.CenterEntity, query.TeamFilter, query.TypeFilter);
    }

    private static List<IEntity> FilterTargets(
        List<IEntity> targets,
        IEntity? centerEntity,
        TeamFilter teamFilter,
        EntityType typeFilter)
    {
        var filtered = new List<IEntity>();
        foreach (var target in targets)
        {
            if (!PassTeamFilter(target, centerEntity, teamFilter)) continue;
            if (!PassTypeFilter(target, typeFilter)) continue;
            if (target.Data.Has(GeneratedDataKey.LifecycleState))
            {
                var state = target.Data.Get<LifecycleState>(GeneratedDataKey.LifecycleState);
                if (state == LifecycleState.Dead || state == LifecycleState.Reviving) continue;
            }

            filtered.Add(target);
        }

        return filtered;
    }

    private static bool PassTeamFilter(IEntity target, IEntity? center, TeamFilter filter)
    {
        if (filter == TeamFilter.None || filter == TeamFilter.All) return true;
        var isSelf = IsSameEntity(target, center);
        if (isSelf) return filter.HasFlag(TeamFilter.Self);
        var targetTeam = target.Data.Get<Team>(GeneratedDataKey.Team);
        if (targetTeam == Team.Neutral) return filter.HasFlag(TeamFilter.Neutral);
        if (center == null) return false;
        var centerTeam = center.Data.Get<Team>(GeneratedDataKey.Team);
        return centerTeam == targetTeam
            ? filter.HasFlag(TeamFilter.Friendly)
            : filter.HasFlag(TeamFilter.Enemy);
    }

    private static bool PassTypeFilter(IEntity target, EntityType filter)
    {
        if (filter == EntityType.None) return true;
        if (!target.Data.Has(GeneratedDataKey.EntityType)) return false;
        var entityType = target.Data.Get<EntityType>(GeneratedDataKey.EntityType);
        return filter.HasFlag(entityType);
    }

    private static void SortTargets(List<IEntity> targets, Vector2 origin, TargetSorting sorting)
    {
        switch (sorting)
        {
            case TargetSorting.None:
                break;
            case TargetSorting.Nearest:
                targets.Sort((a, b) =>
                    GetEntityPosition(a).DistanceTo(origin).CompareTo(GetEntityPosition(b).DistanceTo(origin)));
                break;
            case TargetSorting.Farthest:
                targets.Sort((a, b) =>
                    GetEntityPosition(b).DistanceTo(origin).CompareTo(GetEntityPosition(a).DistanceTo(origin)));
                break;
            case TargetSorting.LowestHealth:
                targets.Sort((a, b) =>
                    a.Data.Get<float>(GeneratedDataKey.CurrentHp).CompareTo(b.Data.Get<float>(GeneratedDataKey.CurrentHp)));
                break;
            case TargetSorting.HighestHealth:
                targets.Sort((a, b) =>
                    b.Data.Get<float>(GeneratedDataKey.CurrentHp).CompareTo(a.Data.Get<float>(GeneratedDataKey.CurrentHp)));
                break;
            case TargetSorting.HighestHealthPercent:
                targets.Sort((a, b) =>
                    b.Data.Get<float>(GeneratedDataKey.HpPercent).CompareTo(a.Data.Get<float>(GeneratedDataKey.HpPercent)));
                break;
            case TargetSorting.LowestHealthPercent:
                targets.Sort((a, b) =>
                    a.Data.Get<float>(GeneratedDataKey.HpPercent).CompareTo(b.Data.Get<float>(GeneratedDataKey.HpPercent)));
                break;
            case TargetSorting.Random:
                Shuffle(targets);
                break;
            case TargetSorting.HighestThreat:
                targets.Sort((a, b) =>
                    (b.Data.Has(GeneratedDataKey.Threat) ? b.Data.Get<float>(GeneratedDataKey.Threat) : 0).CompareTo(
                        a.Data.Has(GeneratedDataKey.Threat) ? a.Data.Get<float>(GeneratedDataKey.Threat) : 0));
                break;
        }
    }

    private static void Shuffle(List<IEntity> targets)
    {
        for (var i = targets.Count - 1; i > 0; i--)
        {
            var j = _random.Next(i + 1);
            (targets[i], targets[j]) = (targets[j], targets[i]);
        }
    }

    private static bool IsSameEntity(IEntity? a, IEntity? b)
    {
        if (a == b) return true;
        if (a is Node n1 && b is Node n2) return n1 == n2;
        return false;
    }

    private static IEnumerable<IEntity> GetAllNode2DEntities()
    {
        return NodeLifecycleManager.GetNodesByInterface<Node2D>().OfType<IEntity>();
    }

    private static Vector2 GetEntityPosition(IEntity entity)
    {
        return entity is Node2D node2D ? node2D.GlobalPosition : Vector2.Zero;
    }
}
