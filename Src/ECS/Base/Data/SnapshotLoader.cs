using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

/// <summary>
/// 运行时快照加载器 - 从 runtime_snapshot.json 读取实体数据并写入 Data 容器
/// 职责：记录（records）→ Data.Set；资源（resources）→ 供 ResourceCatalog 查询
/// </summary>
public static class SnapshotLoader
{
    private static readonly Log _log = new(nameof(SnapshotLoader));

    /// <summary>默认 snapshot 路径（Godot 资源路径）</summary>
    public const string DefaultSnapshotPath = "res://Data/Data/Snapshots/runtime_snapshot.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ================= 公共 API =================

    /// <summary>
    /// 将 snapshot 中指定 table+id 的字段值写入 Data 容器
    /// </summary>
    /// <param name="snapshotPath">snapshot 文件路径（res:// 或文件系统路径）</param>
    /// <param name="data">目标 Data 容器</param>
    /// <param name="table">记录所属 table 名</param>
    /// <param name="id">记录 id</param>
    public static void Apply(string snapshotPath, Data data, string table, string id)
    {
        var doc = LoadDocument(snapshotPath);
        if (doc?.Records == null) return;

        SnapshotRecord? record = null;
        foreach (var r in doc.Records)
        {
            if (string.Equals(r.Table, table, StringComparison.Ordinal)
                && string.Equals(r.Id, id, StringComparison.Ordinal))
            {
                record = r;
                break;
            }
        }

        if (record == null)
        {
            _log.Warn($"Snapshot record not found: table={table}, id={id}");
            return;
        }

        ApplyRecord(record, data);
    }

    /// <summary>
    /// 获取 snapshot 中的资源条目列表（供 ResourceCatalog 使用）
    /// </summary>
    public static IReadOnlyList<SnapshotResourceEntry> GetResources(string snapshotPath)
    {
        var doc = LoadDocument(snapshotPath);
        if (doc?.Resources == null) return Array.Empty<SnapshotResourceEntry>();
        return doc.Resources;
    }

    // ================= 内部逻辑 =================

    private static SnapshotDocument? LoadDocument(string path)
    {
        try
        {
            string json;
            if (path.StartsWith("res://", StringComparison.Ordinal))
            {
                // Godot 虚拟文件系统路径
                using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    _log.Error($"无法打开 snapshot 文件: {path}（FileAccess error={FileAccess.GetOpenError()}）");
                    return null;
                }
                json = file.GetAsText();
            }
            else
            {
                // 普通文件系统路径（测试用）
                json = System.IO.File.ReadAllText(path);
            }

            return JsonSerializer.Deserialize<SnapshotDocument>(json, SerializerOptions);
        }
        catch (Exception e)
        {
            _log.Error($"Snapshot 读取或解析失败: {path} → {e.Message}");
            return null;
        }
    }

    private static void ApplyRecord(SnapshotRecord record, Data data)
    {
        if (record.Fields == null) return;

        var values = new Dictionary<string, object>();
        foreach (var (key, field) in record.Fields)
        {
            var meta = DataRegistry.GetMeta(key);
            if (meta == null)
            {
                // 未在 DataRegistry 注册的 key，跳过但不报错（snapshot 可能包含当前版本未启用的 capability 字段）
                _log.Warn($"Snapshot field 未在 DataRegistry 注册，已跳过: {key}");
                continue;
            }

            var converted = ConvertField(meta, field);
            if (converted != null)
                values[key] = converted;
        }

        data.SetMultiple(values);
    }

    private static object? ConvertField(DataMeta meta, SnapshotField field)
    {
        try
        {
            return field.TypeName switch
            {
                "float" => ConvertNumericToTarget(field.Value.GetSingle(), meta.Type),
                "int" => ConvertNumericToTarget(field.Value.GetInt32(), meta.Type),
                "string" => (object)(field.Value.GetString() ?? ""),
                "bool" => (object)field.Value.GetBoolean(),
                "enum" => ParseEnum(meta.Type, field.Value.GetString()),
                _ => null
            };
        }
        catch (Exception e)
        {
            _log.Warn($"字段转换失败: {meta.Key}（type={field.TypeName}）→ {e.Message}");
            return null;
        }
    }

    private static object ConvertNumericToTarget(float value, Type targetType)
    {
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(double)) return (double)value;
        return value;
    }

    private static object ConvertNumericToTarget(int value, Type targetType)
    {
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        return value;
    }

    private static object? ParseEnum(Type enumType, string? name)
    {
        if (string.IsNullOrEmpty(name) || !enumType.IsEnum) return null;
        return Enum.TryParse(enumType, name, ignoreCase: false, out var result) ? result : null;
    }

    // ================= 内部 DTO（仅用于反序列化）=================

    private class SnapshotDocument
    {
        [JsonPropertyName("records")]
        public List<SnapshotRecord>? Records { get; set; }

        [JsonPropertyName("resources")]
        public List<SnapshotResourceEntry>? Resources { get; set; }
    }

    private class SnapshotRecord
    {
        [JsonPropertyName("table")]
        public string Table { get; set; } = "";

        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("fields")]
        public Dictionary<string, SnapshotField>? Fields { get; set; }
    }

    private class SnapshotField
    {
        [JsonPropertyName("type")]
        public string TypeName { get; set; } = "";

        [JsonPropertyName("value")]
        public JsonElement Value { get; set; }
    }
}

/// <summary>
/// Snapshot 资源条目（供 ResourceCatalog 等外部使用）
/// </summary>
public class SnapshotResourceEntry
{
    [JsonPropertyName("category")]
    public string Category { get; set; } = "";

    [JsonPropertyName("key")]
    public string Key { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("ownerCapability")]
    public string OwnerCapability { get; set; } = "";
}
