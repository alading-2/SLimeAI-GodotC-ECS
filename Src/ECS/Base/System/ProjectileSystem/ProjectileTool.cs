using Godot;

internal static partial class ProjectileTool
{
    private sealed partial class RuntimeProjectileConfig : Resource
    {
        public string? Name { get; set; }
    }

    public static ProjectileEntity? Spawn(
        Vector2 position,
        PackedScene? visualScene,
        string name = "Projectile"
    )
    {
        var config = new RuntimeProjectileConfig
        {
            Name = name
        };

        return EntityManager.Spawn<ProjectileEntity>(new EntitySpawnConfig
        {
            Config = config, // 运行时最小配置
            UsingObjectPool = true, // 投射物统一走对象池
            PoolName = ObjectPoolNames.ProjectilePool, // 投射物对象池名
            Position = position, // 初始位置
            VisualSceneOverride = visualScene // 运行时视觉覆盖
        });
    }
}
