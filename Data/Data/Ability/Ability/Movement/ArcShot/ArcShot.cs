using System.Runtime.CompilerServices;
using System.Collections.Generic;
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

    public override TriggerResult PrepareCast(CastContext context)
    {
        var ability = GetAbility(context);
        var casterNode = GetCasterNode2D(context);
        float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange); //索敌半径
        if (castRange <= 0f)
        {
            castRange = ability.Data.Get<float>(DataKey.AbilityEffectRadius); //回退半径
        }

        var targets = castRange > 0f
            ? EntityTargetSelector.Query(new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle, //查询形状
                Origin = casterNode.GlobalPosition, //查询中心
                Range = castRange, //查询半径
                CenterEntity = context.Caster, //中心实体
                TeamFilter = AbilityTargetTeamFilter.Enemy, //阵营过滤
                Sorting = TargetSorting.Nearest, //排序方式
                MaxTargets = 1 //最大目标数
            })
            : new List<IEntity>();
        if (targets.Count == 0)
        {
            _log.Debug("圆弧弹准备失败：未找到可用目标");
            return TriggerResult.Failed;
        }

        context.Targets = new List<IEntity>
        {
            targets[0]
        };
        return TriggerResult.Success;
    }

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
