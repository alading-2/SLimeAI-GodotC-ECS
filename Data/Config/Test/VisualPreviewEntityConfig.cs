namespace slime.config.Test;

/// <summary>
/// 视觉预览实体配置。
/// </summary>
public sealed class VisualPreviewEntityConfig
{
    /// <summary>预览实体名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>预览视觉场景路径。</summary>
    public string VisualScenePath { get; set; } = string.Empty;

    /// <summary>默认预览动画。</summary>
    public string PreviewDefaultAnimation { get; set; } = string.Empty;
}
