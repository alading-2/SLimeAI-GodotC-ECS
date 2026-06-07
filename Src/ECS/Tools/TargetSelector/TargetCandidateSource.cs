using System.Collections.Generic;
using System.Linq;

/// <summary>
/// TargetSelector 候选来源抽象。
/// </summary>
public interface ITargetCandidateSource
{
    IReadOnlyList<IEntity> GetCandidates(TargetQueryContext context);
}

/// <summary>
/// TargetSelector 查询上下文。
/// </summary>
public sealed class TargetQueryContext
{
    public TargetQueryContext(TargetSelectorQuery query)
    {
        Query = query;
    }

    public TargetSelectorQuery Query { get; }
}

/// <summary>
/// 默认 EntityManager 候选来源。
/// </summary>
public sealed class EntityManagerTargetCandidateSource : ITargetCandidateSource
{
    public static readonly EntityManagerTargetCandidateSource Instance = new();

    private EntityManagerTargetCandidateSource()
    {
    }

    public IReadOnlyList<IEntity> GetCandidates(TargetQueryContext context)
    {
        return EntityManager.GetAllEntities().ToArray();
    }
}

/// <summary>
/// 显式候选来源。
/// </summary>
public sealed class ExplicitTargetCandidateSource : ITargetCandidateSource
{
    private readonly IReadOnlyList<IEntity> _candidates;

    public ExplicitTargetCandidateSource(IReadOnlyList<IEntity> candidates)
    {
        _candidates = candidates;
    }

    public IReadOnlyList<IEntity> GetCandidates(TargetQueryContext context)
    {
        return _candidates;
    }
}
