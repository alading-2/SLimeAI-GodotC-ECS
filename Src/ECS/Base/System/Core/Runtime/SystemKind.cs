/// <summary>
/// 系统运行形态。
/// </summary>
public enum SystemKind
{
    /// <summary>基于 PackedScene 的节点系统。</summary>
    NodeScene,
    /// <summary>基于脚本直接 new 的节点系统。</summary>
    NodeScript,
    /// <summary>非节点纯服务系统。</summary>
    PureService,
}
