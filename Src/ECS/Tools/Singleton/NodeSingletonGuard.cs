using Godot;

/// <summary>
/// Godot Node 单例守卫。
/// <para>不作为基类使用，避免占用 Node/CanvasLayer/Control 等继承位。</para>
/// </summary>
public static class NodeSingletonGuard
{
    /// <summary>
    /// 尝试绑定 Node 单例；检测到重复实例时默认销毁重复节点。
    /// </summary>
    /// <typeparam name="T">Node 派生类型。</typeparam>
    /// <param name="candidate">候选节点。</param>
    /// <param name="instance">当前实例引用。</param>
    /// <param name="log">可选日志。</param>
    /// <param name="queueDuplicate">是否对重复节点调用 QueueFree。</param>
    /// <returns>绑定成功返回 true；重复实例返回 false。</returns>
    public static bool TryBind<T>(T candidate, ref T? instance, Log? log = null, bool queueDuplicate = true) where T : Node
    {
        return SingletonInstanceGuard.TryBind(candidate, ref instance, duplicate =>
        {
            log?.Warn($"检测到重复的 {typeof(T).Name} 实例。{(queueDuplicate ? "正在销毁重复项。" : "已保留重复项。")}");

            if (queueDuplicate && GodotObject.IsInstanceValid(duplicate) && !duplicate.IsQueuedForDeletion())
            {
                duplicate.QueueFree();
            }
        });
    }

    /// <summary>
    /// Node 离树时释放单例引用。
    /// </summary>
    /// <typeparam name="T">Node 派生类型。</typeparam>
    /// <param name="candidate">正在退出的节点。</param>
    /// <param name="instance">当前实例引用。</param>
    /// <returns>成功清空返回 true；非当前实例返回 false。</returns>
    public static bool Release<T>(T candidate, ref T? instance) where T : Node
    {
        return SingletonInstanceGuard.Release(candidate, ref instance);
    }
}
