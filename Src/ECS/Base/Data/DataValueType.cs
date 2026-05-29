/// <summary>
/// Data descriptor 支持的稳定值类型。
/// </summary>
public enum DataValueType
{
    String,
    StringArray,
    Int,
    Float,
    Double,
    Bool,
    Vector2,
    Enum,
    ModifierList,
    ObjectRef
}

/// <summary>
/// Data 字段的存储策略。
/// </summary>
public enum DataStoragePolicy
{
    Persisted,
    RuntimeState,
    RuntimeOnly,
    Computed,
    AuthoringBlob
}

/// <summary>
/// Data 字段的写入策略。
/// </summary>
public enum DataWritePolicy
{
    ReadWrite,
    LoaderOnly,
    SystemOnly,
    ComputedReadonly,
    DebugOnly
}

/// <summary>
/// Data 数值范围处理策略。
/// </summary>
public enum DataRangePolicy
{
    None,
    Validate,
    ClampRuntime,
    RejectRuntime
}

/// <summary>
/// Data modifier 许可策略。
/// </summary>
public enum DataModifierPolicy
{
    None,
    Numeric,
    DebugOnly
}

/// <summary>
/// Entity 迁移时的 Data 字段复制策略。
/// </summary>
public enum DataMigrationPolicy
{
    Default,
    Never,
    Always,
    ProfileOnly
}
