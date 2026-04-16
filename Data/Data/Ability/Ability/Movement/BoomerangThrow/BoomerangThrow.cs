using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 回旋镖技能执行器 - 验证 Boomerang 运动模式
/// 向最近敌人投掷回旋镖，飞出后自动返回，过程中碰撞造成伤害
/// </summary>
internal class BoomerangThrowExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(BoomerangThrowExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BoomerangThrowExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BoomerangThrow;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = GetCaster(context);
        var ability = GetAbility(context);
        var casterNode = GetCasterNode2D(context);

        var damage = GetScaledAbilityDamage(context);

        var throwTarget = GetThrowTarget(caster, casterNode, ability.Data.Get<float>(DataKey.AbilityCastRange));
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition,
            new ProjectileSpawnOptions(projectileScene, "BoomerangThrowProjectile"));
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        float cachedDamage = damage;
        CastContext cachedContext = context;

        // 回旋镖不销毁于碰撞，继续飞行并返回
        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, cachedContext, cachedDamage));

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.Boomerang,
                new MovementParams
                {
                    Mode = MoveMode.Boomerang,
                    TargetPoint = throwTarget,
                    TargetNode = casterNode, // 显式指定返程宿主，避免策略退回到不可靠的祖先回溯。
                    ActionSpeed = 460f,
                    BoomerangArcHeight = 26f,
                    BoomerangPauseTime = 0.08f,
                    BoomerangIsClockwise = true,
                    BoomerangReturnSpeedMultiplier = 1.2f,
                    DestroyOnComplete = true,
                    DestroyOnCollision = false,
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"回旋镖投掷: 目标={throwTarget}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetThrowTarget(IEntity caster, Node2D casterNode, float castRange)
    {
        float effectiveRange = castRange > 0f ? castRange : 600f; //查询半径
        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, //查询形状
            Origin = casterNode.GlobalPosition, //查询中心
            Range = effectiveRange, //查询半径
            CenterEntity = caster, //中心实体
            TeamFilter = AbilityTargetTeamFilter.Enemy, //阵营过滤
            Sorting = TargetSorting.Nearest, //排序方式
            MaxTargets = 1 //最大目标数
        });
        if (targets.Count > 0 && targets[0] is Node2D t)
            return t.GlobalPosition;
        return casterNode.GlobalPosition + new Vector2(280f, 0f);
    }

    private static void OnHit(GameEventType.Unit.MovementCollisionEventData evt, CastContext context, float damage)
    {
        ApplyCollisionDamage(
            context, // 施法上下文
            evt, // 碰撞事件
            damage, // 伤害值
            DamageType.Physical, // 伤害类型
            DamageTags.Ability | DamageTags.Ranged, // 伤害标签
            AbilityTargetTeamFilter.Enemy); // 仅命中敌方
    }
}
