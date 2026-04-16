using Godot;

/// <summary>
/// Ability 子域目标工具。
/// 只提供统一点选请求能力。
/// </summary>
internal static class AbilityTargetingTool
{
    private static readonly Log _log = new(nameof(AbilityTargetingTool));

    /// <summary>
    /// 请求进入统一点选流程。
    /// </summary>
    /// <param name="context">施法上下文。</param>
    /// <returns>固定返回 WaitingForTarget。</returns>
    public static TriggerResult RequestPointTarget(CastContext context)
    {
        if (context.Ability == null)
        {
            return TriggerResult.Failed;
        }

        var abilityName = context.Ability.Data.Get<string>(DataKey.Name);
        var range = context.Ability.Data.Get<float>(DataKey.AbilityCastRange);

        _log.Debug($"技能 {abilityName} 请求点选，射程: {range}");
        GlobalEventBus.Global.Emit(
            GameEventType.Targeting.StartTargeting,
            new GameEventType.Targeting.StartTargetingEventData(context) //施法上下文
        );
        return TriggerResult.WaitingForTarget;
    }
}
