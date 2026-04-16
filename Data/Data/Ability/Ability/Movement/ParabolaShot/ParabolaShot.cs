using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 抛物线弹技能执行器 - 验证 Parabola 运动模式
/// 向最近敌人发射沿抛物线飞行的投射物（弓形向上）
/// </summary>
internal class ParabolaShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(ParabolaShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new ParabolaShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.ParabolaShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = GetCaster(context);
        var ability = GetAbility(context);
        var casterNode = GetCasterNode2D(context);

        var damage = GetScaledAbilityDamage(context);

        var targetPos = GetNearestEnemyPos(caster, casterNode);
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition,
            new ProjectileSpawnOptions(projectileScene, "ParabolaShotProjectile"));
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        float cachedDamage = damage;
        CastContext cachedContext = context;

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, cachedContext, cachedDamage));

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.Parabola,
                new MovementParams
                {
                    Mode = MoveMode.Parabola,
                    TargetPoint = targetPos,
                    ActionSpeed = 380f,
                    ParabolaApexHeight = 160f,
                    BowWorldUp = true,
                    DestroyOnComplete = true,
                    DestroyOnCollision = true,
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"抛物线弹: 目标={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetNearestEnemyPos(IEntity caster, Node2D casterNode)
    {
        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = casterNode.GlobalPosition,
            Range = 700f,
            CenterEntity = caster,
            TeamFilter = AbilityTargetTeamFilter.Enemy,
            Sorting = TargetSorting.Nearest,
            MaxTargets = 1
        };
        var targets = EntityTargetSelector.Query(query);
        if (targets.Count > 0 && targets[0] is Node2D t)
            return t.GlobalPosition;
        return casterNode.GlobalPosition + new Vector2(500f, 0f);
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
