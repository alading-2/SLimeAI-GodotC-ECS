using System.Collections.Generic;

/// <summary>
/// Data descriptor 的运行时字段定义。
/// </summary>
public sealed class DataDefinition
{
    /// <summary>
    /// 稳定字段键。
    /// </summary>
    public required string StableKey { get; init; }

    /// <summary>
    /// 字段基础值类型。
    /// </summary>
    public required DataValueType ValueType { get; init; }

    /// <summary>
    /// 可选运行时类型标识。
    /// </summary>
    public string RuntimeTypeId { get; init; } = string.Empty;

    /// <summary>
    /// descriptor 提供的玩法默认值。
    /// </summary>
    public required object? DefaultValue { get; init; }

    /// <summary>
    /// 字段归属域。
    /// </summary>
    public string OwnerDomain { get; init; } = string.Empty;

    /// <summary>
    /// 字段归属能力。
    /// </summary>
    public string OwnerCapability { get; init; } = string.Empty;

    /// <summary>
    /// 字段归属 skill。
    /// </summary>
    public string OwnerSkill { get; init; } = string.Empty;

    /// <summary>
    /// 字段存储策略。
    /// </summary>
    public DataStoragePolicy StoragePolicy { get; init; } = DataStoragePolicy.Persisted;

    /// <summary>
    /// 字段写入策略。
    /// </summary>
    public DataWritePolicy WritePolicy { get; init; } = DataWritePolicy.ReadWrite;

    /// <summary>
    /// 字段范围策略。
    /// </summary>
    public DataRangePolicy RangePolicy { get; init; } = DataRangePolicy.None;

    /// <summary>
    /// 最小值约束。
    /// </summary>
    public float? MinValue { get; init; }

    /// <summary>
    /// 最大值约束。
    /// </summary>
    public float? MaxValue { get; init; }

    /// <summary>
    /// 字段 modifier 策略。
    /// </summary>
    public DataModifierPolicy ModifierPolicy { get; init; } = DataModifierPolicy.None;

    /// <summary>
    /// 字段允许值列表。
    /// </summary>
    public IReadOnlyList<DataAllowedValue> AllowedValues { get; init; } = [];

    /// <summary>
    /// computed resolver id。
    /// </summary>
    public string ComputeId { get; init; } = string.Empty;

    /// <summary>
    /// computed 依赖 stable key。
    /// </summary>
    public IReadOnlyList<string> Dependencies { get; init; } = [];

    /// <summary>
    /// computed 参数。
    /// </summary>
    public IReadOnlyDictionary<string, string> ComputeParams { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// 字段迁移策略。
    /// </summary>
    public DataMigrationPolicy MigrationPolicy { get; init; } = DataMigrationPolicy.Default;

    /// <summary>
    /// 展示名称。
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// 字段说明。
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// UI 分组。
    /// </summary>
    public string UiGroup { get; init; } = string.Empty;

    /// <summary>
    /// 重置分组。
    /// </summary>
    public string ResetGroup { get; init; } = string.Empty;

    /// <summary>
    /// 展示单位。
    /// </summary>
    public string Unit { get; init; } = string.Empty;

    /// <summary>
    /// 展示格式。
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// 图标路径。
    /// </summary>
    public string IconPath { get; init; } = string.Empty;

    /// <summary>
    /// 是否为 computed 字段。
    /// </summary>
    public bool IsComputed => StoragePolicy == DataStoragePolicy.Computed || !string.IsNullOrWhiteSpace(ComputeId);
}

/// <summary>
/// descriptor 中的允许值定义。
/// </summary>
public sealed class DataAllowedValue
{
    /// <summary>
    /// 实际存储值。
    /// </summary>
    public required string Value { get; init; }

    /// <summary>
    /// 展示标签。
    /// </summary>
    public string Label { get; init; } = string.Empty;
}
