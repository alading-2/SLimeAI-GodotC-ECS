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
                TestProjectStatePublishesInstanceEvents();
                TestLocalProjectStateServiceDoesNotDriveSystemManager();
                TestProjectStatePhasePresets();
                TestSystemRunCondition();
                TestGameplayRunConditionPreset();
                TestSystemRunConditionTreatsNoneAsUnrestricted();
                TestDataOsRuntimeTableSystemCollectionsDoNotContainNull();
                TestSystemConfigPresetCalculatesEnabledSystems();
                TestCoreSystemDescriptorsRegistered();
                TestRequiredSystemCannotBeDisabledOrRemoved();
                TestMissingSystemManagementReportsFailure();
                TestSystemCommandExecutionRespectsRunningState();
                TestStatusCollectionKeepsInvulnerableUntilAllSourcesExpire();
                TestSystemRegistryKeepsFirstDescriptorWhenDuplicateRegistered();
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

            AssertEqual("默认流程状态", GameFlowState.Boot, service.FlowState);
            AssertEqual("默认覆盖层", OverlayFlags.None, service.Overlays);
            AssertEqual("默认模拟状态", SimulationState.Running, service.SimulationState);
        }

        private void TestSystemRunCondition()
        {
            var condition = new SystemRunCondition
            {
                AllowedFlowStates = GameFlowState.SessionPlaying,
                BlockedOverlays = OverlayFlags.PauseMenu,
                AllowedSimulationStates = SimulationState.Running
            };

            var allowedSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionPlaying,
                OverlayFlags.None,
                SimulationState.Running);

            var blockedSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionPlaying,
                OverlayFlags.PauseMenu,
                SimulationState.Running);

            AssertEqual("匹配中的项目状态应允许运行", true, condition.Evaluate(allowedSnapshot));
            AssertEqual("PauseMenu 覆盖层应阻止运行", false, condition.Evaluate(blockedSnapshot));

            var (isBlocked, reason) = condition.GetBlockedReason(blockedSnapshot);
            AssertEqual("GetBlockedReason 应返回阻塞标记", true, isBlocked);
            AssertEqual("GetBlockedReason 应返回可读原因", true, !string.IsNullOrEmpty(reason));
        }

        private void TestProjectStateFlowHelpers()
        {
            var service = new ProjectStateService();

            service.BeginGameplaySession();
            AssertEqual("BeginGameplaySession 应切到局内进行", GameFlowState.SessionPlaying, service.FlowState);
            AssertEqual("BeginGameplaySession 应清空覆盖层", OverlayFlags.None, service.Overlays);
            AssertEqual("BeginGameplaySession 应恢复模拟", SimulationState.Running, service.SimulationState);

            service.OpenPauseMenu();
            AssertEqual("OpenPauseMenu 应打开暂停菜单", OverlayFlags.PauseMenu, service.Overlays);
            AssertEqual("OpenPauseMenu 应暂停模拟", SimulationState.Suspended, service.SimulationState);

            service.ClosePauseMenu();
            AssertEqual("ClosePauseMenu 应关闭暂停菜单", OverlayFlags.None, service.Overlays);
            AssertEqual("ClosePauseMenu 应恢复模拟", SimulationState.Running, service.SimulationState);

            service.SetBlocked(OverlayFlags.Cutscene);
            AssertEqual("SetBlocked 应写入阻塞覆盖层", OverlayFlags.Cutscene, service.Overlays);
            AssertEqual("SetBlocked 应暂停模拟", SimulationState.Suspended, service.SimulationState);

            service.ClearBlocked();
            AssertEqual("ClearBlocked 应清空覆盖层", OverlayFlags.None, service.Overlays);
            AssertEqual("ClearBlocked 应恢复模拟", SimulationState.Running, service.SimulationState);

            service.EndSession();
            AssertEqual("EndSession 应标记会话结束", GameFlowState.SessionEnded, service.FlowState);
            AssertEqual("EndSession 不应保留暂停菜单", OverlayFlags.None, service.Overlays);
            AssertEqual("EndSession 应暂停模拟", SimulationState.Suspended, service.SimulationState);
        }

        private void TestProjectStatePublishesInstanceEvents()
        {
            var service = new ProjectStateService();
            var changedCount = 0;
            ProjectStateChangedEventArgs? lastEvent = null;

            void OnProjectStateChanged(object? sender, ProjectStateChangedEventArgs evt)
            {
                changedCount++;
                lastEvent = evt;
            }

            service.StateChanged += OnProjectStateChanged;

            service.BeginGameplaySession();

            service.StateChanged -= OnProjectStateChanged;

            AssertEqual("ProjectStateService 应通过实例事件广播状态变化", 1, changedCount);
            AssertEqual("StateChanged 应携带事件数据", true, lastEvent != null);
            AssertEqual("StateChanged 应携带旧状态", GameFlowState.Boot, lastEvent!.Previous.FlowState);
            AssertEqual("StateChanged 应携带新状态", GameFlowState.SessionPlaying, lastEvent.Current.FlowState);
        }

        private void TestLocalProjectStateServiceDoesNotDriveSystemManager()
        {
            var manager = SystemManager.Instance;
            if (manager == null)
            {
                Fail("SystemManager.Instance 应存在");
                return;
            }

            manager.ProjectState.EnterFrontEnd();
            var before = manager.GetSystemRuntimeInfo("DamageService");
            var localService = new ProjectStateService();

            localService.BeginGameplaySession();
            var after = manager.GetSystemRuntimeInfo("DamageService");

            AssertEqual("局部 ProjectStateService 不应改变 SystemManager 项目状态", GameFlowState.FrontEnd, manager.ProjectState.FlowState);
            AssertEqual("局部 ProjectStateService 不应驱动 SystemManager 系统运行态", before?.IsRunning, after?.IsRunning);
        }

        private void TestProjectStatePhasePresets()
        {
            AssertEqual("GameFlowState.Gameplay 应只包含局内进行", GameFlowState.SessionPlaying, GameFlowState.Gameplay);
            AssertEqual("GameFlowState.Session 应包含完整单局流程", true,
                (GameFlowState.Session & GameFlowState.SessionPreparing) != 0
                && (GameFlowState.Session & GameFlowState.SessionPlaying) != 0
                && (GameFlowState.Session & GameFlowState.SessionResolving) != 0
                && (GameFlowState.Session & GameFlowState.SessionEnded) != 0);
            AssertEqual("OverlayFlags.Blocking 应包含常见阻塞覆盖层", OverlayFlags.PauseMenu | OverlayFlags.ModalUi | OverlayFlags.Cutscene, OverlayFlags.Blocking);
            AssertEqual("SimulationState.Any 应包含运行和挂起", SimulationState.Running | SimulationState.Suspended, SimulationState.Any);
        }


        private void TestGameplayRunConditionPreset()
        {
            var condition = SystemRunCondition.GameplayRunning();

            var playingSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionPlaying,
                OverlayFlags.None,
                SimulationState.Running);

            var pausedSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionPlaying,
                OverlayFlags.PauseMenu,
                SimulationState.Suspended);

            var endedSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionEnded,
                OverlayFlags.None,
                SimulationState.Suspended);

            AssertEqual("GameplayRunning 在 Playing + Running 应通过", true, condition.Evaluate(playingSnapshot));
            AssertEqual("GameplayRunning 在暂停时应阻止运行", false, condition.Evaluate(pausedSnapshot));
            AssertEqual("GameplayRunning 在会话结束时应阻止运行", false, condition.Evaluate(endedSnapshot));
        }

        private void TestSystemRunConditionTreatsNoneAsUnrestricted()
        {
            var condition = new SystemRunCondition();
            var pausedMenuSnapshot = new ProjectStateSnapshot(
                GameFlowState.SessionPlaying,
                OverlayFlags.PauseMenu,
                SimulationState.Suspended);

            AssertEqual("None 运行条件应表示不限制", true, condition.Evaluate(pausedMenuSnapshot));
        }

        private void TestDataOsRuntimeTableSystemCollectionsDoNotContainNull()
        {
            SystemConfigService.Initialize();
            SystemPresetService.Initialize();

            foreach (var config in SystemConfigService.GetAllConfigs())
            {
                AssertEqual("snapshot system.config 不应包含空配置", true, config != null);
            }

            foreach (var preset in SystemPresetService.GetAllPresets())
            {
                AssertEqual("snapshot system.preset 不应包含空预设", true, preset != null);
            }
        }

        private void TestSystemConfigPresetCalculatesEnabledSystems()
        {
            SystemConfigService.Initialize();
            SystemPresetService.Initialize();

            var enabledSystems = SystemPresetService.CalculateEnabledSystems();
            AssertEqual("系统预设应启用 ObjectPoolInit", true, enabledSystems.Contains("ObjectPoolInit"));
            AssertEqual("系统预设应启用 TimerManager", true, enabledSystems.Contains("TimerManager"));
            AssertEqual("系统预设应启用 EntityManager", true, enabledSystems.Contains("EntityManager"));
            AssertEqual("系统预设应显式启用 TestSystem", true, enabledSystems.Contains("TestSystem"));
            AssertEqual("系统预设应显式启用 MouseSelectionSystem", true, enabledSystems.Contains("MouseSelectionSystem"));
        }

        private void TestCoreSystemDescriptorsRegistered()
        {
            AssertEqual("ObjectPoolInit 描述符应已注册", true, SystemRegistry.GetDescriptor("ObjectPoolInit") != null);
            AssertEqual("TimerManager 描述符应已注册", true, SystemRegistry.GetDescriptor("TimerManager") != null);
            AssertEqual("EntityManager 描述符应已注册", true, SystemRegistry.GetDescriptor("EntityManager") != null);
        }

        private void TestRequiredSystemCannotBeDisabledOrRemoved()
        {
            var manager = SystemManager.Instance;
            if (manager == null)
            {
                Fail("SystemManager.Instance 应存在");
                return;
            }

            var disableResult = manager.TrySetSystemEnabled(
                "ObjectPoolInit", // 必需系统 Id
                false, // 目标启用状态
                out var disableMessage);
            var removeResult = manager.TryRemoveSystem(
                "ObjectPoolInit", // 必需系统 Id
                out var removeMessage);

            AssertEqual("必需系统不允许禁用", false, disableResult);
            AssertEqual("必需系统禁用失败应返回中文原因", true, disableMessage.Contains("必需系统", StringComparison.Ordinal));
            AssertEqual("必需系统不允许移除", false, removeResult);
            AssertEqual("必需系统移除失败应返回中文原因", true, removeMessage.Contains("必需系统", StringComparison.Ordinal));
        }

        private void TestMissingSystemManagementReportsFailure()
        {
            var manager = SystemManager.Instance;
            if (manager == null)
            {
                Fail("SystemManager.Instance 应存在");
                return;
            }

            const string missingSystemId = "SystemCoreRuntimeTest.MissingSystem";
            var addResult = manager.TryAddSystem(
                missingSystemId, // 不存在的系统 Id
                out var addMessage);
            var enableResult = manager.TrySetSystemEnabled(
                missingSystemId, // 不存在的系统 Id
                true, // 目标启用状态
                out var enableMessage);
            var removeResult = manager.TryRemoveSystem(
                missingSystemId, // 不存在的系统 Id
                out var removeMessage);

            AssertEqual("缺失系统不允许添加", false, addResult);
            AssertEqual("缺失系统添加失败应返回中文原因", true, addMessage.Contains("未注册", StringComparison.Ordinal));
            AssertEqual("缺失系统不允许启用", false, enableResult);
            AssertEqual("缺失系统启用失败应返回中文原因", true, enableMessage.Contains("未加载", StringComparison.Ordinal));
            AssertEqual("缺失系统不允许移除", false, removeResult);
            AssertEqual("缺失系统移除失败应返回中文原因", true, removeMessage.Contains("未加载", StringComparison.Ordinal));
        }

        private void TestSystemCommandExecutionRespectsRunningState()
        {
            var manager = SystemManager.Instance;
            if (manager == null)
            {
                Fail("SystemManager.Instance 应存在");
                return;
            }

            manager.ProjectState.EnterFrontEnd();
            var blockedResult = manager.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(
                new DamageProcessRequest(null) // 伤害请求；本测试只验证门禁，不进入伤害处理
            );

            AssertEqual("外部系统命令应被非运行态阻断", false, blockedResult.Success);
            AssertEqual("阻断结果应带可读原因", true, !string.IsNullOrEmpty(blockedResult.Message));

            manager.ProjectState.BeginGameplaySession();
            var executedResult = manager.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(
                new DamageProcessRequest(null) // 伤害请求；运行态下应到达 DamageService 内部合法性检查
            );

            AssertEqual("运行态系统命令应允许进入系统处理器", true, executedResult.Success);
            AssertEqual("DamageService 应报告空伤害请求未处理", false, executedResult.Value.Processed);
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
            SystemRegistry.Register(systemId, static () => new object());
            SystemRegistry.Register(systemId, static () => new Node());

            var registered = SystemRegistry.GetDescriptor(systemId);
            AssertEqual("重复 SystemId 应保留首个注册工厂", typeof(object), registered?.Factory().GetType());
        }

        private void Pass(string message)
        {
            _passedCount++;
            _log.Info($"[PASS] {message}");
        }

        private void Fail(string message)
        {
            _failedCount++;
            _log.Error($"[FAIL] {message}");
        }

        private void AssertEqual<T>(string message, T expected, T actual)
        {
            if (Equals(expected, actual))
            {
                Pass(message);
            }
            else
            {
                Fail($"{message}: expected={expected}, actual={actual}");
            }
        }
    }
}
