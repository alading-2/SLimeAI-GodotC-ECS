using Godot;
using System.Collections.Generic;

/// <summary>
/// 极薄资源加载入口。
/// <para>只负责 strict lookup、source/owner/usage diagnostics 和 Godot 资源加载。</para>
/// </summary>
public static class ResourceLoading
{
    private static readonly Log _log = new(nameof(ResourceLoading));

    /// <summary>
    /// 按 generated catalog key 和 category 加载资源。找不到精确 key 时失败，不做 contains fallback。
    /// </summary>
    public static T? Load<T>(string key, ResourceCategory category) where T : class
    {
        return Load<T>(
            key,
            category,
            ResourceLoadSource.Runtime("ResourceLoading.Load", $"{category}/{key}"));
    }

    /// <summary>
    /// 按 generated catalog key 和 category 加载资源。找不到精确 key 时失败，不做 contains fallback。
    /// </summary>
    public static T? Load<T>(string key, ResourceCategory category, ResourceLoadSource source) where T : class
    {
        var result = TryLoad<T>(key, category, source);
        return result.Resource;
    }

    /// <summary>
    /// 按 generated catalog key 和 category 加载资源，并返回结构化结果。
    /// </summary>
    public static ResourceLoadResult<T> TryLoad<T>(string key, ResourceCategory category, ResourceLoadSource source) where T : class
    {
        if (!ResourcePaths.Resources.TryGetValue(category, out var resources))
        {
            return Fail<T>(
                ResourceLoadErrorCode.CategoryNotFound,
                $"Resource category not found: {category}",
                category,
                key,
                string.Empty,
                source);
        }

        if (!resources.TryGetValue(key, out var data))
        {
            return Fail<T>(
                ResourceLoadErrorCode.KeyNotFound,
                $"Resource key not found: {category}/{key}",
                category,
                key,
                string.Empty,
                source);
        }

        return TryLoadResolvedPath<T>(data.Path, data.Category, key, source);
    }

    /// <summary>
    /// 按明确来源的 Godot path 加载资源。只允许 DataOS/debug/test/明确 runtime owner 调用。
    /// </summary>
    public static T? LoadPath<T>(string path, ResourceLoadSource source) where T : class
    {
        var result = TryLoadPath<T>(path, source);
        return result.Resource;
    }

    /// <summary>
    /// 按明确来源的 Godot path 加载资源，并返回结构化结果。
    /// </summary>
    public static ResourceLoadResult<T> TryLoadPath<T>(string path, ResourceLoadSource source) where T : class
    {
        if (!source.IsSpecified)
        {
            return Fail<T>(
                ResourceLoadErrorCode.MissingSource,
                "Resource LoadPath requires source owner and usage.",
                ResourceCategory.Other,
                string.Empty,
                path,
                source);
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            return Fail<T>(
                ResourceLoadErrorCode.MissingPath,
                "Resource path is empty.",
                ResourceCategory.Other,
                string.Empty,
                path,
                source);
        }

        return TryLoadResolvedPath<T>(path, ResourceCategory.Other, string.Empty, source);
    }

    /// <summary>
    /// PackedScene path 加载快捷入口，替代旧 CommonTool.LoadPackedScene。
    /// </summary>
    public static PackedScene? LoadPackedScenePath(string path, ResourceLoadSource source)
    {
        return LoadPath<PackedScene>(path, source);
    }

    /// <summary>
    /// PackedScene path 加载快捷入口，并返回结构化结果。
    /// </summary>
    public static ResourceLoadResult<PackedScene> TryLoadPackedScenePath(string path, ResourceLoadSource source)
    {
        return TryLoadPath<PackedScene>(path, source);
    }

    /// <summary>
    /// 加载指定分类下的所有资源。
    /// </summary>
    public static List<T> LoadAll<T>(ResourceCategory category, string pathFilter = "") where T : class
    {
        return LoadAll<T>(
            category,
            ResourceLoadSource.Runtime("ResourceLoading.LoadAll", $"{category}"),
            pathFilter);
    }

    /// <summary>
    /// 加载指定分类下的所有资源。
    /// </summary>
    public static List<T> LoadAll<T>(ResourceCategory category, ResourceLoadSource source, string pathFilter = "") where T : class
    {
        var results = new List<T>();
        if (!ResourcePaths.Resources.TryGetValue(category, out var resources))
        {
            _log.Error($"未找到分类字典: {category}");
            return results;
        }

        foreach (var (key, data) in resources)
        {
            if (!string.IsNullOrEmpty(pathFilter) && !data.Path.Contains(pathFilter, System.StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = TryLoadResolvedPath<T>(data.Path, data.Category, key, source);
            if (result.Resource != null)
            {
                results.Add(result.Resource);
            }
            else if (string.IsNullOrEmpty(pathFilter))
            {
                _log.Warn($"加载失败: {category}/{key} ({data.Path}) code={result.ErrorCode}");
            }
        }

        return results;
    }

    private static ResourceLoadResult<T> TryLoadResolvedPath<T>(
        string path,
        ResourceCategory category,
        string key,
        ResourceLoadSource source) where T : class
    {
        var resource = GD.Load<T>(path);
        if (resource == null)
        {
            return Fail<T>(
                ResourceLoadErrorCode.LoadFailed,
                $"Resource load failed: {path}",
                category,
                key,
                path,
                source);
        }

        return ResourceLoadResult<T>.Ok(resource, category, key, path, source);
    }

    private static ResourceLoadResult<T> Fail<T>(
        ResourceLoadErrorCode errorCode,
        string message,
        ResourceCategory category,
        string key,
        string path,
        ResourceLoadSource source) where T : class
    {
        _log.Warn($"{message} owner={source.Owner} usage={source.Usage}");
        return ResourceLoadResult<T>.Fail(errorCode, message, category, key, path, source);
    }
}
