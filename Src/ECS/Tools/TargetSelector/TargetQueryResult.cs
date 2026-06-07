using System;
using System.Collections.Generic;

/// <summary>
/// 目标查询结果。
/// 当前实现持有一次查询生成的只读快照；Dispose 为后续 pooled buffer / lease 预留所有权边界。
/// </summary>
/// <typeparam name="T">目标元素类型。</typeparam>
public sealed class TargetQueryResult<T> : IDisposable
{
    private readonly IReadOnlyList<T> _items;

    public TargetQueryResult(IReadOnlyList<T> items, TargetQueryDiagnostics diagnostics)
    {
        _items = items;
        Diagnostics = diagnostics;
    }

    /// <summary>只读结果视图，调用方不得修改底层集合。</summary>
    public IReadOnlyList<T> Items => _items;

    /// <summary>本次查询诊断。</summary>
    public TargetQueryDiagnostics Diagnostics { get; }

    /// <summary>释放查询结果所有权；当前无池化资源需要释放。</summary>
    public void Dispose()
    {
    }
}
