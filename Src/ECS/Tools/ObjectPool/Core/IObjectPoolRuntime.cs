using System;

/// <summary>
/// 对象池管理器使用的非泛型运行时接口。
/// 只暴露跨泛型管理需要的能力，不提供无类型 Get，避免破坏业务侧类型安全。
/// </summary>
public interface IObjectPoolRuntime
{
    /// <summary>池名称。</summary>
    string PoolName { get; }

    /// <summary>池内对象类型。</summary>
    Type ItemType { get; }

    /// <summary>当前闲置数量。</summary>
    int Count { get; }

    /// <summary>当前活跃数量。</summary>
    int ActiveCount { get; }

    /// <summary>
    /// 跨泛型边界归还对象。
    /// 类型不匹配时返回 false，由调用方决定 fallback 或日志策略。
    /// </summary>
    bool ReleaseUntyped(object instance);

    /// <summary>清理闲置对象，保留指定数量。</summary>
    void Cleanup(int retainCount);

    /// <summary>清空池内闲置对象。</summary>
    void Clear();

    /// <summary>读取运行时统计。</summary>
    PoolStats GetStats();
}
