/// <summary>
/// 技能触发结果 - 统一表示 TryTriggerAbility 的返回状态
/// </summary>
public enum TriggerResult
{
    /// <summary>技能立即执行成功（同步流水线完成）</summary>
    Success,

    /// <summary>触发失败（就绪检查不通过、资源消耗失败等）</summary>
    Failed,
}
