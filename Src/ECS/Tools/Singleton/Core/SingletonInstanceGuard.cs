using System;

/// <summary>
/// 单例实例绑定守卫。
/// <para>只处理引用绑定规则，不依赖 Godot 生命周期；Godot Node 的销毁策略由外层工具负责。</para>
/// </summary>
public static class SingletonInstanceGuard
{
    /// <summary>
    /// 尝试把候选对象绑定为当前实例。
    /// <para>已有不同实例时不覆盖旧实例，并把重复候选交给调用方处理。</para>
    /// </summary>
    /// <typeparam name="T">实例类型。</typeparam>
    /// <param name="candidate">候选实例。</param>
    /// <param name="instance">当前实例引用。</param>
    /// <param name="onDuplicate">重复实例回调。</param>
    /// <returns>绑定成功返回 true；遇到重复实例返回 false。</returns>
    public static bool TryBind<T>(T candidate, ref T? instance, Action<T>? onDuplicate = null) where T : class
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (instance != null && !ReferenceEquals(instance, candidate))
        {
            onDuplicate?.Invoke(candidate);
            return false;
        }

        instance = candidate;
        return true;
    }

    /// <summary>
    /// 当前实例退出时清空引用。
    /// <para>只有传入对象就是当前实例时才清空，避免旧节点离树时误清掉新实例。</para>
    /// </summary>
    /// <typeparam name="T">实例类型。</typeparam>
    /// <param name="candidate">正在退出的实例。</param>
    /// <param name="instance">当前实例引用。</param>
    /// <returns>成功清空返回 true；非当前实例返回 false。</returns>
    public static bool Release<T>(T candidate, ref T? instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!ReferenceEquals(instance, candidate))
        {
            return false;
        }

        instance = null;
        return true;
    }
}
