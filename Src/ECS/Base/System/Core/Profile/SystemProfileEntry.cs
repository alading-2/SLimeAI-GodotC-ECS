using Godot;

/// <summary>
/// 单个系统的 Profile 覆盖项。
/// <para>只有命中 <see cref="SystemId"/> 的系统才会使用这里的 AutoAdd / Enabled，其他系统全部回退描述符默认值。</para>
/// </summary>
[GlobalClass]
public partial class SystemProfileEntry : Resource
{
    /// <summary>目标系统 Id。</summary>
    [Export]
    public string SystemId { get; set; } = string.Empty;

    /// <summary>是否在启动时自动装载。</summary>
    [Export]
    public bool AutoAdd { get; set; } = true;

    /// <summary>系统加入管理后，Profile 层是否允许运行。</summary>
    [Export]
    public bool Enabled { get; set; } = true;
}
