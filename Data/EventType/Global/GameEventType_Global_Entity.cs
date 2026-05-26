/// <summary>
/// Global 实体相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>Entity 生成</summary>
        public readonly record struct EntitySpawned(IEntity Entity);

        /// <summary>Entity 销毁（通用，适用于所有 IEntity）</summary>
        public readonly record struct EntityDestroyed(IEntity Entity);

        /// <summary>Entity 迁移开始</summary>
        public readonly record struct EntityMigrating(
            IEntity SourceEntity, // 源实体
            string TargetEntityType, // 目标实体类型名
            string ProfileName // 迁移 Profile 名称
        );

        /// <summary>Entity 迁移完成</summary>
        public readonly record struct EntityMigrated(
            IEntity SourceEntity, // 源实体
            IEntity TargetEntity, // 目标实体
            string ProfileName // 迁移 Profile 名称
        );

        /// <summary>Entity 关系添加</summary>
        public readonly record struct RelationshipAdded(
            string ParentEntityId, // 父实体Id
            string ChildEntityId, // 子实体Id
            string RelationType // 关系类型
        );

        /// <summary>Entity 关系移除</summary>
        public readonly record struct RelationshipRemoved(
            string ParentEntityId, // 父实体Id
            string ChildEntityId, // 子实体Id
            string RelationType // 关系类型
        );
    }
}
