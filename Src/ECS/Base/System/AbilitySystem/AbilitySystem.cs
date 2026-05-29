using Godot;

/// <summary>
/// 技能系统 - 管理技能激活和执行逻辑
/// 
/// 职责：
/// - 接收 TryTrigger 请求（统一施法入口）
/// - 激活技能（就绪检查 → 消耗 → 冷却 → 执行）
/// - 通过 FeatureSystem / IFeatureHandler.OnExecute 执行具体技能逻辑并回传 AbilityExecutedResult
/// 
/// 注意：技能的增删查由 EntityManager.AddAbility/RemoveAbility/GetAbilities 负责
/// </summary>
public static class AbilitySystem
{
    private static readonly Log _log = new(nameof(AbilitySystem));

    // ==================== TryTrigger 入口 ====================

    /// <summary>
    /// 处理 TryTrigger 事件 - 统一施法入口
    /// </summary>
    public static void HandleTryTrigger(GameEventType.Ability.TryTrigger eventData)
    {
        var context = eventData.Context;
        var resultContext = context.ResponseContext;

        if (context.Ability == null)
        {
            _log.Debug("TryTrigger 失败: Ability 为空");
            resultContext?.SetResult(TriggerResult.Failed);
            return;
        }

        var result = TryTriggerAbilityWithContext(context);
        resultContext?.SetResult(result);
    }

    /// <summary>
    /// 使用施法上下文触发技能（统一流水线入口）
    /// </summary>
    /// <returns>触发结果：Success / Failed</returns>
    private static TriggerResult TryTriggerAbilityWithContext(CastContext abilityContext)
    {
        if (abilityContext.Ability == null || abilityContext.Caster == null) return TriggerResult.Failed;

        // 拦截已死亡角色的技能请求，防止周期性光环等技能死后继续触发新一轮伤害判定。
        if (abilityContext.Caster != null && abilityContext.Caster.Data.Get<bool>(GeneratedDataKey.IsDead))
        {
            _log.Debug($"技能触发失败: 施法者已阵亡");
            return TriggerResult.Failed;
        }

        var ability = abilityContext.Ability;

        // 事件驱动：就绪检查
        if (!CanUseAbility(ability))
        {
            return TriggerResult.Failed;
        }

        // ==================== 资源消耗阶段 ====================
        var consumeContext = new EventContext();
        // 事件驱动：请求消耗资源（充能等）
        if (ability.Data.Get<bool>(GeneratedDataKey.IsAbilityUsesCharges))
        {
            ability.Events.Emit(
                new GameEventType.Ability.ConsumeCharge(consumeContext)
            );
        }

        // 检查消耗是否成功
        if (!consumeContext.Success)
        {
            _log.Debug($"消耗资源失败: {consumeContext.FailReason}");
            return TriggerResult.Failed;
        }

        // 事件驱动:请求启动冷却
        // 注意：如果是 Periodic (周期性) 技能，由 TriggerComponent 负责循环控制频率，
        // 这里不应该再启动 CooldownComponent 的冷却计时，否则会导致 TriggerComponent 下次循环时
        // 技能还在冷却中 (Race Condition) 或 刚结束冷却但 CanUse 检查失败。
        // 因此：周期性技能跳过冷却启动。
        var triggerMode = ability.Data.Get<AbilityTriggerMode>(GeneratedDataKey.AbilityTriggerMode);
        if (!triggerMode.HasFlag(AbilityTriggerMode.Periodic))
        {
            ability.Events.Emit(
                new GameEventType.Ability.StartCooldown()
            );
        }

        // ==================== 消耗阶段 ====================
        // 事件驱动:请求消耗成本 (魔法/能量等)
        var costContext = new EventContext();
        ability.Events.Emit(
            new GameEventType.Ability.ConsumeCost(costContext)
        );

        if (!costContext.Success)
        {
            _log.Debug($"消耗成本失败: {costContext.FailReason}");
            return TriggerResult.Failed;
        }

        // 标记为执行中
        ability.Data.Set(GeneratedDataKey.FeatureIsActive, true);

        // 发送激活事件，技能UI使用
        ability.Events.Emit(
            new GameEventType.Ability.Activated(abilityContext)
        );

        // Feature 生命周期钩子：Activated（AbilitySystem 负责构建 FeatureContext，将 CastContext 存入 ActivationData）
        var featureCtx = new FeatureContext
        {
            Owner = abilityContext.Caster,
            Feature = ability,
            ActivationData = abilityContext,
            Source = abilityContext.SourceEventData
        };
        FeatureSystem.OnFeatureActivated(featureCtx);

        EmitAbilityExecutedEvent(abilityContext, featureCtx);

        // Feature 生命周期钩子：Ended（同步技能同帧完成；异步/引导型能力后续可延迟调用）
        FeatureSystem.OnFeatureEnded(featureCtx, FeatureEndReason.Completed);

        var name = ability.Data.Get<string>(GeneratedDataKey.Name);
        _log.Debug($"激活技能: {name}");
        return TriggerResult.Success;
    }

    // ==================== 就绪检查 ====================

    /// <summary>
    /// 检查技能是否可用
    /// </summary>
    public static bool CanUseAbility(AbilityEntity ability)
    {
        if (ability == null) return false;

        var abilityName = ability.Data.Get<string>(GeneratedDataKey.Name);
        var isEnabled = ability.Data.Get<bool>(GeneratedDataKey.FeatureEnabled);
        var isActive = ability.Data.Get<bool>(GeneratedDataKey.FeatureIsActive);

        // 检查启用状态
        if (!isEnabled)
        {
            _log.Debug($"技能 {abilityName} 未启用");
            return false;
        }

        // 检查Feature是否激活，也就是技能是否在执行中
        if (isActive)
        {
            _log.Debug($"技能 {abilityName} 正在执行中");
            return false;
        }

        // 事件驱动：请求检查可用性（冷却、充能等组件响应）
        var context = new EventContext();
        ability.Events.Emit(
            new GameEventType.Ability.CheckCanUse(context)
        );

        if (!context.Success)
        {
            _log.Debug($"技能 {abilityName} 不可用: {context.FailReason}");
            return false;
        }

        return true;
    }

    // ==================== 效果执行 ====================

    /// <summary>
    /// 发送技能执行完成事件 - 结果由 IFeatureHandler.OnExecute 写入 FeatureContext.ExecuteResult
    /// </summary>
    private static void EmitAbilityExecutedEvent(CastContext context, FeatureContext featureCtx)
    {
        if (context.Ability == null) return;

        var ability = context.Ability;
        var abilityName = ability.Data.Get<string>(GeneratedDataKey.Name) ?? string.Empty;
        var handlerId = ability.Data.Get<string>(GeneratedDataKey.FeatureHandlerId);
        AbilityExecutedResult result;

        if (featureCtx.ExecuteResult is AbilityExecutedResult executedResult)
        {
            result = executedResult;
        }
        else
        {
            if (string.IsNullOrEmpty(handlerId))
            {
                _log.Error($"技能 {abilityName} 未配置 FeatureHandlerId，执行结果将回退为默认值");
            }
            else if (!FeatureHandlerRegistry.HasHandler(handlerId))
            {
                _log.Warn($"技能 {abilityName} 未注册 FeatureHandler: {handlerId}，执行结果将回退为默认值");
            }
            else
            {
                _log.Warn($"技能 {abilityName} 的 FeatureHandler 未写入 AbilityExecutedResult，执行结果将回退为默认值");
            }

            result = new AbilityExecutedResult
            {
                TargetsHit = context.Targets?.Count ?? 0
            };
        }

        _log.Debug($"[AbilitySystem] 技能效果执行完成: '{abilityName}', 命中: {result.TargetsHit}");

        // 发送执行完成事件
        ability.Events.Emit(
            new GameEventType.Ability.Executed(result)
        );
    }
}
