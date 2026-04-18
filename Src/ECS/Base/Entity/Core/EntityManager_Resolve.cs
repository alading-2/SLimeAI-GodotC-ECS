using Godot;

public static partial class EntityManager
{
    /// <summary>
    /// 从任意节点向上回溯所属实体。
    /// </summary>
    public static IEntity? ResolveOwningIEntity(Node? node)
    {
        Node? current = node;
        while (current != null)
        {
            if (current is IEntity iEntity)
            {
                return iEntity;
            }

            current = current.GetParent();
        }

        return null;
    }
}
