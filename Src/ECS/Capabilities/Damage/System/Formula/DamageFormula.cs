using Godot;

/// <summary>
/// Damage capability 拥有的伤害公式。
/// <para>输入来自 Damage pipeline / Data，Math 层不直接读取 Entity 或 Data。</para>
/// </summary>
public static class DamageFormula
{
    /// <summary>
    /// 计算护甲造成的最终伤害倍率。
    /// <para>正护甲按全局护甲系数减伤并受最大减伤限制；负护甲保留当前线性增伤行为。</para>
    /// </summary>
    public static float CalculateArmorDamageMultiplier(float armor)
    {
        if (armor >= 0f)
        {
            float reductionRate = armor / (armor + GlobalConfig.ArmorCoefficient);
            reductionRate = Mathf.Clamp(reductionRate, 0f, GlobalConfig.MaxArmorReduction / 100f);
            return 1f - reductionRate;
        }

        const float negativeArmorCoefficient = 30f;
        float increaseRate = 1f + Mathf.Abs(armor) / negativeArmorCoefficient;
        return 1f + increaseRate;
    }
}
