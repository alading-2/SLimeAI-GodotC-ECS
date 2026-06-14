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
    /// <summary>持久化字段，写入后随 Entity 生命周期保存。</summary>
    Persisted,
    /// <summary>运行时状态字段，Entity 存活期间有效，不持久化。</summary>
    RuntimeState,
    /// <summary>纯运行时字段，仅运行时存在，snapshot 不写入也不读取。</summary>
    RuntimeOnly,
    /// <summary>计算字段，不存储基础值，由 resolver 从依赖字段计算。</summary>
    Computed,
    /// <summary>authoring blob 字段，存储原始 JSON/文本，运行时由 Capability 解析。</summary>
    AuthoringBlob
}

/// <summary>
/// Data 字段的写入策略。
/// </summary>
public enum DataWritePolicy
{
    /// <summary>任意来源可写（Runtime / Loader / System / Debug）。</summary>
    ReadWrite,
    /// <summary>仅 Loader（snapshot apply）可写，运行时不可改。</summary>
    LoaderOnly,
    /// <summary>仅 System 和 Loader 可写，普通 Runtime 不可改。</summary>
    SystemOnly,
    /// <summary>只读 computed 字段，任何来源不可写。</summary>
    ComputedReadonly,
    /// <summary>仅 Debug 来源可写，用于调试工具。</summary>
    DebugOnly
}

/// <summary>
/// Data 数值范围处理策略。
/// </summary>
public enum DataRangePolicy
{
    /// <summary>不校验范围。</summary>
    None,
    /// <summary>写入时校验是否在 [MinValue, MaxValue] 范围内，超出则拒绝。</summary>
    Validate,
    /// <summary>运行时写入自动 clamp 到 [MinValue, MaxValue]。</summary>
    ClampRuntime,
    /// <summary>运行时写入超出范围则拒绝。</summary>
    RejectRuntime
}

/// <summary>
/// Data modifier 许可策略。
/// </summary>
public enum DataModifierPolicy
{
    /// <summary>不允许添加 modifier。</summary>
    None,
    /// <summary>仅数值类型字段允许 modifier。</summary>
    Numeric,
    /// <summary>仅 Debug 来源可添加 modifier。</summary>
    DebugOnly
}

/// <summary>
/// Entity 迁移时的 Data 字段复制策略。
/// </summary>
public enum DataMigrationPolicy
{
    /// <summary>默认迁移行为。</summary>
    Default,
    /// <summary>不迁移该字段。</summary>
    Never,
    /// <summary>始终迁移该字段。</summary>
    Always,
    /// <summary>仅在 profile 迁移时复制该字段。</summary>
    ProfileOnly
}
