using Godot;
using System;
/// <summary>
/// 防御处理器 (护甲/减伤)
/// <para>计算护甲减免。</para>
/// </summary>
public class DefenseProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("DefenseProcessor", LogLevel.Warning);
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {

        // if (info.Type == DamageType.True) return; // 真实伤害无视护甲
        float armor = info.Victim.Data.Get<float>(GeneratedDataKey.Armor);
        float originalDamage = info.FinalDamage;
        float multiplier = DamageFormula.CalculateArmorDamageMultiplier(armor);
        info.FinalDamage *= multiplier;
        _log.Debug($"[DefenseProcessor] armor={armor}, multiplier={multiplier:F4}, FinalDamage: {originalDamage} -> {info.FinalDamage}");

        if (armor >= 0)
        {
            info.AddLog($"护甲({armor}) 减少伤害： {(int)(originalDamage - info.FinalDamage)}");
        }
        else
        {
            info.AddLog($"护甲({armor}) 增加伤害： {(int)(originalDamage - info.FinalDamage)}");
        }
    }
}
