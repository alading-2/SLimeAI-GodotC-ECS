using Godot;

/// <summary>
/// 系统 Profile 资源。
/// <para>只负责声明当前项目启动时哪些系统需要显式覆盖 AutoAdd / Enabled。</para>
/// <para>Profile 中未声明的系统，一律回退到 <see cref="SystemDescriptor.DefaultAutoAdd"/> 与 <see cref="SystemDescriptor.DefaultEnabled"/>。</para>
/// </summary>
[GlobalClass]
public partial class SystemProfile : Resource
{
    /// <summary>Profile 名称。</summary>
    [Export]
    public string Name { get; set; } = "DefaultSystemProfile";

    /// <summary>显式系统覆盖列表。</summary>
    [Export]
    public Godot.Collections.Array<SystemProfileEntry> Systems { get; set; } = [];
}
