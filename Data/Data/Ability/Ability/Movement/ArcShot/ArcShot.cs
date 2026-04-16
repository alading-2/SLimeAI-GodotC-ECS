using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 圆弧弹技能执行器 - 验证 CircularArc 运动模式
/// 向 AbilitySystem 预选中的敌人发射沿圆弧轨迹跟踪飞行的投射物
/// </summary>
internal class ArcShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(ArcShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new ArcShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.ArcShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var casterNode = GetCasterNode2D(context);
        var ability = GetAbility(context);
        var targetNode = GetFirstTargetNode2D(context);

        var damage = GetScaledAbilityDamage(context);

        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition, // 生成位置
            new ProjectileSpawnOptions(projectileScene, "ArcShotProjectile")); // 投射物配置
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        float cachedDamage = damage;
        CastContext cachedContext = context;

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, cachedContext, cachedDamage));

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.CircularArc,
                new MovementParams
                {
                    Mode = MoveMode.CircularArc,
                    TargetPoint = targetNode.GlobalPosition,
                    TargetNode = targetNode,
                    isTrackTarget = true,
                    ActionSpeed = 360f,
                    CircularArcRadius = 220f,
                    CircularArcClockwise = true,
                    BowWorldUp = true,
                    DestroyOnComplete = true,
                    DestroyOnCollision = true,
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"圆弧弹: 跟踪目标={targetNode.Name}, 初始位置={targetNode.GlobalPosition}");
        return new AbilityExecutedResult { TargetsHit = 1 };
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
