using System.Text;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;


class ResourceGenerator
{
    // ============================================================
    // 扫描根：只表达“去哪里找资源”，不再顺便表达分类。
    // 分类由 CategoryRules + Capability 层规则决定，避免 Capabilities 被整体误归为 Component。
    // ============================================================
    private static readonly string[] ScanRoots =
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

    // 显式分类规则按最长前缀匹配，用于资产根和少量非标准 runtime 资源。
    private static readonly Dictionary<string, ResourceCategory> CategoryRules = new(StringComparer.OrdinalIgnoreCase)
    {
        { "assets/Effect", ResourceCategory.AssetEffect },
        { "assets/Unit", ResourceCategory.AssetUnit },
        { "assets/Unit/Player", ResourceCategory.AssetUnitPlayer },
        { "assets/Unit/Enemy", ResourceCategory.AssetUnitEnemy },
        { "assets/Projectile", ResourceCategory.AssetProjectile },
        { "Src/ECS/Test/GlobalTest/VisualPreview/Entity", ResourceCategory.Entity },
        { "Src/ECS/Test/SingleTest/ECS/ECSTest/Entity", ResourceCategory.Entity },
        { "Src/ECS/Tools/Input/MouseSelection", ResourceCategory.System },
        { "Src/ECS/UI/PauseMenu", ResourceCategory.System },
        { "Src/ECS/Runtime/Entity", ResourceCategory.Entity },
        { "Src/ECS/Runtime/Component", ResourceCategory.Component },
        { "Src/ECS/Runtime/System", ResourceCategory.System },
        { "Src/ECS/Tools", ResourceCategory.Tools },
        { "Src/ECS/UI", ResourceCategory.UI },
        { "Src/ECS/Test", ResourceCategory.Test },
    };

    private static readonly string[] ExcludePaths = {
        "addons",
        ".godot",
    };

    private const string OutputFile = "Data/ResourceManagement/ResourcePaths.cs";

    // 通过最长前缀和 Capability 内部分层确定资源分类
    private static ResourceCategory GetCategoryFromPath(string resPath)
    {
        if (string.IsNullOrEmpty(resPath)) return ResourceCategory.Other;
        var relPath = resPath.StartsWith("res://") ? resPath.Substring(6) : resPath;

        if (TryGetBestCategoryRule(relPath, out var explicitCategory, out _))
        {
            return explicitCategory;
        }

        if (TryGetCapabilityLayer(relPath, out _, out var capabilityCategory))
        {
            return capabilityCategory;
        }

        return ResourceCategory.Other;
    }

    static void Main(string[] args)
    {
        Console.WriteLine("开始扫描资源文件...");

        // 1. 确定项目根目录 (假设当前运行目录在 Tools/ResourceGenerator 或项目根目录下)
        // 我们需要找到包含 project.godot 的目录
        var projectRoot = FindProjectRoot(Directory.GetCurrentDirectory());
        if (projectRoot == null)
        {
            Console.Error.WriteLine("API Error: 无法找到项目根目录 (未发现 project.godot)");
            return;
        }

        Console.WriteLine($"项目根目录: {projectRoot}");

        // 修改：使用嵌套字典存储资源 [Category -> [Name -> Path]]
        var resourcesByCategory = new Dictionary<ResourceCategory, Dictionary<string, string>>();
        var duplicates = new List<string>();

        // 2. 扫描目录（路径来源于 ScanRoots；分类在 GetCategoryFromPath 中统一推导）
        foreach (var relativePath in ScanRoots)
        {
            var fullPath = Path.Combine(projectRoot, relativePath);
            if (!Directory.Exists(fullPath))
            {
                Console.WriteLine($"[警告] 路径不存在: {fullPath}");
                continue;
            }

            ScanDirectory(fullPath, projectRoot, resourcesByCategory, duplicates);
        }

        // 3. 生成代码
        GenerateCode(projectRoot, resourcesByCategory);

        // 统计总数
        int totalResources = resourcesByCategory.Sum(x => x.Value.Count);
        Console.WriteLine($"处理完成! 共找到 {totalResources} 个资源。");

        if (duplicates.Count > 0)
        {
            Console.WriteLine($"[警告] 发现 {duplicates.Count} 个分类内重名资源 (已跳过):");
            foreach (var dup in duplicates)
            {
                Console.WriteLine($"  - {dup}");
            }
        }
    }

    private static string? FindProjectRoot(string currentPath)
    {
        var dir = new DirectoryInfo(currentPath);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "project.godot")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private static void ScanDirectory(string dirPath, string projectRoot, Dictionary<ResourceCategory, Dictionary<string, string>> resourcesByCategory, List<string> duplicates)
    {
        var files = Directory.GetFiles(dirPath);
        foreach (var file in files)
        {
            // 处理 .tscn, .tres 和 .cs 文件 (System/Manager 可能是 .cs)
            if (!file.EndsWith(".tscn") && !file.EndsWith(".tres")) continue;

            // 排除 ResourceManagement 本身
            if (file.EndsWith("ResourceManagement.tscn")) continue;

            // 转换为 res:// 路径
            var relativePath = Path.GetRelativePath(projectRoot, file).Replace("\\", "/");
            var resPath = "res://" + relativePath;

            var name = GetNameFromResPath(resPath);

            // 检查排除路径
            bool isExcluded = false;
            foreach (var exclude in ExcludePaths)
            {
                if (relativePath.StartsWith(exclude))
                {
                    isExcluded = true;
                    break;
                }
            }

            if (isExcluded) continue;

            // 获取分类
            ResourceCategory category = GetCategoryFromPath(resPath);

            // 确保分类字典存在
            if (!resourcesByCategory.ContainsKey(category))
            {
                resourcesByCategory[category] = new Dictionary<string, string>();
            }

            var categoryResources = resourcesByCategory[category];

            if (categoryResources.TryGetValue(name, out var existingPath))
            {
                // 冲突解决策略：优先保留 .tscn 文件 (通常是场景)，覆盖 .tres 文件 (可能是资源或 SpriteFrames)
                bool existingIsScene = existingPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);
                bool newIsScene = resPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase);

                if (existingIsScene && !newIsScene)
                {
                    // 已有的是 .tscn，新的是 .tres -> 忽略新的
                    duplicates.Add($"[{category}] {name} ({resPath}) [Skipped: Prefer .tscn]");
                }
                else if (!existingIsScene && newIsScene)
                {
                    // 已有的是 .tres，新的是 .tscn -> 覆盖旧的
                    categoryResources[name] = resPath;
                }
                else
                {
                    // 同样扩展名或无法判断优先级，视为真正冲突
                    duplicates.Add($"[{category}] {name} ({resPath})");
                }
            }
            else
            {
                categoryResources[name] = resPath;
            }
        }

        var subDirs = Directory.GetDirectories(dirPath);
        foreach (var subDir in subDirs)
        {
            // 检查目录是否被排除
            var dirName = Path.GetFileName(subDir);
            if (dirName.StartsWith(".")) continue; // 忽略 .godot 等隐藏目录

            var relativePath = Path.GetRelativePath(projectRoot, subDir).Replace("\\", "/");
            bool isExcluded = false;
            foreach (var exclude in ExcludePaths)
            {
                if (relativePath.StartsWith(exclude) || relativePath == exclude)
                {
                    isExcluded = true;
                    break;
                }
            }
            if (isExcluded) continue;

            // 若该子目录已作为独立扫描根，跳过（由独立扫描处理，避免重复）
            if (IsConfiguredScanRoot(relativePath)) continue;

            ScanDirectory(subDir, projectRoot, resourcesByCategory, duplicates);
        }
    }

    private static string GetNameFromResPath(string resPath)
    {
        string relPath = resPath.StartsWith("res://") ? resPath.Substring(6) : resPath;

        var namingRoot = GetNamingRoot(relPath);
        if (!string.IsNullOrEmpty(namingRoot))
        {
            relPath = relPath.Substring(namingRoot.Length).TrimStart('/');
        }

        if (relPath.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase)) relPath = relPath.Substring(0, relPath.Length - 5);
        if (relPath.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)) relPath = relPath.Substring(0, relPath.Length - 5);

        // 过滤掉 AnimatedSprite2D 中间目录，仅保留有效路径段
        var parts = relPath.Replace("\\", "/").Split('/');
        var filteredParts = System.Array.FindAll(parts,
            p => !string.Equals(p, "AnimatedSprite2D", StringComparison.OrdinalIgnoreCase));

        // 只取文件名（最后一段）；若同分类内出现重名，ResourceGenerator 日志会提示
        return filteredParts.Last().Replace("-", "_");
    }

    private static bool IsConfiguredScanRoot(string relativePath)
    {
        foreach (var root in ScanRoots)
        {
            if (relativePath.Equals(root, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryGetBestCategoryRule(string relPath, out ResourceCategory category, out string prefix)
    {
        category = ResourceCategory.Other;
        prefix = string.Empty;

        foreach (var kv in CategoryRules)
        {
            if (MatchesPrefix(relPath, kv.Key) && kv.Key.Length > prefix.Length)
            {
                prefix = kv.Key;
                category = kv.Value;
            }
        }

        return prefix.Length > 0;
    }

    private static string GetNamingRoot(string relPath)
    {
        var bestPrefix = string.Empty;

        if (TryGetBestCategoryRule(relPath, out _, out var explicitPrefix))
        {
            bestPrefix = explicitPrefix;
        }

        if (TryGetCapabilityLayer(relPath, out var capabilityRoot, out _)
            && capabilityRoot.Length > bestPrefix.Length)
        {
            bestPrefix = capabilityRoot;
        }

        return bestPrefix;
    }

    private static bool TryGetCapabilityLayer(string relPath, out string layerRoot, out ResourceCategory category)
    {
        layerRoot = string.Empty;
        category = ResourceCategory.Other;

        var parts = relPath.Replace("\\", "/").Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 5
            || !string.Equals(parts[0], "Src", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[1], "ECS", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(parts[2], "Capabilities", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var layer = parts[4];
        category = layer.ToLowerInvariant() switch
        {
            "entity" => ResourceCategory.Entity,
            "component" => ResourceCategory.Component,
            "system" => ResourceCategory.System,
            "tests" => ResourceCategory.Test,
            "presets" => ResourceCategory.Preset,
            _ => ResourceCategory.Other,
        };

        if (category == ResourceCategory.Other)
        {
            return false;
        }

        layerRoot = string.Join('/', parts.Take(5));
        return true;
    }

    private static bool MatchesPrefix(string relPath, string prefix)
    {
        return relPath.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase)
               || relPath.Equals(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateCode(string projectRoot, Dictionary<ResourceCategory, Dictionary<string, string>> resourcesByCategory)
    {
        var sb = new StringBuilder();
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine("//* <ResourceGenerator>");
        sb.AppendLine("//*     ResourceGenerator 资源路径生成器工具");
        sb.AppendLine("//*");
        sb.AppendLine("//*     不要修改本文件，因为每次运行ResourceGenerator都会覆盖本文件。");
        sb.AppendLine("//* </ResourceGenerator>");
        sb.AppendLine("//------------------------------------------------------------------------------");
        sb.AppendLine();
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine();
        sb.AppendLine("public struct ResourceData");
        sb.AppendLine("{");
        sb.AppendLine("    public string Path;");
        sb.AppendLine("    public ResourceCategory Category;");
        sb.AppendLine("    public ResourceData(ResourceCategory category, string path)");
        sb.AppendLine("    {");
        sb.AppendLine("        Category = category;");
        sb.AppendLine("        Path = path;");
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine("public static class ResourcePaths");
        sb.AppendLine("{");

        // 1. 生成静态常量 (强类型访问)
        // 格式: ResourcePaths.EnemyConfig_Name
        var allCategories = Enum.GetNames(typeof(ResourceCategory));
        foreach (var categoryName in allCategories)
        {
            sb.AppendLine($"    // --- {categoryName} ---");
            if (resourcesByCategory.TryGetValue(Enum.Parse<ResourceCategory>(categoryName), out var items))
            {
                foreach (var kvp in items.OrderBy(x => x.Key))
                {
                    // 使用 Category_Name 风格的常量
                    sb.AppendLine($"    public const string {categoryName}_{kvp.Key} = \"{kvp.Key}\";");
                }
            }
            sb.AppendLine();
        }

        // 2. 生成统一的 Resources 字典 (运行时查找)
        sb.AppendLine("    public static readonly Dictionary<ResourceCategory, Dictionary<string, ResourceData>> Resources = new()");
        sb.AppendLine("    {");

        foreach (var categoryName in allCategories)
        {
            sb.AppendLine($"        {{ ResourceCategory.{categoryName}, new Dictionary<string, ResourceData>");
            sb.AppendLine("            {");

            if (resourcesByCategory.TryGetValue(Enum.Parse<ResourceCategory>(categoryName), out var items))
            {
                foreach (var kvp in items.OrderBy(x => x.Key))
                {
                    // 使用扁平化的静态常量作为 Key，确保一致性
                    sb.AppendLine($"                {{ {categoryName}_{kvp.Key}, new ResourceData(ResourceCategory.{categoryName}, \"{kvp.Value}\") }},");
                }
            }

            sb.AppendLine("            }");
            sb.AppendLine("        },");
        }

        sb.AppendLine("    };");
        sb.AppendLine("}");

        var outputPath = Path.Combine(projectRoot, OutputFile);
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir!);
        }

        File.WriteAllText(outputPath, sb.ToString());
        Console.WriteLine($"已生成文件: {OutputFile}");
    }


}
