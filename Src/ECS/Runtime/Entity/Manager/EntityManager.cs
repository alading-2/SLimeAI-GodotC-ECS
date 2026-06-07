using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Entity 生成配置参数
/// 类似 TypeScript 的接口对象，支持命名参数初始化
/// </summary>
public readonly record struct EntitySpawnConfig
{
    /// <summary>
    /// 初始化默认值。
    /// </summary>
    public EntitySpawnConfig()
    {
        ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively;
    }

    /// <summary>局部运行参数；不用于 Data stable key 反射或 snapshot record 推断。</summary>
    public required object Config { get; init; }

    /// <summary>descriptor-first Data runtime 启动器（可选；未设置时使用仓库默认 snapshot）</summary>
    public DataRuntimeBootstrap? RuntimeDataBootstrap { get; init; }

    /// <summary>直接指定的 runtime snapshot record（可选；优先级高于 RuntimeDataRecordTable/RuntimeDataRecordId）</summary>
    public RuntimeDataRecordDto? RuntimeDataRecord { get; init; }

    /// <summary>runtime snapshot record table（可选；需与 RuntimeDataRecordId 配套）</summary>
    public string? RuntimeDataRecordTable { get; init; }

    /// <summary>runtime snapshot record id（可选；需与 RuntimeDataRecordTable 配套）</summary>
    public string? RuntimeDataRecordId { get; init; }

    /// <summary>是否使用对象池（默认 false）</summary>
    public bool UsingObjectPool { get; init; }

    /// <summary>对象池名称（UsingObjectPool=true 时必填，如 ObjectPoolNames.EnemyPool）</summary>
    public string? PoolName { get; init; }

    /// <summary>初始位置（可选，仅对 Node2D 生效）</summary>
    public Vector2? Position { get; init; }

    /// <summary>初始旋转角度（度，可选，仅对 Node2D 生效；2D 下 0=右、90=下、180=左，正值顺时针）</summary>
    public float? Rotation { get; init; }

    /// <summary>运行时视觉场景覆盖（可选；优先级高于 runtime snapshot record）</summary>
    public PackedScene? VisualSceneOverride { get; init; }

    /// <summary>生命周期父实体 id；只表达销毁树，不表达 owner/source/target 等业务引用。</summary>
    public EntityId LifecycleParentId { get; init; }

    /// <summary>父实体销毁时对子实体的处理策略（默认级联销毁）</summary>
    public ParentDestroyPolicy ParentDestroyPolicy { get; init; }
}

/// <summary>
/// Entity 管理器。
/// <para>
/// 当前定位是 Runtime Entity 的薄 facade：创建委托给 EntitySpawnPipeline，
/// Component 归属委托给 ComponentRegistrar，生命周期父子关系委托给 LifecycleTree，
/// 业务 owner projection 委托给 OwnedReferenceRegistry。
/// </para>
/// <para>
/// 源码按职责拆在 Core 子目录：
/// Identity / Registry / Spawn / Lifecycle / Components / References / Attribution /
/// Migration / LegacyRelationship / Manager。
/// 新业务入口不要继续加到 EntityManager partial；Projectile、Effect、Ability、UI 等语义应进入对应 capability service。
/// </para>
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _log = new(nameof(EntityManager), LogLevel.Debug);
    private static readonly EntityRegistry _entityRegistry = new();
    private static readonly LifecycleTree _lifecycleTree = new();
    private static readonly OwnedReferenceRegistry _ownedReferenceRegistry = new(_entityRegistry.GetNode);

    // ==================== 实体生成（核心功能）====================

    /// <summary>
    /// 生成 Entity (统一参数版本)
    /// 支持对象池和场景两种创建模式，通过 EntitySpawnConfig 对象传递参数
    /// 
    /// 使用示例：
    /// <code>
    /// // 对象池 Entity
    /// var enemy = EntityManager.Spawn<Enemy>(new EntitySpawnConfig
    /// {
    ///     Config = enemyData,
    ///     UsingObjectPool = true,
    ///     PoolName = ObjectPoolNames.EnemyPool,
    ///     Position = new Vector2(100, 200)
    /// });
    /// 
    /// // 场景 Entity - 类型安全，无需指定 SceneName
    /// var player = EntityManager.Spawn<Player>(new EntitySpawnConfig
    /// {
    ///     Config = playerData,
    ///     UsingObjectPool = false,  // 自动使用 "Player" 查找 ResourceManagement
    ///     Position = new Vector2(500, 300),
    ///     Rotation = 45f
    /// });
    /// </code>
    /// </summary>
    /// <typeparam name="T">Entity 类型(如 Enemy, Player)</typeparam>
    /// <param name="config">生成配置参数</param>
    /// <returns>已配置好的 Entity 实例</returns>
    public static T? Spawn<T>(EntitySpawnConfig config) where T : Node, IEntity
    {
        ObjectPool<T>? pool = null;
        var spawnPipeline = new EntitySpawnPipeline(_entityRegistry, _lifecycleTree, _componentRegistrar);
        var result = spawnPipeline.Spawn(new EntitySpawnRequest<T>
        {
            CreateNode = () => CreateNode(config, out pool),
            Config = config.Config,
            RuntimeDataBootstrap = config.RuntimeDataBootstrap,
            RuntimeDataRecord = config.RuntimeDataRecord,
            RuntimeDataRecordTable = config.RuntimeDataRecordTable,
            RuntimeDataRecordId = config.RuntimeDataRecordId,
            LifecycleParentId = config.LifecycleParentId,
            ParentDestroyPolicy = config.ParentDestroyPolicy,
            Position = config.Position,
            Rotation = config.Rotation,
            VisualSceneOverride = config.VisualSceneOverride,
            AddToSceneTree = node =>
            {
                if (!config.UsingObjectPool)
                    AddToSceneTree(node);
            },
            ActivateNode = node =>
            {
                if (pool == null)
                    return;

                pool.Activate(node);

                // 对象池 CharacterBody2D 必须在 Activate 后再延迟执行一次零速度 MoveAndSlide。
                if (node is CharacterBody2D pooledBody)
                {
                    pooledBody.Velocity = Vector2.Zero;
                    pooledBody.CallDeferred(CharacterBody2D.MethodName.MoveAndSlide);
                }
            },
            RollbackNode = node =>
            {
                if (pool != null && GodotObject.IsInstanceValid(node))
                    ObjectPoolManager.ReturnToPool(node);
            }
        });

        if (!result.Success)
        {
            _log.Error($"Entity spawn failed: {typeof(T).Name}, error={result.Error}");
            return null;
        }

        return result.Node;
    }

    private static T? CreateNode<T>(EntitySpawnConfig config, out ObjectPool<T>? pool) where T : Node, IEntity
    {
        pool = null;
        if (config.UsingObjectPool)
        {
            if (string.IsNullOrEmpty(config.PoolName))
            {
                _log.Error($"使用对象池模式但未提供 PoolName: {typeof(T).Name}");
                return null;
            }

            pool = ObjectPoolManager.GetPool<T>(config.PoolName);
            if (pool == null)
            {
                _log.Error($"对象池不存在: 期望名称 '{config.PoolName}' (类型: {typeof(T).Name})");
                return null;
            }

            var pooledEntity = pool.Get(false);
            _log.Debug($"从对象池获取 Entity: {typeof(T).Name} (池名: {config.PoolName})");
            return pooledEntity;
        }

        var scene = ResourceLoading.Load<PackedScene>(typeof(T).Name, ResourceCategory.Entity);
        if (scene == null)
        {
            _log.Error($"场景加载失败: {typeof(T).Name} (请检查 ResourceGenerator 是否运行)");
            return null;
        }

        var entity = scene.Instantiate<T>();
        _log.Debug($"从场景实例化 Entity: {typeof(T).Name}");
        return entity;
    }

    /// <summary>
    /// 将 Entity 添加到场景树（非对象池模式）
    /// </summary>
    private static void AddToSceneTree<T>(T entity) where T : Node, IEntity
    {
        if (entity.GetParent() != null) return;

        string typeName = typeof(T).Name;
        string path = $"ECS/Entity/{typeName}";
        var parent = RuntimeMountService.GetOrCreate(
            RuntimeMountIds.EntityType(typeName),
            path,
            "Runtime.Entity",
            $"{typeName} Entity 挂载点");

        parent.AddChild(entity);
        _log.Debug($"已将 Entity 添加到场景树: {typeName} -> {path}");
    }

    // ==================== 注册与注销 ====================

    /// <summary>
    /// 注册 Entity/Component 到 EntityManager
    /// </summary>
    /// <param name="node">要注册的节点（Entity 或 Component）</param>
    public static void Register(Node node)
    {
        var entityId = ResolveRuntimeEntityId(node);
        NodeLifecycleManager.Register(node, NodeLifecycleOwner.Entity(entityId.Value), "EntityManager.Register");
        _entityRegistry.Register(entityId, node);
    }

    /// <summary>
    /// 注销 Entity（Entity._ExitTree 时调用）
    /// 同时注销其所有 Component 并清理关系
    /// </summary>
    public static void UnregisterEntity(Node entity)
    {
        string id = entity.GetInstanceId().ToString();

        // 检查是否已注册
        if (_entityRegistry.GetEntityId(entity).IsEmpty && !NodeLifecycleManager.IsRegistered(id))
        {
            _log.Warn($"Entity {id} 未注册，无法注销");
            return;
        }

        // 1. 清理业务 owner 引用，再注销所有 Component。
        // 必须先于 Data/Events 清理，以便 cleanup / Component 仍能访问 Entity 数据。
        _ownedReferenceRegistry.CleanupDestroyedChild(ResolveRuntimeEntityId(entity));
        UnregisterComponents(entity);

        // 2. 统一清理 IEntity 资源
        if (entity is IEntity iEntity)
        {
            // 清空事件
            iEntity.Events.Clear();
            // 清空数据
            iEntity.Data.Clear();
        }

        // 3. 从 NodeLifecycleManager 注销
        NodeLifecycleManager.Unregister(entity);
        _entityRegistry.Unregister(entity);

        // 4. 清理 Entity 自身的所有关系（作为父或子）
        EntityRelationshipManager.RemoveAllRelationships(id);
    }

    /// <summary>
    /// 根据 ID 获取 Entity/Component
    /// <param name="id">Entity/Component 的 节点ID</param>
    /// <returns>Entity/Component 的节点</returns>
    /// </summary>
    public static Node? GetEntityById(string id)
    {
        var typedId = EntityId.From(id);
        var node = _entityRegistry.GetNode(typedId);
        if (node != null)
            return node;

        return NodeLifecycleManager.GetNodeById(id);
    }

    /// <summary>
    /// 通过 typed EntityId 查询 runtime entity node。
    /// </summary>
    public static Node? ResolveEntityNode(EntityId id)
    {
        return _entityRegistry.GetNode(id);
    }

    /// <summary>
    /// 建立生命周期父子关系。
    /// <para>新生成实体优先通过 EntitySpawnConfig.LifecycleParentId 进入；该入口用于手工注册实体、迁移和旧路径收敛。</para>
    /// </summary>
    public static bool AttachLifecycleParent(
        IEntity? parent,
        IEntity? child,
        ParentDestroyPolicy parentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively)
    {
        if (parent is not Node parentNode || child is not Node childNode)
            return false;

        return _lifecycleTree.Attach(
            ResolveRuntimeEntityId(parentNode),
            ResolveRuntimeEntityId(childNode),
            parentDestroyPolicy);
    }

    /// <summary>
    /// 读取实体在 LifecycleTree 中的直接生命周期父级 Id。
    /// </summary>
    public static EntityId GetLifecycleParentId(Node? child)
    {
        if (child == null)
            return EntityId.Empty;

        return _lifecycleTree.GetParent(ResolveRuntimeEntityId(child));
    }

    /// <summary>
    /// 读取实体在 LifecycleTree 中的直接生命周期链接。
    /// </summary>
    public static LifecycleLink? GetLifecycleLink(Node? child)
    {
        if (child == null)
            return null;

        return _lifecycleTree.GetLink(ResolveRuntimeEntityId(child));
    }

    // ==================== 查询接口 ====================

    /// <summary>
    /// 按类型查询所有 Entity
    /// </summary>
    public static IEnumerable<T> GetEntitiesByType<T>() where T : Node
    {
        return _entityRegistry.GetNodesByType<T>();
    }


    // ==================== 全局查询 ====================

    /// <summary>
    /// 获取所有已注册的 Entity（不含 Component）
    /// 常用场景：TargetSelector 的全局查询
    /// </summary>
    /// <returns>所有实现 IEntity 接口的节点</returns>
    public static IEnumerable<IEntity> GetAllEntities()
    {
        return _entityRegistry.GetAllNodes().OfType<IEntity>().ToArray();
    }

    // ==================== 生命周期管理 ====================

    /// <summary>
    /// 销毁 Entity（兼容对象池和非对象池）
    /// - 对象池 Entity：归还到对象池
    /// - 非对象池 Entity：调用 QueueFree 销毁
    /// </summary>
    public static void Destroy(Node entity)
    {
        Destroy(entity, new HashSet<string>());
    }

    /// <summary>
    /// 销毁 Entity（内部递归版本）。
    /// <para>使用 visited 防止异常关系链导致无限递归。</para>
    /// </summary>
    /// <param name="entity">要销毁的实体</param>
    /// <param name="visitedEntityIds">当前销毁链已访问实体 Id 集合</param>
    private static void Destroy(Node entity, HashSet<string> visitedEntityIds)
    {
        if (!GodotObject.IsInstanceValid(entity))
        {
            // 如果节点已经无效（已被引擎释放），仅执行注销逻辑
            UnregisterEntity(entity);
            return;
        }

        string entityId = EntityRelationshipTraversal.ResolveEntityId(entity); // 当前销毁实体 Id
        if (!string.IsNullOrEmpty(entityId) && !visitedEntityIds.Add(entityId))
        {
            _log.Warn($"检测到关系销毁环路，跳过重复销毁: {entityId} ({entity.GetType().Name})");
            return;
        }

        HandleOwnedChildrenOnDestroy(
            entity, // 父实体
            visitedEntityIds // 当前递归访问集合
        );
        HandleLifecycleChildrenOnDestroy(
            entity, // 父实体
            visitedEntityIds // 当前递归访问集合
        );

        // 发送销毁事件（在注销前发送，以便监听者仍能访问实体的 Data/Id）
        if (entity is IEntity iEntity)
        {
            // 通用 Entity 销毁事件（所有 IEntity）
            GlobalEventBus.Global.Emit(new GameEventType.Global.EntityDestroyed(iEntity));
        }

        // 1. 注销（内部已清理 Component、关系、Data、Events）
        UnregisterEntity(entity);

        // 2. 根据类型决定销毁方式
        if (entity is IPoolable)
        {
            // 对象池 Entity：归还到池中
            ObjectPoolManager.ReturnToPool(entity);
        }
        else
        {
            // 非对象池 Entity：直接销毁
            entity.QueueFree();
        }
    }

    /// <summary>
    /// 注册业务 owner 引用 descriptor。
    /// </summary>
    public static bool RegisterOwnedReference(OwnedReferenceDescriptor descriptor)
    {
        return _ownedReferenceRegistry.Register(descriptor);
    }

    /// <summary>
    /// 建立 owner -> child 业务引用；只同步 Data projection，不参与生命周期销毁。
    /// </summary>
    public static bool AddOwnedReference(IEntity owner, IEntity child, OwnedReferenceDescriptor descriptor)
    {
        return _ownedReferenceRegistry.AddReference(owner, child, descriptor);
    }

    /// <summary>
    /// 移除 child 当前 owner 业务引用；只同步 Data projection。
    /// </summary>
    public static bool RemoveOwnedReference(IEntity child, OwnedReferenceDescriptor descriptor)
    {
        return _ownedReferenceRegistry.RemoveReference(child, descriptor);
    }

    /// <summary>
    /// 在父实体注销前，按 PARENT 关系上的生命周期策略处理直接归属子实体。
    /// <para>只有 PARENT 参与生命周期决策，ENTITY_TO_PROJECTILE 等业务关系只负责分类查询。</para>
    /// </summary>
    /// <param name="entity">父实体</param>
    /// <param name="visitedEntityIds">当前递归访问集合</param>
    private static void HandleOwnedChildrenOnDestroy(
        Node entity, // 父实体
        HashSet<string> visitedEntityIds // 当前递归访问集合
    )
    {
        List<EntityRelationshipLifecycle.OwnedChildSnapshot> ownedChildren = EntityRelationshipLifecycle.GetDirectOwnedChildren(
            entity // 父实体
        );

        foreach (EntityRelationshipLifecycle.OwnedChildSnapshot ownedChild in ownedChildren)
        {
            if (!GodotObject.IsInstanceValid(ownedChild.ChildNode))
            {
                continue;
            }

            if (ownedChild.DestroyPolicy != ParentDestroyPolicy.DestroyRecursively)
            {
                continue;
            }

            Destroy(
                ownedChild.ChildNode, // 直接归属子实体
                visitedEntityIds // 复用同一递归保护集合
            );
        }
    }

    /// <summary>
    /// 处理新 LifecycleTree 中的直接生命周期子实体。
    /// <para>这是 T1.7 后的兼容接线：spawn 已写 LifecycleTree，旧 Destroy 仍在本文件中。</para>
    /// </summary>
    private static void HandleLifecycleChildrenOnDestroy(
        Node entity, // 父实体
        HashSet<string> visitedEntityIds // 当前递归访问集合
    )
    {
        var entityId = ResolveRuntimeEntityId(entity);
        if (entityId.IsEmpty)
            return;

        var childSnapshot = _lifecycleTree.GetChildren(entityId).ToArray();
        foreach (var childLink in childSnapshot)
        {
            if (childLink.DestroyPolicy != ParentDestroyPolicy.DestroyRecursively)
                continue;

            var childNode = _entityRegistry.GetNode(childLink.ChildId);
            if (childNode != null && GodotObject.IsInstanceValid(childNode))
                Destroy(childNode, visitedEntityIds);
        }

        foreach (var childLink in childSnapshot)
        {
            _lifecycleTree.Detach(childLink.ChildId);
        }

        _lifecycleTree.DetachAll(entityId);
    }

    private static EntityId ResolveRuntimeEntityId(Node node)
    {
        if (node is IEntity entity)
        {
            var dataId = EntityId.From(entity.Data.Get<string>(GeneratedDataKey.Id));
            if (!dataId.IsEmpty)
                return dataId;
        }

        return EntityId.From(node.GetInstanceId().ToString());
    }

    /// <summary>
    /// 销毁所有指定类型的 Entity，比如所有Enemy
    /// </summary>
    public static void DestroyAllByType<T>() where T : Node
    {
        var entities = _entityRegistry.GetNodesByType<T>().ToList();
        if (entities.Count == 0)
            return;

        // 复制列表避免迭代时修改
        foreach (var entity in entities)
        {
            Destroy(entity);
        }

        _log.Info($"已销毁所有 {typeof(T).Name} 类型的 Entity，共 {entities.Count} 个");
    }

    /// <summary>
    /// 清理所有 Entity（场景切换时调用）
    /// 会真正销毁所有实体并归还到对象池
    /// </summary>
    public static void Clear()
    {
        var allNodes = _entityRegistry.GetAllNodes().ToList();
        int count = allNodes.Count;

        // 使用 Destroy 统一处理，确保逻辑一致
        foreach (var node in allNodes)
        {
            // 只销毁 IEntity，Component 会随 Entity 一起清理
            if (node is IEntity)
            {
                Destroy(node);
            }
        }

        _log.Info($"EntityManager 已清空，共销毁 {count} 个节点");
    }
}
