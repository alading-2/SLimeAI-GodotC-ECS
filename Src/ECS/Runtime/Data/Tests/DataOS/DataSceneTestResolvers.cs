using System.Globalization;

namespace Slime.Test.DataOS;

/// <summary>
/// DataOS 场景测试专用 computed resolver。
/// </summary>
internal sealed class FixedComputeResolver : IDataComputeResolver
{
    public FixedComputeResolver(string computeId)
    {
        ComputeId = computeId;
    }

    public string ComputeId { get; }

    public object? Compute(Data data, DataDefinition definition)
    {
        return definition.DefaultValue;
    }
}

/// <summary>
/// 按 base + bonus * multiplier 计算测试值。
/// </summary>
internal sealed class ParametricAddResolver : IDataComputeResolver
{
    public string ComputeId => "ParametricAdd";

    public object? Compute(Data data, DataDefinition definition)
    {
        var baseValue = data.Get<float>(definition.Dependencies[0]);
        if (definition.Dependencies.Count == 1)
        {
            return baseValue;
        }

        var multiplier = definition.ComputeParams.TryGetValue("bonus_multiplier", out var rawMultiplier)
            ? float.Parse(rawMultiplier, CultureInfo.InvariantCulture)
            : 1f;
        return baseValue + data.Get<float>(definition.Dependencies[1]) * multiplier;
    }
}

/// <summary>
/// 统计调用次数的属性加成 resolver，用于验证 computed cache。
/// </summary>
internal sealed class CountingAttributeBonusResolver : IDataComputeResolver
{
    public int ComputeCount { get; private set; }

    public string ComputeId => "CountingAttributeBonus";

    public object? Compute(Data data, DataDefinition definition)
    {
        ComputeCount++;
        var baseValue = data.Get<float>(definition.Dependencies[0]);
        if (definition.Dependencies.Count == 1)
        {
            return baseValue;
        }

        var bonus = data.Get<float>(definition.Dependencies[1]);
        return baseValue * (1f + bonus / 100f);
    }
}

