using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 视觉预览资源目录服务。
/// <para>只以 ResourcePaths.Resources 为数据源，不扫描 res://。</para>
/// </summary>
internal sealed class VisualPreviewCatalogService
{
    /// <summary>
    /// 获取全部 Asset 分类预览资源。
    /// </summary>
    public IReadOnlyList<VisualPreviewEntry> GetEntries()
    {
        var result = new List<VisualPreviewEntry>();
        foreach (var (category, resources) in ResourcePaths.Resources)
        {
            if (!IsAssetCategory(category))
            {
                continue;
            }

            foreach (var (resourceKey, data) in resources)
            {
                var sceneName = ResolveSceneName(data.Path);
                result.Add(new VisualPreviewEntry(
                    resourceKey, // ResourcePaths 资源键
                    data.Category, // 实际资源分类
                    data.Path, // res:// 路径
                    sceneName, // 场景名
                    ResolveCatalogPath(data.Category), // 左侧分类
                    ResolveDefaultAnimation(data.Category) // 默认动作
                ));
            }
        }

        return result
            .OrderBy(entry => entry.CatalogPath, StringComparer.Ordinal)
            .ThenBy(entry => entry.SceneName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 获取稳定排序后的分类列表。
    /// </summary>
    /// <param name="entries">预览资源条目。</param>
    public IReadOnlyList<string> GetCatalogPaths(IReadOnlyList<VisualPreviewEntry> entries)
    {
        return entries
            .Select(entry => entry.CatalogPath)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// 判断是否属于 ResourcePaths 里的 Asset 分类。
    /// </summary>
    /// <param name="category">资源分类。</param>
    private static bool IsAssetCategory(ResourceCategory category)
    {
        return category.ToString().StartsWith("Asset", StringComparison.Ordinal);
    }

    /// <summary>
    /// 从资源路径取场景名。
    /// </summary>
    /// <param name="resourcePath">res:// 资源路径。</param>
    private static string ResolveSceneName(string resourcePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(resourcePath);
        return string.IsNullOrWhiteSpace(fileName) ? resourcePath : fileName;
    }

    /// <summary>
    /// 获取左侧 UI 分类名。
    /// </summary>
    /// <param name="category">资源分类。</param>
    private static string ResolveCatalogPath(ResourceCategory category)
    {
        return category.ToString();
    }

    /// <summary>
    /// 根据资源分类推断默认动作。
    /// </summary>
    /// <param name="category">资源分类。</param>
    private static string ResolveDefaultAnimation(ResourceCategory category)
    {
        var categoryName = category.ToString();
        if (categoryName.StartsWith("AssetUnit", StringComparison.Ordinal))
        {
            return Anim.Idle;
        }

        if (category == ResourceCategory.AssetEffect)
        {
            return Anim.Effect;
        }

        return string.Empty;
    }
}
