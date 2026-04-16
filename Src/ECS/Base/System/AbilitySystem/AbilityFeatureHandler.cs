using Godot;

/// <summary>
/// Ability 子域接入 FeatureSystem 的统一中转处理器。
/// <para>
/// 负责把通用 FeatureContext 转为 CastContext，并集中断言 AbilitySystem 已经保证的运行时不变量。
/// 具体技能只实现 ExecuteAbility，不再重复读取 ActivationData 或判断 Caster / Ability 是否为空。
/// </para>
/// </summary>
internal abstract class AbilityFeatureHandler : IFeatureHandler
{
    /// <summary>完整唯一 FeatureHandlerId，对应 AbilityConfig.FeatureHandlerId。</summary>
    public abstract string FeatureId { get; }

    /// <summary>
    /// FeatureSystem 调用入口。AbilitySystem 已在进入 FeatureSystem 前校验 CastContext。
    /// </summary>
    /// <param name="featureContext">Feature 运行上下文。</param>
    /// <returns>技能执行结果。</returns>
    public object? OnExecute(FeatureContext featureContext)
    {
        var context = featureContext.GetActivationData<CastContext>();

        // 集中校验 Ability 子域不变量；失败代表接入链路或配置错误，应尽早暴露。
        _ = GetCaster(context);
        _ = GetAbility(context);

        return ExecuteAbility(context);
    }

    /// <summary>
    /// 执行前置施法准备。
    /// 只允许做“是否继续 / 是否等待点选 / 是否直接失败”的决定，不做任何资源消耗。
    /// </summary>
    /// <param name="context">施法上下文。</param>
    /// <returns>继续执行、失败或等待点选。</returns>
    public virtual TriggerResult PrepareCast(CastContext context)
    {
        _ = GetCaster(context);
        _ = GetAbility(context);
        return TriggerResult.Success;
    }

    /// <summary>
    /// 执行具体技能逻辑。
    /// </summary>
    /// <param name="context">施法上下文。</param>
    /// <returns>技能执行结果。</returns>
    protected abstract AbilityExecutedResult ExecuteAbility(CastContext context);

    /// <summary>获取已校验的施法者。</summary>
    protected static IEntity GetCaster(CastContext context)
    {
        return context.Caster
            ?? throw new System.InvalidOperationException("Ability CastContext 缺少 Caster");
    }

    /// <summary>获取已校验的技能实体。</summary>
    protected static AbilityEntity GetAbility(CastContext context)
    {
        return context.Ability
            ?? throw new System.InvalidOperationException("Ability CastContext 缺少 Ability");
    }

    /// <summary>获取要求具备 2D 坐标的施法者节点。</summary>
    protected static Node2D GetCasterNode2D(CastContext context)
    {
        var caster = GetCaster(context);
        return caster as Node2D
            ?? throw new System.InvalidOperationException($"技能 {GetAbility(context).Data.Get<string>(DataKey.Name)} 的施法者必须是 Node2D");
    }

    /// <summary>获取第一个目标实体。需要目标的技能应在 PrepareCast 中先保证目标已写入上下文。</summary>
    protected static IEntity GetFirstTarget(CastContext context)
    {
        if (context.Targets == null || context.Targets.Count == 0 || context.Targets[0] == null)
        {
            throw new System.InvalidOperationException($"技能 {GetAbility(context).Data.Get<string>(DataKey.Name)} 缺少目标");
        }

        return context.Targets[0];
    }

    /// <summary>获取第一个 Node2D 目标实体。</summary>
    protected static Node2D GetFirstTargetNode2D(CastContext context)
    {
        var target = GetFirstTarget(context);
        return target as Node2D
            ?? throw new System.InvalidOperationException($"技能 {GetAbility(context).Data.Get<string>(DataKey.Name)} 的目标必须是 Node2D");
    }

    /// <summary>请求进入统一点选流程。</summary>
    protected static TriggerResult RequestPointTarget(CastContext context)
    {
        return AbilityTargetingTool.RequestPointTarget(context);
    }

    /// <summary>按施法者技能伤害倍率计算最终技能伤害。</summary>
    protected static float GetScaledAbilityDamage(CastContext context)
    {
        var caster = GetCaster(context);
        var ability = GetAbility(context);
        return ability.Data.Get<float>(DataKey.AbilityDamage)
            * caster.Data.Get<float>(DataKey.AbilityDamageBonus) / 100f;
    }

    /// <summary>
    /// 对运动碰撞目标造成技能伤害。未显式指定阵营时默认只打敌人。
    /// </summary>
    protected static bool ApplyCollisionDamage(
        CastContext context,
        GameEventType.Unit.MovementCollisionEventData evt,
        float damage,
        DamageType type,
        DamageTags tags,
        AbilityTargetTeamFilter teamFilter = AbilityTargetTeamFilter.None)
    {
        if (evt.Target is not IEntity targetEntity || evt.Target is not IUnit victim) return false;

        var caster = GetCaster(context);
        var effectiveFilter = teamFilter != AbilityTargetTeamFilter.None
            ? teamFilter
            : AbilityTargetTeamFilter.Enemy;

        if (!PassTeamFilter(targetEntity, caster, effectiveFilter)) return false;

        DamageService.Instance.Process(new DamageInfo
        {
            Attacker = GetAttackerNode(context), // 直接伤害来源
            Victim = victim, // 受害单位
            Damage = damage, // 结算前伤害
            Type = type, // 伤害类型
            Tags = tags // 伤害标签
        });

        return true;
    }

    /// <summary>获取可作为 DamageInfo.Attacker 的节点。</summary>
    protected static Node GetAttackerNode(CastContext context)
    {
        var caster = GetCaster(context);
        return caster as Node
            ?? throw new System.InvalidOperationException("技能施法者必须是 Godot Node 才能作为伤害来源");
    }

    /// <summary>按 TargetSelector 的阵营语义判断目标是否允许命中。</summary>
    private static bool PassTeamFilter(IEntity target, IEntity center, AbilityTargetTeamFilter filter)
    {
        if (filter == AbilityTargetTeamFilter.None || filter == AbilityTargetTeamFilter.All) return true;

        bool isSelf = IsSameEntity(target, center);
        if (isSelf) return filter.HasFlag(AbilityTargetTeamFilter.Self);

        Team targetTeam = target.Data.Get<Team>(DataKey.Team);
        if (targetTeam == Team.Neutral) return filter.HasFlag(AbilityTargetTeamFilter.Neutral);

        Team centerTeam = center.Data.Get<Team>(DataKey.Team);
        bool isSameTeam = centerTeam == targetTeam;
        if (isSameTeam) return filter.HasFlag(AbilityTargetTeamFilter.Friendly);
        return filter.HasFlag(AbilityTargetTeamFilter.Enemy);
    }

    /// <summary>判断两个 IEntity 是否指向同一个实体。</summary>
    private static bool IsSameEntity(IEntity a, IEntity b)
    {
        if (ReferenceEquals(a, b)) return true;

        var aId = a.Data.Get<string>(DataKey.Id);
        var bId = b.Data.Get<string>(DataKey.Id);
        return !string.IsNullOrEmpty(aId) && aId == bId;
    }
}
