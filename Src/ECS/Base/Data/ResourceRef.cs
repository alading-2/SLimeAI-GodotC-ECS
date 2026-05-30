/// <summary>
/// DataOS object_ref 的可序列化资源引用。
/// </summary>
/// <param name="Path">Godot 资源路径或资源稳定键。</param>
public readonly record struct ResourceRef(string Path)
{
    /// <summary>
    /// 是否包含可用引用。
    /// </summary>
    public bool HasValue => !string.IsNullOrWhiteSpace(Path);

    public override string ToString()
    {
        return Path ?? string.Empty;
    }
}
