using System;

/// <summary>
/// TestSystem 模块路径工具。
/// <para>
/// 模块路径使用点分层级，最后一段表示模块名，前面的段表示 UI 分组。
/// </para>
/// </summary>
internal static class TestModulePath
{
    /// <summary>模块路径分隔符。</summary>
    public const char Separator = '.';

    /// <summary>
    /// 标准化模块路径，兼容斜杠写法并剔除空段。
    /// </summary>
    /// <param name="modulePath">模块路径。</param>
    /// <returns>标准化后的点分路径。</returns>
    public static string Normalize(string? modulePath)
    {
        if (string.IsNullOrWhiteSpace(modulePath))
        {
            return string.Empty;
        }

        var normalized = modulePath.Trim().Replace('/', Separator).Replace('\\', Separator);
        var parts = normalized.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return string.Join(Separator, parts);
    }

    /// <summary>
    /// 拆分模块路径。
    /// </summary>
    /// <param name="modulePath">模块路径。</param>
    /// <returns>路径分段。</returns>
    public static string[] Split(string modulePath)
    {
        var normalized = Normalize(modulePath);
        return string.IsNullOrWhiteSpace(normalized)
            ? Array.Empty<string>()
            : normalized.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// 获取模块显示名。
    /// </summary>
    /// <param name="modulePath">模块路径。</param>
    /// <returns>最后一段路径；路径为空时返回空字符串。</returns>
    public static string GetLeafName(string modulePath)
    {
        var parts = Split(modulePath);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }
}
