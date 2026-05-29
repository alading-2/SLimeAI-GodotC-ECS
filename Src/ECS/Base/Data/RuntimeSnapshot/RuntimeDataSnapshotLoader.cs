using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

/// <summary>
/// runtime snapshot Data descriptor 加载器。
/// </summary>
public sealed class RuntimeDataSnapshotLoader
{
    private readonly DataComputeRegistry _computeRegistry;

    /// <summary>
    /// 创建 snapshot loader。
    /// </summary>
    /// <param name="computeRegistry">computed resolver 注册表。</param>
    public RuntimeDataSnapshotLoader(DataComputeRegistry computeRegistry)
    {
        ArgumentNullException.ThrowIfNull(computeRegistry);
        _computeRegistry = computeRegistry; // computed resolver 注册表
    }

    /// <summary>
    /// 从 JSON 字符串读取 runtime snapshot。
    /// </summary>
    /// <param name="json">runtime_snapshot.json 内容。</param>
    public RuntimeDataSnapshot LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("runtime snapshot json 不能为空。", nameof(json));
        }

        var snapshot = JsonSerializer.Deserialize<RuntimeDataSnapshot>(
            json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return snapshot ?? throw new InvalidOperationException("runtime snapshot json 解析失败。");
    }

    /// <summary>
    /// 将单条 snapshot record 应用到 Data 容器。
    /// </summary>
    /// <param name="data">目标 Data 容器。</param>
    /// <param name="catalog">字段定义 catalog。</param>
    /// <param name="record">snapshot record。</param>
    public DataApplyReport ApplyRecord(Data data, DataDefinitionCatalog catalog, RuntimeDataRecordDto record)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(record);

        var report = new DataApplyReport(record.Table, record.Id);
        foreach (var pair in record.Fields)
        {
            var stableKey = pair.Key;
            var field = pair.Value;
            if (!catalog.TryGet(stableKey, out var definition))
            {
                report.AddError("snapshot.unknown_key", stableKey, "record field 没有对应 descriptor。");
                continue;
            }

            if (!TryParseSnapshotValueType(field.Type, out var fieldValueType) || fieldValueType != definition.ValueType)
            {
                report.AddError("snapshot.type_mismatch", stableKey, $"record field type 与 descriptor 不一致：{field.Type} != {definition.ValueType}");
                continue;
            }

            var rawValue = NormalizeRecordValue(field.Value);
            if (!DataValueConverter.TryConvert(rawValue, definition.ValueType, out var convertedValue, out var convertError))
            {
                report.AddError("snapshot.conversion_failed", stableKey, convertError);
                continue;
            }

            if (definition.StoragePolicy is DataStoragePolicy.Computed or DataStoragePolicy.RuntimeOnly)
            {
                report.AddError("snapshot.apply_rejected", stableKey, $"record 不允许写入 storage_policy={definition.StoragePolicy} 字段。");
                continue;
            }

            if (!data.SetUntyped(definition, convertedValue, DataWriteSource.Loader))
            {
                report.AddError("snapshot.apply_rejected", stableKey, "Data 拒绝写入该字段值。");
                continue;
            }

            report.AppliedFieldCount++;
        }

        return report;
    }

    /// <summary>
    /// 从 descriptor DTO 构建 DataDefinitionCatalog。
    /// </summary>
    /// <param name="descriptors">snapshot descriptor 列表。</param>
    public DataDefinitionCatalog BuildCatalog(IEnumerable<RuntimeDataDescriptorDto> descriptors)
    {
        ArgumentNullException.ThrowIfNull(descriptors);
        var catalog = new DataDefinitionCatalog();
        catalog.BindComputeRegistry(_computeRegistry);
        foreach (var descriptor in descriptors)
        {
            var definition = ConvertDescriptor(descriptor);
            ValidateComputeBinding(definition);
            catalog.Register(definition);
        }

        catalog.ValidateAndBuildIndexes();
        return catalog;
    }

    private DataDefinition ConvertDescriptor(RuntimeDataDescriptorDto descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        var valueType = ParseValueType(descriptor.ValueType);
        var storagePolicy = ParseStoragePolicy(descriptor.StoragePolicy);
        var writePolicy = ParseWritePolicy(descriptor.WritePolicy);
        var rangePolicy = ParseRangePolicy(descriptor.RangePolicy);
        var modifierPolicy = ParseModifierPolicy(descriptor.ModifierPolicy);
        var migrationPolicy = ParseMigrationPolicy(descriptor.MigrationPolicy);
        var defaultValue = ConvertDefaultValue(descriptor.DefaultValue, valueType, descriptor.StableKey);

        return new DataDefinition
        {
            StableKey = descriptor.StableKey,
            ValueType = valueType,
            RuntimeTypeId = descriptor.RuntimeTypeId,
            DefaultValue = defaultValue,
            OwnerDomain = descriptor.OwnerDomain,
            OwnerCapability = descriptor.OwnerCapability,
            OwnerSkill = descriptor.OwnerSkill,
            StoragePolicy = storagePolicy,
            WritePolicy = writePolicy,
            RangePolicy = rangePolicy,
            MinValue = descriptor.MinValue,
            MaxValue = descriptor.MaxValue,
            ModifierPolicy = modifierPolicy,
            MigrationPolicy = migrationPolicy,
            ComputeId = descriptor.ComputeId,
            Dependencies = descriptor.Dependencies.ToArray(),
            ComputeParams = new Dictionary<string, string>(descriptor.ComputeParams, StringComparer.Ordinal),
            AllowedValues = descriptor.AllowedValues
                .Select(value => new DataAllowedValue { Value = value.Value, Label = value.Label })
                .ToArray(),
            DisplayName = descriptor.DisplayName,
            Description = descriptor.Description,
            UiGroup = descriptor.UiGroup,
            ResetGroup = descriptor.ResetGroup,
            Unit = descriptor.Unit,
            Format = descriptor.Format,
            IconPath = descriptor.IconPath
        };
    }

    private void ValidateComputeBinding(DataDefinition definition)
    {
        if (definition.StoragePolicy == DataStoragePolicy.Computed && string.IsNullOrWhiteSpace(definition.ComputeId))
        {
            throw new InvalidOperationException($"computed DataDefinition 必须声明 compute_id：{definition.StableKey}");
        }

        if (!string.IsNullOrWhiteSpace(definition.ComputeId) && !_computeRegistry.Contains(definition.ComputeId))
        {
            throw new InvalidOperationException($"DataDefinition 缺少 resolver：{definition.StableKey} -> {definition.ComputeId}");
        }
    }

    private static DataValueType ParseValueType(string raw)
    {
        var normalized = Normalize(raw);
        return normalized switch
        {
            "string" => DataValueType.String,
            "stringarray" => DataValueType.StringArray,
            "string_array" => DataValueType.StringArray,
            "int" => DataValueType.Int,
            "integer" => DataValueType.Int,
            "float" => DataValueType.Float,
            "double" => DataValueType.Double,
            "bool" => DataValueType.Bool,
            "boolean" => DataValueType.Bool,
            "vector2" => DataValueType.Vector2,
            "enum" => DataValueType.Enum,
            "modifierlist" => DataValueType.ModifierList,
            "modifier_list" => DataValueType.ModifierList,
            "objectref" => DataValueType.ObjectRef,
            "object_ref" => DataValueType.ObjectRef,
            _ => throw new InvalidOperationException($"未知 Data value_type：{raw}")
        };
    }

    private static bool TryParseSnapshotValueType(string raw, out DataValueType valueType)
    {
        valueType = default;
        try
        {
            valueType = ParseValueType(raw);
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static DataStoragePolicy ParseStoragePolicy(string raw)
    {
        var normalized = NormalizeOrDefault(raw, "persisted");
        return normalized switch
        {
            "persisted" => DataStoragePolicy.Persisted,
            "runtimestate" => DataStoragePolicy.RuntimeState,
            "runtime_state" => DataStoragePolicy.RuntimeState,
            "runtimeonly" => DataStoragePolicy.RuntimeOnly,
            "runtime_only" => DataStoragePolicy.RuntimeOnly,
            "computed" => DataStoragePolicy.Computed,
            "authoringblob" => DataStoragePolicy.AuthoringBlob,
            "authoring_blob" => DataStoragePolicy.AuthoringBlob,
            _ => throw new InvalidOperationException($"未知 Data storage_policy：{raw}")
        };
    }

    private static DataWritePolicy ParseWritePolicy(string raw)
    {
        var normalized = NormalizeOrDefault(raw, "read_write");
        return normalized switch
        {
            "readwrite" => DataWritePolicy.ReadWrite,
            "read_write" => DataWritePolicy.ReadWrite,
            "loaderonly" => DataWritePolicy.LoaderOnly,
            "loader_only" => DataWritePolicy.LoaderOnly,
            "systemonly" => DataWritePolicy.SystemOnly,
            "system_only" => DataWritePolicy.SystemOnly,
            "computedreadonly" => DataWritePolicy.ComputedReadonly,
            "computed_readonly" => DataWritePolicy.ComputedReadonly,
            "debugonly" => DataWritePolicy.DebugOnly,
            "debug_only" => DataWritePolicy.DebugOnly,
            _ => throw new InvalidOperationException($"未知 Data write_policy：{raw}")
        };
    }

    private static DataRangePolicy ParseRangePolicy(string raw)
    {
        var normalized = NormalizeOrDefault(raw, "none");
        return normalized switch
        {
            "none" => DataRangePolicy.None,
            "validate" => DataRangePolicy.Validate,
            "clampruntime" => DataRangePolicy.ClampRuntime,
            "clamp_runtime" => DataRangePolicy.ClampRuntime,
            "rejectruntime" => DataRangePolicy.RejectRuntime,
            "reject_runtime" => DataRangePolicy.RejectRuntime,
            _ => throw new InvalidOperationException($"未知 Data range_policy：{raw}")
        };
    }

    private static DataModifierPolicy ParseModifierPolicy(string raw)
    {
        var normalized = NormalizeOrDefault(raw, "none");
        return normalized switch
        {
            "none" => DataModifierPolicy.None,
            "numeric" => DataModifierPolicy.Numeric,
            "debugonly" => DataModifierPolicy.DebugOnly,
            "debug_only" => DataModifierPolicy.DebugOnly,
            _ => throw new InvalidOperationException($"未知 Data modifier_policy：{raw}")
        };
    }

    private static DataMigrationPolicy ParseMigrationPolicy(string raw)
    {
        var normalized = NormalizeOrDefault(raw, "default");
        return normalized switch
        {
            "default" => DataMigrationPolicy.Default,
            "never" => DataMigrationPolicy.Never,
            "always" => DataMigrationPolicy.Always,
            "profileonly" => DataMigrationPolicy.ProfileOnly,
            "profile_only" => DataMigrationPolicy.ProfileOnly,
            _ => throw new InvalidOperationException($"未知 Data migration_policy：{raw}")
        };
    }

    private static object? ConvertDefaultValue(object? raw, DataValueType valueType, string stableKey)
    {
        var text = NormalizeDefaultValueText(raw);
        try
        {
            return valueType switch
            {
                DataValueType.String => text,
                DataValueType.StringArray => text,
                DataValueType.Int => int.Parse(text, NumberStyles.Integer, CultureInfo.InvariantCulture),
                DataValueType.Float => float.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                DataValueType.Double => double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture),
                DataValueType.Bool => bool.Parse(text),
                DataValueType.Enum => text,
                DataValueType.Vector2 => text,
                DataValueType.ModifierList => text,
                DataValueType.ObjectRef => text,
                _ => text
            };
        }
        catch (Exception ex) when (ex is FormatException or OverflowException or ArgumentException)
        {
            throw new InvalidOperationException($"DataDefinition default 转换失败：{stableKey} ({valueType}) = {text}", ex);
        }
    }

    private static string NormalizeDefaultValueText(object? raw)
    {
        if (raw == null)
        {
            return string.Empty;
        }

        if (raw is JsonElement element)
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.GetRawText();
        }

        return Convert.ToString(raw, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static object? NormalizeRecordValue(object? raw)
    {
        if (raw is not JsonElement element)
        {
            return raw;
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.Number => element.GetRawText(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            JsonValueKind.Array => element.GetRawText(),
            JsonValueKind.Object => element.GetRawText(),
            _ => element.GetRawText()
        };
    }

    private static string NormalizeOrDefault(string raw, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(raw) ? defaultValue : Normalize(raw);
    }

    private static string Normalize(string raw)
    {
        return raw.Trim().ToLowerInvariant();
    }
}
