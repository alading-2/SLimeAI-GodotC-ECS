using Godot;
using System;

/// <summary>
/// ResourceLoading 契约测试。
/// </summary>
public partial class ResourceLoadingRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(ResourceLoadingRuntimeTest));
    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        try
        {
            Load_ShouldFailFastForMissingExactKey();
            Load_ShouldFailForWrongCategoryWithoutContainsFallback();
            LoadPath_ShouldRequireSourcePolicy();
            LoadPackedScenePath_ShouldReturnStructuredResult();
            ResourceCatalogDiagnostics_ShouldExposeStructuredCounts();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"ResourceLoading 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void Load_ShouldFailFastForMissingExactKey()
    {
        var source = ResourceLoadSource.Test("ResourceLoadingRuntimeTest", "missing exact key");
        var result = ResourceLoading.TryLoad<PackedScene>(
            "Player",
            ResourceCategory.Entity,
            source);

        AssertEqual("缺精确 key 应失败", false, result.Success);
        AssertEqual("缺 key 错误码", ResourceLoadErrorCode.KeyNotFound, result.ErrorCode);
        AssertEqual("diagnostics 保留资源 key", "Player", result.Key);
        AssertEqual("diagnostics 保留 owner", "ResourceLoadingRuntimeTest", result.Source.Owner);
    }

    private void Load_ShouldFailForWrongCategoryWithoutContainsFallback()
    {
        var source = ResourceLoadSource.Test("ResourceLoadingRuntimeTest", "wrong category");
        var result = ResourceLoading.TryLoad<PackedScene>(
            ResourcePaths.Entity_PlayerEntity,
            ResourceCategory.UI,
            source);

        AssertEqual("错误 category 不应跨分类 fallback", false, result.Success);
        AssertEqual("错误 category 应报告 KeyNotFound", ResourceLoadErrorCode.KeyNotFound, result.ErrorCode);
        AssertEqual("diagnostics 保留请求 category", ResourceCategory.UI, result.Category);
    }

    private void LoadPath_ShouldRequireSourcePolicy()
    {
        var result = ResourceLoading.TryLoadPath<PackedScene>(
            "res://Src/ECS/Capabilities/Unit/Entity/Player/PlayerEntity.tscn",
            ResourceLoadSource.None);

        AssertEqual("LoadPath 缺 source 应失败", false, result.Success);
        AssertEqual("LoadPath 缺 source 错误码", ResourceLoadErrorCode.MissingSource, result.ErrorCode);
        AssertTrue("LoadPath error 应包含 owner/usage 诊断", result.Message.Contains("source", StringComparison.OrdinalIgnoreCase));
    }

    private void LoadPackedScenePath_ShouldReturnStructuredResult()
    {
        var source = ResourceLoadSource.DataOS("unit.player", "VisualScene");
        var result = ResourceLoading.TryLoadPackedScenePath(
            "res://missing/scene.tscn",
            source);

        AssertEqual("缺失 PackedScene path 应失败", false, result.Success);
        AssertEqual("缺失 path 错误码", ResourceLoadErrorCode.LoadFailed, result.ErrorCode);
        AssertEqual("diagnostics 保留来源 kind", ResourceLoadSourceKind.DataOS, result.Source.Kind);
        AssertEqual("diagnostics 保留 usage", "VisualScene", result.Source.Usage);
    }

    private void ResourceCatalogDiagnostics_ShouldExposeStructuredCounts()
    {
        var report = ResourceCatalogDiagnostics.RunDefault();

        AssertTrue("ResourceCatalogDiagnostics 应返回 diagnostics 集合", report.Diagnostics != null);
        AssertEqual("duplicate key count 应可读取", report.DuplicateKeyCount, report.DuplicateKeyCount);
        AssertEqual("missing path count 应可读取", report.MissingPathCount, report.MissingPathCount);
        AssertTrue("summary 应包含 errors 字段", report.ToSummary().Contains("errors=", StringComparison.Ordinal));
    }

    private void AssertEqual<T>(string message, T expected, T actual)
    {
        if (Equals(expected, actual))
        {
            Pass(message);
            return;
        }

        Fail($"{message}: expected={expected}, actual={actual}");
    }

    private void AssertTrue(string message, bool condition)
    {
        if (condition)
        {
            Pass(message);
            return;
        }

        Fail(message);
    }

    private void Pass(string message)
    {
        _passedCount++;
        _log.Info($"[PASS] {message}");
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error($"[FAIL] {message}");
    }
}
