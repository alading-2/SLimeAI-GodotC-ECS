/// <summary>
/// 资源加载错误码。
/// </summary>
public enum ResourceLoadErrorCode
{
    None = 0,
    MissingSource = 1,
    MissingPath = 2,
    CategoryNotFound = 3,
    KeyNotFound = 4,
    LoadFailed = 5
}

/// <summary>
/// 资源加载结构化结果。
/// </summary>
public sealed class ResourceLoadResult<T> where T : class
{
    private ResourceLoadResult(
        T? resource,
        ResourceCategory category,
        string key,
        string path,
        ResourceLoadSource source,
        ResourceLoadErrorCode errorCode,
        string message)
    {
        Resource = resource;
        Category = category;
        Key = key;
        Path = path;
        Source = source;
        ErrorCode = errorCode;
        Message = message;
    }

    public T? Resource { get; }
    public bool Success => Resource != null && ErrorCode == ResourceLoadErrorCode.None;
    public ResourceCategory Category { get; }
    public string Key { get; }
    public string Path { get; }
    public ResourceLoadSource Source { get; }
    public ResourceLoadErrorCode ErrorCode { get; }
    public string Message { get; }

    public static ResourceLoadResult<T> Ok(
        T resource,
        ResourceCategory category,
        string key,
        string path,
        ResourceLoadSource source)
    {
        return new ResourceLoadResult<T>(resource, category, key, path, source, ResourceLoadErrorCode.None, string.Empty);
    }

    public static ResourceLoadResult<T> Fail(
        ResourceLoadErrorCode errorCode,
        string message,
        ResourceCategory category,
        string key,
        string path,
        ResourceLoadSource source)
    {
        return new ResourceLoadResult<T>(null, category, key, path, source, errorCode, message);
    }
}
