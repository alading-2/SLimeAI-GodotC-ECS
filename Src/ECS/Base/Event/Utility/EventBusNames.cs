using Godot;
using System.Runtime.CompilerServices;

// ==================================================================================
// EventBusNames — 为 eventbus observation 生成稳定可读的 bus 名称
// ==================================================================================
//
// EntityEventBus 构造时需要 busName 参数，用于 observation dump 中区分不同实体。
// 推荐使用 ForEntity(entity) 生成，格式为 "entity:{InstanceId}:{Name}"。
//
// 对于 Godot Node 实体：使用 InstanceId + Name，InstanceId 在运行时唯一且稳定。
// 对于非 Node 实体：使用类型名 + RuntimeHelpers.GetHashCode，作为降级方案。
// ==================================================================================

/// <summary>
/// 为 eventbus observation 生成稳定可读的 bus 名称。
/// 推荐在 EntityEventBus 构造时使用：new EntityEventBus(EventBusNames.ForEntity(this))
/// </summary>
public static class EventBusNames
{
    /// <summary>
    /// 为实体生成 bus 名称。
    /// Node 实体格式: "entity:{InstanceId}:{Name}"
    /// 非 Node 实体格式: "entity:{TypeName}:{HashCode}"
    /// </summary>
    public static string ForEntity(IEntity entity)
    {
        if (entity is Node node)
        {
            return $"entity:{node.GetInstanceId()}:{node.Name}";
        }

        return $"entity:{entity.GetType().Name}:{RuntimeHelpers.GetHashCode(entity)}";
    }
}
