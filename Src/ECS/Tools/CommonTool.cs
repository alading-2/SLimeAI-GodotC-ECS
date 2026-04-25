using Godot;

/// <summary>
/// 通用杂项工具（先集中放置不便归类的通用函数）。
/// </summary>
public static class CommonTool
{
    private static readonly Log _log = new(nameof(CommonTool));

    /// <summary>
    /// 按 res:// 路径加载 PackedScene。
    /// <para>运行时 Data 只保存路径字符串，最终实例化点再调用此方法加载。</para>
    /// </summary>
    public static PackedScene? LoadPackedScene(
        string path, // res:// 场景路径
        string usageName) // 日志中展示的用途名称
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            _log.Warn($"{usageName} 场景路径为空");
            return null;
        }

        var scene = ResourceManagement.LoadPath<PackedScene>(path);
        if (scene == null)
        {
            _log.Error($"{usageName} 场景加载失败: {path}");
        }

        return scene;
    }
}
