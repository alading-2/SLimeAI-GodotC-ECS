using Godot;
using System.Runtime.CompilerServices;

/// <summary>
/// 为 eventbus observation 生成稳定可读的 bus 名称。
/// </summary>
public static class EventBusNames
{
    public static string ForEntity(IEntity entity)
    {
        if (entity is Node node)
        {
            return $"entity:{node.GetInstanceId()}:{node.Name}";
        }

        return $"entity:{entity.GetType().Name}:{RuntimeHelpers.GetHashCode(entity)}";
    }
}
