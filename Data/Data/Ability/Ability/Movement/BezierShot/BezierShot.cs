using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 贝塞尔曲线弹技能执行器 - 验证 BezierCurve 运动模式
/// 向最近敌人发射沿二次贝塞尔曲线飞行的投射物（弓形抛射）
/// </summary>
internal class BezierShotExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(BezierShotExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new BezierShotExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Projectile.BezierShot;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        var casterNode = (Node2D)caster;

        var damage = ability.Data.Get<float>(DataKey.FinalAbilityDamage); // 最终技能伤害

        // 查找最近敌人作为终点
        var targetPos = GetNearestEnemyPos(caster, casterNode, ability.Data.Get<float>(DataKey.AbilityCastRange));
        var startPos = casterNode.GlobalPosition;
        var midPoint = (startPos + targetPos) / 2f;
        var controlPoint = midPoint + new Vector2(0f, -180f);
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        var projectile = ProjectileTool.Spawn(
            startPos, // 生成位置
            projectileScene, // 投射物视觉
            "BezierShotProjectile" // 投射物名称
        );
        if (projectile == null) return new AbilityExecutedResult { TargetsHit = 0 };

        projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
            GameEventType.Unit.MovementCollision, // 碰撞事件
            (evt) => OnHit(evt, caster, casterNode, damage)); // 碰撞回调

        projectile.Events.Emit(
            GameEventType.Unit.MovementStarted, // 开始移动事件
            new GameEventType.Unit.MovementStartedEventData(
                MoveMode.BezierCurve, // 移动模式：贝塞尔曲线
                new MovementParams
                {
                    Mode = MoveMode.BezierCurve, // 移动模式
                    BezierPoints = new Vector2[] { startPos, controlPoint, targetPos }, // 控制点数组
                    ActionSpeed = 420f, // 移动速度
                    DestroyOnComplete = true, // 到达后销毁
                    DestroyOnCollision = true, // 碰撞后销毁
                    RotateToVelocity = true, // 旋转朝向速度
                }
            )
        );

        _log.Info($"贝塞尔弹: 起={startPos}, 终={targetPos}");
        return new AbilityExecutedResult { TargetsHit = 1 }; // 返回命中结果
    }

    private static Vector2 GetNearestEnemyPos(IEntity caster, Node2D casterNode, float castRange)
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
        if (targets.Count > 0 && targets[0] is Node2D t)
            return t.GlobalPosition;
        return casterNode.GlobalPosition + new Vector2(400f, 0f); // 无目标时返回前方位置
    }

    private static void OnHit(GameEventType.Unit.MovementCollisionEventData evt,
        IEntity caster,
        Node2D casterNode,
        float damage)
    {
        if (evt.Target is not IEntity targetEntity) return;
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
