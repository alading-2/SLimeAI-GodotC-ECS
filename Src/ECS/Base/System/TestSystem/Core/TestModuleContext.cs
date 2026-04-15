/// <summary>
/// TestSystem 模块上下文。
/// <para>
/// 统一向模块注入宿主与选中实体上下文。
/// </para>
/// </summary>
internal sealed class TestModuleContext
    : ITestModuleContext
{
    public TestModuleContext(
        TestSystem host, // 宿主系统
        TestSelectionContext selection // 选中实体上下文
    )
    {
        Host = host;
        Selection = selection;
    }

    /// <summary>宿主 TestSystem。</summary>
    public TestSystem Host { get; }

    /// <summary>统一选中实体上下文。</summary>
    public TestSelectionContext Selection { get; }

    /// <summary>当前被测试面板选中的实体。</summary>
    public IEntity? SelectedEntity => Selection.SelectedEntity;
}
