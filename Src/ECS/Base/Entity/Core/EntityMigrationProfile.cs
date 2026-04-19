using System;

/// <summary>
/// Entity 迁移 Profile。
/// <para>职责：定义一次迁移允许复制哪些 DataKey，以及哪些 DataKey 必须排除。</para>
/// <para>v1 只处理 Data 过滤，不承担事件、组件或整张关系图的复制策略。</para>
/// </summary>
public sealed class EntityMigrationProfile
{
    /// <summary>默认迁移 Profile：遵循 DataMeta.CanMigrate 与安全值类型过滤。</summary>
    public static EntityMigrationProfile Default { get; } = new();

    /// <summary>Profile 名称，用于调试与事件观测。</summary>
    public string Name { get; init; } = "Default";

    /// <summary>显式允许迁移的 DataKey 列表（可选）。</summary>
    public string[]? IncludeDataKeys { get; init; }

    /// <summary>显式排除的 DataKey 列表（可选）。</summary>
    public string[]? ExcludeDataKeys { get; init; }

    /// <summary>
    /// 检查指定 DataKey 是否在显式允许列表中。
    /// </summary>
    public bool Includes(string key) => Contains(IncludeDataKeys, key);

    /// <summary>
    /// 检查指定 DataKey 是否在显式排除列表中。
    /// </summary>
    public bool Excludes(string key) => Contains(ExcludeDataKeys, key);

    private static bool Contains(string[]? keys, string key)
    {
        if (keys == null)
        {
            return false;
        }

        foreach (string item in keys)
        {
            if (string.Equals(item, key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
