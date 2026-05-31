using System;

/// <summary>
/// Runtime Entity 身份值对象。
/// </summary>
public readonly record struct EntityId(string Value)
{
    /// <summary>空引用；用于表达没有实体引用。</summary>
    public static readonly EntityId Empty = new(string.Empty);

    /// <summary>是否为空引用。</summary>
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    /// <summary>
    /// 创建新的 runtime entity id。
    /// </summary>
    public static EntityId New() => new($"entity.{Guid.NewGuid():N}");

    /// <summary>
    /// 从字符串投影显式构造 EntityId，空白值统一收口为 Empty。
    /// </summary>
    public static EntityId From(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? Empty
            : new EntityId(value);
    }

    public override string ToString() => Value;
}
