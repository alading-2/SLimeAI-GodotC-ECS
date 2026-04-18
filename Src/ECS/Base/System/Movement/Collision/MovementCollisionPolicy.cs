using System.Collections.Generic;
using Godot;

/// <summary>
/// 移动碰撞策略执行器。
/// <para>
/// 负责把原始碰撞候选转换为“有效碰撞”，并维护单次运动内的去重与计数状态。
/// </para>
/// </summary>
public sealed class MovementCollisionPolicy
{
    /// <summary>本次运动内已接受碰撞的目标实例ID集合，用于去重。</summary>
    private readonly HashSet<ulong> _acceptedTargets = new();

    /// <summary>当前碰撞参数配置，为 null 表示策略未启用。</summary>
    private MovementCollisionParams? _config;
    /// <summary>本次运动内已接受碰撞的累计次数。</summary>
    private int _acceptedCollisionCount;

    /// <summary>当前策略是否启用。</summary>
    public bool IsEnabled => _config.HasValue;

    /// <summary>
    /// 用新的运动参数重置策略状态。
    /// </summary>
    public void Reset(in MovementParams @params)
    {
        _config = @params.Collision;
        _acceptedCollisionCount = 0;
        _acceptedTargets.Clear();
    }

    /// <summary>
    /// 尝试接受一次原始碰撞。
    /// <para>依次执行：启用检查 → 阵营/类型/目标过滤 → 去重 → 计数 → 判断是否触发停止。</para>
    /// </summary>
    public bool TryAccept(
        IEntity sourceEntity,
        MoveMode mode,
        in MovementParams @params,
        Node2D? target,
        out MovementCollisionContext context)
    {
        context = default;

        // 策略未启用或目标为空，直接拒绝
        if (!_config.HasValue || target == null)
        {
            return false;
        }

        var config = _config.Value;
        // 将碰撞节点解析为其所属的 IEntity（可能为 null，如静态障碍物）
        var targetEntity = EntityManager.ResolveOwningIEntity(target);
        // 阵营 / 实体类型 / 目标匹配 三重过滤
        if (!PassFilters(sourceEntity, config, @params, target, targetEntity))
        {
            return false;
        }

        // 生成碰撞去重键，同一目标在单次运动内只接受一次
        ulong collisionKey = ResolveCollisionKey(target, targetEntity);
        if (collisionKey == 0 || !_acceptedTargets.Add(collisionKey))
        {
            return false;
        }

        _acceptedCollisionCount++;
        // StopAfterCollisionCount >= 0 表示有碰撞次数上限；达到上限后标记 willStop 通知驱动器停止运动
        bool willStop = config.StopAfterCollisionCount >= 0
            && _acceptedCollisionCount >= config.StopAfterCollisionCount;

        context = new MovementCollisionContext(
            mode,
            target,
            targetEntity,
            _acceptedCollisionCount,
            willStop,
            @params);
        return true;
    }

    /// <summary>
    /// 依次执行阵营过滤、实体类型过滤、目标匹配过滤，任一不通过即返回 false。
    /// </summary>
    private static bool PassFilters(
        IEntity sourceEntity,
        MovementCollisionParams config,
        in MovementParams @params,
        Node2D target,
        IEntity? targetEntity)
    {
        if (!PassTeamFilter(sourceEntity, targetEntity, config.TeamFilter))
        {
            return false;
        }

        if (!PassEntityTypeFilter(targetEntity, config.EntityTypeFilter))
        {
            return false;
        }

        return PassTargetMatch(@params, config, target, targetEntity);
    }

    /// <summary>阵营过滤：All/None 放行所有，否则委托 AbilityTool 判断敌我关系。</summary>
    private static bool PassTeamFilter(IEntity sourceEntity, IEntity? targetEntity, TeamFilter filter)
    {
        if (filter == TeamFilter.All || filter == TeamFilter.None)
        {
            return true;
        }

        if (targetEntity == null)
        {
            return false;
        }

        return AbilityTool.MatchesTeamFilter(sourceEntity, targetEntity, filter);
    }

    /// <summary>实体类型过滤：None 放行所有，否则按位枚举匹配目标类型。</summary>
    private static bool PassEntityTypeFilter(IEntity? targetEntity, EntityType filter)
    {
        if (filter == EntityType.None)
        {
            return true;
        }

        if (targetEntity == null || !targetEntity.Data.Has(DataKey.EntityType))
        {
            return false;
        }

        EntityType targetType = targetEntity.Data.Get<EntityType>(DataKey.EntityType);
        return (targetType & filter) != 0;
    }

    /// <summary>
    /// 目标匹配过滤：Any 放行所有；TrackedTargetOnly 仅匹配当前追踪目标；SpecificNode 仅匹配指定节点。
    /// </summary>
    private static bool PassTargetMatch(
        in MovementParams @params,
        MovementCollisionParams config,
        Node2D target,
        IEntity? targetEntity)
    {
        switch (config.TargetMatchMode)
        {
            case MovementCollisionTargetMatchMode.Any:
                return true;
            case MovementCollisionTargetMatchMode.TrackedTargetOnly:
                return MatchesTarget(@params.TargetNode, target, targetEntity);
            case MovementCollisionTargetMatchMode.SpecificNode:
                return MatchesTarget(config.SpecificTargetNode, target, targetEntity);
            default:
                return false;
        }
    }

    /// <summary>
    /// 判断实际碰撞目标是否为期望目标：先比较节点实例ID，再比较所属 IEntity 引用。
    /// </summary>
    private static bool MatchesTarget(Node2D? expectedNode, Node2D actualTarget, IEntity? actualTargetEntity)
    {
        if (expectedNode == null)
        {
            return false;
        }

        // 同一 Godot 节点实例，直接匹配
        if (actualTarget.GetInstanceId() == expectedNode.GetInstanceId())
        {
            return true;
        }

        // 节点不同但可能属于同一 Entity（如子节点碰撞），比较 Entity 引用
        var expectedEntity = EntityManager.ResolveOwningIEntity(expectedNode);
        if (expectedEntity == null || actualTargetEntity == null)
        {
            return false;
        }

        return ReferenceEquals(expectedEntity, actualTargetEntity);
    }

    /// <summary>
    /// 生成碰撞去重键：优先使用 Entity 节点实例ID（保证同一 Entity 的不同子节点映射到同一键），
    /// 回退到碰撞节点自身的实例ID（如静态障碍物无 Entity）。
    /// </summary>
    private static ulong ResolveCollisionKey(Node2D target, IEntity? targetEntity)
    {
        if (targetEntity is Node entityNode)
        {
            return entityNode.GetInstanceId();
        }

        return target.GetInstanceId();
    }
}
