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
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        var targetPos = GetNearestEnemyPos(caster, casterNode, ability.Data.Get<float>(DataKey.AbilityCastRange));
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition, // 生成位置
            projectileScene, // 投射物视觉
            "ParabolaShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

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
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, //阵营过滤
                        EntityTypeFilter = EntityType.Unit, //实体类型过滤
                        StopAfterCollisionCount = 1, //首个有效碰撞停止
                        DestroyOnStop = true, //停止后销毁
                        OnCollision = collisionCtx => OnHit(collisionCtx, caster, casterNode, damage) //命中回调
                    },
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"抛物线弹: 目标={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetNearestEnemyPos(IEntity caster, Node2D casterNode, float castRange)
    {
        float effectiveRange = castRange > 0f ? castRange : 700f; //查询半径
        var targets = EntityTargetSelector.Query(new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle, //查询形状
            Origin = casterNode.GlobalPosition, //查询中心
            Range = effectiveRange, //查询半径
            CenterEntity = caster, //中心实体
            TeamFilter = TeamFilter.Enemy, //阵营过滤
            Sorting = TargetSorting.Nearest, //排序方式
            MaxTargets = 1 //最大目标数
        });
        if (targets.Count > 0 && targets[0] is Node2D t)
            return t.GlobalPosition;
        return casterNode.GlobalPosition + new Vector2(500f, 0f);
    }

    private static void OnHit(MovementCollisionContext collisionCtx,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (collisionCtx.TargetEntity == null) return;
        var targetEntity = collisionCtx.TargetEntity;
        if (!AbilityTool.MatchesTeamFilter(caster, targetEntity, TeamFilter.Enemy)) return;

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
