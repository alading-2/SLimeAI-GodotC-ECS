using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 旧 DataMeta 与新 descriptor catalog 的一次性审计报告。
/// </summary>
public sealed class LegacyDataAuditReport
{
    /// <summary>
    /// 旧 C# 有但 snapshot descriptor 缺失的字段。
    /// </summary>
    public List<string> MissingInSnapshot { get; } = new();

    /// <summary>
    /// snapshot descriptor 有但旧 C# 缺失的字段。
    /// </summary>
    public List<string> MissingInCSharp { get; } = new();

    /// <summary>
    /// 类型不一致的字段。
    /// </summary>
    public List<LegacyDataAuditMismatch> TypeMismatches { get; } = new();

    /// <summary>
    /// 默认值不一致的字段。
    /// </summary>
    public List<LegacyDataAuditMismatch> DefaultMismatches { get; } = new();

    /// <summary>
    /// 范围约束不一致的字段。
    /// </summary>
    public List<LegacyDataAuditMismatch> RangeMismatches { get; } = new();

    /// <summary>
    /// computed 绑定不一致的字段。
    /// </summary>
    public List<LegacyDataAuditMismatch> ComputedMismatches { get; } = new();

    /// <summary>
    /// 仍引用旧 Data 输入路径的文件。
    /// </summary>
    public List<string> OldPathReferences { get; } = new();

    /// <summary>
    /// 构建一次性审计报告。
    /// </summary>
    /// <param name="legacyMetas">旧 DataMeta 清单。</param>
    /// <param name="definitions">新 DataDefinition 清单。</param>
    /// <param name="referencedPaths">待扫描路径清单。</param>
    public static LegacyDataAuditReport Build(
        IEnumerable<DataMeta> legacyMetas,
        IEnumerable<DataDefinition> definitions,
        IEnumerable<string> referencedPaths)
    {
        ArgumentNullException.ThrowIfNull(legacyMetas);
        ArgumentNullException.ThrowIfNull(definitions);
        ArgumentNullException.ThrowIfNull(referencedPaths);

        var report = new LegacyDataAuditReport();
        var legacyByKey = legacyMetas.ToDictionary(meta => meta.Key, StringComparer.Ordinal);
        var definitionByKey = definitions.ToDictionary(definition => definition.StableKey, StringComparer.Ordinal);

        foreach (var legacy in legacyByKey.Values)
        {
            if (!definitionByKey.TryGetValue(legacy.Key, out var definition))
            {
                report.MissingInSnapshot.Add(legacy.Key);
                continue;
            }

            AddMismatchIfDifferent(report.TypeMismatches, legacy.Key, MapLegacyType(legacy.Type).ToString(), definition.ValueType.ToString());
            AddMismatchIfDifferent(report.DefaultMismatches, legacy.Key, NormalizeValue(legacy.GetDefaultValue()), NormalizeValue(definition.DefaultValue));
            AddRangeMismatch(report, legacy, definition);
            AddComputedMismatch(report, legacy, definition);
        }

        foreach (var definition in definitionByKey.Values)
        {
            if (!legacyByKey.ContainsKey(definition.StableKey))
            {
                report.MissingInCSharp.Add(definition.StableKey);
            }
        }

        foreach (var path in referencedPaths)
        {
            if (IsOldPathReference(path))
            {
                report.OldPathReferences.Add(path);
            }
        }

        return report;
    }

    private static void AddRangeMismatch(LegacyDataAuditReport report, DataMeta legacy, DataDefinition definition)
    {
        var legacyRange = $"{legacy.MinValue?.ToString() ?? string.Empty}..{legacy.MaxValue?.ToString() ?? string.Empty}";
        var descriptorRange = $"{definition.MinValue?.ToString() ?? string.Empty}..{definition.MaxValue?.ToString() ?? string.Empty}";
        AddMismatchIfDifferent(report.RangeMismatches, legacy.Key, legacyRange, descriptorRange);
    }

    private static void AddComputedMismatch(LegacyDataAuditReport report, DataMeta legacy, DataDefinition definition)
    {
        var legacyComputed = legacy.IsComputed;
        var descriptorComputed = definition.IsComputed;
        if (legacyComputed != descriptorComputed)
        {
            report.ComputedMismatches.Add(new LegacyDataAuditMismatch(legacy.Key, legacyComputed.ToString(), descriptorComputed.ToString()));
            return;
        }

        if (!legacyComputed)
        {
            return;
        }

        var legacyDependencies = string.Join(",", legacy.Dependencies ?? Array.Empty<string>());
        var descriptorDependencies = string.Join(",", definition.Dependencies);
        AddMismatchIfDifferent(report.ComputedMismatches, legacy.Key, legacyDependencies, descriptorDependencies);
    }

    private static void AddMismatchIfDifferent(List<LegacyDataAuditMismatch> target, string stableKey, string legacyValue, string descriptorValue)
    {
        if (!string.Equals(legacyValue, descriptorValue, StringComparison.Ordinal))
        {
            target.Add(new LegacyDataAuditMismatch(stableKey, legacyValue, descriptorValue));
        }
    }

    private static bool IsOldPathReference(string path)
    {
        return path.Contains("legacy runtime table", StringComparison.Ordinal)
            || path.Contains("DataOS removed legacy Data/", StringComparison.Ordinal)
            || path.Contains("LoadFrom" + "Config", StringComparison.Ordinal)
            || path.Contains("SingleTest/ECS/" + "Data", StringComparison.Ordinal);
    }

    private static DataValueType MapLegacyType(Type type)
    {
        if (type == typeof(string)) return DataValueType.String;
        if (type == typeof(int)) return DataValueType.Int;
        if (type == typeof(float)) return DataValueType.Float;
        if (type == typeof(double)) return DataValueType.Double;
        if (type == typeof(bool)) return DataValueType.Bool;
        if (type.IsEnum) return DataValueType.Enum;
        return DataValueType.ObjectRef;
    }

    private static string NormalizeValue(object? value)
    {
        return value?.ToString() ?? string.Empty;
    }
}

/// <summary>
/// 旧 DataMeta 与新 descriptor 的单字段差异。
/// </summary>
public sealed record LegacyDataAuditMismatch(string StableKey, string LegacyValue, string DescriptorValue);
