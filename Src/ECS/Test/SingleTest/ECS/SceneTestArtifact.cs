using System;
using System.IO;
using System.Text.Json;

/// <summary>
/// Godot 场景测试 artifact 写入工具。
/// </summary>
public static class SceneTestArtifact
{
    /// <summary>
    /// 写入 scene gate 需要的标准答案 artifact。
    /// </summary>
    public static string? Write(
        string sceneName,
        bool passed,
        int passedCount,
        int failedCount,
        string[] expectedInputs,
        string[] expectedObservations,
        string[] passCriteria,
        string[] failCriteria,
        string[] checks)
    {
        var artifactDir = Environment.GetEnvironmentVariable("GODOT_SCENE_TEST_ARTIFACT_DIR");
        if (string.IsNullOrWhiteSpace(artifactDir))
        {
            return null;
        }

        Directory.CreateDirectory(artifactDir);
        var artifactPath = Path.Combine(artifactDir, "scene-artifact.json");
        var payload = new
        {
            schemaVersion = 1,
            sceneName,
            status = passed ? "pass" : "fail",
            passedCount,
            failedCount,
            failureReasons = passed ? Array.Empty<string>() : new[] { "scene assertions failed" },
            expectedInputs,
            expectedObservations,
            passCriteria,
            failCriteria,
            artifactPath,
            checks = Array.ConvertAll(checks, name => new
            {
                name,
                status = passed ? "pass" : "fail"
            })
        };

        File.WriteAllText(
            artifactPath,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));

        return artifactPath;
    }
}
