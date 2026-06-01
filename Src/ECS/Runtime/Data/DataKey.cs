/// <summary>
/// Data 字段的类型安全 stable key 句柄。
/// </summary>
/// <typeparam name="T">字段运行时值类型。</typeparam>
/// <param name="StableKey">descriptor stable key。</param>
public readonly record struct DataKey<T>(string StableKey)
{
}
