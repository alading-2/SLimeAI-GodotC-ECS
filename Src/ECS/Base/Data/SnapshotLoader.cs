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

    // 按路径缓存已解析的文档，避免初始化阶段重复 IO
    private static readonly Dictionary<string, SnapshotDocument?> _docCache = new(StringComparer.Ordinal);

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

    // ================= 扩展公开 API =================

    /// <summary>获取指定 table 的所有记录 ID 列表。</summary>
    public static IReadOnlyList<string> GetRecordIds(string snapshotPath, string table)
    {
        var doc = LoadDocument(snapshotPath);
        if (doc?.Records == null) return Array.Empty<string>();
        var result = new List<string>();
        foreach (var r in doc.Records)
            if (string.Equals(r.Table, table, StringComparison.Ordinal))
                result.Add(r.Id);
        return result;
    }

    /// <summary>
    /// 获取指定记录的原始字段（fieldName → (TypeName, StringValue)）。
    /// StringValue 已转为适合 Parse 的字符串（string/enum 原文，bool 为 "true"/"false"，int/float 为数字字符串，string_array 为逗号拼接）。
    /// </summary>
    public static IReadOnlyDictionary<string, (string TypeName, string StringValue)> GetRecordFields(
        string snapshotPath, string table, string id)
    {
        var doc = LoadDocument(snapshotPath);
        if (doc?.Records == null) return new Dictionary<string, (string, string)>();
        foreach (var r in doc.Records)
        {
            if (!string.Equals(r.Table, table, StringComparison.Ordinal)) continue;
            if (!string.Equals(r.Id, id, StringComparison.Ordinal)) continue;
            if (r.Fields == null) return new Dictionary<string, (string, string)>();
            var result = new Dictionary<string, (string, string)>(StringComparer.Ordinal);
            foreach (var (key, field) in r.Fields)
                result[key] = (field.TypeName, ExtractStringValue(field));
            return result;
        }
        return new Dictionary<string, (string, string)>();
    }

    /// <summary>
    /// 将 SnapshotField 的 JsonElement 值转为适合 Data.Set / Parse 的字符串表示
    /// </summary>
    private static string ExtractStringValue(SnapshotField field)
    {
        try
        {
            return field.TypeName switch
            {
                "string" or "enum" => field.Value.GetString() ?? "",
                "string_array" => field.Value.GetString() ?? "", // 逗号拼接存储
                "bool" => field.Value.GetBoolean() ? "true" : "false",
                "int" => field.Value.GetInt32().ToString(System.Globalization.CultureInfo.InvariantCulture),
                "float" => field.Value.GetSingle().ToString(System.Globalization.CultureInfo.InvariantCulture),
                _ => field.Value.GetRawText()
            };
        }
        catch { return ""; }
    }

    // ================= 内部逻辑 =================

    /// <summary>
    /// 加载并缓存 snapshot 文档。支持 res://（Godot VFS）和普通文件系统路径。
    /// 解析失败时缓存 null 以避免重复 IO。
    /// </summary>
    private static SnapshotDocument? LoadDocument(string path)
    {
        // 先查缓存（初始化阶段多次调用同一路径）
        if (_docCache.TryGetValue(path, out var cached)) return cached;

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
                    _docCache[path] = null;
                    return null;
                }
                json = file.GetAsText();
            }
            else
            {
                // 普通文件系统路径（测试用）
                json = System.IO.File.ReadAllText(path);
            }

            var doc = JsonSerializer.Deserialize<SnapshotDocument>(json, SerializerOptions);
            _docCache[path] = doc;
            return doc;
        }
        catch (Exception e)
        {
            _log.Error($"Snapshot 读取或解析失败: {path} → {e.Message}");
            _docCache[path] = null;
            return null;
        }
    }

    /// <summary>
    /// 将单条记录的所有字段转换后批量写入 Data 容器。
    /// 未注册的 key 跳过（兼容当前版本未启用的 capability 字段）。
    /// </summary>
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

    /// <summary>
    /// 根据 DataMeta.Type 将 snapshot 字段值转换为目标运行时类型。
    /// 支持 float/int/string/bool/enum 五种类型名。
    /// </summary>
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

    /// <summary>float 源 → 目标数值类型（int/double/float）</summary>
    private static object ConvertNumericToTarget(float value, Type targetType)
    {
        if (targetType == typeof(int)) return (int)value;
        if (targetType == typeof(double)) return (double)value;
        return value;
    }

    /// <summary>int 源 → 目标数值类型（float/double/int）</summary>
    private static object ConvertNumericToTarget(int value, Type targetType)
    {
        if (targetType == typeof(float)) return (float)value;
        if (targetType == typeof(double)) return (double)value;
        return value;
    }

    /// <summary>枚举名解析，区分大小写；失败返回 null</summary>
    private static object? ParseEnum(Type enumType, string? name)
    {
        if (string.IsNullOrEmpty(name) || !enumType.IsEnum) return null;
        return Enum.TryParse(enumType, name, ignoreCase: false, out var result) ? result : null;
    }

    // ================= 内部 DTO（仅用于反序列化）=================

    /// <summary>snapshot JSON 顶层文档结构</summary>
    private class SnapshotDocument
    {
        /// <summary>实体数据记录列表（table + id + fields）</summary>
        [JsonPropertyName("records")]
        public List<SnapshotRecord>? Records { get; set; }

        /// <summary>资源条目列表（供 ResourceCatalog 查询）</summary>
        [JsonPropertyName("resources")]
        public List<SnapshotResourceEntry>? Resources { get; set; }
    }

    /// <summary>单条实体记录：按 table + id 定位，fields 为字段字典</summary>
    private class SnapshotRecord
    {
        /// <summary>记录所属表名（如 "enemy", "weapon"）</summary>
        [JsonPropertyName("table")]
        public string Table { get; set; } = "";

        /// <summary>记录唯一标识（如 "slime_basic"）</summary>
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        /// <summary>字段字典：key 为 DataKey 名，value 为类型+值</summary>
        [JsonPropertyName("fields")]
        public Dictionary<string, SnapshotField>? Fields { get; set; }
    }

    /// <summary>单个字段：TypeName 决定如何解析 Value（float/int/string/bool/enum/string_array）</summary>
    private class SnapshotField
    {
        /// <summary>字段类型名，如 "float", "int", "string", "bool", "enum"</summary>
        [JsonPropertyName("type")]
        public string TypeName { get; set; } = "";

        /// <summary>原始 JSON 值，按 TypeName 选择对应 GetXxx() 解析</summary>
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
