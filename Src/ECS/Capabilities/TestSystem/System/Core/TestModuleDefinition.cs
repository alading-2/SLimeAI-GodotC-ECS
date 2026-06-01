/// <summary>
/// TestSystem 模块定义。
/// <para>
/// 统一描述模块稳定 Id 与 UI 展示路径。
/// </para>
/// </summary>
internal readonly record struct TestModuleDefinition(
    string Id, // 稳定模块 Id
    string ModulePath // 点分模块路径，最后一段为模块名，前面为分组
)
{
    /// <summary>模块显示名，取 ModulePath 最后一段。</summary>
    public string DisplayName => TestModulePath.GetLeafName(ModulePath);
}
