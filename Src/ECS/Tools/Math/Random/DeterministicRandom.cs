using Godot;

/// <summary>
/// 可复现随机源工厂。
/// <para>Math/TargetSelector/测试需要 deterministic replay 时从这里创建 Godot RNG。</para>
/// </summary>
public static class DeterministicRandom
{
    /// <summary>
    /// 默认可复现随机流。需要隔离序列的调用方应使用 Create(seed) 自行持有 RNG。
    /// </summary>
    public static RandomNumberGenerator Shared { get; } = Create(0);

    /// <summary>
    /// 用固定 seed 创建 Godot 随机数生成器。
    /// </summary>
    public static RandomNumberGenerator Create(ulong seed)
    {
        var rng = new RandomNumberGenerator();
        rng.Seed = seed;
        return rng;
    }

    /// <summary>
    /// 用 int seed 创建 Godot 随机数生成器，便于 gameplay 配置和测试直接传入。
    /// </summary>
    public static RandomNumberGenerator Create(int seed)
    {
        return Create(unchecked((ulong)(uint)seed));
    }
}
