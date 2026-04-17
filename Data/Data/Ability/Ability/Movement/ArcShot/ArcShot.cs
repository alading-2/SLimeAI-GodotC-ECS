using System.Runtime.CompilerServices;
using System.Collections.Generic;
using Godot;

/// <summary>
/// 圆弧弹技能执行器 - 验证 CircularArc 运动模式
/// 在执行阶段自行选择最近敌人，并发射沿圆弧轨迹跟踪飞行的投射物。
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
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;
        var target = FindTarget(caster, ability, casterNode);
        if (target is not Node2D targetNode)
        {
            _log.Debug("圆弧弹执行跳过：未找到可用目标");
            return new AbilityExecutedResult { TargetsHit = 0 };
        }

        context.Targets = new List<IEntity>
        {
            target
        };

        var damage = ability.Data.Get<float>(DataKey.AbilityDamage) // 技能基础伤害
            * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f; // 施法者技能伤害倍率

        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition, // 生成位置
            new ProjectileSpawnOptions(projectileScene, "ArcShotProjectile")); // 投射物配置
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, caster, casterNode, damage));

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

    /// <summary>
    /// 在技能执行阶段选择本次追踪目标；无目标时让技能打空而不是阻断流水线。
    /// </summary>
    /// <param name="caster">施法实体。</param>
    /// <param name="ability">技能实体。</param>
    /// <param name="casterNode">施法者节点。</param>
    /// <returns>最近敌方目标，找不到则返回 null。</returns>
    private static IEntity? FindTarget(IEntity caster, AbilityEntity ability, Node2D casterNode)
    {
        float castRange = ability.Data.Get<float>(DataKey.AbilityCastRange); //索敌半径
        if (castRange <= 0f)
        {
            castRange = ability.Data.Get<float>(DataKey.AbilityEffectRadius); //回退半径
        }

        if (castRange <= 0f) return null;

        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, //查询形状
            Origin = casterNode.GlobalPosition, //查询中心
            Range = castRange, //查询半径
            CenterEntity = caster, //中心实体
            TeamFilter = AbilityTargetTeamFilter.Enemy, //阵营过滤
            Sorting = TargetSorting.Nearest, //排序方式
            MaxTargets = 1 //最大目标数
        });

        return targets.Count > 0 ? targets[0] : null;
    }

    private static void OnHit(GameEventType.Unit.MovementCollisionEventData evt, IEntity caster, Node2D casterNode, float damage)
    {
        if (evt.Target is not IEntity targetEntity) return;
        if (!AbilityTool.MatchesTeamFilter(caster, targetEntity, AbilityTargetTeamFilter.Enemy)) return;

        AbilityImpactTool.Execute(caster, new AbilityImpactOptions
        {
            Targets = new[] { targetEntity },
            Damage = new DamageApplyOptions
            {
                Damage = damage, // 伤害值
                Type = DamageType.Physical, // 伤害类型
                Tags = DamageTags.Ability | DamageTags.Ranged, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }
}
