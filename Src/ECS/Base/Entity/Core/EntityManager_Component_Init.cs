using System.Runtime.CompilerServices;
using Godot;

public partial class EntityManager
{
    /// <summary>
    /// EntityManager 初始化注册器
    /// </summary>
    public static class Init
    {
        /// <summary>
        /// 模块初始化入口
        /// </summary>
        [ModuleInitializer]
        public static void Initialize()
        {
            SystemRegistry.Register(new SystemDescriptor(nameof(EntityManager), SystemKind.PureService, SystemLifetime.Persistent)
            {
                Factory = static () => new EntityManagerComponentWarmupRuntime(),
            });
        }

        private sealed class EntityManagerComponentWarmupRuntime : ISystemRuntime
        {
            public void OnSystemRegistered(SystemRegistrationContext context)
            {
                PrewarmComponentCache();
            }
        }
    }
}
