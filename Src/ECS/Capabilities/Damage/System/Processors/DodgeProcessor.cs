using Godot;

/// <summary>
/// 闪避处理器
/// <para>处理闪避逻辑，若闪避成功则伤害归零并终止后续大部分流程。</para>
/// </summary>
public class DodgeProcessor : IDamageProcessor
{
    private static readonly Log _log = new Log("DodgeProcessor");
    public int Priority { get; set; }

    public void Process(DamageInfo info)
    {
        if (info.Victim == null) return;

        // 真实伤害不可闪避
        if (info.Type == DamageType.True)
        {
            return;
        }

        float dodgeChance = info.Victim!.Data.Get<float>(GeneratedDataKey.DodgeChance);

        if (ProbabilityTool.RollPercent(dodgeChance))
        {
            // 闪避成功，伤害归零，结束伤害流程
            info.IsDodged = true;
            info.IsEnd = true;
            info.FinalDamage = 0;
            info.AddLog("闪避");
            _log.Debug($"目标 {info.Victim.Data.Get<string>(GeneratedDataKey.Name)} 触发了闪避! (几率: {dodgeChance}%)");

            // 发出闪避事件（供飘字系统显示 MISS）
            var dodgedData = new GameEventType.Unit.Dodged(info.Victim, info.Attacker as IEntity);
            info.Victim.Events.Emit(dodgedData);
            GlobalEventBus.Global.Emit(dodgedData);
        }
    }
}
