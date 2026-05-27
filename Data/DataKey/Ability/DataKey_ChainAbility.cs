/// <summary>
/// 链式技能相关的 DataKey 定义
/// </summary>
public static partial class DataKey
{
    // ============ 链式技能 ============
    // 链式弹跳次数
    public static readonly DataKey<int> AbilityChainCount = DataRegistry.Register<int>(
        new DataMeta
        {
            Key = nameof(AbilityChainCount), DisplayName = "链式弹跳次数", Category = DataCategory_Ability.Effect,
            Type = typeof(int), DefaultValue = 1, MinValue = 1
        });

    // 链式弹跳范围（每跳的搜索半径）
    public static readonly DataKey<float> AbilityChainRange = DataRegistry.Register<float>(
        new DataMeta
        {
            Key = nameof(AbilityChainRange), DisplayName = "链式弹跳范围", Category = DataCategory_Ability.Effect,
            Type = typeof(float), DefaultValue = 300f, MinValue = 0
        });

    // 链式弹跳延时（秒）
    public static readonly DataKey<float> AbilityChainDelay = DataRegistry.Register<float>(
        new DataMeta
        {
            Key = nameof(AbilityChainDelay), DisplayName = "链式弹跳延时", Category = DataCategory_Ability.Effect,
            Type = typeof(float), DefaultValue = 0.2f, MinValue = 0
        });

    // 链式伤害衰减系数（0-100，100=无衰减）
    public static readonly DataKey<float> AbilityChainDamageDecay = DataRegistry.Register<float>(
        new DataMeta
        {
            Key = nameof(AbilityChainDamageDecay), DisplayName = "链式伤害衰减系数", Category = DataCategory_Ability.Effect,
            Type = typeof(float), DefaultValue = 100f, MinValue = 0, MaxValue = 100, IsPercentage = true
        });
}
