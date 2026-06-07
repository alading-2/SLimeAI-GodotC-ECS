using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class TargetSelectorTest : Node
{
    private static readonly Log _log = new Log("TargetSelectorTest");

    /// <summary>
    /// 轻量测试实体。
    /// - 继承 Node2D，便于直接参与空间距离计算；
    /// - 实现 IEntity，便于走 TargetQueryEngine 全流程过滤。
    /// </summary>
    public partial class MockEntity : Node2D, IEntity
    {
        public Data Data { get; } = new Data();
        public EventBus Events { get; } = new EventBus();

        public MockEntity(string name, Vector2 pos, Team team = Team.Enemy, EntityType type = EntityType.Unit)
        {
            Name = name;
            GlobalPosition = pos;
            Data.Set(GeneratedDataKey.Team, team);
            Data.Set(GeneratedDataKey.EntityType, type);
            Data.Set(GeneratedDataKey.Id, GetInstanceId().ToString());
        }
    }

    public override void _Ready()
    {
        Run();
        GetTree().Quit();
    }

    public void Run()
    {
        _log.Info("开始测试 TargetSelector...");

        TestEntityQuery_FilterAndSort();
        TestEntityQuery_ResultDiagnostics();
        TestEntityQuery_DiagnosticsShouldExplainFilteringAndUseResolvedOrigin();
        TestEntityQuery_RandomSortingShouldBeSeeded();
        TestEntityQuery_InvalidGeometryShouldReturnErrors();
        TestEntityQuery_SingleExplicitTargetShouldUseSourceAndFilter();
        TestEntityQuery_ExplicitCandidatesShouldNotScanEntityManager();
        TestEntityQuery_MissingSortDataShouldWarn();
        TestEntityQuery_TypeFilterShouldCountFilteredTargets();
        TestPositionQuery_ShouldUseSeededRandom();
        TestPositionQuery_Ring();
        TestGeometry2D_Circle();
        TestGeometry2D_OtherShapes();

        _log.Info("TargetSelector 测试完成");
    }

    /// <summary>
    /// 验证普通几何查询的完整流程：
    /// Circle 命中 -> TeamFilter 过滤 -> Sorting.Nearest 排序 -> MaxTargets 截断。
    /// </summary>
    private void TestEntityQuery_FilterAndSort()
    {
        _log.Info("测试实体查询 (过滤 + 排序 + 截断)...");

        var center = new MockEntity("Center", new Vector2(0, 0), Team.Player, EntityType.Unit);
        var enemyNear = new MockEntity("EnemyNear", new Vector2(40, 0), Team.Enemy, EntityType.Unit);
        var enemyFar = new MockEntity("EnemyFar", new Vector2(80, 0), Team.Enemy, EntityType.Unit);
        var friendly = new MockEntity("Friendly", new Vector2(20, 0), Team.Player, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(enemyNear);
        EntityManager.Register(enemyFar);
        EntityManager.Register(friendly);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = center.GlobalPosition,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = TargetSorting.Nearest,
                MaxTargets = 2
            };

            using var result = TargetQueryEngine.QueryEntities(query);
            var results = result.Items;
            var resultNames = string.Join(", ", results.Select(r => (r as Node)?.Name ?? "null"));
            _log.Info($"查询结果: [{resultNames}]");

            AssertTrue(results.Count == 2, $"应只保留 2 个目标 (实际: {results.Count})");
            if (results.Count >= 2)
            {
                AssertTrue(results[0] == enemyNear, $"第 1 个应为 EnemyNear, 实际: {(results[0] as Node)?.Name}");
                AssertTrue(results[1] == enemyFar, $"第 2 个应为 EnemyFar, 实际: {(results[1] as Node)?.Name}");
            }

            AssertTrue(!results.Contains(friendly), "友军不应被 Enemy 过滤器选中");
            AssertTrue(!results.Contains(center), "自身不应被 Enemy 过滤器选中");
        }
        finally
        {
            CleanupEntities(center, enemyNear, enemyFar, friendly);
        }
    }

    /// <summary>
    /// 回归测试：
    /// 新查询入口必须暴露结果 ownership 和诊断信息，避免调用方只拿到可变 List 后无法判断截断原因。
    /// </summary>
    private void TestEntityQuery_ResultDiagnostics()
    {
        _log.Info("测试实体查询结果 diagnostics...");

        var center = new MockEntity("DiagnosticCenter", new Vector2(0, 0), Team.Player, EntityType.Unit);
        var enemyNear = new MockEntity("DiagnosticEnemyNear", new Vector2(10, 0), Team.Enemy, EntityType.Unit);
        var enemyFar = new MockEntity("DiagnosticEnemyFar", new Vector2(20, 0), Team.Enemy, EntityType.Unit);
        var friendly = new MockEntity("DiagnosticFriendly", new Vector2(30, 0), Team.Player, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(enemyNear);
        EntityManager.Register(enemyFar);
        EntityManager.Register(friendly);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = center.GlobalPosition,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = TargetSorting.Nearest,
                MaxTargets = 1
            };

            using var result = TargetQueryEngine.QueryEntities(query);

            AssertTrue(result.Items.Count == 1, $"应只返回 1 个目标 (实际: {result.Items.Count})");
            AssertTrue(result.Items[0] == enemyNear, $"第 1 个应为 DiagnosticEnemyNear, 实际: {(result.Items[0] as Node)?.Name}");
            AssertTrue(result.Diagnostics.CandidateCount == 2, $"候选数量应为 2 (实际: {result.Diagnostics.CandidateCount})");
            AssertTrue(result.Diagnostics.ReturnedCount == 1, $"返回数量应为 1 (实际: {result.Diagnostics.ReturnedCount})");
            AssertTrue(result.Diagnostics.MaxTargets == 1, $"MaxTargets 应为 1 (实际: {result.Diagnostics.MaxTargets})");
            AssertTrue(result.Diagnostics.Truncated, "查询结果应标记为已截断");
        }
        finally
        {
            CleanupEntities(center, enemyNear, enemyFar, friendly);
        }
    }

    private void TestPositionQuery_Ring()
    {
        _log.Info("测试 TargetQueryEngine.QueryPositions (Ring 圆环)...");

        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring,
            Origin = new Vector2(100, 100),
            InnerRange = 50f,
            Range = 150f,
            MaxTargets = 10
        };

        using var result = TargetQueryEngine.QueryPositions(query);
        var results = result.Items;

        AssertTrue(results.Count == 10, $"应生成 10 个随机点 (实际: {results.Count})");

        int validCount = 0;
        foreach (var p in results)
        {
            float dist = p.DistanceTo(query.Origin);
            if (dist >= query.InnerRange - 0.1f && dist <= query.Range + 0.1f)
            {
                validCount++;
            }
        }

        AssertTrue(validCount == 10, $"所有点都应在 [50, 150] 距离内 (有效: {validCount}/10)");
    }

    private void TestEntityQuery_DiagnosticsShouldExplainFilteringAndUseResolvedOrigin()
    {
        _log.Info("测试 TargetQueryEngine diagnostics 和 resolved origin...");

        var center = new MockEntity("DiagnosticOriginCenter", new Vector2(0, 0), Team.Player, EntityType.Unit);
        var enemy = new MockEntity("DiagnosticOriginEnemy", new Vector2(210, 0), Team.Enemy, EntityType.Unit);
        var friendly = new MockEntity("DiagnosticOriginFriendly", new Vector2(220, 0), Team.Player, EntityType.Unit);
        var deadEnemy = new MockEntity("DiagnosticOriginDead", new Vector2(230, 0), Team.Enemy, EntityType.Unit);
        deadEnemy.Data.Set(GeneratedDataKey.LifecycleState, LifecycleState.Dead);

        EntityManager.Register(center);
        EntityManager.Register(enemy);
        EntityManager.Register(friendly);
        EntityManager.Register(deadEnemy);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = Vector2.Zero,
                OriginProvider = () => new Vector2(200, 0),
                Range = 50f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = TargetSorting.Nearest,
                MaxTargets = 1
            };

            using var result = TargetQueryEngine.QueryEntities(query);
            var diagnostics = result.Diagnostics;

            AssertTrue(result.Items.Count == 1, $"应返回 1 个目标 (实际: {result.Items.Count})");
            AssertTrue(result.Items[0] == enemy, $"应命中 resolved origin 附近敌人，实际: {(result.Items[0] as Node)?.Name}");
            AssertTrue(diagnostics.ResolvedOrigin == new Vector2(200, 0), $"diagnostics 应记录 resolved origin, 实际: {diagnostics.ResolvedOrigin}");
            AssertTrue(diagnostics.CandidateCount >= 4, $"candidate count 应记录全量候选，实际: {diagnostics.CandidateCount}");
            AssertTrue(diagnostics.GeometryHitCount == 3, $"geometry hit count 应为 3，实际: {diagnostics.GeometryHitCount}");
            AssertTrue(diagnostics.FilteredByTeamCount == 1, $"team filter count 应为 1，实际: {diagnostics.FilteredByTeamCount}");
            AssertTrue(diagnostics.FilteredByLifecycleCount == 1, $"lifecycle filter count 应为 1，实际: {diagnostics.FilteredByLifecycleCount}");
            AssertTrue(diagnostics.LimitApplied, "MaxTargets=1 时 diagnostics 应标记 limit applied");
        }
        finally
        {
            CleanupEntities(center, enemy, friendly, deadEnemy);
        }
    }

    private void TestEntityQuery_RandomSortingShouldBeSeeded()
    {
        _log.Info("测试 TargetQueryEngine random sorting seed...");

        var center = new MockEntity("RandomCenter", new Vector2(0, 0), Team.Player, EntityType.Unit);
        var first = new MockEntity("RandomFirst", new Vector2(10, 0), Team.Enemy, EntityType.Unit);
        var second = new MockEntity("RandomSecond", new Vector2(20, 0), Team.Enemy, EntityType.Unit);
        var third = new MockEntity("RandomThird", new Vector2(30, 0), Team.Enemy, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(first);
        EntityManager.Register(second);
        EntityManager.Register(third);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = center.GlobalPosition,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = TargetSorting.Random,
                MaxTargets = -1,
                RandomSeed = 12345
            };

            using var firstResult = TargetQueryEngine.QueryEntities(query);
            using var secondResult = TargetQueryEngine.QueryEntities(query);

            var firstOrder = string.Join(",", firstResult.Items.Select(item => (item as Node)?.Name));
            var secondOrder = string.Join(",", secondResult.Items.Select(item => (item as Node)?.Name));

            AssertTrue(firstOrder == secondOrder, $"固定 seed random sorting 应复现: {firstOrder} vs {secondOrder}");
        }
        finally
        {
            CleanupEntities(center, first, second, third);
        }
    }

    private void TestPositionQuery_ShouldUseSeededRandom()
    {
        _log.Info("测试 TargetQueryEngine position query seed...");

        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Ring,
            Origin = new Vector2(100, 100),
            InnerRange = 50f,
            Range = 150f,
            MaxTargets = 4,
            RandomSeed = 777
        };

        using var first = TargetQueryEngine.QueryPositions(query);
        using var second = TargetQueryEngine.QueryPositions(query);

        var firstPoints = string.Join("|", first.Items.Select(point => point.ToString()));
        var secondPoints = string.Join("|", second.Items.Select(point => point.ToString()));

        AssertTrue(firstPoints == secondPoints, $"固定 seed position query 应复现: {firstPoints} vs {secondPoints}");
        AssertTrue(first.Diagnostics.ResolvedOrigin == query.Origin, $"position diagnostics 应记录 origin: {first.Diagnostics.ResolvedOrigin}");
    }

    private void TestEntityQuery_InvalidGeometryShouldReturnErrors()
    {
        _log.Info("测试 TargetQueryEngine invalid geometry diagnostics...");

        var query = new TargetSelectorQuery
        {
            Geometry = GeometryType.Circle,
            Origin = Vector2.Zero,
            Range = 0f,
            MaxTargets = 1
        };

        using var result = TargetQueryEngine.QueryEntities(query);

        AssertTrue(result.Items.Count == 0, $"非法 query 不应返回目标 (实际: {result.Items.Count})");
        AssertTrue(result.Diagnostics.HasErrors, "非法 query 应返回 diagnostics errors");
        AssertTrue(result.Diagnostics.Errors.Any(error => error.Contains("Range")), $"errors 应说明 Range 非法: {string.Join(" | ", result.Diagnostics.Errors)}");
    }

    private void TestEntityQuery_SingleExplicitTargetShouldUseSourceAndFilter()
    {
        _log.Info("测试 TargetQueryEngine Single explicit target...");

        var center = new MockEntity("SingleCenter", Vector2.Zero, Team.Player, EntityType.Unit);
        var explicitTarget = new MockEntity("SingleTarget", new Vector2(999, 999), Team.Enemy, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(explicitTarget);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Single,
                Origin = Vector2.Zero,
                CenterEntity = center,
                ExplicitTarget = explicitTarget,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                MaxTargets = 1
            };

            using var result = TargetQueryEngine.QueryEntities(query);

            AssertTrue(result.Items.Count == 1, $"Single explicit target 应返回 1 个目标 (实际: {result.Items.Count})");
            AssertTrue(result.Items[0] == explicitTarget, $"Single explicit target 应返回指定目标，实际: {(result.Items.FirstOrDefault() as Node)?.Name}");
            AssertTrue(result.Diagnostics.CandidateCount == 1, $"Single source candidate 应为 1，实际: {result.Diagnostics.CandidateCount}");
            AssertTrue(result.Diagnostics.GeometryHitCount == 1, $"Single geometry hit 应为 1，实际: {result.Diagnostics.GeometryHitCount}");
        }
        finally
        {
            CleanupEntities(center, explicitTarget);
        }
    }

    private void TestEntityQuery_ExplicitCandidatesShouldNotScanEntityManager()
    {
        _log.Info("测试 TargetQueryEngine explicit candidates source...");

        var center = new MockEntity("ExplicitCenter", Vector2.Zero, Team.Player, EntityType.Unit);
        var registeredEnemy = new MockEntity("ExplicitRegisteredEnemy", new Vector2(10, 0), Team.Enemy, EntityType.Unit);
        var explicitEnemy = new MockEntity("ExplicitOnlyEnemy", new Vector2(20, 0), Team.Enemy, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(registeredEnemy);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = Vector2.Zero,
                Range = 100f,
                CenterEntity = center,
                ExplicitCandidates = new[] { explicitEnemy },
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                MaxTargets = -1
            };

            using var result = TargetQueryEngine.QueryEntities(query);

            AssertTrue(result.Items.Count == 1, $"显式候选只应返回 1 个目标 (实际: {result.Items.Count})");
            AssertTrue(result.Items[0] == explicitEnemy, $"显式候选不应扫描 EntityManager，实际: {(result.Items.FirstOrDefault() as Node)?.Name}");
            AssertTrue(result.Diagnostics.CandidateCount == 1, $"显式候选 candidate count 应为 1，实际: {result.Diagnostics.CandidateCount}");
        }
        finally
        {
            CleanupEntities(center, registeredEnemy, explicitEnemy);
        }
    }

    private void TestEntityQuery_MissingSortDataShouldWarn()
    {
        _log.Info("测试 TargetQueryEngine missing sort data warning...");

        var center = new MockEntity("SortWarningCenter", Vector2.Zero, Team.Player, EntityType.Unit);
        var first = new MockEntity("SortWarningFirst", new Vector2(10, 0), Team.Enemy, EntityType.Unit);
        var second = new MockEntity("SortWarningSecond", new Vector2(20, 0), Team.Enemy, EntityType.Unit);

        EntityManager.Register(center);
        EntityManager.Register(first);
        EntityManager.Register(second);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = Vector2.Zero,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                Sorting = TargetSorting.HighestThreat,
                MaxTargets = -1
            };

            using var result = TargetQueryEngine.QueryEntities(query);

            AssertTrue(result.Items.Count == 2, $"缺 Threat 字段时仍应返回目标 (实际: {result.Items.Count})");
            AssertTrue(result.Diagnostics.Warnings.Any(warning => warning.Contains("Threat")), $"warnings 应说明 Threat fallback: {string.Join(" | ", result.Diagnostics.Warnings)}");
            AssertTrue(result.Diagnostics.SortApplied == TargetSorting.HighestThreat.ToString(), $"sort applied 应记录 HighestThreat，实际: {result.Diagnostics.SortApplied}");
        }
        finally
        {
            CleanupEntities(center, first, second);
        }
    }

    private void TestEntityQuery_TypeFilterShouldCountFilteredTargets()
    {
        _log.Info("测试 TargetQueryEngine type filter count...");

        var center = new MockEntity("TypeFilterCenter", Vector2.Zero, Team.Player, EntityType.Unit);
        var unit = new MockEntity("TypeFilterUnit", new Vector2(10, 0), Team.Enemy, EntityType.Unit);
        var projectile = new MockEntity("TypeFilterProjectile", new Vector2(20, 0), Team.Enemy, EntityType.Projectile);

        EntityManager.Register(center);
        EntityManager.Register(unit);
        EntityManager.Register(projectile);

        try
        {
            var query = new TargetSelectorQuery
            {
                Geometry = GeometryType.Circle,
                Origin = Vector2.Zero,
                Range = 100f,
                CenterEntity = center,
                TeamFilter = TeamFilter.Enemy,
                TypeFilter = EntityType.Unit,
                MaxTargets = -1
            };

            using var result = TargetQueryEngine.QueryEntities(query);

            AssertTrue(result.Items.Count == 1, $"TypeFilter 应只返回 Unit (实际: {result.Items.Count})");
            AssertTrue(result.Items[0] == unit, $"TypeFilter 应保留 Unit，实际: {(result.Items.FirstOrDefault() as Node)?.Name}");
            AssertTrue(result.Diagnostics.FilteredByTypeCount == 1, $"type filter count 应为 1，实际: {result.Diagnostics.FilteredByTypeCount}");
        }
        finally
        {
            CleanupEntities(center, unit, projectile);
        }
    }

    private void TestGeometry2D_Circle()
    {
        _log.Info("测试 Geometry2D (Circle 圆形)...");

        Vector2 center = new Vector2(0, 0);
        float radius = 100f;

        Vector2 p1 = new Vector2(50, 50); // Inside, dist ~70.7
        Vector2 p2 = new Vector2(100, 0); // On edge
        Vector2 p3 = new Vector2(101, 0); // Outside

        AssertTrue(Geometry2D.IsPointInCircle(p1, center, radius), "p1(50,50) 应在 Circle(100) 内");
        AssertTrue(Geometry2D.IsPointInCircle(p2, center, radius), "p2(100,0) 应在边缘");
        AssertTrue(!Geometry2D.IsPointInCircle(p3, center, radius), "p3(101,0) 应在外部");

        // Test random point generator bounds
        var randPoint = Geometry2D.GetRandomPointInCircle(center, radius, DeterministicRandom.Create(42));
        AssertTrue(Geometry2D.IsPointInCircle(randPoint, center, radius), $"随机生成的点 {randPoint} 必须在圆内");
    }

    private void TestGeometry2D_OtherShapes()
    {
        _log.Info("测试 Geometry2D (Box, Line, Cone)...");

        Vector2 origin = new Vector2(0, 0);
        Vector2 forward = Vector2.Right; // (1, 0)

        // --- Box Test ---
        float boxWidth = 20f; // Y-range: -10 to 10
        float boxLength = 50f; // X-range: 0 to 50
        AssertTrue(Geometry2D.IsPointInBox(new Vector2(25, 0), origin, forward, boxWidth, boxLength), "Box内");
        AssertTrue(Geometry2D.IsPointInBox(new Vector2(0, 10), origin, forward, boxWidth, boxLength), "Box边缘");
        AssertTrue(!Geometry2D.IsPointInBox(new Vector2(-1, 0), origin, forward, boxWidth, boxLength), "Box后方");
        AssertTrue(!Geometry2D.IsPointInBox(new Vector2(25, 11), origin, forward, boxWidth, boxLength),
            "Box外部(宽)");
        AssertTrue(!Geometry2D.IsPointInBox(new Vector2(51, 0), origin, forward, boxWidth, boxLength),
            "Box外部(长)");

        // --- Line (Capsule) Test ---
        float lineWidth = 20f; // R = 10
        float lineLength = 50f; // X 0->50
        AssertTrue(Geometry2D.IsPointInCapsule(new Vector2(25, 0), origin, forward, lineLength, lineWidth),
            "Line内");
        AssertTrue(Geometry2D.IsPointInCapsule(new Vector2(-5, 5), origin, forward, lineLength, lineWidth),
            "Line起点半圆内");
        AssertTrue(Geometry2D.IsPointInCapsule(new Vector2(55, 5), origin, forward, lineLength, lineWidth),
            "Line终点半圆内");
        AssertTrue(!Geometry2D.IsPointInCapsule(new Vector2(25, 11), origin, forward, lineLength, lineWidth),
            "Line外部");

        // --- Cone Test ---
        float coneRange = 50f;
        float coneAngle = 90f; // +/- 45 度
        AssertTrue(Geometry2D.IsPointInCone(new Vector2(25, 0), origin, forward, coneRange, coneAngle),
            "Cone内");
        AssertTrue(Geometry2D.IsPointInCone(new Vector2(25, 25), origin, forward, coneRange, coneAngle),
            "Cone边缘(45度)");
        AssertTrue(!Geometry2D.IsPointInCone(new Vector2(25, 26), origin, forward, coneRange, coneAngle),
            "Cone外部(角度)");
        AssertTrue(!Geometry2D.IsPointInCone(new Vector2(51, 0), origin, forward, coneRange, coneAngle),
            "Cone外部(距离)");
        AssertTrue(!Geometry2D.IsPointInCone(new Vector2(-10, 0), origin, forward, coneRange, coneAngle),
            "Cone后方");
    }

    private void AssertTrue(bool condition, string message)
    {
        if (condition)
        {
            _log.Info($"[通过] {message}");
        }
        else
        {
            _log.Error($"[失败] {message}");
        }
    }

    /// <summary>
    /// 统一清理测试实体。
    /// 遵循项目实体生命周期规范：
    /// 禁止直接 QueueFree，统一通过 EntityManager.Destroy 回收。
    /// </summary>
    private void CleanupEntities(params Node[] entities)
    {
        foreach (var entity in entities)
        {
            if (GodotObject.IsInstanceValid(entity))
            {
                EntityManager.Destroy(entity);
            }
        }
    }
}
