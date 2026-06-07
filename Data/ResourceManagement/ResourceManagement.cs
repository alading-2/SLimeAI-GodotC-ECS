using System;
using System.Collections.Generic;
using System.Linq;
/// <summary>
/// 旧资源加载入口。
/// <para>current API 是 <see cref="ResourceLoading"/>；本类只保留为迁移期内部转发，不再提供 contains fallback。</para>
/// </summary>
public static class ResourceManagement
{
    /// <summary>
    /// 从指定分类精确加载资源。
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="name">资源名称</param>
    /// <param name="category">资源分类</param>
    /// <returns>加载的资源，失败返回 null</returns>
    public static T? Load<T>(string name, ResourceCategory category) where T : class
    {
        return ResourceLoading.Load<T>(
            name,
            category,
            ResourceLoadSource.Runtime("ResourceManagementCompatibility", $"Load:{category}/{name}"));
    }

    /// <summary>
    /// 按 Godot 资源路径加载资源。
    /// <para>DataOS snapshot/resources 使用 res:// 路径保存场景/贴图引用时，通过此入口统一加载。</para>
    /// </summary>
    /// <typeparam name="T">资源类型。</typeparam>
    /// <param name="path">Godot 资源路径，例如 res://assets/Effect/xxx.tscn。</param>
    /// <returns>加载的资源，失败返回 null。</returns>
    public static T? LoadPath<T>(string path) where T : class
    {
        return ResourceLoading.LoadPath<T>(
            path,
            ResourceLoadSource.Runtime("ResourceManagementCompatibility", $"LoadPath:{typeof(T).Name}"));
    }



    /// <summary>
    /// 加载指定分类下的所有资源
    /// </summary>
    /// <typeparam name="T">资源类型</typeparam>
    /// <param name="category">资源分类</param>
    /// <param name="pathFilter">路径过滤（可选，如 "Unit/Enemy"）</param>
    /// <returns>资源列表</returns>
    public static List<T> LoadAll<T>(ResourceCategory category, string pathFilter = "") where T : class
    {
        return ResourceLoading.LoadAll<T>(
            category,
            ResourceLoadSource.Runtime("ResourceManagementCompatibility", $"LoadAll:{category}"),
            pathFilter);
    }

    /// <summary>
    /// 获取分类下所有资源名称
    /// </summary>
    public static List<string> GetNames(ResourceCategory category, string pathFilter = "")
    {
        var dict = GetDictionaryByCategory(category);
        if (string.IsNullOrEmpty(pathFilter))
            return dict.Keys.ToList();

        return dict.Where(kvp => kvp.Value.Path.Contains(pathFilter, StringComparison.OrdinalIgnoreCase))
                   .Select(kvp => kvp.Key)
                   .ToList();
    }

    /// <summary>
    /// 根据分类获取对应的字典
    /// </summary>
    private static Dictionary<string, ResourceData> GetDictionaryByCategory(ResourceCategory category)
    {
        if (ResourcePaths.Resources.TryGetValue(category, out var dict))
        {
            return dict;
        }
        return new Dictionary<string, ResourceData>();
    }
}
