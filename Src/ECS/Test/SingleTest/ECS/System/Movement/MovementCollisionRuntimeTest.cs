using Godot;
using System.Collections.Generic;

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
                TestOrientationParamsDefaults();
                TestOrientationComponentDefaults();
                TestCollisionParamsOnCollisionCallback();
                TestStopRequestedDefaults();
                TestOrientationStartedDefaults();
                TestOrientationStoppedDefaults();
                TestMovementCompletedEventCarriesReason();
                TestStopCoordinatorResolution();
                TestBindParentRelationshipsCreatesOwnershipChain();
                TestBindParentRelationshipsRejectsSecondParent();
                TestDestroyRecursivelyDestroysOwnedChildren();
                TestDestroyDetachKeepsOwnedChildrenAlive();
                TestMigrateReplacesSourceAndInheritsDirectParent();
                TestMigrateProfileFiltersDataAndDoesNotCopyEvents();
                TestCollisionPolicyCountsAndFilters();
                TestCollisionPolicyUsesOwnerUnitForTeamFilter();
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

        private void TestOrientationParamsDefaults()
        {
            var orientation = new OrientationParams();

            AssertEqual("默认朝向模式", OrientationMode.FollowMovement, orientation.Mode);
            AssertEqual("默认角速度", 0f, orientation.AngularSpeed);
            AssertEqual("默认角加速度", 0f, orientation.AngularAcceleration);
            AssertEqual("默认总角度", -1f, orientation.TotalAngle);
            AssertEqual("默认初始角", 0f, orientation.InitialAngle);
            AssertEqual("默认旋转方向", true, orientation.IsClockwise);
        }

        private void TestOrientationComponentDefaults()
        {
            var component = new EntityOrientationComponent();

            AssertEqual("朝向组件默认输出目标应为 RootRotation", OrientationSink.RootRotation, component.Sink);
        }

        private void TestCollisionParamsOnCollisionCallback()
        {
            int callbackCount = 0;
            MovementCollisionContext receivedContext = default;
            var source = new MockEntity("CallbackSource", Team.Player, EntityType.Projectile);
            var enemy = new MockEntity("CallbackEnemy", Team.Enemy, EntityType.Unit);

            AddChild(source);
            AddChild(enemy);

            var policy = new MovementCollisionPolicy();
            var @params = new MovementParams
            {
                Mode = MoveMode.SineWave,
                CollisionParams = new MovementCollisionParams
                {
                    TeamFilter = TeamFilter.Enemy, //只接受敌方
                    EntityTypeFilter = EntityType.Unit, //只接受单位
                    EmitCollisionEvent = false, //关闭事件，锁定本地回调路径
                    OnCollision = ctx =>
                    {
                        callbackCount++;
                        receivedContext = ctx;
                    }
                }
            };
            policy.Reset(@params);

            bool accepted = policy.TryAccept(source, MoveMode.SineWave, @params, enemy, out var context);
            if (accepted)
            {
                @params.CollisionParams?.OnCollision?.Invoke(context);
            }

            AssertEqual("本地碰撞回调应执行 1 次", 1, callbackCount);
            AssertEqual("本地碰撞回调应收到目标实体", enemy, receivedContext.TargetEntity);
            AssertEqual("本地碰撞回调应收到原始参数", @params.Mode, receivedContext.Params.Mode);

            source.QueueFree();
            enemy.QueueFree();
        }

        private void TestStopRequestedDefaults()
        {
            var evt = new GameEventType.Unit.MovementStopRequested();

            AssertEqual("默认停止原因", MovementStopReason.Requested, evt.Reason);
            AssertEqual("默认发完成事件", true, evt.EmitCompletedEvent);
            AssertEqual("默认下一模式", MoveMode.None, evt.NextMode);
            AssertEqual("默认不销毁实体", false, evt.DestroyEntity);
            AssertEqual("默认无碰撞目标", null, evt.CollisionTarget);
        }

        private void TestOrientationStartedDefaults()
        {
            var evt = new GameEventType.Unit.OrientationStarted();

            AssertEqual("默认朝向来源", OrientationSource.Standalone, evt.Source);
            AssertEqual("默认朝向参数模式", OrientationMode.FollowMovement, evt.Params.Mode);
            AssertEqual("默认独立朝向不自动停止", false, evt.StopWithMovement);
        }

        private void TestOrientationStoppedDefaults()
        {
            var evt = new GameEventType.Unit.OrientationStopped();

            AssertEqual("默认停止来源", OrientationSource.Standalone, evt.Source);
            AssertEqual("默认停止原因", MovementStopReason.Requested, evt.Reason);
        }

        private void TestMovementCompletedEventCarriesReason()
        {
            var collisionTarget = new Node2D { Name = "CollisionTarget" };
            AddChild(collisionTarget);

            var evt = new GameEventType.Unit.MovementCompleted(
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
                    CollisionParams = new MovementCollisionParams
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
                CollisionParams = new MovementCollisionParams
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

        /// <summary>
        /// 验证统一绑定入口会同时建立业务关系和 PARENT 关系，并且可通过统一追溯入口回到归属单位。
        /// </summary>
        private void TestBindParentRelationshipsCreatesOwnershipChain()
        {
            var owner = new MockEntity("Owner", Team.Player, EntityType.Unit);
            var projectile = new MockEntity("Projectile", Team.Neutral, EntityType.Projectile);

            AddChild(owner);
            AddChild(projectile);

            EntityManager.Register(owner);
            EntityManager.Register(projectile);

            try
            {
                bool bound = EntityManager.BindParentRelationships(
                    projectile, // 子实体：投射物
                    owner, // 父实体：归属单位
                    autoAddParentRelation: true, // 自动补 PARENT，供统一溯源
                    relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 业务关系：拥有者 -> 投射物
                );

                bool hasBusinessRelation = EntityRelationshipManager.HasRelationship(
                    owner.Data.Get<string>(DataKey.Id), // 父实体 Id
                    projectile.Data.Get<string>(DataKey.Id), // 子实体 Id
                    EntityRelationshipType.ENTITY_TO_PROJECTILE // 投射物业务关系
                );
                bool hasParentRelation = EntityRelationshipManager.HasRelationship(
                    owner.Data.Get<string>(DataKey.Id), // 父实体 Id
                    projectile.Data.Get<string>(DataKey.Id), // 子实体 Id
                    EntityRelationshipType.PARENT // 统一父子关系
                );
                var ownerEntity = EntityRelationshipTraversal.FindAncestorOfType<IEntity>(projectile); // 统一沿 PARENT 追溯归属实体

                AssertEqual("统一绑定入口应返回成功", true, bound);
                AssertEqual("统一绑定入口应建立业务关系", true, hasBusinessRelation);
                AssertEqual("统一绑定入口应自动建立 PARENT 关系", true, hasParentRelation);
                AssertEqual("统一追溯入口应能回溯到归属实体", owner, ownerEntity);
            }
            finally
            {
                CleanupEntities(owner, projectile);
            }
        }

        /// <summary>
        /// 验证 PARENT 链是单父契约，同一个子实体不能再绑定第二个直接父级。
        /// </summary>
        private void TestBindParentRelationshipsRejectsSecondParent()
        {
            var ownerA = new MockEntity("OwnerA", Team.Player, EntityType.Unit);
            var ownerB = new MockEntity("OwnerB", Team.Player, EntityType.Unit);
            var projectile = new MockEntity("Projectile", Team.Neutral, EntityType.Projectile);

            AddChild(ownerA);
            AddChild(ownerB);
            AddChild(projectile);

            EntityManager.Register(ownerA);
            EntityManager.Register(ownerB);
            EntityManager.Register(projectile);

            try
            {
                bool firstBound = EntityManager.BindParentRelationships(
                    projectile, // 子实体：投射物
                    ownerA, // 第一个父实体
                    autoAddParentRelation: true, // 自动补 PARENT
                    relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 业务关系
                );
                bool secondBound = EntityManager.BindParentRelationships(
                    projectile, // 同一个子实体
                    ownerB, // 第二个父实体，理论上应被拒绝
                    autoAddParentRelation: true, // 仍尝试补 PARENT
                    relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 同一业务关系也应拒绝二次归属
                );
                var directParent = EntityRelationshipTraversal.GetDirectParent(projectile); // 统一获取直接父级

                AssertEqual("首次绑定应成功", true, firstBound);
                AssertEqual("第二次绑定直接父级应被拒绝", false, secondBound);
                AssertEqual("子实体直接父级应保持第一次绑定结果", ownerA, directParent);
            }
            finally
            {
                CleanupEntities(ownerA, ownerB, projectile);
            }
        }

        /// <summary>
        /// 验证归属链默认采用级联销毁策略时，父实体销毁会递归销毁直接子实体。
        /// </summary>
        private void TestDestroyRecursivelyDestroysOwnedChildren()
        {
            var owner = new MockEntity("CascadeOwner", Team.Player, EntityType.Unit);
            var projectile = new MockEntity("CascadeProjectile", Team.Neutral, EntityType.Projectile);

            AddChild(owner);
            AddChild(projectile);

            EntityManager.Register(owner);
            EntityManager.Register(projectile);

            string ownerId = owner.Data.Get<string>(DataKey.Id);
            string projectileId = projectile.Data.Get<string>(DataKey.Id);

            bool bound = EntityManager.BindParentRelationships(
                projectile, // 子实体：投射物
                owner, // 父实体：拥有者
                autoAddParentRelation: true, // 自动补 PARENT
                parentDestroyPolicy: ParentDestroyPolicy.DestroyRecursively, // 父死子死
                relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 业务关系
            );

            AssertEqual("级联销毁测试前置绑定应成功", true, bound);

            EntityManager.Destroy(owner);

            AssertEqual("级联销毁后父实体应注销", false, NodeLifecycleManager.IsRegistered(ownerId));
            AssertEqual("级联销毁后子实体应一并注销", false, NodeLifecycleManager.IsRegistered(projectileId));
        }

        /// <summary>
        /// 验证归属链采用 Detach 策略时，父实体销毁仅断开关系，不销毁子实体。
        /// </summary>
        private void TestDestroyDetachKeepsOwnedChildrenAlive()
        {
            var owner = new MockEntity("DetachOwner", Team.Player, EntityType.Unit);
            var projectile = new MockEntity("DetachProjectile", Team.Neutral, EntityType.Projectile);

            AddChild(owner);
            AddChild(projectile);

            EntityManager.Register(owner);
            EntityManager.Register(projectile);

            string ownerId = owner.Data.Get<string>(DataKey.Id);
            string projectileId = projectile.Data.Get<string>(DataKey.Id);

            bool bound = EntityManager.BindParentRelationships(
                projectile, // 子实体：投射物
                owner, // 父实体：拥有者
                autoAddParentRelation: true, // 自动补 PARENT
                parentDestroyPolicy: ParentDestroyPolicy.Detach, // 父死仅断开关系
                relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 业务关系
            );

            AssertEqual("Detach 测试前置绑定应成功", true, bound);

            EntityManager.Destroy(owner);

            var directParent = EntityRelationshipTraversal.GetDirectParent(projectile); // Detach 后不应再有直接父级
            bool projectileStillRegistered = NodeLifecycleManager.IsRegistered(projectileId);
            bool ownerStillRegistered = NodeLifecycleManager.IsRegistered(ownerId);

            AssertEqual("Detach 后父实体应注销", false, ownerStillRegistered);
            AssertEqual("Detach 后子实体应继续存活", true, projectileStillRegistered);
            AssertEqual("Detach 后子实体不应再保留直接父级", null, directParent);

            CleanupEntities(projectile);
        }

        /// <summary>
        /// 验证迁移会生成新的目标实体、继承直接父级归属，并销毁旧实体。
        /// </summary>
        private void TestMigrateReplacesSourceAndInheritsDirectParent()
        {
            var owner = new MockEntity("MigrationOwner", Team.Player, EntityType.Unit);
            var source = new MockEntity("MigrationSource", Team.Neutral, EntityType.Projectile);

            AddChild(owner);
            AddChild(source);

            EntityManager.Register(owner);
            EntityManager.Register(source);

            source.Data.Set(DataKey.Name, "MigratedProjectile");
            source.Data.Set(DataKey.Description, "payload");
            source.Data.Set("CustomCount", 7);

            EntityManager.BindParentRelationships(
                source, // 子实体：待迁移源实体
                owner, // 父实体：归属者
                autoAddParentRelation: true, // 自动补 PARENT
                parentDestroyPolicy: ParentDestroyPolicy.Detach, // 验证父销毁策略会被继承
                relationTypes: EntityRelationshipType.ENTITY_TO_PROJECTILE // 业务关系
            );

            string sourceId = source.Data.Get<string>(DataKey.Id);

            VisualPreviewEntity? target = null;
            try
            {
                target = EntityManager.Migrate<VisualPreviewEntity>(
                    source, // 源实体
                    new EntityMigrationConfig
                    {
                        TargetSpawn = new EntitySpawnConfig
                        {
                            Config = new Resource(), // 目标实体测试配置
                            UsingObjectPool = false // 直接用场景实例化
                        }
                    }
                );

                AssertEqual("迁移应返回目标实体", false, target == null);
                AssertEqual("迁移后源实体应被注销", false, NodeLifecycleManager.IsRegistered(sourceId));
                AssertEqual("迁移后目标实体应持有新的 Id", false, target!.Data.Get<string>(DataKey.Id) == sourceId);
                AssertEqual("迁移后应复制基础字符串数据", "MigratedProjectile", target.Data.Get<string>(DataKey.Name));
                AssertEqual("迁移后应复制自定义基础数据", 7, target.Data.Get<int>("CustomCount"));
                AssertEqual("迁移后应记录直接来源实体 Id", sourceId, target.Data.Get<string>(DataKey.SourceEntityId));
                AssertEqual("迁移后应沿用第一来源实体 Id 作为 Origin", sourceId, target.Data.Get<string>(DataKey.OriginEntityId));
                AssertEqual("迁移后应继承直接父级", owner, EntityRelationshipTraversal.GetDirectParent(target));

                ParentDestroyPolicy destroyPolicy = EntityRelationshipLifecycle.ReadParentDestroyPolicy(
                    owner.Data.Get<string>(DataKey.Id), // 父实体 Id
                    target.Data.Get<string>(DataKey.Id) // 目标实体 Id
                );
                AssertEqual("迁移后应继承直接父级上的销毁策略", ParentDestroyPolicy.Detach, destroyPolicy);
            }
            finally
            {
                CleanupEntities(owner);
                if (target != null)
                {
                    CleanupEntities(target);
                }
            }
        }

        /// <summary>
        /// 验证迁移 Profile 会过滤 Data，且局部 EventBus 的订阅不会被复制到新实体。
        /// </summary>
        private void TestMigrateProfileFiltersDataAndDoesNotCopyEvents()
        {
            var source = new MockEntity("ProfileSource", Team.Player, EntityType.Projectile);
            AddChild(source);
            EntityManager.Register(source);

            source.Data.Set(DataKey.Name, "ShouldBeFiltered");
            source.Data.Set(DataKey.Description, "ShouldStay");
            source.Data.Set("UnsafeNodeRef", new Node2D { Name = "UnsafeRef" });

            int callbackCount = 0;
            // 使用一个具体的事件类型 struct 来替代字符串事件
            source.Events.On<GameEventType.Test.MigrationTestEvent>(_ => callbackCount++);

            string sourceId = source.Data.Get<string>(DataKey.Id);

            VisualPreviewEntity? target = null;
            try
            {
                target = EntityManager.Migrate<VisualPreviewEntity>(
                    source, // 源实体
                    new EntityMigrationConfig
                    {
                        TargetSpawn = new EntitySpawnConfig
                        {
                            Config = new Resource(), // 目标实体测试配置
                            UsingObjectPool = false // 直接用场景实例化
                        },
                        Profile = new EntityMigrationProfile
                        {
                            Name = "FilterName", // Profile 名称
                            ExcludeDataKeys = [DataKey.Name] // 排除名称键
                        },
                        DataOverrides = new Dictionary<string, object>
                        {
                            [DataKey.Team] = Team.Enemy, // 覆写阵营
                            [DataKey.Description] = "OverrideDescription" // 覆写描述
                        }
                    }
                );

                target!.Events.Emit(new GameEventType.Test.MigrationTestEvent());

                AssertEqual("Profile 排除的键不应被迁移", string.Empty, target.Data.Get<string>(DataKey.Name));
                AssertEqual("DataOverrides 应覆盖迁移后的最终值", "OverrideDescription", target.Data.Get<string>(DataKey.Description));
                AssertEqual("DataOverrides 应覆盖阵营", Team.Enemy, target.Data.Get<Team>(DataKey.Team));
                AssertEqual("Node 引用类型默认不应迁移", false, target.Data.Has("UnsafeNodeRef"));
                AssertEqual("源实体的局部事件订阅不应复制到目标实体", 0, callbackCount);
                AssertEqual("迁移后仍应销毁源实体", false, NodeLifecycleManager.IsRegistered(sourceId));
            }
            finally
            {
                if (target != null)
                {
                    CleanupEntities(target);
                }
            }
        }

        /// <summary>
        /// 验证投射物会沿 PARENT 关系回溯到归属单位判敌我，而不是直接拿自身 Team（通常为 Neutral）判断。
        /// </summary>
        private void TestCollisionPolicyUsesOwnerUnitForTeamFilter()
        {
            var owner = new MockEntity("Owner", Team.Player, EntityType.Unit);
            var projectile = new MockEntity("Projectile", Team.Neutral, EntityType.Projectile);
            var friendly = new MockEntity("Friendly", Team.Player, EntityType.Unit);
            var enemy = new MockEntity("Enemy", Team.Enemy, EntityType.Unit);

            AddChild(owner);
            AddChild(projectile);
            AddChild(friendly);
            AddChild(enemy);

            EntityManager.Register(owner);
            EntityManager.Register(projectile);
            EntityManager.Register(friendly);
            EntityManager.Register(enemy);

            EntityRelationshipManager.AddRelationship(
                owner.Data.Get<string>(DataKey.Id), // 父实体：归属单位
                projectile.Data.Get<string>(DataKey.Id), // 子实体：投射物
                EntityRelationshipType.PARENT // 统一溯源关系
            );

            try
            {
                var policy = new MovementCollisionPolicy();
                var @params = new MovementParams
                {
                    Mode = MoveMode.SineWave,
                    CollisionParams = new MovementCollisionParams
                    {
                        TeamFilter = TeamFilter.Enemy, // 只接受敌方
                        EntityTypeFilter = EntityType.Unit // 只接受单位
                    }
                };
                policy.Reset(@params);

                bool friendlyAccepted = policy.TryAccept(projectile, MoveMode.SineWave, @params, friendly, out _);
                bool enemyAccepted = policy.TryAccept(projectile, MoveMode.SineWave, @params, enemy, out _);

                AssertEqual("投射物应继承 owner 阵营，友军碰撞必须被过滤", false, friendlyAccepted);
                AssertEqual("投射物应继承 owner 阵营，敌方碰撞必须通过", true, enemyAccepted);
            }
            finally
            {
                CleanupEntities(owner, projectile, friendly, enemy);
            }
        }

        private void TestCollisionPolicyTrackedTargetOnly()
        {
            var source = new MockEntity("ArcSource", Team.Player, EntityType.Projectile);
            var trackedEnemy = new MockEntity("TrackedEnemy", Team.Enemy, EntityType.Unit);
            var otherEnemy = new MockEntity("OtherEnemy", Team.Enemy, EntityType.Unit);
            AddChild(source);
            AddChild(trackedEnemy);
            AddChild(otherEnemy);

            var policy = new MovementCollisionPolicy();
            var @params = new MovementParams
            {
                Mode = MoveMode.CircularArc,
                TargetNode = trackedEnemy, //锁定目标
                CollisionParams = new MovementCollisionParams
                {
                    TeamFilter = TeamFilter.Enemy, //只接受敌方
                    TargetMatchMode = MovementCollisionTargetMatchMode.TrackedTargetOnly, //仅锁定目标
                    StopAfterCollisionCount = 1 //首个有效碰撞停止
                }
            };
            policy.Reset(@params);

            // 现行契约：运动碰撞目标直接就是 IEntity 根节点，不再从 hurtbox 子节点向上回溯宿主。
            bool otherAccepted = policy.TryAccept(source, MoveMode.CircularArc, @params, otherEnemy, out _);
            AssertEqual("非锁定目标应被拒绝", false, otherAccepted);

            bool trackedAccepted = policy.TryAccept(source, MoveMode.CircularArc, @params, trackedEnemy,
                out var trackedContext);
            AssertEqual("锁定目标应通过匹配", true, trackedAccepted);
            AssertEqual("锁定目标首碰即停", true, trackedContext.WillStop);
            AssertEqual("锁定目标实体解析", trackedEnemy, trackedContext.TargetEntity);

            source.QueueFree();
            trackedEnemy.QueueFree();
            otherEnemy.QueueFree();
        }

        /// <summary>
        /// 统一清理注册过的测试实体，避免生命周期管理器和关系表残留脏数据。
        /// </summary>
        private static void CleanupEntities(params Node[] entities)
        {
            foreach (var entity in entities)
            {
                if (GodotObject.IsInstanceValid(entity))
                {
                    EntityManager.Destroy(entity);
                }
            }
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
