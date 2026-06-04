using System.Collections.Generic;

/// <summary>
/// System preflight 可调选项。
/// </summary>
public sealed class SystemPreflightOptions
{
    /// <summary>允许没有 system.config 的测试专用 descriptor。</summary>
    public HashSet<string> AllowedDescriptorOnlySystemIds { get; } = new(System.StringComparer.Ordinal);

    /// <summary>允许没有 system.config 的测试专用 descriptor 前缀。</summary>
    public List<string> AllowedDescriptorOnlySystemIdPrefixes { get; } = new();

    /// <summary>descriptor-only 未进入 allow-list 时是否作为 error；默认 warning，避免生产启动误杀。</summary>
    public bool TreatDescriptorOnlyAsError { get; init; }

    /// <summary>
    /// 默认选项。SystemCoreRuntimeTest 会注册临时 descriptor，允许该前缀避免测试污染 preflight。
    /// </summary>
    public static SystemPreflightOptions Default()
    {
        var options = new SystemPreflightOptions();
        options.AllowedDescriptorOnlySystemIdPrefixes.Add("SystemCoreRuntimeTest.");
        return options;
    }
}
