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
    private static SnapshotDocument? _snapshot;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, Type> TableTypes = new(StringComparer.Ordinal)
    {
        ["PlayerData"] = typeof(PlayerData),
        ["EnemyData"] = typeof(EnemyData),
        ["TargetingIndicatorData"] = typeof(TargetingIndicatorData),
        ["AbilityData"] = typeof(AbilityData),
        ["ChainAbilityData"] = typeof(ChainAbilityData),
        ["SystemData"] = typeof(SystemData),
        ["SystemPresetData"] = typeof(SystemPresetData),
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
    /// 清理缓存，供测试或重新生成 snapshot 后刷新。
    /// </summary>
    public static void ClearCache()
    {
        lock (LockObject)
        {
            _snapshot = null;
        }
    }

    private static SnapshotDocument EnsureSnapshot()
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

    private static SnapshotDocument LoadSnapshot()
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

        var snapshot = JsonSerializer.Deserialize<SnapshotDocument>(jsonText, SerializerOptions)
            ?? throw new InvalidOperationException($"DataOS runtime snapshot 反序列化失败: {SnapshotPath}");

        if (snapshot.Records.Count == 0)
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 没有 records: {SnapshotPath}");
        }

        return snapshot;
    }

    private static Type ResolveConcreteType(RuntimeDataRecord record)
    {
        if (!TableTypes.TryGetValue(record.Table, out var concreteType))
        {
            throw new InvalidOperationException($"DataOS runtime snapshot 存在未知表: table={record.Table}, id={record.Id}");
        }

        return concreteType;
    }

    private static void ApplyRecordData(object instance, RuntimeDataRecord record)
    {
        foreach (var property in instance.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanWrite)
            {
                continue;
            }

            if (!record.Data.TryGetProperty(property.Name, out var valueElement))
            {
                continue;
            }

            if (valueElement.ValueKind == JsonValueKind.Null)
            {
                continue;
            }

            try
            {
                var value = ConvertJsonValue(valueElement, property.PropertyType);
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

    private sealed record SnapshotDocument(
        [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
        [property: JsonPropertyName("generatedAtUtc")] string GeneratedAtUtc,
        [property: JsonPropertyName("source")] string Source,
        [property: JsonPropertyName("records")] List<RuntimeDataRecord> Records,
        [property: JsonPropertyName("resources")] List<RuntimeResourceRecord> Resources
    );
}

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
