/// <summary>
/// 项目状态观察协议。
/// <para>仅当系统需要主动感知 ProjectState 变化细节时实现，避免所有系统都被迫接收状态事件。</para>
/// </summary>
public interface IProjectStateAwareSystem
{
    /// <summary>
    /// 项目状态发生切换时触发。
    /// </summary>
    /// <param name="args">状态切换参数。</param>
    void OnProjectStateChanged(ProjectStateChangedEventArgs args)
    {
    }
}
