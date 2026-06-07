/// <summary>
/// Ability 子域接入 FeatureSystem 的统一中转处理器。
/// 只负责把通用 FeatureContext 转为 CastContext，并把执行阶段转交给具体技能逻辑。
/// </summary>
internal abstract class AbilityFeatureHandler : IFeatureHandler
{
    private static readonly Log _log = new(nameof(AbilityFeatureHandler));

    /// <summary>完整唯一 FeatureHandlerId，对应 AbilityData.FeatureHandlerId。</summary>
    public abstract string FeatureId { get; }

    /// <summary>
    /// FeatureSystem 调用入口。
    /// </summary>
    /// <param name="featureContext">Feature 运行上下文。</param>
    public void OnExecute(FeatureContext featureContext)
    {
        if (!featureContext.TryGetActivation<CastContext>(out var context))
        {
            _log.Error("AbilityFeatureHandler.OnExecute: ActivationPayload 不是 CastContext");
            return;
        }

        featureContext.SetExecutionResult(ExecuteAbility(context));
    }

    /// <summary>
    /// 执行具体技能逻辑。
    /// </summary>
    /// <param name="context">施法上下文。</param>
    /// <returns>技能执行结果。</returns>
    protected abstract AbilityExecutedResult ExecuteAbility(CastContext context);
}
