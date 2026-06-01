using Godot;
using System.Collections.Generic;

/// <summary>
/// Entity 迁移配置。
/// <para>v1 固定语义：生成目标实体 → 迁移受控 Data → 销毁源实体。</para>
/// </summary>
public readonly record struct EntityMigrationConfig
{
    /// <summary>
    /// 初始化默认值。
    /// </summary>
    public EntityMigrationConfig()
    {
        InheritDirectParent = true;
    }

    /// <summary>目标实体生成配置（必填）。</summary>
    public required EntitySpawnConfig TargetSpawn { get; init; }

    /// <summary>迁移 Profile（可选；为空时使用默认 Profile）。</summary>
    public EntityMigrationProfile? Profile { get; init; }

    /// <summary>迁移完成后额外覆写到目标实体的 Data（可选）。</summary>
    public Dictionary<string, object>? DataOverrides { get; init; }

    /// <summary>是否自动继承源实体的直接 PARENT 归属链（默认 true）。</summary>
    public bool InheritDirectParent { get; init; }
}
