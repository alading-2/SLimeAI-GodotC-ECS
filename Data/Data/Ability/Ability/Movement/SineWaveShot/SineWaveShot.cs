using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 正弦波弹技能执行器 - 验证 SineWave 运动模式
/// 向最近敌人方向发射正弦波前进的投射物
/// </summary>
internal class SineWaveShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(SineWaveShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new SineWaveShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.SineWaveShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        // 确定射击方向：优先最近敌人，否则朝右
        var dir = GetShootDirection(caster, casterNode, ability.Data.Get<float>(DataKey.AbilityCastRange));
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            casterNode.GlobalPosition, // 生成位置
            projectileScene, // 投射物视觉
            "SineWaveShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision,
            (evt) => OnHit(evt, caster, casterNode, damage));

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted,
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.SineWave,
                new MovementParams
                {
                    Mode = MoveMode.SineWave,
                    Angle = Mathf.RadToDeg(dir.Angle()),
                    ActionSpeed = 350f,
                    WaveAmplitude = 60f,
                    WaveFrequency = 2f,
                    MaxDistance = 900f,
                    DestroyOnComplete = true,
                    Collision = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, //阵营过滤
                        EntityTypeFilter = EntityType.Unit, //实体类型过滤
                        StopAfterCollisionCount = 1, //首个有效碰撞停止
                        DestroyOnStop = true //停止后销毁
                    },
                    RotateToVelocity = true,
                }
            )
        );

        _log.Info($"正弦波弹: 方向={dir}, 角度={Mathf.RadToDeg(dir.Angle()):F1}°");
        return new AbilityExecutedResult { TargetsHit = 1 };
    }

    private static Vector2 GetShootDirection(IEntity caster, Node2D casterNode, float castRange)
    {
        float effectiveRange = castRange > 0f ? castRange : 600f; //查询半径
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
        if (targets.Count > 0 && targets[0] is Node2D targetNode)
            return (targetNode.GlobalPosition - casterNode.GlobalPosition).Normalized();
        return Vector2.Right;
    }

    private static void OnHit(GameEventType.Unit.MovementCollisionEventData evt,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (evt.TargetEntity == null) return;
        var targetEntity = evt.TargetEntity;
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
