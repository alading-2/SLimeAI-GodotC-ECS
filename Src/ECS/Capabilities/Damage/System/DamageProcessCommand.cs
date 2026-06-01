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
public readonly record struct DamageProcessResult(bool Processed, string Message);
