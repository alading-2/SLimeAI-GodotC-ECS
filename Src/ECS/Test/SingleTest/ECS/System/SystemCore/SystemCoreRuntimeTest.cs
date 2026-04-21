using Godot;
using System;

namespace Slime.Test.SystemCore
{
    /// <summary>
    /// System Core 运行时回归测试。
    /// <para>使用项目原生 SingleTest 场景验证系统核心与状态核心的基础行为。</para>
    /// </summary>
    public partial class SystemCoreRuntimeTest : Node
    {
        private static readonly Log _log = new(nameof(SystemCoreRuntimeTest));

        private int _passedCount;
        private int _failedCount;

        public override void _Ready()
        {
            _log.Info("开始 System Core 运行时测试");

            try
            {
                TestProjectStateDefaults();
                TestProjectStateFlowHelpers();
                TestSystemRunCondition();
                TestGameplayRunConditionPreset();
                TestStatusCollectionKeepsInvulnerableUntilAllSourcesExpire();
                TestSystemRegistryKeepsFirstDescriptorWhenDuplicateRegistered();
                TestSystemManagerAppliesParentPathWithinLifetimeHost();
                TestSystemManagerOnlyTransitionsOnStateChange();
                TestSystemManagerBootstrapsOnlyOnce();
                TestProjectStateBridgeRespondsToGlobalEvents();
                TestCorePauseRelatedSystemsImplementRuntimeLifecycle();
            }
            catch (Exception ex)
            {
                Fail($"测试过程中发生异常: {ex}");
            }

            _log.Info($"System Core 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
            GetTree().Quit(_failedCount == 0 ? 0 : 1);
        }

        private void TestProjectStateDefaults()
        {
            var service = new ProjectStateService();

            AssertEqual("默认 AppPhase", AppPhase.Boot, service.AppPhase);
            AssertEqual("默认 SessionPhase", SessionPhase.None, service.SessionPhase);
            AssertEqual("默认 OverlayPhase", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("默认 ExecutionPhase", ExecutionPhase.Running, service.ExecutionPhase);
        }

        private void TestSystemRunCondition()
        {
            var condition = new SystemRunCondition
            {
                AllowedAppPhases = [AppPhase.InSession],
                AllowedSessionPhases = [SessionPhase.Playing],
                BlockedOverlayPhases = [OverlayPhase.PauseMenu],
                AllowedExecutionPhases = [ExecutionPhase.Running]
            };

            var allowedSnapshot = new ProjectStateSnapshot(
                AppPhase.InSession,
                SessionPhase.Playing,
                OverlayPhase.None,
                ExecutionPhase.Running);

            var blockedSnapshot = new ProjectStateSnapshot(
                AppPhase.InSession,
                SessionPhase.Playing,
                OverlayPhase.PauseMenu,
                ExecutionPhase.Running);

            AssertEqual("匹配中的项目状态应允许运行", true, condition.Evaluate(allowedSnapshot));
            AssertEqual("PauseMenu 覆盖层应阻止运行", false, condition.Evaluate(blockedSnapshot));
        }

        private void TestProjectStateFlowHelpers()
        {
            var service = new ProjectStateService();

            service.BeginGameplaySession();
            AssertEqual("BeginGameplaySession 应切到局内主流程", AppPhase.InSession, service.AppPhase);
            AssertEqual("BeginGameplaySession 应切到 Playing", SessionPhase.Playing, service.SessionPhase);
            AssertEqual("BeginGameplaySession 应清空覆盖层", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("BeginGameplaySession 应恢复执行", ExecutionPhase.Running, service.ExecutionPhase);

            service.OpenPauseMenu();
            AssertEqual("OpenPauseMenu 应打开暂停菜单", OverlayPhase.PauseMenu, service.OverlayPhase);
            AssertEqual("OpenPauseMenu 应暂停执行", ExecutionPhase.Paused, service.ExecutionPhase);

            service.ClosePauseMenu();
            AssertEqual("ClosePauseMenu 应关闭暂停菜单", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("ClosePauseMenu 应恢复执行", ExecutionPhase.Running, service.ExecutionPhase);

            service.SetBlocked(OverlayPhase.Cutscene);
            AssertEqual("SetBlocked 应写入阻塞覆盖层", OverlayPhase.Cutscene, service.OverlayPhase);
            AssertEqual("SetBlocked 应进入 Blocked", ExecutionPhase.Blocked, service.ExecutionPhase);

            service.ClearBlocked();
            AssertEqual("ClearBlocked 应清空覆盖层", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("ClearBlocked 应恢复 Running", ExecutionPhase.Running, service.ExecutionPhase);

            service.EndSession();
            AssertEqual("EndSession 应标记会话结束", SessionPhase.Ended, service.SessionPhase);
            AssertEqual("EndSession 不应保留暂停菜单", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("EndSession 应进入 Blocked", ExecutionPhase.Blocked, service.ExecutionPhase);
        }

        private void TestGameplayRunConditionPreset()
        {
            var condition = SystemRunCondition.GameplayRunning();

            var playingSnapshot = new ProjectStateSnapshot(
                AppPhase.InSession,
                SessionPhase.Playing,
                OverlayPhase.None,
                ExecutionPhase.Running);

            var pausedSnapshot = new ProjectStateSnapshot(
                AppPhase.InSession,
                SessionPhase.Playing,
                OverlayPhase.PauseMenu,
                ExecutionPhase.Paused);

            var endedSnapshot = new ProjectStateSnapshot(
                AppPhase.InSession,
                SessionPhase.Ended,
                OverlayPhase.None,
                ExecutionPhase.Blocked);

            AssertEqual("GameplayRunning 在 Playing + Running 应通过", true, condition.Evaluate(playingSnapshot));
            AssertEqual("GameplayRunning 在暂停时应阻止运行", false, condition.Evaluate(pausedSnapshot));
            AssertEqual("GameplayRunning 在会话结束时应阻止运行", false, condition.Evaluate(endedSnapshot));
        }

        private void TestStatusCollectionKeepsInvulnerableUntilAllSourcesExpire()
        {
            var collection = new StatusCollection();
            var definition = new StatusDefinition(
                "Invulnerable",
                "无敌",
                StatusEffectFlags.Invulnerable);

            collection.Apply(new StatusInstance("skill-a", "Invulnerable", definition, -1f));
            collection.Apply(new StatusInstance("skill-b", "Invulnerable", definition, -1f));

            AssertEqual("双来源无敌应生效", true, collection.BuildSnapshot().IsInvulnerable);

            collection.Remove("skill-a", "Invulnerable");
            AssertEqual("移除一个来源后应保留剩余无敌", true, collection.BuildSnapshot().IsInvulnerable);

            collection.Remove("skill-b", "Invulnerable");
            AssertEqual("全部来源移除后无敌应关闭", false, collection.BuildSnapshot().IsInvulnerable);
        }

        private void TestSystemRegistryKeepsFirstDescriptorWhenDuplicateRegistered()
        {
            var systemId = $"SystemCoreRuntimeTest.{Guid.NewGuid():N}";
            var firstDescriptor = new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                Factory = static () => new object()
            };
            var duplicateDescriptor = new SystemDescriptor(systemId, SystemKind.NodeScript, SystemLifetime.Debug)
            {
                ParentPath = "ShouldNotReplace",
                Factory = static () => new Node()
            };

            SystemRegistry.Register(firstDescriptor);
            SystemRegistry.Register(duplicateDescriptor);

            var registered = SystemRegistry.GetDescriptor(systemId);
            AssertEqual("重复 SystemId 应保留首个描述符实例", true, ReferenceEquals(firstDescriptor, registered));
            AssertEqual("重复注册不应覆盖首个 Kind", SystemKind.PureService, registered?.Kind ?? SystemKind.NodeScene);
            AssertEqual("重复注册不应覆盖首个 Lifetime", SystemLifetime.Test, registered?.Lifetime ?? SystemLifetime.Persistent);
        }

        private void TestSystemManagerAppliesParentPathWithinLifetimeHost()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var systemId = $"SystemCoreRuntimeTest.ParentPath.{Guid.NewGuid():N}";
            manager.EnsureSystem(new SystemDescriptor(systemId, SystemKind.NodeScript, SystemLifetime.Test)
            {
                ParentPath = "Nested/Leaf",
                Factory = static () => new ParentPathProbeNode()
            });

            var probe = manager.Resolve<ParentPathProbeNode>();
            AssertEqual("ParentPath 测试节点应被创建", true, probe != null);
            AssertEqual("ParentPath 末端节点名称应命中", "Leaf", probe?.GetParent()?.Name?.ToString() ?? string.Empty);
            AssertEqual("ParentPath 中间节点名称应命中", "Nested", probe?.GetParent()?.GetParent()?.Name?.ToString() ?? string.Empty);

            manager.QueueFree();
        }

        private void TestSystemManagerOnlyTransitionsOnStateChange()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var systemId = $"SystemCoreRuntimeTest.Transition.{Guid.NewGuid():N}";
            manager.EnsureSystem(new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                RunCondition = new SystemRunCondition
                {
                    AllowedAppPhases = [AppPhase.FrontEnd]
                },
                Factory = static () => new TransitionProbeRuntime()
            });

            var runtime = manager.Resolve<TransitionProbeRuntime>();
            AssertEqual("状态探针运行时应被创建", true, runtime != null);

            manager.ProjectState.SetAppPhase(AppPhase.FrontEnd);
            manager.ProjectState.SetSessionPhase(SessionPhase.None);
            manager.ProjectState.SetOverlayPhase(OverlayPhase.None);

            AssertEqual("进入允许运行状态后只应启用一次", 1, runtime?.EnableCount ?? -1);

            manager.ProjectState.SetAppPhase(AppPhase.Boot);
            manager.ProjectState.SetExecutionPhase(ExecutionPhase.Running);

            AssertEqual("离开允许运行状态后只应禁用一次", 1, runtime?.DisableCount ?? -1);

            manager.QueueFree();
        }

        private void TestSystemManagerBootstrapsOnlyOnce()
        {
            var manager = new SystemManager();
            AddChild(manager);
            var bootstrapSignalCount = 0;
            manager.BootstrapCompleted += () => bootstrapSignalCount++;

            manager.Initialize();
            manager.BootstrapRegisteredSystems();
            manager.BootstrapRegisteredSystems();

            AssertEqual("SystemManager 应标记为已完成启动", true, manager.IsBootstrapped);
            AssertEqual("BootstrapCompleted 只应触发一次", 1, bootstrapSignalCount);

            manager.QueueFree();
        }

        private void TestProjectStateBridgeRespondsToGlobalEvents()
        {
            var service = new ProjectStateService();
            var descriptor = new SystemDescriptor("SystemCoreRuntimeTest.ProjectStateBridge", SystemKind.PureService, SystemLifetime.Test)
            {
                Factory = static () => new ProjectStateBridge()
            };
            var bridge = new ProjectStateBridge();

            bridge.OnSystemRegistered(new SystemRegistrationContext(descriptor, service));
            bridge.OnSystemEnabled(service.Snapshot);

            GlobalEventBus.TriggerGameStart();
            AssertEqual("ProjectStateBridge 应响应 GameStart", AppPhase.InSession, service.AppPhase);
            AssertEqual("ProjectStateBridge 应把会话切到 Playing", SessionPhase.Playing, service.SessionPhase);

            GlobalEventBus.Global.Emit(GameEventType.Global.GamePause, new GameEventType.Global.GamePauseEventData());
            AssertEqual("ProjectStateBridge 应响应 GamePause", OverlayPhase.PauseMenu, service.OverlayPhase);
            AssertEqual("ProjectStateBridge 应把执行态切到 Paused", ExecutionPhase.Paused, service.ExecutionPhase);

            GlobalEventBus.Global.Emit(GameEventType.Global.GameResume, new GameEventType.Global.GameResumeEventData());
            AssertEqual("ProjectStateBridge 应响应 GameResume", OverlayPhase.None, service.OverlayPhase);
            AssertEqual("ProjectStateBridge 应恢复 Running", ExecutionPhase.Running, service.ExecutionPhase);

            GlobalEventBus.TriggerGameOver(false);
            AssertEqual("ProjectStateBridge 应响应 GameOver", SessionPhase.Ended, service.SessionPhase);
            AssertEqual("ProjectStateBridge 应在 GameOver 后进入 Blocked", ExecutionPhase.Blocked, service.ExecutionPhase);

            bridge.OnSystemDisabled(service.Snapshot);
        }

        private void TestCorePauseRelatedSystemsImplementRuntimeLifecycle()
        {
            AssertEqual("TimerManager 应实现 ISystemRuntime", true, new TimerManager() is ISystemRuntime);
            AssertEqual("SpawnSystem 应实现 ISystemRuntime", true, new SpawnSystem() is ISystemRuntime);
            AssertEqual("DamageStatisticsSystem 应实现 ISystemRuntime", true, new DamageStatisticsSystem() is ISystemRuntime);
            AssertEqual("PauseMenuSystem 应实现 ISystemRuntime", true, new PauseMenuSystem() is ISystemRuntime);
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

        private sealed partial class ParentPathProbeNode : Node
        {
        }

        private sealed class TransitionProbeRuntime : ISystemRuntime
        {
            public int EnableCount { get; private set; }

            public int DisableCount { get; private set; }

            public void OnSystemEnabled(ProjectStateSnapshot snapshot)
            {
                EnableCount++;
            }

            public void OnSystemDisabled(ProjectStateSnapshot snapshot)
            {
                DisableCount++;
            }
        }
    }
}
