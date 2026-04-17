using System.Runtime.CompilerServices;
using Godot;

/// <summary>
/// 环绕护盾技能执行器 - 验证 Orbit 运动模式
/// 生成多个投射物围绕玩家旋转，碰撞敌人时造成伤害
/// </summary>
internal class OrbitSkillExecutor : AbilityFeatureHandler
{
    private static readonly Log _log = new(nameof(OrbitSkillExecutor));

    [ModuleInitializer]
    public static void Initialize()
    {
        FeatureHandlerRegistry.Register(new OrbitSkillExecutor());
    }

    public override string FeatureId => global::FeatureId.Ability.Passive.OrbitSkill;

    protected override AbilityExecutedResult ExecuteAbility(CastContext context)
    {
        var caster = context.Caster!;
        var ability = context.Ability!;
        if (caster is not Node2D casterNode)
        {
            _log.Warn("环绕护盾施法失败：施法者不是 Node2D");
            return new AbilityExecutedResult();
        }

        var damage = ability.Data.Get<float>(DataKey.AbilityDamage) // 技能基础伤害
            * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f; // 施法者技能伤害倍率
        var orbitCount = 3;
        var orbitRadius = 100f;
        var orbitDuration = 6f;
        var projectileScene = ability.Data.Get<PackedScene>(DataKey.ProjectileScene);

        for (int i = 0; i < orbitCount; i++)
        {
            float initAngle = i * (360f / orbitCount);
            var projectile = ProjectileTool.Spawn(
                casterNode.GlobalPosition,
                new ProjectileSpawnOptions(projectileScene, "OrbitSkillProjectile"));
            if (projectile == null) continue;

            projectile.Events.On<GameEventType.Unit.MovementCollisionEventData>(
                GameEventType.Unit.MovementCollision,
                (evt) => OnHit(evt, caster, casterNode, damage));

            projectile.Events.Emit(
                GameEventType.Unit.MovementStarted,
                new GameEventType.Unit.MovementStartedEventData(
                    MoveMode.Orbit,
                    new MovementParams
                    {
                        Mode = MoveMode.Orbit,
                        TargetNode = casterNode,
                        OrbitRadius = orbitRadius,
                        OrbitInitAngle = initAngle,
                        OrbitAngularSpeed = 180f,
                        IsOrbitClockwise = true,
                        MaxDuration = orbitDuration,
                        DestroyOnComplete = true,
                        DestroyOnCollision = false,
                        RotateToVelocity = false,
                    }
                )
            );
        }

        _log.Info($"环绕护盾: 生成 {orbitCount} 个轨道投射物");
        return new AbilityExecutedResult { TargetsHit = orbitCount };
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
                Type = DamageType.Magical, // 伤害类型
                Tags = DamageTags.Area | DamageTags.Ability, // 伤害标签
                Attacker = casterNode // 伤害来源
            }
        });
    }
}
