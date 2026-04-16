/// <summary>
/// 视觉预览资源条目。
/// </summary>
public readonly record struct VisualPreviewEntry(
    string ResourceKey, // ResourcePaths 中的资源键
    ResourceCategory Category, // ResourceManagement 加载分类
    string ResourcePath, // res:// 资源路径
    string SceneName, // 资源路径最后场景名
    string CatalogPath, // UI 分类路径
    string DefaultAnimation // 该资源的默认回退动作
)
{
    public bool SupportsAnimationHint => Category != ResourceCategory.AssetProjectile;
}
