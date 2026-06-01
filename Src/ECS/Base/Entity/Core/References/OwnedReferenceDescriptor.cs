/// <summary>
/// 描述一个 child -> owner 单引用和 owner -> child list 多引用的 Data projection。
/// </summary>
public readonly record struct OwnedReferenceDescriptor(
    DataKey<string> ChildToOwnerKey,
    DataKey<string[]> OwnerListKey)
{
    /// <summary>descriptor 是否可用于运行时注册。</summary>
    public bool IsValid =>
        !string.IsNullOrWhiteSpace(ChildToOwnerKey.StableKey)
        && !string.IsNullOrWhiteSpace(OwnerListKey.StableKey);
}
