/// <summary>
/// Data 字段的类型安全 stable key 句柄。
/// </summary>
/// <typeparam name="T">字段运行时值类型。</typeparam>
/// <param name="StableKey">descriptor stable key。</param>
public readonly record struct DataKey<T>(string StableKey)
{
    /// <summary>
    /// 兼容旧 DataMeta 调用点的 stable key 别名。
    /// </summary>
    public string Key => StableKey;

    /// <summary>
    /// 迁移期允许 typed handle 进入仍接收 string 的旧 API。
    /// </summary>
    /// <param name="key">typed DataKey handle。</param>
    public static implicit operator string(DataKey<T> key) => key.StableKey;
}
