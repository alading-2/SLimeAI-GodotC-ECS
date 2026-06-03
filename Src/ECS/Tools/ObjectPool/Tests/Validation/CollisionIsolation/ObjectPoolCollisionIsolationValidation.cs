using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Slime.Test;

/// <summary>
/// ObjectPool 碰撞隔离验证场景。
/// <para>自动验证 ParkedInTree、ActivationFrameEmbargo、parking grid 和 fallback control，并输出 scene-gate artifact。</para>
/// </summary>
public partial class ObjectPoolCollisionIsolationValidation : Node
{
    private const string ArtifactPath = ".ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json";
    private const string ExpectedInputs = "headless ObjectPool collision roots: Area2D and CharacterBody2D released, reactivated and observed through runtime state";
    private const string ExpectedObservations = "released collision roots stay parked in tree, collision logic guard rejects pooled and first-frame nodes, raw callbacks are separated from business events, parking positions are distributed";
    private const string PassCriteria = "all ObjectPool collision isolation checks pass and artifact oracle fields are complete";
    private const string FailCriteria = "any default release detaches a collision root, keeps business collision active in pool, allows first activation frame, accepts pooled raw callbacks as business events or misses artifact checks";

    private readonly List<ObjectPoolValidationCheck> _checks = new();
    private readonly List<string> _collisionEvents = new();
    private readonly List<string> _businessCollisionEvents = new();
    private readonly List<Action> _cleanupActions = new();

    public override void _Ready()
    {
        var passed = false;
        try
        {
            AddChild(new Node { Name = "Pools" });
            ParentManager.Init(this);
            ObjectPoolRuntimeStateStore.Clear();
            ObjectPoolObservability.Clear();

            RunAllChecks();
            passed = _checks.All(static check => check.Passed);
        }
        catch (Exception ex)
        {
            _checks.Add(new ObjectPoolValidationCheck
            {
                Name = "Fatal",
                Passed = false,
                Message = ex.ToString()
            });
        }

        try
        {
            WriteArtifact(passed);
        }
        catch (Exception ex)
        {
            passed = false;
            GD.PushError($"ObjectPoolCollisionIsolationValidation artifact write failed: {ex}");
        }
        finally
        {
            CleanupCreatedPools();
            ObjectPoolRuntimeStateStore.Clear();
            ObjectPoolObservability.Clear();
        }

        if (passed)
        {
            GD.Print("PASS ObjectPoolCollisionIsolationValidation");
        }
        else
        {
            GD.PushError("FAIL ObjectPoolCollisionIsolationValidation");
        }

        GetTree().Quit(passed ? 0 : 1);
    }

    private void RunAllChecks()
    {
        RunCheck("collision_area_release_parked_in_tree", CheckAreaReleaseParkedInTree);
        RunCheck("collision_character_release_parked_in_tree", CheckCharacterReleaseParkedInTree);
        RunCheck("collision_activate_first_frame_embargo", CheckActivateFirstFrameEmbargo);
        RunCheck("collision_activate_after_ready_frame", CheckActivateAfterReadyFrame);
        RunCheck("collision_immediate_reuse_same_frame", CheckImmediateReuseSameFrame);
        RunCheck("collision_guard_event_oracle", CheckCollisionGuardEventOracle);
        RunCheck("collision_parking_grid_pressure", CheckParkingGridPressure);
        RunCheck("collision_detach_fallback_control", CheckDetachFallbackControl);
        RunCheck("collision_artifact_oracle_complete", CheckArtifactOracleComplete);
    }

    private void RunCheck(string name, Action<ObjectPoolValidationCheck> action)
    {
        var check = new ObjectPoolValidationCheck { Name = name };
        try
        {
            action(check);
            check.Passed = true;
            check.Message = "PASS";
        }
        catch (Exception ex)
        {
            check.Passed = false;
            check.Message = ex.Message;
        }

        _checks.Add(check);
    }

    private void CheckAreaReleaseParkedInTree(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "ValidationArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/CollisionAreaRelease",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/CollisionAreaRelease"
            });

        var area = pool.Get();
        pool.Release(area);

        AssertTrue("Area should stay inside tree after release", area.IsInsideTree());
        AssertEqual("Area should be hidden after release", false, area.Visible);
        AssertEqual("Area should stop processing after release", Node.ProcessModeEnum.Disabled, area.ProcessMode);
        AssertTrue("Area should write runtime state", ObjectPoolRuntimeStateStore.TryGet(area, out var state));
        AssertEqual("Area state IsInPool", true, state.IsInPool);
        AssertEqual("Area state CollisionLogicActive", false, state.CollisionLogicActive);
        AssertEqual("Area default path should not use fallback detach", false, state.DetachFallbackEnabled);
        AssertEqual("Area guard rejects pooled node", false, CollisionLogicGuard.CanProcessCollision(area));

        Add(check, "insideTree", area.IsInsideTree());
        Add(check, "parkingPosition", FormatVector(state.ParkingPosition));
        Add(check, "collisionLogicActive", state.CollisionLogicActive);
    }

    private void CheckCharacterReleaseParkedInTree(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new CharacterBody2D { Name = "ValidationBody", Velocity = new Vector2(10f, 10f) },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/CollisionCharacterRelease",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/CollisionCharacterRelease"
            });

        var body = pool.Get();
        body.Velocity = new Vector2(100f, 50f);
        pool.Release(body);

        AssertTrue("CharacterBody should stay inside tree after release", body.IsInsideTree());
        AssertEqual("CharacterBody velocity should be cleared on park", Vector2.Zero, body.Velocity);
        AssertTrue("CharacterBody should write runtime state", ObjectPoolRuntimeStateStore.TryGet(body, out var state));
        AssertEqual("CharacterBody state IsInPool", true, state.IsInPool);
        AssertEqual("CharacterBody guard rejects pooled node", false, CollisionLogicGuard.CanProcessCollision(body));

        Add(check, "insideTree", body.IsInsideTree());
        Add(check, "velocity", FormatVector(body.Velocity));
        Add(check, "parkingPosition", FormatVector(state.ParkingPosition));
    }

    private void CheckActivateFirstFrameEmbargo(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "EmbargoArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/CollisionEmbargo",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/CollisionEmbargo"
            });

        var area = pool.Get(activateNode: false);
        pool.Activate(area);

        AssertTrue("Activate should write runtime state", ObjectPoolRuntimeStateStore.TryGet(area, out var state));
        var allowed = CollisionLogicGuard.CanProcessCollision(area, state.LastAcquirePhysicsFrame);
        AssertEqual("First acquire physics frame should be embargoed", false, allowed);
        if (allowed)
        {
            _businessCollisionEvents.Add("first-frame-allowed");
        }

        Add(check, "lastAcquirePhysicsFrame", state.LastAcquirePhysicsFrame);
        Add(check, "collisionReadyPhysicsFrame", state.CollisionReadyPhysicsFrame);
    }

    private void CheckActivateAfterReadyFrame(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "ReadyArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/CollisionReadyFrame",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/CollisionReadyFrame"
            });

        var area = pool.Get(activateNode: false);
        pool.Activate(area);

        AssertTrue("Activate should write runtime state", ObjectPoolRuntimeStateStore.TryGet(area, out var state));
        var allowed = CollisionLogicGuard.CanProcessCollision(area, state.CollisionReadyPhysicsFrame);
        AssertEqual("Ready frame should be accepted", true, allowed);
        if (allowed)
        {
            _businessCollisionEvents.Add("ready-frame-accepted");
        }

        Add(check, "readyFrameAllowed", allowed);
    }

    private void CheckImmediateReuseSameFrame(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "ReuseArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/ImmediateReuse",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/ImmediateReuse"
            });

        var area = pool.Get();
        pool.Release(area);
        var reused = pool.Get(activateNode: false);
        pool.Activate(reused);

        AssertTrue("Immediate reuse should keep same node", ReferenceEquals(area, reused));
        AssertTrue("Immediate reuse should write runtime state", ObjectPoolRuntimeStateStore.TryGet(reused, out var state));
        AssertEqual("Immediate reuse acquire frame should be embargoed", false, CollisionLogicGuard.CanProcessCollision(reused, state.LastAcquirePhysicsFrame));

        _collisionEvents.Add("release-get-activate-same-node");
        Add(check, "lastReleasePhysicsFrame", state.LastReleasePhysicsFrame);
        Add(check, "lastAcquirePhysicsFrame", state.LastAcquirePhysicsFrame);
        Add(check, "collisionReadyPhysicsFrame", state.CollisionReadyPhysicsFrame);
    }

    private void CheckCollisionGuardEventOracle(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "GuardOracleArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/GuardEventOracle",
                InitialSize = 0,
                MaxSize = 4,
                ParentPath = "Pools/GuardEventOracle"
            });

        var area = pool.Get();
        var target = new Area2D { Name = "GuardOracleTarget" };
        AddChild(target);

        pool.Release(area);
        RecordCollisionCallback("pooled-release", area, target, ObjectPoolRuntimeStateStore.CurrentPhysicsFrame);

        var reused = pool.Get(activateNode: false);
        pool.Activate(reused);
        AssertTrue("Guard oracle should write runtime state", ObjectPoolRuntimeStateStore.TryGet(reused, out var state));

        RecordCollisionCallback("activate-frame", reused, target, state.LastAcquirePhysicsFrame);
        RecordCollisionCallback("ready-frame", reused, target, state.CollisionReadyPhysicsFrame);

        AssertEqual("Pooled callback must not become business event", false, HasAcceptedBusinessEvent("pooled-release"));
        AssertEqual("Activation frame callback must not become business event", false, HasAcceptedBusinessEvent("activate-frame"));
        AssertEqual("Ready frame callback should become business event", true, HasAcceptedBusinessEvent("ready-frame"));

        Add(check, "rawCollisionEvents", _collisionEvents.Count);
        Add(check, "businessCollisionEvents", _businessCollisionEvents.Count);
        Add(check, "pooledAccepted", HasAcceptedBusinessEvent("pooled-release"));
        Add(check, "activateFrameAccepted", HasAcceptedBusinessEvent("activate-frame"));
        Add(check, "readyFrameAccepted", HasAcceptedBusinessEvent("ready-frame"));

        target.QueueFree();
    }

    private void CheckParkingGridPressure(ObjectPoolValidationCheck check)
    {
        var pool = CreatePool(
            () => new Area2D { Name = "ParkingArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/ParkingPressure",
                InitialSize = 0,
                MaxSize = 64,
                ParentPath = "Pools/ParkingPressure"
            });

        var released = new List<Area2D>();
        for (var i = 0; i < 32; i++)
        {
            var area = pool.Get();
            released.Add(area);
        }

        foreach (var area in released)
        {
            pool.Release(area);
        }

        var positions = ObjectPoolRuntimeStateStore.GetAllNodeStateSnapshots()
            .Where(snapshot => snapshot.PoolName == pool.PoolName)
            .Select(snapshot => FormatVector(snapshot.ParkingPosition))
            .ToHashSet(StringComparer.Ordinal);

        AssertEqual("Parking grid should distribute every released node", released.Count, positions.Count);
        Add(check, "releasedCount", released.Count);
        Add(check, "uniqueParkingPositions", positions.Count);
    }

    private void CheckDetachFallbackControl(ObjectPoolValidationCheck check)
    {
        var node = new Area2D { Name = "FallbackArea" };
        AddChild(node);

        var parkingPosition = new Vector2(123456f, 654321f);
        DetachFallbackStrategy.Detach(node, "Test/ObjectPool/FallbackControl", parkingPosition);

        AssertEqual("Fallback control should detach explicitly", null, node.GetParent());
        AssertTrue("Fallback control should write runtime state", ObjectPoolRuntimeStateStore.TryGet(node, out var state));
        AssertEqual("Fallback state should be marked", true, state.DetachFallbackEnabled);

        Add(check, "detachFallbackEnabled", state.DetachFallbackEnabled);
        Add(check, "insideTree", node.IsInsideTree());
        node.Free();
    }

    private void CheckArtifactOracleComplete(ObjectPoolValidationCheck check)
    {
        string[] expectedChecks =
        [
            "collision_area_release_parked_in_tree",
            "collision_character_release_parked_in_tree",
            "collision_activate_first_frame_embargo",
            "collision_activate_after_ready_frame",
            "collision_immediate_reuse_same_frame",
            "collision_guard_event_oracle",
            "collision_parking_grid_pressure",
            "collision_detach_fallback_control"
        ];

        foreach (var expectedCheck in expectedChecks)
        {
            AssertTrue($"Expected check should already be registered: {expectedCheck}",
                _checks.Any(existing => string.Equals(existing.Name, expectedCheck, StringComparison.Ordinal)));
        }

        AssertTrue("expectedInputs should be non-empty", !string.IsNullOrWhiteSpace(ExpectedInputs));
        AssertTrue("expectedObservations should be non-empty", !string.IsNullOrWhiteSpace(ExpectedObservations));
        AssertTrue("passCriteria should be non-empty", !string.IsNullOrWhiteSpace(PassCriteria));
        AssertTrue("failCriteria should be non-empty", !string.IsNullOrWhiteSpace(FailCriteria));
        AssertTrue("artifactPath should be non-empty", !string.IsNullOrWhiteSpace(ArtifactPath));
        Add(check, "expectedCheckCount", expectedChecks.Length);
    }

    private void WriteArtifact(bool passed)
    {
        var artifact = new ObjectPoolValidationArtifact
        {
            Status = passed ? "PASS" : "FAIL",
            ExpectedInputs = ExpectedInputs,
            ExpectedObservations = ExpectedObservations,
            PassCriteria = PassCriteria,
            FailCriteria = FailCriteria,
            ArtifactPath = ArtifactPath,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Checks = new List<ObjectPoolValidationCheck>(_checks),
            PoolStats = ObjectPoolManager.GetAllStats()
                .Select(static kv => new ObjectPoolValidationPoolStats
                {
                    PoolName = kv.Key,
                    Count = kv.Value.Count,
                    ActiveCount = kv.Value.ActiveCount,
                    TotalCreated = kv.Value.TotalCreated,
                    TotalAcquired = kv.Value.TotalAcquired,
                    TotalReleased = kv.Value.TotalReleased,
                    TotalDiscarded = kv.Value.TotalDiscarded
                })
                .ToList(),
            NodeStates = ObjectPoolRuntimeStateStore.GetAllNodeStateSnapshots(),
            CollisionEvents = new List<string>(_collisionEvents),
            BusinessCollisionEvents = new List<string>(_businessCollisionEvents),
            FailureReasons = _checks.Where(static check => !check.Passed).Select(static check => $"{check.Name}: {check.Message}").ToList()
        };

        var json = JsonSerializer.Serialize(artifact, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

        var absolutePath = Path.Combine(ProjectSettings.GlobalizePath("res://"), ArtifactPath);
        WriteJsonFile(absolutePath, json);

        var runArtifactDirectory = System.Environment.GetEnvironmentVariable("GODOT_SCENE_TEST_ARTIFACT_DIR");
        if (!string.IsNullOrWhiteSpace(runArtifactDirectory))
        {
            WriteJsonFile(Path.Combine(runArtifactDirectory, Path.GetFileName(ArtifactPath)), json);
        }
    }

    private static void WriteJsonFile(string path, string json)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, json);
    }

    private static void Add(ObjectPoolValidationCheck check, string key, object? value)
    {
        check.Observations[key] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private void RecordCollisionCallback(string phase, Node self, Node target, long physicsFrame)
    {
        var allowed = CollisionLogicGuard.CanProcessCollision(self, target, physicsFrame);
        _collisionEvents.Add($"{phase}:raw:frame={physicsFrame}:allowed={allowed}");
        if (allowed)
        {
            _businessCollisionEvents.Add($"{phase}:accepted:frame={physicsFrame}");
        }
    }

    private bool HasAcceptedBusinessEvent(string phase)
    {
        return _businessCollisionEvents.Any(entry => entry.StartsWith($"{phase}:accepted:", StringComparison.Ordinal));
    }

    private ObjectPool<T> CreatePool<T>(Func<T> createFunc, ObjectPoolConfig config) where T : class
    {
        var pool = new ObjectPool<T>(createFunc, config);
        _cleanupActions.Add(pool.Destroy);
        return pool;
    }

    private void CleanupCreatedPools()
    {
        for (var i = _cleanupActions.Count - 1; i >= 0; i--)
        {
            try
            {
                _cleanupActions[i]();
            }
            catch (Exception ex)
            {
                GD.PushWarning($"ObjectPoolCollisionIsolationValidation cleanup failed: {ex.Message}");
            }
        }

        _cleanupActions.Clear();
    }

    private static string FormatVector(Vector2 value)
    {
        return $"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)}";
    }

    private static void AssertTrue(string label, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }

    private static void AssertEqual<T>(string label, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{label}: expected={expected}, actual={actual}");
        }
    }

    private sealed class ObjectPoolValidationArtifact
    {
        public string Status { get; set; } = string.Empty;
        public string ExpectedInputs { get; set; } = string.Empty;
        public string ExpectedObservations { get; set; } = string.Empty;
        public string PassCriteria { get; set; } = string.Empty;
        public string FailCriteria { get; set; } = string.Empty;
        public string ArtifactPath { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = string.Empty;
        public List<ObjectPoolValidationCheck> Checks { get; set; } = new();
        public List<ObjectPoolValidationPoolStats> PoolStats { get; set; } = new();
        public List<PoolNodeStateSnapshot> NodeStates { get; set; } = new();
        public List<string> CollisionEvents { get; set; } = new();
        public List<string> BusinessCollisionEvents { get; set; } = new();
        public List<string> FailureReasons { get; set; } = new();
    }

    private sealed class ObjectPoolValidationPoolStats
    {
        public string PoolName { get; set; } = string.Empty;
        public int Count { get; set; }
        public int ActiveCount { get; set; }
        public int TotalCreated { get; set; }
        public int TotalAcquired { get; set; }
        public int TotalReleased { get; set; }
        public int TotalDiscarded { get; set; }
    }

    private sealed class ObjectPoolValidationCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Observations { get; } = new(StringComparer.Ordinal);
    }
}
