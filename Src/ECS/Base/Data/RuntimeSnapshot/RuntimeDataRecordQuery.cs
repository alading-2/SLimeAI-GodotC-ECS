using System;
using System.Collections.Generic;
using System.Text.Json;

/// <summary>
/// runtime snapshot record 只读查询入口。
/// </summary>
public sealed class RuntimeDataRecordQuery
{
    private readonly RuntimeDataSnapshot _snapshot;
    private readonly Dictionary<string, List<RuntimeDataRecordDto>> _recordsByTable = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeDataRecordDto> _recordsByTableId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, RuntimeDataRecordDto> _recordsByTableName = new(StringComparer.Ordinal);

    /// <summary>
    /// 使用 runtime snapshot 创建查询索引。
    /// </summary>
    /// <param name="snapshot">runtime snapshot。</param>
    public RuntimeDataRecordQuery(RuntimeDataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _snapshot = snapshot; // 保留 snapshot 引用，避免复制 records
        BuildIndexes(snapshot.Records);
    }

    /// <summary>
    /// 使用已初始化的 DataRuntimeBootstrap 创建查询索引。
    /// </summary>
    /// <param name="bootstrap">runtime bootstrap。</param>
    public RuntimeDataRecordQuery(DataRuntimeBootstrap bootstrap)
        : this(bootstrap?.Snapshot ?? throw new ArgumentNullException(nameof(bootstrap)))
    {
    }

    /// <summary>
    /// 当前 snapshot。
    /// </summary>
    public RuntimeDataSnapshot Snapshot => _snapshot;

    /// <summary>
    /// 获取指定 table 的全部 records。
    /// </summary>
    /// <param name="table">record table。</param>
    public IReadOnlyList<RuntimeDataRecordDto> GetRecords(string table)
    {
        if (string.IsNullOrWhiteSpace(table))
        {
            throw new ArgumentException("runtime snapshot record table 不能为空。", nameof(table));
        }

        return _recordsByTable.TryGetValue(table, out var records)
            ? records
            : Array.Empty<RuntimeDataRecordDto>();
    }

    /// <summary>
    /// 按 table/id 获取 record，未找到时 fail-fast。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="id">record id。</param>
    public RuntimeDataRecordDto GetRequired(string table, string id)
    {
        if (_recordsByTableId.TryGetValue(MakeKey(table, id), out var record))
        {
            return record;
        }

        throw new KeyNotFoundException($"runtime snapshot record 不存在：{table}/{id}");
    }

    /// <summary>
    /// 按 table/name 获取 record，未找到时 fail-fast。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="name">record name。</param>
    public RuntimeDataRecordDto GetRequiredByName(string table, string name)
    {
        if (_recordsByTableName.TryGetValue(MakeKey(table, name), out var record))
        {
            return record;
        }

        throw new KeyNotFoundException($"runtime snapshot record 不存在：{table}/name:{name}");
    }

    private void BuildIndexes(IEnumerable<RuntimeDataRecordDto> records)
    {
        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Table) || string.IsNullOrWhiteSpace(record.Id))
            {
                throw new InvalidOperationException($"runtime snapshot record 缺少 table/id：table={record.Table}, id={record.Id}");
            }

            if (!_recordsByTable.TryGetValue(record.Table, out var tableRecords))
            {
                tableRecords = new List<RuntimeDataRecordDto>();
                _recordsByTable.Add(record.Table, tableRecords);
            }

            tableRecords.Add(record);
            AddUnique(_recordsByTableId, MakeKey(record.Table, record.Id), record, "id");
            if (!string.IsNullOrWhiteSpace(record.Name))
            {
                AddUnique(_recordsByTableName, MakeKey(record.Table, record.Name), record, "name");
            }
        }
    }

    private static void AddUnique(
        Dictionary<string, RuntimeDataRecordDto> index,
        string key,
        RuntimeDataRecordDto record,
        string indexName)
    {
        if (index.ContainsKey(key))
        {
            throw new InvalidOperationException($"runtime snapshot record {indexName} 重复：{key}");
        }

        index.Add(key, record);
    }

    private static string MakeKey(string table, string value)
    {
        return $"{table}\u001f{value}";
    }
}

/// <summary>
/// 生成系统使用的单位生成规则投影。
/// </summary>
public sealed record UnitSpawnDefinition(
    string Table,
    string RecordId,
    string Name,
    string VisualScenePath,
    bool IsEnableSpawnRule,
    SpawnPositionStrategy SpawnStrategy,
    int SpawnMinWave,
    int SpawnMaxWave,
    float SpawnInterval,
    int SpawnMaxCountPerWave,
    int SingleSpawnCount,
    int SingleSpawnVariance,
    float SpawnStartDelay,
    int SpawnWeight);

/// <summary>
/// 技能展示和授予使用的 snapshot 投影。
/// </summary>
public sealed record AbilityDefinitionView(
    string Table,
    string RecordId,
    string Name,
    string FeatureGroupId,
    string FeatureHandlerId,
    string Description,
    AbilityType AbilityType,
    AbilityTriggerMode TriggerMode);

/// <summary>
/// SystemCore 配置 snapshot 投影。
/// </summary>
public sealed record SystemConfigDefinition(
    string Table,
    string RecordId,
    string SystemId,
    SystemGroup MountGroup,
    SystemTag Tags,
    bool Required,
    bool AutoLoad,
    bool StartEnabled,
    int Priority,
    GameFlowState AllowedFlowStates,
    OverlayFlags RequiredOverlays,
    OverlayFlags BlockedOverlays,
    SimulationState AllowedSimulationStates,
    string[] Dependencies,
    string Description);

/// <summary>
/// SystemCore 预设 snapshot 投影。
/// </summary>
public sealed record SystemPresetDefinition(
    string Table,
    string RecordId,
    string PresetName,
    bool IsActive,
    SystemTag EnabledTags,
    string[] EnabledSystemIds,
    string[] DisabledSystemIds,
    string Description);

/// <summary>
/// ResourceCatalog 使用的 snapshot 资源投影。
/// </summary>
public sealed record ResourceCatalogProjection(
    string Category,
    string Key,
    string Path,
    string OwnerCapability,
    string SourceTable,
    string SourceRowId,
    string SourceColumn);

/// <summary>
/// runtime snapshot record typed projection 集中入口。
/// </summary>
public static class RuntimeDataRecordProjection
{
    /// <summary>
    /// 投影 unit.enemy record 为生成规则。
    /// </summary>
    public static UnitSpawnDefinition ToUnitSpawnDefinition(RuntimeDataRecordDto record)
    {
        EnsureTable(record, "unit.enemy");
        return new UnitSpawnDefinition(
            record.Table,
            record.Id,
            Read<string>(record, "Name"),
            Read<string>(record, "VisualScenePath"),
            Read<bool>(record, "IsEnableSpawnRule"),
            ReadEnum<SpawnPositionStrategy>(record, "SpawnStrategy"),
            Read<int>(record, "SpawnMinWave"),
            Read<int>(record, "SpawnMaxWave"),
            Read<float>(record, "SpawnInterval"),
            Read<int>(record, "SpawnMaxCountPerWave"),
            Read<int>(record, "SingleSpawnCount"),
            Read<int>(record, "SingleSpawnVariance"),
            Read<float>(record, "SpawnStartDelay"),
            Read<int>(record, "SpawnWeight"));
    }

    /// <summary>
    /// 投影 ability record 为测试面板和授予入口视图。
    /// </summary>
    public static AbilityDefinitionView ToAbilityDefinitionView(RuntimeDataRecordDto record)
    {
        EnsureTable(record, "ability");
        return new AbilityDefinitionView(
            record.Table,
            record.Id,
            Read<string>(record, "Name"),
            Read<string>(record, "AbilityFeatureGroup"),
            Read<string>(record, "FeatureHandlerId"),
            Read<string>(record, "Description"),
            ReadEnum<AbilityType>(record, "AbilityType"),
            ReadEnum<AbilityTriggerMode>(record, "AbilityTriggerMode"));
    }

    /// <summary>
    /// 投影 system.config record。
    /// </summary>
    public static SystemConfigDefinition ToSystemConfigDefinition(RuntimeDataRecordDto record)
    {
        EnsureTable(record, "system.config");
        return new SystemConfigDefinition(
            record.Table,
            record.Id,
            Read<string>(record, "SystemId"),
            ReadEnum<SystemGroup>(record, "MountGroup"),
            ReadFlags<SystemTag>(record, "Tags"),
            Read<bool>(record, "Required"),
            Read<bool>(record, "AutoLoad"),
            Read<bool>(record, "StartEnabled"),
            Read<int>(record, "Priority"),
            ReadFlags<GameFlowState>(record, "AllowedFlowStates"),
            ReadFlags<OverlayFlags>(record, "RequiredOverlays"),
            ReadFlags<OverlayFlags>(record, "BlockedOverlays"),
            ReadFlags<SimulationState>(record, "AllowedSimulationStates"),
            ReadStringArray(record, "Dependencies"),
            Read<string>(record, "Description"));
    }

    /// <summary>
    /// 投影 system.preset record。
    /// </summary>
    public static SystemPresetDefinition ToSystemPresetDefinition(RuntimeDataRecordDto record)
    {
        EnsureTable(record, "system.preset");
        return new SystemPresetDefinition(
            record.Table,
            record.Id,
            Read<string>(record, "PresetName"),
            Read<bool>(record, "IsActive"),
            ReadFlags<SystemTag>(record, "EnabledTags"),
            ReadStringArray(record, "EnabledSystemIds"),
            ReadStringArray(record, "DisabledSystemIds"),
            Read<string>(record, "Description"));
    }

    /// <summary>
    /// 投影 runtime snapshot resources。
    /// </summary>
    public static IReadOnlyList<ResourceCatalogProjection> ToResourceCatalogProjections(RuntimeDataSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var result = new List<ResourceCatalogProjection>(snapshot.Resources.Count);
        foreach (var resource in snapshot.Resources)
        {
            result.Add(new ResourceCatalogProjection(
                ReadResourceString(resource, "category"),
                ReadResourceString(resource, "key"),
                ReadResourceString(resource, "path"),
                ReadResourceString(resource, "ownerCapability"),
                ReadResourceString(resource, "sourceTable"),
                ReadResourceString(resource, "sourceRowId"),
                ReadResourceString(resource, "sourceColumn")));
        }

        return result;
    }

    private static T Read<T>(RuntimeDataRecordDto record, string fieldKey)
    {
        var field = GetRequiredField(record, fieldKey);
        var valueType = ParseValueType(field.Type, record, fieldKey);
        var rawValue = NormalizeRecordValue(field.Value);
        if (!DataValueConverter.TryConvert(rawValue, valueType, out var convertedValue, out var error))
        {
            throw new InvalidOperationException($"runtime snapshot projection 转换失败：{FormatRecord(record)} field={fieldKey}, error={error}");
        }

        return (T)DataValueConverter.ConvertForRead(convertedValue, typeof(T), valueType)!;
    }

    private static T ReadEnum<T>(RuntimeDataRecordDto record, string fieldKey)
        where T : struct, Enum
    {
        var text = Read<string>(record, fieldKey);
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        return Enum.Parse<T>(text, ignoreCase: false);
    }

    private static T ReadFlags<T>(RuntimeDataRecordDto record, string fieldKey)
        where T : struct, Enum
    {
        var text = Read<string>(record, fieldKey);
        if (string.IsNullOrWhiteSpace(text))
        {
            return default;
        }

        T result = default;
        var resultValue = Convert.ToUInt64(result);
        var parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (var i = 0; i < parts.Length; i++)
        {
            var parsed = Enum.Parse<T>(parts[i], ignoreCase: false);
            resultValue |= Convert.ToUInt64(parsed);
        }

        return (T)Enum.ToObject(typeof(T), resultValue);
    }

    private static string[] ReadStringArray(RuntimeDataRecordDto record, string fieldKey)
    {
        var value = Read<object>(record, fieldKey);
        return value switch
        {
            string[] array => array,
            string text when string.IsNullOrWhiteSpace(text) => Array.Empty<string>(),
            string text => text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            _ => Array.Empty<string>()
        };
    }

    private static RuntimeDataFieldDto GetRequiredField(RuntimeDataRecordDto record, string fieldKey)
    {
        if (record.Fields.TryGetValue(fieldKey, out var field))
        {
            return field;
        }

        throw new InvalidOperationException($"runtime snapshot projection 缺少字段：{FormatRecord(record)} field={fieldKey}");
    }

    private static void EnsureTable(RuntimeDataRecordDto record, string expectedTable)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!string.Equals(record.Table, expectedTable, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"runtime snapshot projection table 不匹配：expected={expectedTable}, actual={record.Table}/{record.Id}");
        }
    }

    private static DataValueType ParseValueType(string raw, RuntimeDataRecordDto record, string fieldKey)
    {
        var normalized = raw.Trim().ToLowerInvariant();
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
            _ => throw new InvalidOperationException($"runtime snapshot projection 未知字段类型：{FormatRecord(record)} field={fieldKey}, type={raw}")
        };
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

    private static string ReadResourceString(Dictionary<string, JsonElement> resource, string key)
    {
        if (!resource.TryGetValue(key, out var value))
        {
            throw new InvalidOperationException($"runtime snapshot resource 缺少字段：{key}");
        }

        return value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? string.Empty
            : value.GetRawText();
    }

    private static string FormatRecord(RuntimeDataRecordDto record)
    {
        return $"{record.Table}/{record.Id}";
    }
}
