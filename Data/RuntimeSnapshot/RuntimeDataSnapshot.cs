using Godot;
using slime.data.Abilities;
using slime.data.Systems;
using slime.data.Units;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace slime.data;

/// <summary>
/// DataOS 运行时快照读取器。
/// <para>运行时只读 generated snapshot，不查询 SQLite，也不扫描 DataNew 静态字段。</para>
/// </summary>
public static class RuntimeDataSnapshot
{
    public const string SnapshotPath = "res://DataOS/Snapshots/runtime_snapshot.json";

    private static readonly object LockObject = new();
    private static RuntimeTypedSnapshotDocument? _snapshot;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, Type> TableTypes = new(StringComparer.Ordinal)
    {
        ["PlayerData"] = typeof(PlayerData),
        ["unit.player"] = typeof(PlayerData),
        ["EnemyData"] = typeof(EnemyData),
        ["unit.enemy"] = typeof(EnemyData),
        ["TargetingIndicatorData"] = typeof(TargetingIndicatorData),
        ["unit.targeting_indicator"] = typeof(TargetingIndicatorData),
        ["AbilityData"] = typeof(AbilityData),
        ["ChainAbilityData"] = typeof(ChainAbilityData),
        ["ability"] = typeof(AbilityData),
        ["SystemData"] = typeof(SystemData),
        ["system.config"] = typeof(SystemData),
        ["SystemPresetData"] = typeof(SystemPresetData),
        ["system.preset"] = typeof(SystemPresetData),
    };

    /// <summary>
    /// 获取指定 DTO 类型可接收的全部快照记录。
    /// </summary>
    public static IReadOnlyList<T> GetAll<T>() where T : class
    {
        var requestedType = typeof(T);
        var snapshot = EnsureSnapshot();
        var result = new List<T>();

        foreach (var record in snapshot.Records)
        {
            var concreteType = ResolveConcreteType(record);
            if (!requestedType.IsAssignableFrom(concreteType))
            {
                continue;
            }

            var instance = Activator.CreateInstance(concreteType)
                ?? throw new InvalidOperationException($"无法创建 DataOS 快照 DTO: {concreteType.FullName}");

            ApplyRecordData(instance, record);
            result.Add((T)instance);
        }

        if (result.Count == 0)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 缺少表: {requestedType.Name}");
        }

        return result;
    }

    /// <summary>
    /// 获取 snapshot 中的资源索引。
    /// </summary>
    public static IReadOnlyList<RuntimeResourceRecord> GetResources()
    {
        return EnsureSnapshot().Resources;
    }

    /// <summary>
    /// 解析测试/验证场景使用的 typed snapshot 文本。
    /// </summary>
    public static RuntimeTypedSnapshotDocument ParseTypedSnapshot(string jsonText)
    {
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException("Runtime Data typed snapshot 为空");
        }

        var parsed = Json.ParseString(jsonText);
        if (parsed.VariantType == Variant.Type.Nil)
        {
            throw new InvalidOperationException("Runtime Data typed snapshot JSON 无效");
        }

        return JsonSerializer.Deserialize<RuntimeTypedSnapshotDocument>(jsonText, SerializerOptions)
            ?? throw new InvalidOperationException("Runtime Data typed snapshot 反序列化失败");
    }

    /// <summary>
    /// 将 typed snapshot 中指定 record 应用到 Data 容器。
    /// </summary>
    public static DataApplyReport ApplyRecordToData(
        RuntimeTypedSnapshotDocument document,
        string table,
        string id,
        Data data)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(data);

        var report = new DataApplyReport();
        var descriptors = document.Descriptors.ToDictionary(GetDescriptorKey, StringComparer.Ordinal);
        var record = document.Records.FirstOrDefault(candidate =>
            string.Equals(candidate.Table, table, StringComparison.Ordinal)
            && string.Equals(candidate.Id, id, StringComparison.Ordinal));

        if (record == null)
        {
            report.AddError("snapshot.record_missing", $"Snapshot record not found: table={table}, id={id}");
            return report;
        }

        foreach (var resource in document.Resources)
        {
            if (!string.Equals(resource.OwnerCapability, "shared", StringComparison.Ordinal)
                && !document.Manifest.EnabledCapabilities.Contains(resource.OwnerCapability, StringComparer.Ordinal))
            {
                report.AddError(
                    "snapshot.resource_disabled_capability",
                    $"Resource owner capability is disabled: {resource.OwnerCapability}",
                    resource.Key);
            }
        }

        foreach (var field in record.Fields)
        {
            var key = field.Key;
            var value = field.Value;

            if (!data.Catalog.TryResolve(key, out var dataKey))
            {
                report.AddError("snapshot.unknown_key", $"Unknown DataKey: {key}", key);
                continue;
            }

            if (!descriptors.TryGetValue(key, out var descriptor))
            {
                report.AddError("snapshot.missing_descriptor", $"Missing descriptor for DataKey: {key}", key);
                continue;
            }

            var descriptorType = GetDescriptorType(descriptor);
            if (!SnapshotTypeMatches(dataKey.ValueType, descriptorType))
            {
                report.AddError("snapshot.descriptor_type_drift", $"Snapshot descriptor type drift: {key}", key);
                continue;
            }

            if (!string.Equals(value.Type, descriptorType, StringComparison.Ordinal))
            {
                report.AddError("snapshot.field_type_mismatch", $"Snapshot field type mismatch: {key}", key);
                continue;
            }

            if (!SnapshotDefaultMatches(dataKey, descriptor.DefaultValue))
            {
                report.AddError("snapshot.default_drift", $"Snapshot descriptor default drift: {key}", key);
                continue;
            }

            if (!TryConvertSnapshotValue(value.Value, dataKey.ValueType, out var converted))
            {
                report.AddError("snapshot.wrong_type", $"Snapshot field value type mismatch: {key}", key);
                continue;
            }

            data.SetUntyped(dataKey, converted);
        }

        return report;
    }

    /// <summary>
    /// 按 DataOS DTO 的 Name 从当前 snapshot 找到 typed record 并应用到 Data。
    /// <para>返回 false 表示该 config 不是 snapshot-backed DTO，调用方可走迁移 fallback。</para>
    /// </summary>
    public static bool TryApplyConfigToData(object config, Data data, out DataApplyReport report)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(data);

        report = new DataApplyReport();
        if (!TryResolveConfigRecord(config, out var table, out var legacyTable, out var recordName))
        {
            return false;
        }

        var snapshot = EnsureSnapshot();
        var record = snapshot.Records.FirstOrDefault(candidate =>
            string.Equals(candidate.Table, table, StringComparison.Ordinal)
            && string.Equals(candidate.Name, recordName, StringComparison.Ordinal)
            && (string.IsNullOrWhiteSpace(legacyTable)
                || string.Equals(candidate.LegacyTable, legacyTable, StringComparison.Ordinal)));

        if (record == null)
        {
            report.AddError("snapshot.record_missing", $"Snapshot record not found: table={table}, name={recordName}");
            return true;
        }

        report = ApplyRecordToData(snapshot, record.Table, record.Id, data);
        return true;
    }

    /// <summary>
    /// 清理缓存，供测试或重新生成 snapshot 后刷新。
    /// </summary>
    public static void ClearCache()
    {
        lock (LockObject)
        {
            _snapshot = null;
        }
    }

    private static RuntimeTypedSnapshotDocument EnsureSnapshot()
    {
        if (_snapshot != null)
        {
            return _snapshot;
        }

        lock (LockObject)
        {
            _snapshot ??= LoadSnapshot();
            return _snapshot;
        }
    }

    private static RuntimeTypedSnapshotDocument LoadSnapshot()
    {
        if (!Godot.FileAccess.FileExists(SnapshotPath))
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 不存在: {SnapshotPath}");
        }

        using var file = Godot.FileAccess.Open(SnapshotPath, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            throw new InvalidOperationException($"无法打开 DataOS runtime snapshot: {SnapshotPath}");
        }

        var jsonText = file.GetAsText();
        if (string.IsNullOrWhiteSpace(jsonText))
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 为空: {SnapshotPath}");
        }

        var parsed = Json.ParseString(jsonText);
        if (parsed.VariantType == Variant.Type.Nil)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot JSON 无效: {SnapshotPath}");
        }

        var snapshot = ParseTypedSnapshot(jsonText);

        if (snapshot.Manifest == null)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 缺少 manifest: {SnapshotPath}");
        }

        if (snapshot.Descriptors.Count == 0)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 缺少 descriptors: {SnapshotPath}");
        }

        if (snapshot.Records.Count == 0)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 没有 records: {SnapshotPath}");
        }

        return snapshot;
    }

    private static RuntimeTypedSnapshotDocument ParseLegacySnapshot(string jsonText)
    {
        var snapshot = JsonSerializer.Deserialize<RuntimeTypedSnapshotDocument>(jsonText, SerializerOptions)
            ?? throw new InvalidOperationException($"DataOS runtime snapshot 反序列化失败: {SnapshotPath}");

        return snapshot;
    }

    private static Type ResolveConcreteType(RuntimeTypedSnapshotRecord record)
    {
        var table = string.IsNullOrWhiteSpace(record.LegacyTable) ? record.Table : record.LegacyTable;
        if (!TableTypes.TryGetValue(table, out var concreteType))
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 存在未知表: table={record.Table}, id={record.Id}");
        }

        if (table == "ability" && record.Fields.ContainsKey(DataKey.AbilityChainCount.Key))
        {
            return typeof(ChainAbilityData);
        }

        return concreteType;
    }

    private static bool TryResolveConfigRecord(object config, out string table, out string legacyTable, out string recordName)
    {
        table = string.Empty;
        legacyTable = string.Empty;
        recordName = string.Empty;

        var configType = config.GetType();
        if (configType == typeof(PlayerData))
        {
            table = "unit.player";
            legacyTable = "PlayerData";
        }
        else if (configType == typeof(EnemyData))
        {
            table = "unit.enemy";
            legacyTable = "EnemyData";
        }
        else if (configType == typeof(TargetingIndicatorData))
        {
            table = "unit.targeting_indicator";
            legacyTable = "TargetingIndicatorData";
        }
        else if (configType == typeof(ChainAbilityData))
        {
            table = "ability";
            legacyTable = "ChainAbilityData";
        }
        else if (configType == typeof(AbilityData))
        {
            table = "ability";
            legacyTable = "AbilityData";
        }
        else
        {
            return false;
        }

        var nameProperty = configType.GetProperty(DataKey.Name, BindingFlags.Public | BindingFlags.Instance);
        recordName = nameProperty?.GetValue(config) as string ?? string.Empty;
        return !string.IsNullOrWhiteSpace(recordName);
    }

    private static void ApplyRecordData(object instance, RuntimeTypedSnapshotRecord record)
    {
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            var fieldKey = ResolveFieldKey(property);
            var valueElement = ResolveRecordValue(record, property.Name, fieldKey);
            if (valueElement == null)
            {
                continue;
            }

            if (valueElement.Value.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            try
            {
                var value = ConvertJsonValue(valueElement.Value, property.PropertyType);
                property.SetValue(instance, value);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"DataOS runtime snapshot 字段类型不匹配: table={record.Table}, id={record.Id}, field={property.Name}, targetType={property.PropertyType.Name}",
                    ex);
            }
        }
    }

    private static string ResolveFieldKey(PropertyInfo property)
    {
        var attr = property.GetCustomAttribute<DataKeyAttribute>();
        return attr?.Key ?? property.Name;
    }

    private static JsonElement? ResolveRecordValue(RuntimeTypedSnapshotRecord record, string propertyName, string fieldKey)
    {
        if (record.LegacyData.ValueKind == JsonValueKind.Object
            && record.LegacyData.TryGetProperty(propertyName, out var legacyValue))
        {
            return legacyValue;
        }

        if (record.Fields.TryGetValue(fieldKey, out var field))
        {
            return field.Value;
        }

        if (record.Fields.TryGetValue(propertyName, out var propertyField))
        {
            return propertyField.Value;
        }

        return null;
    }

    private static string GetDescriptorKey(RuntimeTypedSnapshotDescriptor descriptor)
    {
        return string.IsNullOrWhiteSpace(descriptor.StableKey) ? descriptor.Key : descriptor.StableKey;
    }

    private static string GetDescriptorType(RuntimeTypedSnapshotDescriptor descriptor)
    {
        return string.IsNullOrWhiteSpace(descriptor.ValueType) ? descriptor.Type : descriptor.ValueType;
    }

    private static object? ConvertJsonValue(JsonElement element, Type targetType)
    {
        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (nullableType != null)
        {
            return ConvertJsonValue(element, nullableType);
        }

        if (targetType == typeof(string))
        {
            return element.ValueKind == JsonValueKind.String
                ? element.GetString() ?? string.Empty
                : element.ToString();
        }

        if (targetType == typeof(bool))
        {
            return element.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => element.GetInt32() != 0,
                JsonValueKind.String => ParseBoolString(element.GetString()),
                _ => false
            };
        }

        if (targetType == typeof(int))
        {
            return element.ValueKind == JsonValueKind.String
                ? int.Parse(element.GetString() ?? "0")
                : element.GetInt32();
        }

        if (targetType == typeof(float))
        {
            return element.ValueKind == JsonValueKind.String
                ? float.Parse(element.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture)
                : (float)element.GetDouble();
        }

        if (targetType == typeof(double))
        {
            return element.ValueKind == JsonValueKind.String
                ? double.Parse(element.GetString() ?? "0", System.Globalization.CultureInfo.InvariantCulture)
                : element.GetDouble();
        }

        if (targetType == typeof(string[]))
        {
            return ConvertStringArray(element);
        }

        if (targetType.IsEnum)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                var text = element.GetString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    return Activator.CreateInstance(targetType);
                }

                return Enum.Parse(targetType, text, ignoreCase: false);
            }

            return Enum.ToObject(targetType, element.GetInt32());
        }

        throw new NotSupportedException($"DataOS runtime snapshot 不支持的字段类型: {targetType.FullName}");
    }

    private static bool ParseBoolString(string? text)
    {
        return text?.Trim().ToLowerInvariant() switch
        {
            "true" => true,
            "1" => true,
            "false" => false,
            "0" => false,
            "" => false,
            null => false,
            _ => bool.Parse(text)
        };
    }

    private static string[] ConvertStringArray(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            return element.EnumerateArray()
                .Select(item => item.GetString())
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Select(item => item!.Trim())
                .ToArray();
        }

        var text = element.ValueKind == JsonValueKind.String
            ? element.GetString()
            : element.ToString();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        return text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static bool SnapshotTypeMatches(Type valueType, string snapshotType)
    {
        return snapshotType.Trim().ToLowerInvariant() switch
        {
            "string" or "string_array" => valueType == typeof(string) || valueType == typeof(string[]),
            "float" => valueType == typeof(float) || valueType == typeof(double),
            "int" or "integer" => valueType == typeof(int),
            "bool" or "boolean" => valueType == typeof(bool),
            "enum" => valueType.IsEnum || valueType == typeof(string),
            _ => false
        };
    }

    private static bool SnapshotDefaultMatches(IDataKey key, JsonElement defaultValue)
    {
        if (!TryConvertSnapshotValue(defaultValue, key.ValueType, out var convertedDefault))
        {
            return false;
        }

        var expectedDefault = key.UntypedDefaultValue;
        if (expectedDefault is float expectedFloat && convertedDefault is float actualFloat)
        {
            return Math.Abs(expectedFloat - actualFloat) < 0.001f;
        }

        if (expectedDefault is double expectedDouble && convertedDefault is double actualDouble)
        {
            return Math.Abs(expectedDouble - actualDouble) < 0.001d;
        }

        return Equals(expectedDefault, convertedDefault);
    }

    private static bool TryConvertSnapshotValue(JsonElement element, Type targetType, out object? value)
    {
        try
        {
            value = ConvertJsonValue(element, targetType);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

}

public sealed record RuntimeTypedSnapshotDocument(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("generatedAtUtc")] string GeneratedAtUtc,
    [property: JsonPropertyName("manifest")] RuntimeTypedSnapshotManifest Manifest,
    [property: JsonPropertyName("descriptors")] List<RuntimeTypedSnapshotDescriptor> Descriptors,
    [property: JsonPropertyName("records")] List<RuntimeTypedSnapshotRecord> Records,
    [property: JsonPropertyName("resources")] List<RuntimeResourceRecord> Resources
);

public sealed record RuntimeTypedSnapshotManifest(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("generatedAtUtc")] string GeneratedAtUtc,
    [property: JsonPropertyName("profile")] string Profile,
    [property: JsonPropertyName("catalogId")] string CatalogId,
    [property: JsonPropertyName("enabledCapabilities")] List<string> EnabledCapabilities,
    [property: JsonPropertyName("descriptorCount")] int DescriptorCount,
    [property: JsonPropertyName("recordCount")] int RecordCount,
    [property: JsonPropertyName("resourceCount")] int ResourceCount
);

public sealed record RuntimeTypedSnapshotDescriptor(
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("stableKey")] string StableKey,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("valueType")] string ValueType,
    [property: JsonPropertyName("defaultValue")] JsonElement DefaultValue
);

public sealed record RuntimeTypedSnapshotRecord(
    [property: JsonPropertyName("table")] string Table,
    [property: JsonPropertyName("legacyTable")] string LegacyTable,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("fields")] Dictionary<string, RuntimeTypedSnapshotField> Fields,
    [property: JsonPropertyName("legacyData")] JsonElement LegacyData
);

public sealed record RuntimeTypedSnapshotField(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("value")] JsonElement Value
);

public sealed record RuntimeDataRecord(
    [property: JsonPropertyName("table")] string Table,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("data")] JsonElement Data
);

public sealed record RuntimeResourceRecord(
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("key")] string Key,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("ownerCapability")] string OwnerCapability,
    [property: JsonPropertyName("legacyStatus")] string LegacyStatus,
    [property: JsonPropertyName("sourceTable")] string SourceTable,
    [property: JsonPropertyName("sourceRowId")] string SourceRowId,
    [property: JsonPropertyName("sourceColumn")] string SourceColumn
);
