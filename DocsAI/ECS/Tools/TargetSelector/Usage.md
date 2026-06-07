<!-- migrated-from: Src/ECS/Tools/TargetSelector/README.md -->

> 迁移来源：`Src/ECS/Tools/TargetSelector/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# TargetSelector 目标选择工具

## 模块定位

`TargetSelector` 负责统一“范围查询”和“随机落点”逻辑，避免业务层反复手写距离遍历、角度判断与随机采样。

当前结构分为两层 owner：

1. `Geometry2D`
   - 通用二维几何算法层
   - 提供形状判定、距离计算、随机采样
2. `TargetQueryEngine`
   - 目标选择领域层
   - 负责实体过滤、排序、数量裁剪、位置批量生成、结果 ownership 和 diagnostics

## 典型入口

### 查询实体

```csharp
using var result = TargetQueryEngine.QueryEntities(new TargetSelectorQuery
{
    Geometry = GeometryType.Circle,
    Origin = caster.GlobalPosition,
    Range = 250f,
    CenterEntity = caster,
    TeamFilter = TeamFilter.Enemy,
    Sorting = TargetSorting.Nearest,
    MaxTargets = 5,
    RandomSeed = 1234,
});
var targets = result.Items;
var wasTruncated = result.Diagnostics.Truncated;
var geometryHits = result.Diagnostics.GeometryHitCount;
```

### 生成位置

```csharp
using var result = TargetQueryEngine.QueryPositions(new TargetSelectorQuery
{
    Geometry = GeometryType.Ring,
    Origin = caster.GlobalPosition,
    InnerRange = 120f,
    Range = 280f,
    MaxTargets = 3,
    RandomSeed = 1234,
});
var points = result.Items;
```

### 直接使用几何层

```csharp
bool inCone = Geometry2D.IsPointInCone(targetPos, casterPos, facing, 300f, 60f);
Vector2 random = Geometry2D.GetRandomPointInCircle(casterPos, 150f);
```

## 边界说明

- `Geometry2D` 只做纯算法，不依赖 `IEntity`
- `GeometryType`、`TargetSelectorQuery`、`TargetQueryResult<T>` 和 `TargetQueryDiagnostics` 仍留在 `TargetSelector` 域。
- `EntityTargetSelector` / `PositionTargetSelector` 旧 list-only facade 已删除；业务主链路必须使用 `TargetQueryEngine`。
- `GeometryCalculator` 旧门面已删除；只需要纯几何时直接调用 `Geometry2D`。

## 当前支持的几何语义

- `Circle`
- `Ring`
- `Box`
- `Line`（胶囊）
- `Cone`
- `Global`
- `Single`（通常由外部显式指定目标）

## 约束

1. 不要在业务里直接 `GetTree().GetNodesInGroup(...)` 再手写几何判断。
2. 只需要纯几何能力时，优先调用 `Geometry2D`。
3. 只需要目标选择语义时，使用 `TargetQueryEngine.QueryEntities` 或 `TargetQueryEngine.QueryPositions`。
4. 随机排序和位置采样需要复现时传 `RandomSeed` 或 `RandomSource`。
