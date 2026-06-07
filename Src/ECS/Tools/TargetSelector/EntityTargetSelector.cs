using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 实体目标选择器。
/// 负责从已注册实体中筛出满足几何范围、阵营/类型过滤、排序与数量限制的目标集合。
///
/// 执行顺序：
/// 1) 几何候选收集（或 Chain 路径生成）；
/// 2) 阵营与类型过滤；
/// 3) 排序（Chain 保留路径顺序不排序）；
/// 4) MaxTargets 截断。
/// </summary>
public static class EntityTargetSelector
{
    private static readonly Log _log = new(nameof(EntityTargetSelector));

    /// <summary>
    /// 查询并返回符合条件的实体列表。
    /// 支持常规几何范围扫描（Circle/Ring/Box/Line/Cone/Global）。
    /// </summary>
    /// <param name="query">查询配置参数</param>
    /// <returns>符合条件的 List&lt;IEntity&gt;</returns>
    public static List<IEntity> Query(TargetSelectorQuery query)
    {
        using var result = TargetQueryEngine.QueryEntities(query);
        return result.Items.ToList();
    }
}
