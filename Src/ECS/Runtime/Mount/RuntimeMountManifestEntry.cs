/// <summary>
/// Runtime 挂载点 manifest 条目。
/// </summary>
public sealed record RuntimeMountManifestEntry(
    RuntimeMountId Id,
    string RelativePath,
    RuntimeMountCreationMode CreationMode,
    string Owner,
    string Usage);
