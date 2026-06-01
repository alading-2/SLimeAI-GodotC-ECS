using System.Collections.Generic;
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
            SystemRegistry.Register(nameof(EntityManager),
                static () => new EntityManagerComponentWarmupRuntime());
        }

        private sealed class EntityManagerComponentWarmupRuntime : ISystem
        {
            public void OnRegistered(SystemRegistrationContext context)
            {
                PrewarmComponentCache();
            }

            public SystemRuntimeInfo GetSystemRuntimeInfo()
            {
                return new SystemRuntimeInfo
                {
                    SystemId = nameof(EntityManager),
                    CustomStats = new List<SystemStat>()
                };
            }
        }
    }
}