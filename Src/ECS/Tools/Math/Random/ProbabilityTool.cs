using Godot;

/// <summary>
/// 概率判定工具。
/// <para>概率单位统一为百分比 0-100；越界值按边界 clamp，避免调用点重复写保护。</para>
/// </summary>
public static class ProbabilityTool
{
    /// <summary>
    /// 用显式 RNG 判定百分比概率。
    /// </summary>
    public static bool RollPercent(float chancePercent, RandomNumberGenerator rng)
    {
        float chance = ClampPercent(chancePercent);
        if (chance <= 0f) return false;
        if (chance >= 100f) return true;
        return rng.Randf() * 100f < chance;
    }

    /// <summary>
    /// 用固定 seed 判定百分比概率，主要用于测试和 replay。
    /// </summary>
    public static bool RollPercent(float chancePercent, ulong seed)
    {
        return RollPercent(chancePercent, DeterministicRandom.Create(seed));
    }

    /// <summary>
    /// 默认 deterministic 路径。调用方需要不同序列时应传入 RNG 或 seed。
    /// </summary>
    public static bool RollPercent(float chancePercent)
    {
        return RollPercent(chancePercent, DeterministicRandom.Shared);
    }

    /// <summary>
    /// 将百分比概率限制到 [0, 100]。
    /// </summary>
    public static float ClampPercent(float chancePercent)
    {
        return Mathf.Clamp(chancePercent, 0f, 100f);
    }
}
