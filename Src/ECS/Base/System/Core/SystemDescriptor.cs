using System;

/// <summary>
/// 系统描述符。
/// <para>简化为只包含 SystemId + Factory，其他元数据从 snapshot-backed SystemData 读取。</para>
/// </summary>
public sealed class SystemDescriptor
{
    /// <summary>
    /// 构造系统描述符。
    /// </summary>
    /// <param name="systemId">系统唯一 Id。</param>
    /// <param name="factory">系统实例工厂。</param>
    public SystemDescriptor(string systemId, Func<object> factory)
    {
        if (string.IsNullOrWhiteSpace(systemId))
        {
            throw new ArgumentException("SystemId cannot be empty", nameof(systemId));
        }

        if (factory == null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        SystemId = systemId;
        Factory = factory;
    }

    /// <summary>系统唯一 Id。</summary>
    public string SystemId { get; }

    /// <summary>
    /// 系统实例工厂。
    /// <para>NodeScript / PureService 统一由工厂创建；NodeScene 可返回 PackedScene.Instantiate() 的结果。</para>
    /// </summary>
    public Func<object> Factory { get; }
}
