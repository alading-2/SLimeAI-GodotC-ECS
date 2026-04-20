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
                TestSystemRunCondition();
                TestStatusCollectionKeepsInvulnerableUntilAllSourcesExpire();
                TestSystemRegistryKeepsFirstDescriptorWhenDuplicateRegistered();
                TestSystemManagerAppliesParentPathWithinLifetimeHost();
                TestSystemManagerOnlyTransitionsOnStateChange();
                TestSystemManagerBootstrapsOnlyOnce();
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
