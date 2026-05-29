using System.Collections.Generic;

/// <summary>
/// runtime_snapshot.json descriptors 中单个 Data 字段定义 DTO。
/// </summary>
public sealed record RuntimeDataDescriptorDto
{
    /// <summary>
    /// 稳定字段键。
    /// </summary>
    public string StableKey { get; init; } = string.Empty;

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
    /// descriptor 值类型。
    /// </summary>
    public string ValueType { get; init; } = string.Empty;

    /// <summary>
    /// 可选运行时类型标识。
    /// </summary>
    public string RuntimeTypeId { get; init; } = string.Empty;

    /// <summary>
    /// 玩法默认值。
    /// </summary>
    public object? DefaultValue { get; init; }

    /// <summary>
    /// 字段存储策略。
    /// </summary>
    public string StoragePolicy { get; init; } = string.Empty;

    /// <summary>
    /// 字段写入策略。
    /// </summary>
    public string WritePolicy { get; init; } = string.Empty;

    /// <summary>
    /// 字段范围策略。
    /// </summary>
    public string RangePolicy { get; init; } = string.Empty;

    /// <summary>
    /// 字段 modifier 策略。
    /// </summary>
    public string ModifierPolicy { get; init; } = string.Empty;

    /// <summary>
    /// 字段迁移策略。
    /// </summary>
    public string MigrationPolicy { get; init; } = string.Empty;

    /// <summary>
    /// 最小值约束。
    /// </summary>
    public float? MinValue { get; init; }

    /// <summary>
    /// 最大值约束。
    /// </summary>
    public float? MaxValue { get; init; }

    /// <summary>
    /// computed resolver id。
    /// </summary>
    public string ComputeId { get; init; } = string.Empty;

    /// <summary>
    /// computed 依赖 stable key。
    /// </summary>
    public List<string> Dependencies { get; init; } = new();

    /// <summary>
    /// computed 参数。
    /// </summary>
    public Dictionary<string, string> ComputeParams { get; init; } = new();

    /// <summary>
    /// 允许值列表。
    /// </summary>
    public List<DataAllowedValueDto> AllowedValues { get; init; } = new();

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
}

/// <summary>
/// runtime_snapshot.json descriptor 允许值 DTO。
/// </summary>
public sealed record DataAllowedValueDto
{
    /// <summary>
    /// 实际存储值。
    /// </summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>
    /// 展示标签。
    /// </summary>
    public string Label { get; init; } = string.Empty;
}
