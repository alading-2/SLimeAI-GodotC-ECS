/// <summary>
/// Ability capability 拥有的冷却、充能等公式。
/// </summary>
public static class AbilityFormula
{
    /// <summary>
    /// 计算冷却/充能的最终时间。
    /// <para>baseTime 单位为秒，reductionPercent 单位为百分比 0-100。</para>
    /// </summary>
    public static float CalculateFinalCooldownTime(float baseTime, float reductionPercent)
    {
        return baseTime * (1f - reductionPercent / 100f);
    }
}
