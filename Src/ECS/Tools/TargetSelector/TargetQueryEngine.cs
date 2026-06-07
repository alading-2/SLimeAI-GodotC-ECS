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
    private const int DefaultRandomSeed = 0;

    /// <summary>查询实体目标。</summary>
    public static TargetQueryResult<IEntity> QueryEntities(TargetSelectorQuery query)
    {
        var resolved = query.Resolve();
        var warnings = new List<string>();
        var errors = ValidateQuery(resolved, forPositions: false);
        if (errors.Count > 0)
        {
            return new TargetQueryResult<IEntity>(
                Array.Empty<IEntity>(),
                CreateDiagnostics(resolved, 0, 0, 0, 0, 0, 0, resolved.MaxTargets, false, false, warnings, errors));
        }

        var source = ResolveCandidateSource(resolved);
        var sourceCandidates = source.GetCandidates(new TargetQueryContext(resolved));
        var geometryHits = CollectGeometryHits(sourceCandidates, resolved);
        var filtered = ApplyFilters(geometryHits, resolved, warnings, out var teamFiltered, out var typeFiltered, out var lifecycleFiltered);

        if (filtered.Count > 1)
        {
            SortTargets(filtered, resolved, warnings);
        }

        var maxTargets = resolved.MaxTargets;
        var limitApplied = maxTargets > 0;
        var truncated = limitApplied && filtered.Count > maxTargets;
        IReadOnlyList<IEntity> returned = truncated
            ? filtered.GetRange(0, maxTargets)
            : filtered.ToArray();

        var diagnostics = CreateDiagnostics(
            resolved,
            sourceCandidates.Count,
            geometryHits.Count,
            teamFiltered,
            typeFiltered,
            lifecycleFiltered,
            returned.Count,
            maxTargets,
            limitApplied,
            truncated,
            warnings,
            errors);

        return new TargetQueryResult<IEntity>(returned, diagnostics);
    }

    /// <summary>查询或生成位置目标。</summary>
    public static TargetQueryResult<Vector2> QueryPositions(TargetSelectorQuery query)
    {
        var resolved = query.Resolve();
        var warnings = new List<string>();
        var errors = ValidateQuery(resolved, forPositions: true);
        if (errors.Count > 0)
        {
            return new TargetQueryResult<Vector2>(
                Array.Empty<Vector2>(),
                CreateDiagnostics(resolved, 0, 0, 0, 0, 0, 0, resolved.MaxTargets, false, false, warnings, errors));
        }

        var results = new List<Vector2>();
        var count = resolved.MaxTargets > 0 ? resolved.MaxTargets : 1;

        var rng = CreateGodotRandom(resolved);

        for (var i = 0; i < count; i++)
        {
            results.Add(GetRandomPointInGeometry(resolved, rng));
        }

        var diagnostics = CreateDiagnostics(
            resolved,
            results.Count,
            results.Count,
            0,
            0,
            0,
            results.Count,
            resolved.MaxTargets,
            resolved.MaxTargets > 0,
            false,
            warnings,
            errors);

        return new TargetQueryResult<Vector2>(results, diagnostics);
    }

    private static ITargetCandidateSource ResolveCandidateSource(TargetSelectorQuery query)
    {
        if (query.Geometry == GeometryType.Single && query.ExplicitTarget != null)
        {
            return new ExplicitTargetCandidateSource(new[] { query.ExplicitTarget });
        }

        if (query.ExplicitCandidates != null)
        {
            return new ExplicitTargetCandidateSource(query.ExplicitCandidates);
        }

        return EntityManagerTargetCandidateSource.Instance;
    }

    private static List<IEntity> CollectGeometryHits(IReadOnlyList<IEntity> candidates, TargetSelectorQuery query)
    {
        if (query.Geometry == GeometryType.Single)
        {
            return candidates.Take(1).ToList();
        }

        var hits = new List<IEntity>();
        foreach (var entity in candidates)
        {
            if (query.Geometry == GeometryType.Global)
            {
                hits.Add(entity);
                continue;
            }

            if (entity is Node2D node2D && IsPointInGeometry(node2D.GlobalPosition, query))
            {
                hits.Add(entity);
            }
        }

        return hits;
    }

    private static List<IEntity> ApplyFilters(
        IReadOnlyList<IEntity> geometryHits,
        TargetSelectorQuery query,
        List<string> warnings,
        out int teamFiltered,
        out int typeFiltered,
        out int lifecycleFiltered)
    {
        teamFiltered = 0;
        typeFiltered = 0;
        lifecycleFiltered = 0;

        var filtered = new List<IEntity>();
        foreach (var target in geometryHits)
        {
            if (!PassTeamFilter(target, query.CenterEntity, query.TeamFilter, warnings))
            {
                teamFiltered++;
                continue;
            }

            if (!PassTypeFilter(target, query.TypeFilter, warnings))
            {
                typeFiltered++;
                continue;
            }

            if (!PassLifecycleFilter(target, warnings))
            {
                lifecycleFiltered++;
                continue;
            }

            filtered.Add(target);
        }

        return filtered;
    }

    private static bool PassTeamFilter(IEntity target, IEntity? center, TeamFilter filter, List<string> warnings)
    {
        if (filter == TeamFilter.None || filter == TeamFilter.All) return true;
        var isSelf = IsSameEntity(target, center);
        if (isSelf) return filter.HasFlag(TeamFilter.Self);
        var targetTeam = TryGetData(target, GeneratedDataKey.Team, Team.Neutral, warnings, "TeamFilter target team", warnWhenUsingDefault: false);
        if (targetTeam == Team.Neutral) return filter.HasFlag(TeamFilter.Neutral);
        if (center == null) return false;
        var centerTeam = TryGetData(center, GeneratedDataKey.Team, Team.Neutral, warnings, "TeamFilter center team", warnWhenUsingDefault: false);
        return centerTeam == targetTeam
            ? filter.HasFlag(TeamFilter.Friendly)
            : filter.HasFlag(TeamFilter.Enemy);
    }

    private static bool PassTypeFilter(IEntity target, EntityType filter, List<string> warnings)
    {
        if (filter == EntityType.None) return true;
        var entityType = TryGetData(target, GeneratedDataKey.EntityType, EntityType.None, warnings, "TypeFilter entity type", warnWhenUsingDefault: false);
        return filter.HasFlag(entityType);
    }

    private static bool PassLifecycleFilter(IEntity target, List<string> warnings)
    {
        var state = TryGetData(target, GeneratedDataKey.LifecycleState, LifecycleState.Alive, warnings, "LifecycleFilter state", warnWhenUsingDefault: false);
        return state != LifecycleState.Dead && state != LifecycleState.Reviving;
    }

    private static void SortTargets(List<IEntity> targets, TargetSelectorQuery query, List<string> warnings)
    {
        switch (query.Sorting)
        {
            case TargetSorting.None:
                break;
            case TargetSorting.Nearest:
                targets.Sort((a, b) =>
                    GetEntityPosition(a).DistanceTo(query.Origin).CompareTo(GetEntityPosition(b).DistanceTo(query.Origin)));
                break;
            case TargetSorting.Farthest:
                targets.Sort((a, b) =>
                    GetEntityPosition(b).DistanceTo(query.Origin).CompareTo(GetEntityPosition(a).DistanceTo(query.Origin)));
                break;
            case TargetSorting.LowestHealth:
                targets.Sort((a, b) =>
                    TryGetData(a, GeneratedDataKey.CurrentHp, 0f, warnings, "LowestHealth").CompareTo(
                        TryGetData(b, GeneratedDataKey.CurrentHp, 0f, warnings, "LowestHealth")));
                break;
            case TargetSorting.HighestHealth:
                targets.Sort((a, b) =>
                    TryGetData(b, GeneratedDataKey.CurrentHp, 0f, warnings, "HighestHealth").CompareTo(
                        TryGetData(a, GeneratedDataKey.CurrentHp, 0f, warnings, "HighestHealth")));
                break;
            case TargetSorting.HighestHealthPercent:
                targets.Sort((a, b) =>
                    TryGetData(b, GeneratedDataKey.HpPercent, 0f, warnings, "HighestHealthPercent").CompareTo(
                        TryGetData(a, GeneratedDataKey.HpPercent, 0f, warnings, "HighestHealthPercent")));
                break;
            case TargetSorting.LowestHealthPercent:
                targets.Sort((a, b) =>
                    TryGetData(a, GeneratedDataKey.HpPercent, 0f, warnings, "LowestHealthPercent").CompareTo(
                        TryGetData(b, GeneratedDataKey.HpPercent, 0f, warnings, "LowestHealthPercent")));
                break;
            case TargetSorting.Random:
                Shuffle(targets, query);
                break;
            case TargetSorting.HighestThreat:
                targets.Sort((a, b) =>
                    TryGetData(b, GeneratedDataKey.Threat, 0f, warnings, "HighestThreat").CompareTo(
                        TryGetData(a, GeneratedDataKey.Threat, 0f, warnings, "HighestThreat")));
                break;
        }
    }

    private static void Shuffle(List<IEntity> targets, TargetSelectorQuery query)
    {
        if (query.RandomSource != null)
        {
            ShuffleWithRandomSource(targets, query.RandomSource);
            return;
        }

        var state = unchecked((uint)(query.RandomSeed ?? DefaultRandomSeed));
        for (var i = targets.Count - 1; i > 0; i--)
        {
            var j = (int)(NextDeterministic(ref state) % (uint)(i + 1));
            (targets[i], targets[j]) = (targets[j], targets[i]);
        }
    }

    private static bool IsPointInGeometry(Vector2 point, TargetSelectorQuery query)
    {
        return query.Geometry switch
        {
            GeometryType.Circle => Geometry2D.IsPointInCircle(point, query.ResolveOrigin(), query.Range),
            GeometryType.Ring => Geometry2D.IsPointInRing(point, query.ResolveOrigin(), query.InnerRange, query.Range),
            GeometryType.Box => Geometry2D.IsPointInBox(point, query.ResolveOrigin(), query.ResolveForward(), query.Width, query.Length),
            GeometryType.Line => Geometry2D.IsPointInCapsule(point, query.ResolveOrigin(), query.ResolveForward(), query.Length, query.Width),
            GeometryType.Cone => Geometry2D.IsPointInCone(point, query.ResolveOrigin(), query.ResolveForward(), query.Range, query.Angle),
            GeometryType.Global => true,
            _ => false
        };
    }

    private static Vector2 GetRandomPointInGeometry(TargetSelectorQuery query, RandomNumberGenerator rng)
    {
        return query.Geometry switch
        {
            GeometryType.Circle => Geometry2D.GetRandomPointInRing(query.ResolveOrigin(), 0f, query.Range, rng),
            GeometryType.Ring => Geometry2D.GetRandomPointInRing(query.ResolveOrigin(), query.InnerRange, query.Range, rng),
            GeometryType.Box => Geometry2D.GetRandomPointInBox(
                query.ResolveOrigin() + query.ResolveForward() * (query.Length * 0.5f),
                query.ResolveForward(),
                query.Width,
                query.Length,
                rng),
            GeometryType.Line => Geometry2D.GetRandomPointInBox(
                query.ResolveOrigin() + query.ResolveForward() * (query.Length * 0.5f),
                query.ResolveForward(),
                query.Width,
                query.Length,
                rng),
            GeometryType.Cone => Geometry2D.GetRandomPointInCone(query.ResolveOrigin(), query.ResolveForward(), query.Range, query.Angle, rng),
            _ => query.ResolveOrigin()
        };
    }

    private static void ShuffleWithRandomSource(List<IEntity> targets, Random random)
    {
        for (var i = targets.Count - 1; i > 0; i--)
        {
            var j = random.Next(i + 1);
            (targets[i], targets[j]) = (targets[j], targets[i]);
        }
    }

    private static uint NextDeterministic(ref uint state)
    {
        // 轻量 xorshift32，仅用于 TargetSelector deterministic replay，不作为加密随机源。
        if (state == 0)
        {
            state = 0x6D2B79F5u;
        }

        state ^= state << 13;
        state ^= state >> 17;
        state ^= state << 5;
        return state;
    }

    private static List<string> ValidateQuery(TargetSelectorQuery query, bool forPositions)
    {
        var errors = new List<string>();
        if (query.MaxTargets == 0)
        {
            errors.Add("MaxTargets must be -1 or a positive number.");
        }

        switch (query.Geometry)
        {
            case GeometryType.Circle:
                RequirePositive(query.Range, nameof(query.Range), errors);
                break;
            case GeometryType.Ring:
                RequirePositive(query.Range, nameof(query.Range), errors);
                if (query.InnerRange < 0f) errors.Add("InnerRange must be >= 0 for Ring geometry.");
                if (query.InnerRange >= query.Range) errors.Add("InnerRange must be smaller than Range for Ring geometry.");
                break;
            case GeometryType.Box:
            case GeometryType.Line:
                RequirePositive(query.Width, nameof(query.Width), errors);
                RequirePositive(query.Length, nameof(query.Length), errors);
                break;
            case GeometryType.Cone:
                RequirePositive(query.Range, nameof(query.Range), errors);
                RequirePositive(query.Angle, nameof(query.Angle), errors);
                if (query.Angle > 360f) errors.Add("Angle must be <= 360 for Cone geometry.");
                break;
            case GeometryType.Single:
                if (!forPositions && query.ExplicitTarget == null && (query.ExplicitCandidates == null || query.ExplicitCandidates.Count == 0))
                {
                    errors.Add("Single entity query requires ExplicitTarget or ExplicitCandidates.");
                }
                break;
            case GeometryType.Global:
                break;
            default:
                errors.Add($"Unsupported geometry: {query.Geometry}.");
                break;
        }

        return errors;
    }

    private static void RequirePositive(float value, string name, List<string> errors)
    {
        if (value <= 0f)
        {
            errors.Add($"{name} must be > 0.");
        }
    }

    private static TargetQueryDiagnostics CreateDiagnostics(
        TargetSelectorQuery query,
        int candidateCount,
        int geometryHitCount,
        int filteredByTeamCount,
        int filteredByTypeCount,
        int filteredByLifecycleCount,
        int returnedCount,
        int maxTargets,
        bool limitApplied,
        bool truncated,
        IReadOnlyList<string> warnings,
        IReadOnlyList<string> errors)
    {
        return new TargetQueryDiagnostics(
            query.Origin,
            query.ResolveForward(),
            candidateCount,
            geometryHitCount,
            filteredByTeamCount,
            filteredByTypeCount,
            filteredByLifecycleCount,
            returnedCount,
            maxTargets,
            limitApplied,
            truncated,
            query.Sorting.ToString(),
            warnings.ToArray(),
            errors.ToArray());
    }

    private static RandomNumberGenerator CreateGodotRandom(TargetSelectorQuery query)
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = unchecked((ulong)(uint)(query.RandomSeed ?? DefaultRandomSeed));
        return rng;
    }

    private static T TryGetData<T>(
        IEntity entity,
        DataKey<T> key,
        T fallback,
        List<string> warnings,
        string usage,
        bool warnWhenUsingDefault = true)
    {
        try
        {
            if (entity.Data.TryGetValue(key.StableKey, out T value))
            {
                return value;
            }

            if (warnWhenUsingDefault)
            {
                var defaultName = entity is Node defaultNode ? defaultNode.Name.ToString() : entity.GetType().Name;
                warnings.Add($"{usage}: entity '{defaultName}' has no explicit data '{key.StableKey}', fallback '{fallback}'.");
            }

            return fallback;
        }
        catch (Exception ex)
        {
            var name = entity is Node node ? node.Name.ToString() : entity.GetType().Name;
            warnings.Add($"{usage}: entity '{name}' missing or invalid data '{key.StableKey}', fallback '{fallback}'. {ex.GetType().Name}: {ex.Message}");
            return fallback;
        }
    }

    private static bool IsSameEntity(IEntity? a, IEntity? b)
    {
        if (a == b) return true;
        if (a is Node n1 && b is Node n2) return n1 == n2;
        return false;
    }

    private static Vector2 GetEntityPosition(IEntity entity)
    {
        return entity is Node2D node2D ? node2D.GlobalPosition : Vector2.Zero;
    }
}
