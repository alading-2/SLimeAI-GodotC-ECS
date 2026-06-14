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
    /// 按 table/display name 获取 record，未找到时 fail-fast。
    /// 仅供 debug、editor 和测试入口使用；生产身份必须使用 table/id。
    /// </summary>
    /// <param name="table">record table。</param>
    /// <param name="displayName">record display name。</param>
    public RuntimeDataRecordDto GetRequiredByDisplayNameForDebug(string table, string displayName)
    {
        if (_recordsByTableName.TryGetValue(MakeKey(table, displayName), out var record))
        {
            return record;
        }

        throw new KeyNotFoundException($"runtime snapshot record 不存在：{table}/displayName:{displayName}");
    }

    /// <summary>
    /// 构建三级索引：table → records 列表、table+id → 单条 record、table+name → 单条 record。
    /// </summary>
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

    /// <summary>
    /// 向索引添加记录，key 重复时 fail-fast。
    /// </summary>
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

    /// <summary>
    /// 生成索引 key：table + （单元分隔符）+ value，避免 table 和 value 拼接冲突。
    /// </summary>
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
            Read(record, GeneratedDataKey.Name),
            Read(record, GeneratedDataKey.VisualScenePath),
            Read(record, GeneratedDataKey.IsEnableSpawnRule),
            Read(record, GeneratedDataKey.SpawnStrategy),
            Read(record, GeneratedDataKey.SpawnMinWave),
            Read(record, GeneratedDataKey.SpawnMaxWave),
            Read(record, GeneratedDataKey.SpawnInterval),
            Read(record, GeneratedDataKey.SpawnMaxCountPerWave),
            Read(record, GeneratedDataKey.SingleSpawnCount),
            Read(record, GeneratedDataKey.SingleSpawnVariance),
            Read(record, GeneratedDataKey.SpawnStartDelay),
            Read(record, GeneratedDataKey.SpawnWeight));
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
            Read(record, GeneratedDataKey.Name),
            Read(record, GeneratedDataKey.AbilityFeatureGroup),
            Read(record, GeneratedDataKey.FeatureHandlerId),
            Read(record, GeneratedDataKey.Description),
            Read(record, GeneratedDataKey.AbilityType),
            Read(record, GeneratedDataKey.AbilityTriggerMode));
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
            Read(record, GeneratedDataKey.SystemId),
            ReadEnum<SystemGroup>(record, GeneratedDataKey.MountGroup),
            ReadFlags<SystemTag>(record, GeneratedDataKey.Tags),
            Read(record, GeneratedDataKey.Required),
            Read(record, GeneratedDataKey.AutoLoad),
            Read(record, GeneratedDataKey.StartEnabled),
            Read(record, GeneratedDataKey.Priority),
            ReadFlags<GameFlowState>(record, GeneratedDataKey.AllowedFlowStates),
            ReadFlags<OverlayFlags>(record, GeneratedDataKey.RequiredOverlays),
            ReadFlags<OverlayFlags>(record, GeneratedDataKey.BlockedOverlays),
            ReadFlags<SimulationState>(record, GeneratedDataKey.AllowedSimulationStates),
            ReadStringArray(record, GeneratedDataKey.Dependencies),
            Read(record, GeneratedDataKey.Description));
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
            Read(record, GeneratedDataKey.PresetName),
            Read(record, GeneratedDataKey.IsActive),
            ReadFlags<SystemTag>(record, GeneratedDataKey.EnabledTags),
            ReadStringArray(record, GeneratedDataKey.EnabledSystemIds),
            ReadStringArray(record, GeneratedDataKey.DisabledSystemIds),
            Read(record, GeneratedDataKey.Description));
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

    /// <summary>
    /// 从 record field 读取 typed 值：校验类型兼容性 → 转换原始值 → 转为目标 CLR 类型。
    /// </summary>
    private static T Read<T>(RuntimeDataRecordDto record, DataKey<T> key)
    {
        return Read<T>(record, key.StableKey);
    }

    private static T Read<T>(RuntimeDataRecordDto record, string fieldKey)
    {
        var field = GetRequiredField(record, fieldKey);
        var valueType = ParseValueType(field.Type, record, fieldKey);
        if (!DataValueConverter.IsCompatible<T>(valueType))
        {
            throw new InvalidOperationException($"runtime snapshot projection 字段类型不匹配：{FormatRecord(record)} field={fieldKey}, snapshotType={field.Type}, expectedClr={typeof(T).Name}");
        }

        var rawValue = NormalizeRecordValue(field.Value);
        if (!DataValueConverter.TryConvert(rawValue, valueType, out var convertedValue, out var error))
        {
            throw new InvalidOperationException($"runtime snapshot projection 转换失败：{FormatRecord(record)} field={fieldKey}, error={error}");
        }

        try
        {
            return (T)DataValueConverter.ConvertForRead(convertedValue, typeof(T), valueType)!;
        }
        catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
        {
            throw new InvalidOperationException($"runtime snapshot projection 读取失败：{FormatRecord(record)} field={fieldKey}, expectedClr={typeof(T).Name}, error={ex.Message}", ex);
        }
    }

    /// <summary>
    /// 从 record field 读取 string 并解析为 enum。空文本返回 default。
    /// </summary>
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

    private static T ReadEnum<T>(RuntimeDataRecordDto record, DataKey<string> key)
        where T : struct, Enum
    {
        return ReadEnum<T>(record, key.StableKey);
    }

    /// <summary>
    /// 从 record field 读取逗号分隔的 flags 文本并合并为 flags enum。如 "SystemA,SystemB" → SystemTag.A | SystemTag.B。
    /// </summary>
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

    private static T ReadFlags<T>(RuntimeDataRecordDto record, DataKey<string> key)
        where T : struct, Enum
    {
        return ReadFlags<T>(record, key.StableKey);
    }

    private static string[] ReadStringArray(RuntimeDataRecordDto record, DataKey<string[]> key)
    {
        return Read(record, key);
    }

    /// <summary>
    /// 从 record field 读取 string[]。支持直接传入数组或逗号分隔文本。
    /// </summary>
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

    /// <summary>
    /// 获取 record 中指定 field，不存在时 fail-fast。
    /// </summary>
    private static RuntimeDataFieldDto GetRequiredField(RuntimeDataRecordDto record, string fieldKey)
    {
        if (record.Fields.TryGetValue(fieldKey, out var field))
        {
            return field;
        }

        throw new InvalidOperationException($"runtime snapshot projection 缺少字段：{FormatRecord(record)} field={fieldKey}");
    }

    /// <summary>
    /// 校验 record table 是否与期望值一致，不一致则 fail-fast。
    /// </summary>
    private static void EnsureTable(RuntimeDataRecordDto record, string expectedTable)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!string.Equals(record.Table, expectedTable, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"runtime snapshot projection table 不匹配：expected={expectedTable}, actual={record.Table}/{record.Id}");
        }
    }

    /// <summary>
    /// 解析 snapshot field type 文本为 DataValueType 枚举。支持 camelCase 和 snake_case 两种格式。
    /// </summary>
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

    /// <summary>
    /// 将 snapshot record 的 JsonElement 字段值转为 CLR 值：string→string, number→string(原始文本), true/false→bool, null→null。
    /// 数值保留原始文本而非 double，由下游 TryConvert 按目标类型解析。
    /// </summary>
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

    /// <summary>
    /// 从 snapshot resource 字典读取 string 值，不存在时 fail-fast。
    /// </summary>
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
