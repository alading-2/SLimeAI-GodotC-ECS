using Godot;

internal static partial class ProjectileTool
{
    /// <summary>
    /// 生成投射物实体，并自动建立“拥有者 → 投射物”的关系链。
    /// </summary>
    public static ProjectileEntity? Spawn(
        IEntity owner, // 投射物归属者
        Vector2 position, // 投射物初始位置
        string visualScenePath, // 投射物视觉场景路径
        string name = "Projectile"
    )
    {
        var config = new RuntimeDataRecordDto
        {
            Table = "runtime.projectile",
            Id = $"projectile.{name}",
            Name = name,
            Fields = new System.Collections.Generic.Dictionary<string, RuntimeDataFieldDto>
            {
                [GeneratedDataKey.Name.StableKey] = new() { Type = "string", Value = name },
                [GeneratedDataKey.VisualScenePath.StableKey] = new() { Type = "string", Value = visualScenePath },
                [GeneratedDataKey.EntityType.StableKey] = new() { Type = "enum", Value = nameof(EntityType.Projectile) }
            }
        };

        var projectile = EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
        {
            Config = config, // 运行时最小配置
            RuntimeDataRecord = config,
            UsingObjectPool = true, // 投射物统一走对象池
            PoolName = ObjectPoolNames.ProjectilePool, // 投射物对象池名
            Position = position, // 初始位置
            ParentEntity = owner, // 父实体/归属者
            AutoAddParentRelation = true, // 自动补 PARENT，供归属链统一溯源
            ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively, // 归属者销毁时递归销毁投射物
            ParentRelationTypes = [EntityRelationshipType.ENTITY_TO_PROJECTILE] // 业务关系：拥有者 -> 投射物
        });

        if (projectile == null)
        {
            return null;
        }

        projectile.Data.Set(GeneratedDataKey.EntityType, EntityType.Projectile); // 标记实体类型为投射物

        return projectile;
    }
}
