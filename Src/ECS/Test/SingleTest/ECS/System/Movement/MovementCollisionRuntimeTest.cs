using Godot;

namespace Slime.Test
{
    /// <summary>
    /// 移动碰撞语义运行时测试。
    /// <para>该测试用于锁定新碰撞协议的最小契约，确保重构期间类型与默认行为不回退。</para>
    /// </summary>
    internal partial class MovementCollisionRuntimeTest : Node
    {
        private static readonly Log _log = new(nameof(MovementCollisionRuntimeTest));

        private sealed partial class MockEntity : Node2D, IEntity
        {
            public Data Data { get; } = new Data();
            public EventBus Events { get; } = new EventBus();

            public MockEntity(string name, Team team, EntityType type)
            {
                Name = name;
                Data.Set(DataKey.Team, team);
                Data.Set(DataKey.EntityType, type);
                Data.Set(DataKey.Id, GetInstanceId().ToString());
                Data.Set(DataKey.DefaultMoveMode, MoveMode.None);
                Data.Set(DataKey.MoveMode, MoveMode.None);
                Data.Set(DataKey.IsDead, false);
            }
        }

        private int _passedCount;
        private int _failedCount;

        public override void _Ready()
        {
            _log.Info("开始 MovementCollision 运行时测试");

            try
            {
                TestCollisionParamsDefaults();
                TestStopRequestedDefaults();
                TestMovementCompletedEventCarriesReason();
                TestStopCoordinatorResolution();
                TestCollisionPolicyCountsAndFilters();
                TestCollisionPolicyTrackedTargetOnly();
            }
            catch (System.Exception ex)
            {
                Fail($"测试过程中发生异常: {ex}");
            }

            _log.Info($"MovementCollision 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
            GetTree().Quit(_failedCount == 0 ? 0 : 1);
        }

        private void TestCollisionParamsDefaults()
        {
            var collision = new MovementCollisionParams();

            AssertEqual("默认 TeamFilter", TeamFilter.All, collision.TeamFilter);
            AssertEqual("默认 EntityTypeFilter", EntityType.None, collision.EntityTypeFilter);
            AssertEqual("默认 TargetMatchMode", MovementCollisionTargetMatchMode.Any, collision.TargetMatchMode);
            AssertEqual("默认 StopAfterCollisionCount", -1, collision.StopAfterCollisionCount);
            AssertEqual("默认 DestroyOnStop", false, collision.DestroyOnStop);
            AssertEqual("默认 EmitCollisionEvent", true, collision.EmitCollisionEvent);
        }

        private void TestStopRequestedDefaults()
        {
            var evt = new GameEventType.Unit.MovementStopRequestedEventData();

            AssertEqual("默认停止原因", MovementStopReason.Requested, evt.Reason);
            AssertEqual("默认发完成事件", true, evt.EmitCompletedEvent);
            AssertEqual("默认下一模式", MoveMode.None, evt.NextMode);
            AssertEqual("默认不销毁实体", false, evt.DestroyEntity);
            AssertEqual("默认无碰撞目标", null, evt.CollisionTarget);
        }

        private void TestMovementCompletedEventCarriesReason()
        {
            var collisionTarget = new Node2D { Name = "CollisionTarget" };
            AddChild(collisionTarget);

            var evt = new GameEventType.Unit.MovementCompletedEventData(
                MoveMode.CircularArc,
                1.5f,
                123f,
                MovementStopReason.Collision,
                collisionTarget);

            AssertEqual("完成事件携带停止原因", MovementStopReason.Collision, evt.Reason);
            AssertEqual("完成事件携带碰撞目标", collisionTarget, evt.CollisionTarget);

            collisionTarget.QueueFree();
        }

        private void TestStopCoordinatorResolution()
        {
            var collisionStop = MovementStopCoordinator.Resolve(
                MoveMode.SineWave,
                MoveMode.PlayerInput,
                new MovementParams
                {
                    Mode = MoveMode.SineWave,
                    Collision = new MovementCollisionParams
                    {
                        StopAfterCollisionCount = 1,
                        DestroyOnStop = true
                    }
                },
                MovementStopReason.Collision);

            AssertEqual("碰撞停止应销毁实体", true, collisionStop.DestroyEntity);
            AssertEqual("碰撞销毁时不应回退默认模式", MoveMode.None, collisionStop.NextMode);
            AssertEqual("碰撞停止默认发完成事件", true, collisionStop.EmitCompletedEvent);

            var completedStop = MovementStopCoordinator.Resolve(
                MoveMode.Parabola,
                MoveMode.PlayerInput,
                new MovementParams
                {
                    Mode = MoveMode.Parabola,
                    DestroyOnComplete = true
                },
                MovementStopReason.Completed);

            AssertEqual("自然完成可按配置销毁实体", true, completedStop.DestroyEntity);

            var requestedStop = MovementStopCoordinator.Resolve(
                MoveMode.CircularArc,
                MoveMode.PlayerInput,
                new MovementParams
                {
                    Mode = MoveMode.CircularArc
                },
                MovementStopReason.Requested,
                emitCompletedEvent: false);

            AssertEqual("外部请求可关闭完成事件", false, requestedStop.EmitCompletedEvent);
            AssertEqual("未显式指定下一模式时回退默认模式", MoveMode.PlayerInput, requestedStop.NextMode);
            AssertEqual("外部请求默认不销毁实体", false, requestedStop.DestroyEntity);

            var explicitNextModeStop = MovementStopCoordinator.Resolve(
                MoveMode.CircularArc,
                MoveMode.PlayerInput,
                new MovementParams
                {
                    Mode = MoveMode.CircularArc
                },
                MovementStopReason.Requested,
                emitCompletedEvent: true,
                requestedNextMode: MoveMode.AIControlled);

            AssertEqual("外部请求可显式指定下一模式", MoveMode.AIControlled, explicitNextModeStop.NextMode);
        }

        private void TestCollisionPolicyCountsAndFilters()
        {
            var source = new MockEntity("Source", Team.Player, EntityType.Projectile);
            var enemyA = new MockEntity("EnemyA", Team.Enemy, EntityType.Unit);
            var enemyB = new MockEntity("EnemyB", Team.Enemy, EntityType.Unit);
            var friendly = new MockEntity("Friendly", Team.Player, EntityType.Unit);

            AddChild(source);
            AddChild(enemyA);
            AddChild(enemyB);
            AddChild(friendly);

            var policy = new MovementCollisionPolicy();
            var @params = new MovementParams
            {
                Mode = MoveMode.SineWave,
                Collision = new MovementCollisionParams
                {
                    TeamFilter = TeamFilter.Enemy, //只接受敌方
                    EntityTypeFilter = EntityType.Unit, //只接受单位
                    StopAfterCollisionCount = 2 //第2次停止
                }
            };
            policy.Reset(@params);

            bool firstAccepted = policy.TryAccept(source, MoveMode.SineWave, @params, enemyA, out var firstContext);
            AssertEqual("第1次敌方碰撞应接受", true, firstAccepted);
            AssertEqual("第1次碰撞计数", 1, firstContext.CollisionCount);
            AssertEqual("第1次碰撞不停", false, firstContext.WillStop);

            bool duplicateAccepted = policy.TryAccept(source, MoveMode.SineWave, @params, enemyA, out _);
            AssertEqual("同一目标重复碰撞不重复计数", false, duplicateAccepted);

            bool friendlyAccepted = policy.TryAccept(source, MoveMode.SineWave, @params, friendly, out _);
            AssertEqual("友军碰撞应被过滤", false, friendlyAccepted);

            bool secondAccepted = policy.TryAccept(source, MoveMode.SineWave, @params, enemyB, out var secondContext);
            AssertEqual("第2个敌方碰撞应接受", true, secondAccepted);
            AssertEqual("第2次碰撞计数", 2, secondContext.CollisionCount);
            AssertEqual("第2次碰撞应触发停止", true, secondContext.WillStop);

            source.QueueFree();
            enemyA.QueueFree();
            enemyB.QueueFree();
            friendly.QueueFree();
        }

        private void TestCollisionPolicyTrackedTargetOnly()
        {
            var source = new MockEntity("ArcSource", Team.Player, EntityType.Projectile);
            var trackedEnemy = new MockEntity("TrackedEnemy", Team.Enemy, EntityType.Unit);
            var otherEnemy = new MockEntity("OtherEnemy", Team.Enemy, EntityType.Unit);
            var trackedHurtbox = new Area2D();
            var otherHurtbox = new Area2D();

            trackedEnemy.AddChild(trackedHurtbox);
            otherEnemy.AddChild(otherHurtbox);
            AddChild(source);
            AddChild(trackedEnemy);
            AddChild(otherEnemy);

            var policy = new MovementCollisionPolicy();
            var @params = new MovementParams
            {
                Mode = MoveMode.CircularArc,
                TargetNode = trackedEnemy, //锁定目标
                Collision = new MovementCollisionParams
                {
                    TeamFilter = TeamFilter.Enemy, //只接受敌方
                    TargetMatchMode = MovementCollisionTargetMatchMode.TrackedTargetOnly, //仅锁定目标
                    StopAfterCollisionCount = 1 //首个有效碰撞停止
                }
            };
            policy.Reset(@params);

            bool otherAccepted = policy.TryAccept(source, MoveMode.CircularArc, @params, otherHurtbox, out _);
            AssertEqual("非锁定目标应被拒绝", false, otherAccepted);

            bool trackedAccepted = policy.TryAccept(source, MoveMode.CircularArc, @params, trackedHurtbox, out var trackedContext);
            AssertEqual("锁定目标应通过匹配", true, trackedAccepted);
            AssertEqual("锁定目标首碰即停", true, trackedContext.WillStop);
            AssertEqual("锁定目标实体解析", trackedEnemy, trackedContext.TargetEntity);

            source.QueueFree();
            trackedEnemy.QueueFree();
            otherEnemy.QueueFree();
        }

        private void AssertEqual<T>(string name, T expected, T actual)
        {
            if (Equals(expected, actual))
            {
                Pass($"{name} | expected={expected} actual={actual}");
                return;
            }

            Fail($"{name} | expected={expected} actual={actual}");
        }

        private void Pass(string message)
        {
            _passedCount++;
            _log.Success($"[PASS] {message}");
        }

        private void Fail(string message)
        {
            _failedCount++;
            _log.Error($"[FAIL] {message}");
        }
    }
}
