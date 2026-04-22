using Godot;
using System;
using System.Reflection;

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
                TestSystemManagerSeparatesEnableDisableFromRunTransitions();
                TestSystemManagerSupportsAddRemoveAndDependencyProtection();
                TestSystemProfileSchemaIsSimplified();
                TestSystemManagerBootstrapsWithExplicitProfileEntries();
                TestSystemManagerFallsBackToDescriptorDefaultsWhenProfileDoesNotDeclareSystem();
                TestSystemManagerReconcilesProfileEnabledWhenActiveProfileChanges();
                TestSystemProfileUsesLastDuplicateSystemEntry();
                TestSystemManagerBootstrapsOnlyOnce();
                TestProjectStateBridgeRespondsToGlobalEvents();
                TestCorePauseRelatedSystemsImplementSystemInterface();
                TestSystemCoreDoesNotExposeLegacyRuntimeInterface();
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
            manager.AddSystem(new SystemDescriptor(systemId, SystemKind.NodeScript, SystemLifetime.Test)
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

        private void TestSystemManagerSeparatesEnableDisableFromRunTransitions()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var systemId = $"SystemCoreRuntimeTest.Lifecycle.{Guid.NewGuid():N}";
            manager.AddSystem(new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                RunCondition = new SystemRunCondition
                {
                    AllowedAppPhases = [AppPhase.FrontEnd]
                },
                Factory = static () => new LifecycleProbeRuntime()
            });

            var runtime = manager.Resolve<LifecycleProbeRuntime>();
            AssertEqual("生命周期探针运行时应被创建", true, runtime != null);
            AssertEqual("Add 时应先触发一次 Added", 1, runtime?.AddedCount ?? -1);
            AssertEqual("Add 时不应隐式触发 Enabled", 0, runtime?.EnabledCount ?? -1);
            AssertEqual("Add 时不应隐式触发 Started", 0, runtime?.StartedCount ?? -1);

            manager.ProjectState.SetAppPhase(AppPhase.FrontEnd);
            manager.ProjectState.SetSessionPhase(SessionPhase.None);
            manager.ProjectState.SetOverlayPhase(OverlayPhase.None);

            AssertEqual("Phase 放行后只应触发一次 Started", 1, runtime?.StartedCount ?? -1);
            AssertEqual("Phase 放行不应触发 Enabled", 0, runtime?.EnabledCount ?? -1);

            manager.ProjectState.SetAppPhase(AppPhase.Boot);
            manager.ProjectState.SetExecutionPhase(ExecutionPhase.Running);

            AssertEqual("Phase 收紧后只应触发一次 Stopped", 1, runtime?.StoppedCount ?? -1);
            AssertEqual("Phase 收紧不应触发 Disabled", 0, runtime?.DisabledCount ?? -1);

            manager.DisableSystem(systemId);
            AssertEqual("显式 Disable 才应触发 Disabled", 1, runtime?.DisabledCount ?? -1);
            AssertEqual("显式 Disable 处于未运行态时不应重复 Stop", 1, runtime?.StoppedCount ?? -1);

            manager.EnableSystem(systemId);
            AssertEqual("显式 Enable 才应触发 Enabled", 1, runtime?.EnabledCount ?? -1);
            AssertEqual("状态仍不允许时 Enable 不应直接 Start", 1, runtime?.StartedCount ?? -1);

            manager.ProjectState.SetAppPhase(AppPhase.FrontEnd);
            AssertEqual("重新满足条件后应再次 Started", 2, runtime?.StartedCount ?? -1);

            manager.QueueFree();
        }

        private void TestSystemManagerSupportsAddRemoveAndDependencyProtection()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var dependencyId = $"SystemCoreRuntimeTest.Dependency.{Guid.NewGuid():N}";
            var parentId = $"SystemCoreRuntimeTest.Parent.{Guid.NewGuid():N}";

            SystemRegistry.Register(new SystemDescriptor(dependencyId, SystemKind.PureService, SystemLifetime.Test)
            {
                Factory = static () => new DependencyProbeRuntime()
            });

            var parentDescriptor = new SystemDescriptor(parentId, SystemKind.PureService, SystemLifetime.Test)
            {
                Dependencies = [dependencyId],
                Factory = static () => new ParentProbeRuntime()
            };

            manager.AddSystem(parentDescriptor);

            var dependency = manager.Resolve<DependencyProbeRuntime>();
            var parent = manager.Resolve<ParentProbeRuntime>();
            AssertEqual("AddSystem 应递归补齐依赖系统", true, dependency != null);
            AssertEqual("AddSystem 应创建目标父系统", true, parent != null);
            AssertEqual("依赖系统应收到 Added 回调", 1, dependency?.AddedCount ?? -1);
            AssertEqual("父系统应收到 Added 回调", 1, parent?.AddedCount ?? -1);

            AssertEqual("存在反向依赖时 RemoveSystem 应拒绝依赖系统", false, manager.RemoveSystem(dependencyId));
            AssertEqual("目标系统应允许被移除", true, manager.RemoveSystem(parentId));
            AssertEqual("移除父系统后应触发 Removed", 1, parent?.RemovedCount ?? -1);
            AssertEqual("父系统移除后依赖系统才可移除", true, manager.RemoveSystem(dependencyId));
            AssertEqual("依赖系统最终也应收到 Removed", 1, dependency?.RemovedCount ?? -1);

            manager.QueueFree();
        }

        private void TestSystemProfileSchemaIsSimplified()
        {
            var profileType = typeof(SystemProfile);
            var profileEntryType = profileType.Assembly.GetType("SystemProfileEntry");

            AssertEqual("SystemProfile 应提供 Systems 入口", true, profileType.GetProperty("Systems") != null);
            AssertEqual("SystemProfile 不应再暴露 DefaultAutoAdd", true, profileType.GetProperty("DefaultAutoAdd") == null);
            AssertEqual("SystemProfile 不应再暴露 DefaultEnabled", true, profileType.GetProperty("DefaultEnabled") == null);
            AssertEqual("SystemProfile 不应再暴露 TagOverrides", true, profileType.GetProperty("TagOverrides") == null);
            AssertEqual("SystemProfile 不应再暴露 SystemOverrides", true, profileType.GetProperty("SystemOverrides") == null);
            AssertEqual("System Core 应提供精简后的 SystemProfileEntry", true, profileEntryType != null);
            AssertEqual("System Core 不应再暴露 SystemProfileTagOverride", true, profileType.Assembly.GetType("SystemProfileTagOverride") == null);
            AssertEqual("System Core 不应再暴露 SystemProfileSystemOverride", true, profileType.Assembly.GetType("SystemProfileSystemOverride") == null);
            AssertEqual("System Core 不应再暴露 SystemProfileSwitchMode", true, profileType.Assembly.GetType("SystemProfileSwitchMode") == null);
            AssertEqual("System Core 不应再暴露 SystemProfileAuthoringCatalog", true, profileType.Assembly.GetType("SystemProfileAuthoringCatalog") == null);
        }

        private void TestSystemManagerBootstrapsWithExplicitProfileEntries()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var enabledSystemId = $"SystemCoreRuntimeTest.ProfileEnabled.{Guid.NewGuid():N}";
            var skippedSystemId = $"SystemCoreRuntimeTest.ProfileSkipped.{Guid.NewGuid():N}";

            SystemRegistry.Register(new SystemDescriptor(enabledSystemId, SystemKind.PureService, SystemLifetime.Test)
            {
                DefaultAutoAdd = false,
                Factory = static () => new ProfileEnabledProbeRuntime()
            });

            SystemRegistry.Register(new SystemDescriptor(skippedSystemId, SystemKind.PureService, SystemLifetime.Test)
            {
                DefaultAutoAdd = false,
                Factory = static () => new ProfileSkippedProbeRuntime()
            });

            manager.SetActiveProfile(CreateProfile(
                ("SystemCoreRuntimeTestExplicitProfile", enabledSystemId, true, true)));

            manager.BootstrapRegisteredSystems();

            AssertEqual("Profile 覆盖的系统应被自动装载", true, manager.Resolve<ProfileEnabledProbeRuntime>() != null);
            AssertEqual("未被 Profile 覆盖的系统不应自动装载", true, manager.Resolve<ProfileSkippedProbeRuntime>() == null);

            manager.QueueFree();
        }

        private void TestSystemManagerFallsBackToDescriptorDefaultsWhenProfileDoesNotDeclareSystem()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();

            var fallbackAutoAddSystemId = $"SystemCoreRuntimeTest.ProfileFallback.AutoAdd.{Guid.NewGuid():N}";
            var fallbackDisabledSystemId = $"SystemCoreRuntimeTest.ProfileFallback.Disabled.{Guid.NewGuid():N}";

            SystemRegistry.Register(new SystemDescriptor(fallbackAutoAddSystemId, SystemKind.PureService, SystemLifetime.Test)
            {
                DefaultAutoAdd = true,
                Factory = static () => new ProfileEnabledProbeRuntime()
            });

            SystemRegistry.Register(new SystemDescriptor(fallbackDisabledSystemId, SystemKind.PureService, SystemLifetime.Test)
            {
                DefaultAutoAdd = true,
                DefaultEnabled = false,
                Factory = static () => new LifecycleProbeRuntime()
            });

            manager.SetActiveProfile(CreateProfile(
                ("SystemCoreRuntimeTestFallbackProfile", $"SystemCoreRuntimeTest.ProfileFallback.Other.{Guid.NewGuid():N}", false, false)));

            manager.BootstrapRegisteredSystems();

            var fallbackAutoAddRuntime = manager.Resolve<ProfileEnabledProbeRuntime>();
            var fallbackDisabledRuntime = manager.Resolve<LifecycleProbeRuntime>();
            AssertEqual("Profile 未声明时应回退 descriptor.DefaultAutoAdd", true, fallbackAutoAddRuntime != null);
            AssertEqual("Profile 未声明时应创建默认 AutoAdd 的系统", true, fallbackDisabledRuntime != null);
            AssertEqual("Profile 未声明时应回退 descriptor.DefaultEnabled", 0, fallbackDisabledRuntime?.StartedCount ?? -1);

            manager.QueueFree();
        }

        private void TestSystemManagerReconcilesProfileEnabledWhenActiveProfileChanges()
        {
            var manager = new SystemManager();
            AddChild(manager);
            manager.Initialize();
            manager.ProjectState.SetAppPhase(AppPhase.Boot);
            manager.ProjectState.SetSessionPhase(SessionPhase.None);
            manager.ProjectState.SetOverlayPhase(OverlayPhase.None);
            manager.ProjectState.SetExecutionPhase(ExecutionPhase.Running);

            var systemId = $"SystemCoreRuntimeTest.ProfileRefresh.{Guid.NewGuid():N}";
            manager.SetActiveProfile(CreateProfile(
                ("SystemCoreRuntimeTestProfileEnabled", systemId, true, true)));

            manager.AddSystem(new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                Factory = static () => new LifecycleProbeRuntime()
            });

            var runtime = manager.Resolve<LifecycleProbeRuntime>();
            AssertEqual("Profile 允许时系统应进入运行态", 1, runtime?.StartedCount ?? -1);
            AssertEqual("Profile 切换前不应触发 Disabled", 0, runtime?.DisabledCount ?? -1);

            manager.SetActiveProfile(CreateProfile(
                ("SystemCoreRuntimeTestProfileDisabled", systemId, true, false)));

            AssertEqual("切换到禁用 Profile 后系统应停止运行", 1, runtime?.StoppedCount ?? -1);
            AssertEqual("切换 Profile 不应触发 Disabled", 0, runtime?.DisabledCount ?? -1);

            manager.SetActiveProfile(null);

            AssertEqual("清空 Profile 后应回退 descriptor.DefaultEnabled 并恢复运行", 2, runtime?.StartedCount ?? -1);
            AssertEqual("清空 Profile 仍不应触发 Enabled", 0, runtime?.EnabledCount ?? -1);

            manager.QueueFree();
        }

        private void TestSystemProfileUsesLastDuplicateSystemEntry()
        {
            var systemId = $"SystemCoreRuntimeTest.ProfileDuplicate.{Guid.NewGuid():N}";
            var descriptor = new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                DefaultAutoAdd = false,
                DefaultEnabled = true,
                Factory = static () => new ProfileEnabledProbeRuntime()
            };

            var service = new SystemProfileService();
            service.SetActiveProfile(CreateProfile(
                ("SystemCoreRuntimeTestDuplicateProfile", systemId, false, true),
                ("SystemCoreRuntimeTestDuplicateProfile", systemId, true, false)));

            AssertEqual("重复 SystemId 时后写条目应覆盖 AutoAdd", true, service.ResolveAutoAdd(descriptor));
            AssertEqual("重复 SystemId 时后写条目应覆盖 Enabled", false, service.ResolveEnabled(descriptor));
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

            bridge.OnAdded(new SystemRegistrationContext(descriptor, service));
            bridge.OnStarted(service.Snapshot);

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

            bridge.OnStopped(service.Snapshot);
        }

        private void TestCorePauseRelatedSystemsImplementSystemInterface()
        {
            AssertEqual("TimerManager 应实现 ISystem", true, new TimerManager() is ISystem);
            AssertEqual("SpawnSystem 应实现 ISystem", true, new SpawnSystem() is ISystem);
            AssertEqual("DamageStatisticsSystem 应实现 ISystem", true, new DamageStatisticsSystem() is ISystem);
            AssertEqual("PauseMenuSystem 应实现 ISystem", true, new PauseMenuSystem() is ISystem);
        }

        private void TestSystemCoreDoesNotExposeLegacyRuntimeInterface()
        {
            var assembly = typeof(SystemManager).Assembly;
            var legacyInterface = Array.Find(
                assembly.GetTypes(),
                static type => string.Equals(type.Name, "ISystemRuntime", StringComparison.Ordinal));

            AssertEqual("System Core 不应再暴露 ISystemRuntime 兼容接口", true, legacyInterface == null);
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

        private static SystemProfile CreateProfile(params (string profileName, string systemId, bool autoAdd, bool enabled)[] entries)
        {
            var systemsProperty = typeof(SystemProfile).GetProperty("Systems");
            if (systemsProperty == null)
            {
                throw new InvalidOperationException("SystemProfile 必须暴露 Systems 属性");
            }

            var profileEntryType = typeof(SystemProfile).Assembly.GetType("SystemProfileEntry");
            if (profileEntryType == null)
            {
                throw new InvalidOperationException("System Core 必须提供 SystemProfileEntry 类型");
            }

            var systems = Activator.CreateInstance(systemsProperty.PropertyType)
                ?? throw new InvalidOperationException("SystemProfile.Systems 无法实例化");
            var addMethod = systemsProperty.PropertyType.GetMethod("Add")
                ?? throw new InvalidOperationException("SystemProfile.Systems 缺少 Add 方法");

            for (var i = 0; i < entries.Length; i++)
            {
                var entry = Activator.CreateInstance(profileEntryType)
                    ?? throw new InvalidOperationException("SystemProfileEntry 无法实例化");
                SetRequiredProperty(profileEntryType, entry, "SystemId", entries[i].systemId);
                SetRequiredProperty(profileEntryType, entry, "AutoAdd", entries[i].autoAdd);
                SetRequiredProperty(profileEntryType, entry, "Enabled", entries[i].enabled);
                addMethod.Invoke(systems, [entry]);
            }

            var profile = new SystemProfile
            {
                Name = entries.Length > 0 ? entries[0].profileName : "SystemCoreRuntimeTestProfile"
            };
            systemsProperty.SetValue(profile, systems);
            return profile;
        }

        private static void SetRequiredProperty(Type ownerType, object target, string propertyName, object value)
        {
            var property = ownerType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
            {
                throw new InvalidOperationException($"{ownerType.Name} 缺少 {propertyName} 属性");
            }

            property.SetValue(target, value);
        }

        private sealed partial class ParentPathProbeNode : Node
        {
        }

        private sealed class LifecycleProbeRuntime : ISystem
        {
            public int AddedCount { get; private set; }

            public int RemovedCount { get; private set; }

            public int EnabledCount { get; private set; }

            public int DisabledCount { get; private set; }

            public int StartedCount { get; private set; }

            public int StoppedCount { get; private set; }

            public int StateChangedCount { get; private set; }

            public void OnAdded(SystemRegistrationContext context)
            {
                AddedCount++;
            }

            public void OnRemoved()
            {
                RemovedCount++;
            }

            public void OnEnabled(ProjectStateSnapshot snapshot)
            {
                EnabledCount++;
            }

            public void OnDisabled(ProjectStateSnapshot snapshot)
            {
                DisabledCount++;
            }

            public void OnStarted(ProjectStateSnapshot snapshot)
            {
                StartedCount++;
            }

            public void OnStopped(ProjectStateSnapshot snapshot)
            {
                StoppedCount++;
            }

            public void OnProjectStateChanged(ProjectStateChangedEventArgs args)
            {
                StateChangedCount++;
            }
        }

        private sealed class DependencyProbeRuntime : ISystem
        {
            public int AddedCount { get; private set; }

            public int RemovedCount { get; private set; }

            public void OnAdded(SystemRegistrationContext context)
            {
                AddedCount++;
            }

            public void OnRemoved()
            {
                RemovedCount++;
            }
        }

        private sealed class ParentProbeRuntime : ISystem
        {
            public int AddedCount { get; private set; }

            public int RemovedCount { get; private set; }

            public void OnAdded(SystemRegistrationContext context)
            {
                AddedCount++;
            }

            public void OnRemoved()
            {
                RemovedCount++;
            }
        }

        private sealed class ProfileEnabledProbeRuntime : ISystem
        {
        }

        private sealed class ProfileSkippedProbeRuntime : ISystem
        {
        }
    }
}
