/// <summary>
/// 系统运行形态。
/// <para>NodeScene / NodeScript 实例为 Node，挂到 Host 后由 ProcessMode 控制启停；</para>
/// <para>PureService 实例为纯 C# 对象，不挂树，启停仅靠 ISystemRuntime 钩子。</para>
/// </summary>
public enum SystemKind
{
    /// <summary>
    /// 基于 PackedScene 的节点系统。
    /// <para>挂到 Host → ProcessMode 控制帧调度 → 适合有场景骨架或子节点树的系统。</para>
    /// </summary>
    NodeScene,

    /// <summary>
    /// 基于脚本直接 new 的节点系统。
    /// <para>挂到 Host → ProcessMode 控制帧调度 → 适合无 .tscn 但需要 _Process 的桥接类。</para>
    /// </summary>
    NodeScript,

    /// <summary>
    /// 非节点纯服务系统。
    /// <para>不挂树 → 无 ProcessMode → 启停完全依赖 ISystemRuntime 钩子手动订阅/退订。</para>
    /// </summary>
    PureService,
}