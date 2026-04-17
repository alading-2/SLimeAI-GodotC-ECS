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
                TestSystemRegistryRejectsDuplicateIds();
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

        private void TestSystemRegistryRejectsDuplicateIds()
        {
            var systemId = $"SystemCoreRuntimeTest.{Guid.NewGuid():N}";
            var descriptor = new SystemDescriptor(systemId, SystemKind.PureService, SystemLifetime.Test)
            {
                Factory = static () => new object()
            };

            SystemRegistry.Register(descriptor);

            var duplicateThrown = false;
            try
            {
                SystemRegistry.Register(descriptor);
            }
            catch (InvalidOperationException)
            {
                duplicateThrown = true;
            }

            AssertEqual("重复 SystemId 应抛出异常", true, duplicateThrown);
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
