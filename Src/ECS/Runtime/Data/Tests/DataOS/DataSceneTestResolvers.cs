using System;
using System.Globalization;

namespace Slime.Test.DataOS;

/// <summary>
/// DataOS 场景测试专用 computed resolver。
/// </summary>
internal sealed class FixedComputeResolver : IDataComputeResolver<float>
{
    public FixedComputeResolver(string computeId)
    {
        ComputeId = computeId;
    }

    public string ComputeId { get; }

    public Type OutputClrType => typeof(float);

    public float Compute(Data data, DataDefinition definition)
    {
        return definition.DefaultValue == null
            ? 0f
            : Convert.ToSingle(definition.DefaultValue, CultureInfo.InvariantCulture);
    }
}

/// <summary>
/// 按 base + bonus * multiplier 计算测试值。
/// </summary>
internal sealed class ParametricAddResolver : IDataComputeResolver<float>
{
    public string ComputeId => "ParametricAdd";

    public Type OutputClrType => typeof(float);

    public float Compute(Data data, DataDefinition definition)
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
internal sealed class CountingAttributeBonusResolver : IDataComputeResolver<float>
{
    public int ComputeCount { get; private set; }

    public string ComputeId => "CountingAttributeBonus";

    public Type OutputClrType => typeof(float);

    public float Compute(Data data, DataDefinition definition)
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

/// <summary>
/// 故意返回 string 的测试 resolver，用于验证 computed 字段类型不匹配时 fail-fast。
/// </summary>
internal sealed class StringComputeResolver : IDataComputeResolver<string>
{
    public const string ResolverId = "StringCompute";

    public string ComputeId => ResolverId;

    public Type OutputClrType => typeof(string);

    public string Compute(Data data, DataDefinition definition)
    {
        return "bad";
    }
}
