using System;
using System.Collections.Generic;

/// <summary>
/// Data computed resolver 注册表。
/// </summary>
public sealed class DataComputeRegistry
{
    private readonly Dictionary<string, IDataComputeResolver> _resolvers = new(StringComparer.Ordinal);

    /// <summary>
    /// 注册 computed resolver。
    /// </summary>
    /// <param name="resolver">computed resolver。</param>
    public void Register(IDataComputeResolver resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        if (string.IsNullOrWhiteSpace(resolver.ComputeId))
        {
            throw new InvalidOperationException("Data compute resolver 的 ComputeId 不能为空。");
        }

        if (!_resolvers.TryAdd(resolver.ComputeId, resolver))
        {
            throw new InvalidOperationException($"重复 Data compute resolver：{resolver.ComputeId}");
        }
    }

    /// <summary>
    /// 检查 resolver 是否存在。
    /// </summary>
    /// <param name="computeId">computed resolver id。</param>
    public bool Contains(string computeId)
    {
        return _resolvers.ContainsKey(computeId);
    }

    /// <summary>
    /// 获取 resolver，未注册时报错。
    /// </summary>
    /// <param name="computeId">computed resolver id。</param>
    public IDataComputeResolver GetRequired(string computeId)
    {
        if (_resolvers.TryGetValue(computeId, out var resolver))
        {
            return resolver;
        }

        throw new KeyNotFoundException($"未注册 Data compute resolver：{computeId}");
    }
}

/// <summary>
/// Data computed 字段的纯计算 resolver。
/// </summary>
public interface IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    string ComputeId { get; }

    /// <summary>
    /// 计算字段值。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    object? Compute(Data data, DataDefinition definition);
}

/// <summary>
/// 基础属性百分比加成 resolver。
/// </summary>
public sealed class AttributeBonusComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "AttributeBonus";

    /// <summary>
    /// 按 base * (1 + bonus / 100) 计算最终属性。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var baseValue = data.Get<float>(definition.Dependencies[0]);
        var bonus = definition.Dependencies.Count > 1 ? data.Get<float>(definition.Dependencies[1]) : 0f;
        return baseValue * (1f + bonus / 100f);
    }
}

/// <summary>
/// 当前值 / 最大值百分比 resolver。
/// </summary>
public sealed class PercentComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "Percent";

    /// <summary>
    /// 按 current / max * 100 计算百分比。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var current = data.Get<float>(definition.Dependencies[0]);
        var max = definition.Dependencies.Count > 1 ? data.Get<float>(definition.Dependencies[1]) : 0f;
        return max > 0f ? current / max * 100f : 0f;
    }
}

/// <summary>
/// 攻击间隔 resolver。
/// </summary>
public sealed class AttackIntervalComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "AttackInterval";

    /// <summary>
    /// 按 1 / (attackSpeed / 100) 计算攻击间隔。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var speed = data.Get<float>(definition.Dependencies[0]);
        return speed > 0f ? 1f / (speed / 100f) : 0f;
    }
}

/// <summary>
/// 基础恢复 + 最大值百分比恢复 resolver。
/// </summary>
public sealed class RegenComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "Regen";

    /// <summary>
    /// 按 base regen * (1 + bonus / 100) + max * percent / 100 计算恢复值。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var baseRegen = data.Get<float>(definition.Dependencies[0]);
        var bonus = definition.Dependencies.Count > 1 ? data.Get<float>(definition.Dependencies[1]) : 0f;
        var percent = definition.Dependencies.Count > 2 ? data.Get<float>(definition.Dependencies[2]) : 0f;
        var max = definition.Dependencies.Count > 3 ? data.Get<float>(definition.Dependencies[3]) : 0f;
        return baseRegen * (1f + bonus / 100f) + max * (percent / 100f);
    }
}

/// <summary>
/// 有效生命 resolver。
/// </summary>
public sealed class EffectiveHpComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "EffectiveHp";

    /// <summary>
    /// 按 hp * (1 + defense / 100) 计算有效生命。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var hp = data.Get<float>(definition.Dependencies[0]);
        var defense = definition.Dependencies.Count > 1 ? data.Get<float>(definition.Dependencies[1]) : 0f;
        return hp * (1f + defense / 100f);
    }
}

/// <summary>
/// 每秒伤害估算 resolver。
/// </summary>
public sealed class DpsComputeResolver : IDataComputeResolver
{
    /// <summary>
    /// descriptor 中引用的计算语义 id。
    /// </summary>
    public string ComputeId => "Dps";

    /// <summary>
    /// 按 attack * attackSpeed / 100 * crit multiplier 计算 DPS。
    /// </summary>
    /// <param name="data">当前 Data 容器。</param>
    /// <param name="definition">字段定义。</param>
    public object? Compute(Data data, DataDefinition definition)
    {
        var attack = data.Get<float>(definition.Dependencies[0]);
        var speed = definition.Dependencies.Count > 1 ? data.Get<float>(definition.Dependencies[1]) : 0f;
        var critRate = definition.Dependencies.Count > 2 ? data.Get<float>(definition.Dependencies[2]) : 0f;
        var critDamage = definition.Dependencies.Count > 3 ? data.Get<float>(definition.Dependencies[3]) : 100f;
        var critMultiplier = 1f + (critRate / 100f) * (critDamage / 100f);
        return attack * (speed / 100f) * critMultiplier;
    }
}
