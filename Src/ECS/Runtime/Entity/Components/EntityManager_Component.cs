using System;
using System.Collections.Generic;
using System.Linq;
using Godot;



/// <summary>
/// EntityManager 的 Component 扩展
/// 
/// 职责：管理 Component 的生命周期（注册、注销、增删查）
/// 注意：核心注册方法（Register, UnregisterEntity）保留在主文件，因为它们同时服务于 Entity 和 Component
/// </summary>
public static partial class EntityManager
{
    // ==================== Component 缓存 ====================

    private static readonly Log _componentLog = new("EntityManager_Component", LogLevel.Debug);
    private static readonly ComponentRegistrar _componentRegistrar = new();

    /// <summary>
    /// Component 结构缓存
    /// Key: Entity 场景文件的路径 (scene.ResourcePath) 或 Entity 类型名称
    /// Value: 该 Entity 原型中 Component 的相对路径列表
    /// </summary>
    private static readonly Dictionary<string, List<NodePath>> _componentPathCache = new();

    public static bool TryGetCachedComponentPaths(Node entity, out List<NodePath> paths)
    {
        paths = default!;
        if (entity == null) return false;

        string cacheKey = entity.SceneFilePath;
        if (string.IsNullOrEmpty(cacheKey)) cacheKey = entity.GetType().Name;

        return _componentPathCache.TryGetValue(cacheKey, out paths!) && paths != null && paths.Count > 0;
    }

    /// <summary>
    /// [优化] 预热 Component 缓存
    /// 遍历所有 Entity 资源，实例化并扫描 Component 结构，存入缓存。
    /// 避免在游戏运行时频繁进行 FindChildren 查找。
    /// </summary>
    public static void PrewarmComponentCache()
    {
        _componentLog.Info("🔥 开始预热 Entity Component 缓存...");
        int entityCount = 0;
        int totalComponentCount = 0;

        // 1. 加载所有 Entity 资源
        var entities = ResourceLoading.LoadAll<PackedScene>(ResourceCategory.Entity);

        foreach (var scene in entities)
        {
            try
            {
                // 暂时实例化以扫描结构 (不放入 SceneTree，开销较小)
                Node instance = scene.Instantiate();
                string cacheKey = instance.SceneFilePath;

                if (string.IsNullOrEmpty(cacheKey))
                {
                    // 如果没有文件路径（理论上 LoadAll 出来的都有），尝试用类型名
                    cacheKey = instance.GetType().Name;
                }

                if (_componentPathCache.ContainsKey(cacheKey))
                {
                    instance.Free(); // 释放
                    continue;
                }

                var componentPaths = new List<NodePath>();

                // 执行与 RegisterComponents 相同的查找逻辑
                // 注意：FindChildren 在未添加到 SceneTree 的节点上工作正常 (owned=false)
                var allChildren = instance.FindChildren("*", "Node", true, false);
                foreach (Node child in allChildren)
                {
                    bool isComponent = false;
                    string typeName = child.GetType().Name;

                    if (child is IComponent || typeName.EndsWith("Component"))
                    {
                        isComponent = true;
                    }

                    if (isComponent)
                    {
                        // 记录相对路径
                        componentPaths.Add(instance.GetPathTo(child));
                    }
                }

                // 仅当找到 Component 时才缓存，避免缓存错误状态导致后续跳过查找
                if (componentPaths.Count > 0)
                {
                    _componentPathCache[cacheKey] = componentPaths;
                    entityCount++;
                    totalComponentCount += componentPaths.Count;
                    _componentLog.Debug($"  - 缓存 {cacheKey}: {componentPaths.Count} components");
                }
                else
                {
                    _componentLog.Warn($"  - 预热警告: {cacheKey} 未找到任何 Component (可能结构特殊)");
                }

                // 立即释放实例
                instance.Free();
            }
            catch (Exception ex)
            {
                _componentLog.Error($"预热失败: {ex.Message}");
            }
        }

        _componentLog.Info($"✅ 缓存预热完成: {entityCount} 个 Entity, 共 {totalComponentCount} 个 Component 路径已缓存。");
    }

    // ==================== Component 注册 ====================

    /// <summary>
    /// 自动注册 Entity 的所有 Component（递归查找所有层级）
    /// 识别规则（按优先级）：
    /// 1. 实现了 IComponent 接口（最高优先级）
    /// 2. 类名以 "Component" 结尾（命名约定）
    /// 
    /// 自动建立 Entity-Component 内部 owner 索引（通过 ComponentRegistrar）
    /// 
    /// 注意：优先使用预热缓存(_componentPathCache)，命中失败则回退到 FindChildren()
    /// </summary>
    public static void RegisterComponents(Node entity)
    {
        int registeredCount = 0;
        var hasCompositionProvider = entity is IComponentCompositionProvider;
        var composedCount = ComponentComposer.Compose(entity);

        // 尝试从缓存获取
        string cacheKey = entity.SceneFilePath;
        if (string.IsNullOrEmpty(cacheKey)) cacheKey = entity.GetType().Name;

        IList<Node> componentsToRegister = new List<Node>();

        // Check if cache exists AND has content
        if (!hasCompositionProvider && composedCount == 0 && _componentPathCache.TryGetValue(cacheKey, out var cachedPaths) && cachedPaths.Count > 0)
        {
            // [Hit Cache] 使用缓存路径直接获取节点
            foreach (var path in cachedPaths)
            {
                var node = entity.GetNodeOrNull(path);
                if (node != null)
                {
                    componentsToRegister.Add(node);
                }
                else
                {
                    _componentLog.Warn($"[Cache Warn] Entity {entity.Name} 缓存路径失效: {path}");
                }
            }
            // _componentLog.Debug($"[Cache Hit] {entity.Name} ({componentsToRegister.Count})");
        }
        else
        {
            // [Miss Cache] 回退到递归查找
            // compose 创建的新组件不一定存在于预热缓存中，因此本次必须实时扫描。
            // _componentLog.Debug($"[Cache Miss] Entity {entity.Name} (Key: {cacheKey})"); 
            var allChildren = entity.FindChildren("*", "Node", true, false);

            foreach (Node child in allChildren)
            {
                bool isComponent = false;
                string componentType = child.GetType().Name;

                if (child is IComponent || componentType.EndsWith("Component"))
                {
                    isComponent = true;
                }

                if (isComponent)
                {
                    componentsToRegister.Add(child);
                }
            }
        }

        // 统一处理注册；Component owner 索引由 ComponentRegistrar 维护，不再进入 Relationship 图。
        registeredCount = _componentRegistrar.RegisterComponents(entity, componentsToRegister);

        if (registeredCount > 0)
        {
            _componentLog.Debug($"Entity {entity.Name} 共注册 {registeredCount} 个 Component");
        }
    }

    /// <summary>
    /// 注销 Entity 的所有 Component（包括清理 ComponentRegistrar owner 索引）
    /// 通过 ComponentRegistrar 内部索引查询，不再公开 Entity-Component 关系
    /// 
    /// 注意：此方法为 internal，由 UnregisterEntity 调用
    /// </summary>
    internal static void UnregisterComponents(Node entity)
    {
        int unregisteredCount = _componentRegistrar.UnregisterComponents(entity);

        if (unregisteredCount > 0)
        {
            _componentLog.Debug($"Entity {entity.Name} 共注销 {unregisteredCount} 个 Component");
        }
    }

    // ==================== Component 查询 ====================

    /// <summary>
    /// 按类型查询所有 Component
    /// 常用场景：获取所有 HealthComponent 以显示血条
    /// </summary>
    public static IEnumerable<T> GetComponentsByType<T>() where T : Node
    {
        return _componentRegistrar.GetComponentsByType<T>();
    }

    /// <summary>
    /// 获取所有指定类型 Component 的 ID 列表
    /// 常用场景：调试或迁移期兼容旧 ID 查询
    /// </summary>
    /// <returns>Component 的 ID 列表</returns>
    public static IEnumerable<string> GetComponentIdsByType<T>()
    {
        return _componentRegistrar
            .GetComponentsByType<Node>()
            .Where(component => component is T || component.GetType().Name == typeof(T).Name)
            .Select(component => component.GetInstanceId().ToString());
    }

    /// <summary>
    /// 通过 Component 查找所属 Entity
    /// 常用场景：Component 需要访问 Entity 数据
    /// </summary>
    public static Node? GetEntityByComponent(Node component)
    {
        return _componentRegistrar.GetEntityByComponent(component);
    }

    /// <summary>
    /// 获取 Component 所属 Entity 的 Data 容器
    /// 常用于 Component 访问 Entity 的运行时数据
    /// </summary>
    /// <param name="component">Component 节点</param>
    /// <returns>Entity 的 Data 容器，如果 Entity 未找到或不是 IEntity 则返回 null</returns>
    public static Data? GetEntityData(Node component)
    {
        var entity = GetEntityByComponent(component);
        if (entity is IEntity iEntity)
            return iEntity.Data;
        return null;
    }

    // ==================== 动态 Component 管理 ====================

    /// <summary>
    /// 动态添加 Component 到 Entity
    /// 自动处理：挂载节点 → 注册 → 建立内部 owner 索引 → 触发回调
    /// 常用场景：运行时添加 Buff、技能等
    /// 
    /// 注意：Component 会被添加到 Entity/Component 路径下，如果 Component 节点不存在会自动创建
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要添加的 Component</param>
    public static void AddComponent<T>(Node entity, T component) where T : Node
    {
        // 1. 获取或创建 Component 容器节点
        Node componentContainer = entity.GetNodeOrNull("Component");
        if (componentContainer == null)
        {
            componentContainer = new Node();
            componentContainer.Name = "Component";
            entity.AddChild(componentContainer);
            _componentLog.Debug($"为 Entity {entity.Name} 创建 Component 容器节点");
        }

        // 2. 挂载到 Component 容器下
        componentContainer.AddChild(component);

        // 3. 注册 Component，并建立内部 owner 索引。
        string componentType = typeof(T).Name;
        _componentRegistrar.RegisterComponent(entity, component);

        _componentLog.Info($"已动态添加 Component: {componentType} 到 Entity: {entity.Name}/Component");
    }

    /// <summary>
    /// 从 Entity 获取指定类型的 Component
    /// 常用场景：获取 Entity 上的特定组件（如 HealthComponent）
    /// </summary>
    /// <typeparam name="T">Component 类型</typeparam>
    /// <param name="entity">目标 Entity</param>
    /// <returns>找到的 Component，如果不存在则返回 null</returns>
    public static T? GetComponent<T>(Node entity) where T : Node
    {
        var component = _componentRegistrar.GetComponent<T>(entity);
        if (component != null)
            return component;

        _componentLog.Warn($"Entity {entity.Name} 未找到 Component: {typeof(T).Name}");
        return null;
    }

    /// <summary>
    /// 从 Entity 移除 Component（通过类型字符串）
    /// 自动处理：查找 Component → 触发回调 → 移除内部 owner 索引 → 注销 → 销毁节点
    /// 常用场景：通过组件类型名称移除组件（如 "HealthComponent"）
    /// </summary>
    /// <param name="entity">目标 Entity</param>
    /// <param name="componentType">Component 类型名称（如 "HealthComponent"）</param>
    /// <returns>是否成功移除</returns>
    public static bool RemoveComponent(Node entity, string componentType)
    {
        foreach (var component in _componentRegistrar.GetComponents(entity))
        {
            if (component.GetType().Name != componentType)
                continue;

            RemoveComponent(entity, component);
            return true;
        }

        _componentLog.Warn($"Entity {entity.Name} 未找到 Component: {componentType}，无法移除");
        return false;
    }

    /// <summary>
    /// 从 Entity 移除 Component（通过 Component 实例）
    /// 自动处理：触发回调 → 移除内部 owner 索引 → 注销 → 销毁节点
    /// </summary>
    /// <param name="entity">目标 Entity</param>
    /// <param name="component">要移除的 Component 实例</param>
    public static void RemoveComponent(Node entity, Node component)
    {
        string componentType = component.GetType().Name;

        _componentRegistrar.RemoveComponent(entity, component);

        _componentLog.Info($"已移除 Component: {componentType} 从 Entity: {entity.Name}");
    }
}
