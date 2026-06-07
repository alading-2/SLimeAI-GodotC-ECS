/// <summary>
/// FeatureContext 旧扩展入口。
/// 新代码直接使用 FeatureContext 的 typed helper。
/// </summary>
public static class FeatureContextExtensions
{
    /// <summary>
    /// 读取指定类型的 activation payload。
    /// </summary>
    [System.Obsolete("使用 FeatureContext.GetActivation<T>()。")]
    public static T GetActivationData<T>(this FeatureContext context)
    {
        return context.GetActivation<T>();
    }

    /// <summary>
    /// 尝试把 activation payload 读取为指定子系统上下文类型。
    /// </summary>
    [System.Obsolete("使用 FeatureContext.TryGetActivation<T>()。")]
    public static bool TryGetActivationData<T>(this FeatureContext context, out T data)
    {
        return context.TryGetActivation(out data);
    }
}
