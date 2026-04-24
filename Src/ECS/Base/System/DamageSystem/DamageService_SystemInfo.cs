using System.Collections.Generic;

/// <summary>
/// DamageService 系统信息扩展。
/// </summary>
public partial class DamageService : ISystem
{
    // 统计数据
    private long _totalDamageDealt;
    private int _totalCritCount;
    private int _totalDodgeCount;
    private int _totalDamageEvents;

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(DamageService),
            CustomStats = new List<SystemStat>
            {
                new SystemStat
                {
                    Name = "总伤害",
                    Value = _totalDamageDealt.ToString(),
                    Category = "统计"
                },
                new SystemStat
                {
                    Name = "暴击次数",
                    Value = _totalCritCount.ToString(),
                    Category = "统计"
                },
                new SystemStat
                {
                    Name = "闪避次数",
                    Value = _totalDodgeCount.ToString(),
                    Category = "统计"
                },
                new SystemStat
                {
                    Name = "伤害事件数",
                    Value = _totalDamageEvents.ToString(),
                    Category = "统计"
                },
                new SystemStat
                {
                    Name = "已注册处理器",
                    Value = _processors.Count.ToString(),
                    Category = "配置"
                }
            }
        };
    }

    public void OnProjectStateChanged(ProjectStateChangedEventArgs args)
    {
        // DamageService 不需要响应项目状态变化
    }

    /// <summary>
    /// 记录伤害统计数据（由 StatisticsProcessor 调用）。
    /// </summary>
    public void RecordDamageEvent(DamageInfo info)
    {
        _totalDamageEvents++;
        _totalDamageDealt += (long)info.FinalDamage;

        if (info.IsCritical)
        {
            _totalCritCount++;
        }

        if (info.IsDodged)
        {
            _totalDodgeCount++;
        }
    }

    /// <summary>
    /// 重置统计数据。
    /// </summary>
    public void ResetStatistics()
    {
        _totalDamageDealt = 0;
        _totalCritCount = 0;
        _totalDodgeCount = 0;
        _totalDamageEvents = 0;
    }
}
