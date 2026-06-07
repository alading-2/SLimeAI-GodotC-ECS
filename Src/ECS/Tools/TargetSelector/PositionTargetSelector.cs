using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 空间位置生成工具
/// 利用 Geometry2D/GeometryCalculator 进行形状采样，生成指定数量的随机坐标点
/// </summary>
public static class PositionTargetSelector
{
    private static readonly Log _log = new(nameof(PositionTargetSelector));

    /// <summary>
    /// 根据查询配置在指定的几何形状内生成随机目标位置点
    /// </summary>
    /// <param name="query">查询参数（几何类型、原点、范围、数量等）。</param>
    /// <returns>随机生成的位置列表，长度至少为 1。</returns>
    public static List<Vector2> Query(TargetSelectorQuery query)
    {
        using var result = TargetQueryEngine.QueryPositions(query);
        return result.Items.ToList();
    }
}
