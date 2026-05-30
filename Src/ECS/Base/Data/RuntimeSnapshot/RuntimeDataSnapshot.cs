using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// runtime_snapshot.json 根 DTO。
/// </summary>
public sealed record RuntimeDataSnapshot
{
    /// <summary>
    /// snapshot schema 版本。
    /// </summary>
    public int SchemaVersion { get; init; }

    /// <summary>
    /// 生成时间字符串。
    /// </summary>
    public string GeneratedAtUtc { get; init; } = string.Empty;

    /// <summary>
    /// snapshot manifest 原始对象。
    /// </summary>
    public Dictionary<string, object?> Manifest { get; init; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Data descriptor 列表。
    /// </summary>
    public List<RuntimeDataDescriptorDto> Descriptors { get; init; } = new();

    /// <summary>
    /// Data record 列表。
    /// </summary>
    public List<RuntimeDataRecordDto> Records { get; init; } = new();

    /// <summary>
    /// 资源记录列表，当前仅保留 JSON 形状。
    /// </summary>
    public List<Dictionary<string, JsonElement>> Resources { get; init; } = new();

    /// <summary>
    /// 按 table/id 查找 record，未找到时报错。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="id">record id。</param>
    public RuntimeDataRecordDto FindRecord(string table, string id)
    {
        for (var i = 0; i < Records.Count; i++)
        {
            var record = Records[i];
            if (string.Equals(record.Table, table, StringComparison.Ordinal) && string.Equals(record.Id, id, StringComparison.Ordinal))
            {
                return record;
            }
        }

        throw new KeyNotFoundException($"runtime snapshot record 不存在：{table}/{id}");
    }

    /// <summary>
    /// 按 table/name 查找 record，未找到时报错。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="name">record 展示名。</param>
    public RuntimeDataRecordDto FindRecordByName(string table, string name)
    {
        for (var i = 0; i < Records.Count; i++)
        {
            var record = Records[i];
            if (string.Equals(record.Table, table, StringComparison.Ordinal) && string.Equals(record.Name, name, StringComparison.Ordinal))
            {
                return record;
            }
        }

        throw new KeyNotFoundException($"runtime snapshot record 不存在：{table}/name:{name}");
    }
}

/// <summary>
/// runtime_snapshot.json records 中单条 record DTO。
/// </summary>
public sealed record RuntimeDataRecordDto
{
    /// <summary>
    /// record 所属 table。
    /// </summary>
    public string Table { get; init; } = string.Empty;

    /// <summary>
    /// record 稳定 id。
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// record 展示名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// stable key 到字段值的映射。
    /// </summary>
    public Dictionary<string, RuntimeDataFieldDto> Fields { get; init; } = new(StringComparer.Ordinal);

}

/// <summary>
/// runtime_snapshot.json record.fields 中单个字段 DTO。
/// </summary>
public sealed record RuntimeDataFieldDto
{
    /// <summary>
    /// snapshot 中声明的字段值类型。
    /// </summary>
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// snapshot 中声明的字段值。
    /// </summary>
    public object? Value { get; init; }
}
