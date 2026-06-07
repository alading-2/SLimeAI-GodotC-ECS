using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// 资源目录诊断错误码。
/// </summary>
public enum ResourceCatalogDiagnosticCode
{
    DuplicateKey,
    MissingPath,
    StaleGeneratedSource,
    DataOsResourceLoadFailed
}

/// <summary>
/// 单条资源目录诊断。
/// </summary>
public sealed record ResourceCatalogDiagnostic(
    ResourceCatalogDiagnosticCode Code,
    ResourceCategory Category,
    string Key,
    string Path,
    string Message);

/// <summary>
/// 资源目录诊断报告。
/// </summary>
public sealed class ResourceCatalogDiagnosticsReport
{
    private readonly List<ResourceCatalogDiagnostic> _diagnostics = new();

    public IReadOnlyList<ResourceCatalogDiagnostic> Diagnostics => _diagnostics;
    public int ErrorCount => _diagnostics.Count;
    public bool HasErrors => ErrorCount > 0;

    public int DuplicateKeyCount => Count(ResourceCatalogDiagnosticCode.DuplicateKey);
    public int MissingPathCount => Count(ResourceCatalogDiagnosticCode.MissingPath);
    public int StaleGeneratedSourceCount => Count(ResourceCatalogDiagnosticCode.StaleGeneratedSource);
    public int DataOsResourceLoadFailedCount => Count(ResourceCatalogDiagnosticCode.DataOsResourceLoadFailed);

    public void Add(ResourceCatalogDiagnostic diagnostic)
    {
        _diagnostics.Add(diagnostic);
    }

    public string ToSummary()
    {
        return $"ResourceCatalogDiagnostics errors={ErrorCount}, duplicate={DuplicateKeyCount}, missingPath={MissingPathCount}, staleGenerated={StaleGeneratedSourceCount}, dataOsLoadFailed={DataOsResourceLoadFailedCount}";
    }

    private int Count(ResourceCatalogDiagnosticCode code)
    {
        return _diagnostics.Count(diagnostic => diagnostic.Code == code);
    }
}

/// <summary>
/// ResourcePaths / DataOS resource ref 离线诊断入口。
/// </summary>
public static class ResourceCatalogDiagnostics
{
    private static readonly string[] ResourceExtensions = { ".tscn", ".tres" };
    private static readonly string[] DataOsResourceFields =
    {
        GeneratedDataKey.AbilityIcon.StableKey,
        GeneratedDataKey.EffectScene.StableKey,
        GeneratedDataKey.ProjectileScene.StableKey,
        GeneratedDataKey.VisualScenePath.StableKey
    };

    /// <summary>
    /// 检查 generated catalog、当前项目资源和 DataOS 选定资源引用。
    /// </summary>
    public static ResourceCatalogDiagnosticsReport RunDefault()
    {
        var report = new ResourceCatalogDiagnosticsReport();
        var projectRoot = ResolveProjectRoot();
        CheckGeneratedCatalog(report);
        CheckGeneratedPaths(report, projectRoot);
        CheckStaleGeneratedSource(report, projectRoot);
        CheckDataOsResourceRefs(report);
        return report;
    }

    private static void CheckGeneratedCatalog(ResourceCatalogDiagnosticsReport report)
    {
        foreach (var (category, resources) in ResourcePaths.Resources)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (key, data) in resources)
            {
                if (!seen.Add(key))
                {
                    report.Add(new ResourceCatalogDiagnostic(
                        ResourceCatalogDiagnosticCode.DuplicateKey,
                        category,
                        key,
                        data.Path,
                        $"Duplicate generated key in category {category}: {key}"));
                }
            }
        }
    }

    private static void CheckGeneratedPaths(ResourceCatalogDiagnosticsReport report, string projectRoot)
    {
        foreach (var (category, resources) in ResourcePaths.Resources)
        {
            foreach (var (key, data) in resources)
            {
                if (!ResourcePathExists(projectRoot, data.Path))
                {
                    report.Add(new ResourceCatalogDiagnostic(
                        ResourceCatalogDiagnosticCode.MissingPath,
                        category,
                        key,
                        data.Path,
                        $"Generated resource path missing: {data.Path}"));
                }
            }
        }
    }

    private static void CheckStaleGeneratedSource(ResourceCatalogDiagnosticsReport report, string projectRoot)
    {
        var generated = ResourcePaths.Resources
            .SelectMany(pair => pair.Value.Select(item => NormalizeResourcePath(item.Value.Path)))
            .ToHashSet(StringComparer.Ordinal);

        foreach (var path in EnumerateCurrentResourcePaths(projectRoot))
        {
            if (generated.Contains(path))
            {
                continue;
            }

            report.Add(new ResourceCatalogDiagnostic(
                ResourceCatalogDiagnosticCode.StaleGeneratedSource,
                ResolveCategoryForPath(path),
                string.Empty,
                path,
                $"Current resource file is missing from generated ResourcePaths: {path}"));
        }
    }

    private static void CheckDataOsResourceRefs(ResourceCatalogDiagnosticsReport report)
    {
        var snapshot = DataRuntimeBootstrap.Default.Snapshot;
        foreach (var projection in RuntimeDataRecordProjection.ToResourceCatalogProjections(snapshot))
        {
            if (!string.IsNullOrWhiteSpace(projection.Path) && !CanLoadDataOsPath(projection.Path))
            {
                report.Add(new ResourceCatalogDiagnostic(
                    ResourceCatalogDiagnosticCode.DataOsResourceLoadFailed,
                    ResourceCategory.Data,
                    projection.Key,
                    projection.Path,
                    $"DataOS resource_entry load failed: {projection.SourceTable}/{projection.SourceRowId}.{projection.SourceColumn}"));
            }
        }

        foreach (var record in snapshot.Records)
        {
            foreach (var fieldKey in DataOsResourceFields)
            {
                if (!record.Fields.TryGetValue(fieldKey, out var field))
                {
                    continue;
                }

                var path = NormalizeFieldPath(field.Value);
                if (string.IsNullOrWhiteSpace(path))
                {
                    continue;
                }

                if (!CanLoadDataOsPath(path))
                {
                    report.Add(new ResourceCatalogDiagnostic(
                        ResourceCatalogDiagnosticCode.DataOsResourceLoadFailed,
                        ResourceCategory.Data,
                        record.Id,
                        path,
                        $"DataOS selected resource load failed: {record.Table}/{record.Id}.{fieldKey}"));
                }
            }
        }
    }

    private static bool CanLoadDataOsPath(string path)
    {
        var source = ResourceLoadSource.DataOS("ResourceCatalogDiagnostics", path);
        var result = ResourceLoading.TryLoadPath<Resource>(path, source);
        return result.Success;
    }

    private static IEnumerable<string> EnumerateCurrentResourcePaths(string projectRoot)
    {
        foreach (var relativeRoot in GetScanRoots())
        {
            var root = Path.Combine(projectRoot, relativeRoot);
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (!ResourceExtensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var relative = Path.GetRelativePath(projectRoot, file).Replace('\\', '/');
                if (relative.Contains("/.godot/", StringComparison.OrdinalIgnoreCase)
                    || relative.StartsWith(".godot/", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return "res://" + relative;
            }
        }
    }

    private static string[] GetScanRoots()
    {
        return new[]
        {
            "assets/Effect",
            "assets/Unit",
            "assets/Unit/Player",
            "assets/Unit/Enemy",
            "assets/Projectile",
            "Src/ECS/Runtime",
            "Src/ECS/Capabilities",
            "Src/ECS/Tools",
            "Src/ECS/UI",
            "Src/ECS/Test",
        };
    }

    private static ResourceCategory ResolveCategoryForPath(string resourcePath)
    {
        if (resourcePath.StartsWith("res://assets/Effect", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AssetEffect;
        if (resourcePath.StartsWith("res://assets/Unit/Player", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AssetUnitPlayer;
        if (resourcePath.StartsWith("res://assets/Unit/Enemy", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AssetUnitEnemy;
        if (resourcePath.StartsWith("res://assets/Unit", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AssetUnit;
        if (resourcePath.StartsWith("res://assets/Projectile", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.AssetProjectile;
        if (resourcePath.StartsWith("res://Src/ECS/UI", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.UI;
        if (resourcePath.StartsWith("res://Src/ECS/Tools", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Tools;
        if (resourcePath.StartsWith("res://Src/ECS/Test", StringComparison.OrdinalIgnoreCase)) return ResourceCategory.Test;
        return ResourceCategory.Other;
    }

    private static bool ResourcePathExists(string projectRoot, string resourcePath)
    {
        var fullPath = ToProjectPath(projectRoot, resourcePath);
        return File.Exists(fullPath);
    }

    private static string ToProjectPath(string projectRoot, string resourcePath)
    {
        var relativePath = resourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase)
            ? resourcePath[6..]
            : resourcePath;
        return Path.Combine(projectRoot, relativePath);
    }

    private static string ResolveProjectRoot()
    {
        var globalized = ProjectSettings.GlobalizePath("res://");
        if (!string.IsNullOrWhiteSpace(globalized) && Directory.Exists(globalized))
        {
            return Path.GetFullPath(globalized);
        }

        var current = new DirectoryInfo(System.Environment.CurrentDirectory);
        while (current != null)
        {
            if (File.Exists(Path.Combine(current.FullName, "project.godot")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return System.Environment.CurrentDirectory;
    }

    private static string NormalizeResourcePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static string NormalizeFieldPath(object? value)
    {
        return value switch
        {
            null => string.Empty,
            string text => text,
            ResourceRef resourceRef => resourceRef.Path,
            System.Text.Json.JsonElement element when element.ValueKind == System.Text.Json.JsonValueKind.String => element.GetString() ?? string.Empty,
            System.Text.Json.JsonElement element when element.TryGetProperty("path", out var pathElement) && pathElement.ValueKind == System.Text.Json.JsonValueKind.String => pathElement.GetString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }
}
