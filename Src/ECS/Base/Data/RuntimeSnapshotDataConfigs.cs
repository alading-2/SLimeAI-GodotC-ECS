using System;
using System.Collections.Generic;

/// <summary>
/// DataOS snapshot 运行时实体配置读取器。
/// </summary>
public static class RuntimeSnapshotDataConfigs
{
    public const string AbilityTable = "ability";
    public const string EnemyTable = "unit.enemy";
    public const string PlayerTable = "unit.player";
    public const string TargetingIndicatorTable = "unit.targeting_indicator";
    public const string TargetingIndicatorId = "unit.targeting_indicator";

    public static IReadOnlyList<RuntimeAbilityConfig> LoadAbilities()
    {
        var ids = SnapshotLoader.GetRecordIds(SnapshotLoader.DefaultSnapshotPath, AbilityTable);
        var result = new List<RuntimeAbilityConfig>(ids.Count);
        foreach (var id in ids)
        {
            result.Add(LoadAbility(id));
        }
        return result;
    }

    public static RuntimeAbilityConfig LoadAbility(string snapshotId)
    {
        var data = LoadData(AbilityTable, snapshotId);
        return new RuntimeAbilityConfig
        {
            SnapshotId = snapshotId,
            Name = data.Get(DataKey.Name),
            Description = data.Get(DataKey.Description),
            FeatureGroupId = data.Get(DataKey.AbilityFeatureGroup),
            FeatureHandlerId = data.Get(DataKey.FeatureHandlerId),
            AbilityType = data.Get(DataKey.AbilityType),
            AbilityTriggerMode = data.Get(DataKey.AbilityTriggerMode),
        };
    }

    public static IReadOnlyList<RuntimeEnemyConfig> LoadEnemies()
    {
        var ids = SnapshotLoader.GetRecordIds(SnapshotLoader.DefaultSnapshotPath, EnemyTable);
        var result = new List<RuntimeEnemyConfig>(ids.Count);
        foreach (var id in ids)
        {
            result.Add(LoadEnemy(id));
        }
        return result;
    }

    public static RuntimeEnemyConfig LoadEnemy(string snapshotId)
    {
        var data = LoadData(EnemyTable, snapshotId);
        return new RuntimeEnemyConfig
        {
            SnapshotId = snapshotId,
            Name = data.Get(DataKey.Name),
            IsEnableSpawnRule = data.Get(DataKey.IsEnableSpawnRule),
            SpawnStrategy = data.Get(DataKey.SpawnStrategy),
            SpawnMinWave = data.Get(DataKey.SpawnMinWave),
            SpawnMaxWave = data.Get(DataKey.SpawnMaxWave),
            SpawnInterval = data.Get(DataKey.SpawnInterval),
            SingleSpawnCount = data.Get(DataKey.SingleSpawnCount),
            SingleSpawnVariance = data.Get(DataKey.SingleSpawnVariance),
            SpawnStartDelay = data.Get(DataKey.SpawnStartDelay),
        };
    }

    public static RuntimeEnemyConfig? FindEnemyByName(string name)
    {
        foreach (var enemy in LoadEnemies())
        {
            if (string.Equals(enemy.Name, name, StringComparison.Ordinal))
            {
                return enemy;
            }
        }
        return null;
    }

    private static Data LoadData(string table, string snapshotId)
    {
        var data = new Data();
        SnapshotLoader.Apply(SnapshotLoader.DefaultSnapshotPath, data, table, snapshotId);
        return data;
    }
}

/// <summary>
/// snapshot 中的技能配置索引项。
/// </summary>
public sealed class RuntimeAbilityConfig
{
    public string SnapshotId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FeatureGroupId { get; init; } = string.Empty;
    public string FeatureHandlerId { get; init; } = string.Empty;
    public AbilityType AbilityType { get; init; } = AbilityType.Passive;
    public AbilityTriggerMode AbilityTriggerMode { get; init; } = AbilityTriggerMode.None;
}

/// <summary>
/// snapshot 中的敌人生成配置索引项。
/// </summary>
public sealed class RuntimeEnemyConfig
{
    public string SnapshotId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsEnableSpawnRule { get; init; } = true;
    public SpawnPositionStrategy SpawnStrategy { get; init; } = SpawnPositionStrategy.Rectangle;
    public int SpawnMinWave { get; init; }
    public int SpawnMaxWave { get; init; } = -1;
    public float SpawnInterval { get; init; } = 1f;
    public int SingleSpawnCount { get; init; } = 1;
    public int SingleSpawnVariance { get; init; }
    public float SpawnStartDelay { get; init; }
}

/// <summary>
/// 运行时构造的通用 Feature 定义。
/// </summary>
public sealed class RuntimeFeatureDefinition
{
    public string Name { get; init; } = string.Empty;
    public string FeatureHandlerId { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string FeatureCategory { get; init; } = string.Empty;
    public EntityType EntityType { get; init; } = EntityType.Ability;
    public bool FeatureEnabled { get; init; } = true;

    [DataKey(DataKey.FeatureModifiers)]
    public List<RuntimeFeatureModifierEntry> Modifiers { get; init; } = new();
}

/// <summary>
/// 运行时 Feature 属性修改器定义。
/// </summary>
public sealed class RuntimeFeatureModifierEntry
{
    public string DataKeyName { get; init; } = string.Empty;
    public ModifierType ModifierType { get; init; } = ModifierType.Additive;
    public float Value { get; init; }
    public int Priority { get; init; }
}
