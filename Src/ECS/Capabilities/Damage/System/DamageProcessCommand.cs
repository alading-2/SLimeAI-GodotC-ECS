/// <summary>
/// 伤害处理命令请求。
/// </summary>
/// <param name="Info">伤害上下文；为空时由 DamageService 返回未处理结果。</param>
public readonly record struct DamageProcessRequest(DamageInfo? Info);

/// <summary>
/// 伤害处理命令结果。
/// </summary>
/// <param name="Processed">是否进入并完成伤害管线。</param>
/// <param name="Message">领域处理说明。</param>
/// <param name="AppliedDamage">是否实际扣除了生命值。</param>
/// <param name="ActualDamage">实际扣除的生命值。</param>
/// <param name="FinalDamage">管线计算出的最终伤害。</param>
/// <param name="WasDodged">本次伤害是否被闪避。</param>
public readonly record struct DamageProcessResult(
    bool Processed,
    string Message,
    bool AppliedDamage = false,
    float ActualDamage = 0f,
    float FinalDamage = 0f,
    bool WasDodged = false);
