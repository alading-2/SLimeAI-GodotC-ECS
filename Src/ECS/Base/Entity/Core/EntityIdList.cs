using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// EntityId 不可变列表，用于表达业务 owner 的多实体引用。
/// </summary>
public readonly record struct EntityIdList
{
    private readonly EntityId[]? _values;

    public EntityIdList(IEnumerable<EntityId> values)
    {
        _values = Normalize(values);
    }

    /// <summary>空实体引用列表。</summary>
    public static EntityIdList Empty => new(Array.Empty<EntityId>());

    /// <summary>引用数量。</summary>
    public int Count => Values.Count;

    /// <summary>按插入顺序去重后的实体引用。</summary>
    public IReadOnlyList<EntityId> Values => _values ?? Array.Empty<EntityId>();

    /// <summary>
    /// 从 Data string_array projection 创建 typed 列表。
    /// </summary>
    public static EntityIdList FromStringArray(IEnumerable<string>? values)
    {
        return values == null
            ? Empty
            : new EntityIdList(values.Select(EntityId.From));
    }

    /// <summary>
    /// 添加实体引用；空引用和重复引用会被忽略，并返回新列表。
    /// </summary>
    public EntityIdList Add(EntityId entityId)
    {
        if (entityId.IsEmpty || Contains(entityId))
            return this;

        return new EntityIdList(Values.Concat(new[] { entityId }));
    }

    /// <summary>
    /// 移除实体引用，并返回新列表。
    /// </summary>
    public EntityIdList Remove(EntityId entityId)
    {
        if (entityId.IsEmpty || !Contains(entityId))
            return this;

        return new EntityIdList(Values.Where(id => id != entityId));
    }

    /// <summary>
    /// 判断列表是否包含指定实体引用。
    /// </summary>
    public bool Contains(EntityId entityId)
    {
        return !entityId.IsEmpty && Values.Contains(entityId);
    }

    /// <summary>
    /// 返回 EntityId 数组副本。
    /// </summary>
    public EntityId[] ToArray() => Values.ToArray();

    /// <summary>
    /// 转换为 Data string_array projection。
    /// </summary>
    public string[] ToStringArray()
    {
        return Values
            .Where(id => !id.IsEmpty)
            .Select(id => id.Value)
            .ToArray();
    }

    private static EntityId[] Normalize(IEnumerable<EntityId> values)
    {
        var result = new List<EntityId>();
        foreach (var id in values)
        {
            if (id.IsEmpty || result.Contains(id))
                continue;

            result.Add(id);
        }

        return result.ToArray();
    }
}
