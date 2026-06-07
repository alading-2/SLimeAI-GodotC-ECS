using Godot;
using System.Collections.Generic;

/// <summary>
/// 通用 Node 生命周期管理器
/// 
/// 职责：提供底层的 Node 注册、查询、注销功能
/// 
/// 设计理念：
/// - EntityManager（管理 IEntity）和 UIManager（管理 UIBase）都基于此类构建
/// - 本类只负责"注册表"管理，不涉及具体业务逻辑
/// - 关系管理由 EntityRelationshipManager 负责
/// 
/// 使用示例：
/// <code>
/// 注册
/// NodeLifecycleManager.Register(node);
/// 
/// 查询
/// var node = NodeLifecycleManager.GetNodeById(id);
/// var enemies = NodeLifecycleManager.GetNodesByType<Enemy>();
/// 
/// 注销
/// NodeLifecycleManager.Unregister(node);
/// </code>
/// </summary>
public static class NodeLifecycleManager
{
    private static readonly Log _log = new("NodeLifecycleManager", LogLevel.Warning);
    private static readonly NodeLifecycleRegistry _registry = new();

    // ==================== 注册 ====================

    /// <summary>
    /// 注册 Node 到管理器
    /// </summary>
    /// <param name="node">要注册的节点</param>
    /// <returns>是否成功注册（false 表示已存在）</returns>
    public static bool Register(Node node)
    {
        return Register(node, NodeLifecycleOwner.Unknown(node.GetInstanceId().ToString()), "legacy");
    }

    public static bool Register(Node node, NodeLifecycleOwner owner, string source)
    {
        string id = node.GetInstanceId().ToString();
        string nodeType = node.GetType().Name;

        if (!_registry.Register(node, owner, source))
        {
            _log.Warn($"Node {id} ({nodeType}) 已注册，跳过");
            return false;
        }

        _log.Debug($"已注册 Node: {nodeType} (ID: {id})");
        return true;
    }

    /// <summary>
    /// 检查 Node 是否已注册
    /// </summary>
    public static bool IsRegistered(string nodeId)
    {
        return _registry.IsRegistered(nodeId);
    }

    /// <summary>
    /// 检查 Node 是否已注册
    /// </summary>
    public static bool IsRegistered(Node node)
    {
        return IsRegistered(node.GetInstanceId().ToString());
    }

    // ==================== 注销 ====================

    /// <summary>
    /// 从管理器注销 Node
    /// </summary>
    /// <param name="node">要注销的节点</param>
    /// <returns>是否成功注销（false 表示不存在）</returns>
    public static bool Unregister(Node node)
    {
        return Unregister(node.GetInstanceId().ToString());
    }

    /// <summary>
    /// 从管理器注销 Node（通过 ID）
    /// </summary>
    /// <param name="nodeId">节点 ID</param>
    /// <returns>是否成功注销</returns>
    public static bool Unregister(string nodeId)
    {
        if (!_registry.Unregister(nodeId))
        {
            _log.Warn($"Node {nodeId} 未注册，无法注销");
            return false;
        }

        _log.Debug($"已注销 Node: {nodeId}");
        return true;
    }

    // ==================== 查询 ====================

    /// <summary>
    /// 根据 ID 获取 Node
    /// </summary>
    public static Node? GetNodeById(string nodeId)
    {
        return _registry.GetNodeById(nodeId);
    }

    /// <summary>
    /// 按类型查询所有 Node
    /// </summary>
    /// <typeparam name="T">Node 类型</typeparam>
    /// <returns>匹配的节点集合</returns>
    internal static IReadOnlyList<T> GetNodesByType<T>() where T : Node
    {
        return _registry.GetNodesByType<T>();
    }

    /// <summary>
    /// 获取所有已注册的 Node
    /// </summary>
    internal static IReadOnlyList<Node> GetAllNodes()
    {
        return _registry.GetAllNodes();
    }

    /// <summary>
    /// 获取所有实现指定接口/基类的 Node
    /// </summary>
    /// <typeparam name="T">接口或基类类型</typeparam>
    internal static IReadOnlyList<T> GetNodesByInterface<T>() where T : class
    {
        return _registry.GetNodesByInterface<T>();
    }

    // ==================== 统计与清理 ====================

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public static (int TotalNodes, int TypeCount) GetStats()
    {
        return _registry.GetStats();
    }

    public static NodeLifecycleSnapshot GetSnapshot()
    {
        return _registry.GetSnapshot();
    }

    public static int CleanupInvalid()
    {
        return _registry.CleanupInvalid();
    }

    /// <summary>
    /// 清理所有注册
    /// </summary>
    public static void Clear()
    {
        int count = _registry.GetStats().TotalNodes;
        _registry.Clear();
        _log.Info($"NodeLifecycleManager 已清空，共清理 {count} 个 Node");
    }

    // ==================== 调试方法 ====================

    /// <summary>
    /// 获取调试信息
    /// </summary>
    public static string GetDebugInfo()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("=== NodeLifecycleManager 统计信息 ===");
        var snapshot = _registry.GetSnapshot();
        sb.AppendLine($"总节点数: {snapshot.TotalCount}");
        sb.AppendLine($"Invalid: {snapshot.InvalidCount}");
        sb.AppendLine();

        sb.AppendLine("=== 按类型统计 ===");
        foreach (var entry in snapshot.Entries)
        {
            sb.AppendLine($"{entry.NodeType}: {entry.NodeId} owner={entry.Owner.Kind}/{entry.Owner.OwnerId} source={entry.Source}");
        }

        return sb.ToString();
    }
}
