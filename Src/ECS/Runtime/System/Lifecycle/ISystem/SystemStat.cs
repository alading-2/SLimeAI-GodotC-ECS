/// <summary>
/// 系统自定义统计信息。
/// <para>用于系统向外暴露运行时统计数据（如总伤害、活跃实体数等）。</para>
/// </summary>
public class SystemStat
{
    /// <summary>统计项名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>统计项值（字符串形式，便于显示）。</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>统计项分类（可选，用于分组显示）。</summary>
    public string Category { get; set; } = string.Empty;
}
