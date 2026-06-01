/// <summary>
/// TestSystem 模块上下文协议。
/// </summary>
internal interface ITestModuleContext
{
    /// <summary>宿主 TestSystem。</summary>
    TestSystem Host { get; }

    /// <summary>统一选中实体上下文。</summary>
    TestSelectionContext Selection { get; }

    /// <summary>当前被测试面板选中的实体。</summary>
    IEntity? SelectedEntity { get; }
}
