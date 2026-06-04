using System;
using System.Collections.Generic;

/// <summary>
/// System diagnostics 路由元数据。
/// <para>只用于 AI 诊断和 TestSystem 展示，不参与系统装载、运行条件或配置裁决。</para>
/// </summary>
public static class SystemDiagnosticsMetadata
{
    private static readonly Dictionary<string, SystemRouteMetadata> _routes = new(StringComparer.Ordinal)
    {
        ["ObjectPoolInit"] = new("Tools/ObjectPool", "Src/ECS/Tools/ObjectPool/Management/ObjectPoolInit.cs", "system.object_pool_init"),
        ["TimerManager"] = new("Tools/Timer", "Src/ECS/Tools/Timer/TimerManager.cs", "system.timer_manager"),
        ["ProjectStateBridge"] = new("Runtime/System", "Src/ECS/Runtime/System/State/ProjectStateBridge.cs", "system.project_state_bridge"),
        ["EntityManager"] = new("Runtime/Entity", "Src/ECS/Runtime/Entity/Components/EntityManager_Component_Init.cs", "system.entity_manager"),
        ["DamageService"] = new("Damage", "Src/ECS/Capabilities/Damage/System/DamageService.cs", "system.damage_service"),
        ["DamageStatisticsSystem"] = new("Damage", "Src/ECS/Capabilities/Damage/System/DamageStatisticsSystem.cs", "system.damage_statistics"),
        ["RecoverySystem"] = new("Unit", "Src/ECS/Capabilities/Unit/System/RecoverySystem/RecoverySystem.cs", "system.recovery"),
        ["SpawnSystem"] = new("Spawn", "Src/ECS/Capabilities/Spawn/System/SpawnSystem.cs", "system.spawn"),
        ["TargetingManagerRuntime"] = new("Ability/Targeting", "Src/ECS/Capabilities/Ability/System/TargetingSystem/TargetingManagerRuntime.cs", "system.targeting_manager"),
        ["PauseMenuSystem"] = new("UI", "Src/ECS/UI/PauseMenu/PauseMenuSystem.cs", "system.pause_menu"),
        ["UIManager"] = new("UI", "Src/ECS/UI/Core/UIManager.cs", "system.ui_manager"),
        ["DamageNumberRuntimeBridge"] = new("UI", "Src/ECS/UI/UI/DamageNumberUI/DamageNumberRuntimeBridge.cs", "system.damage_number_bridge"),
        ["TestSystem"] = new("TestSystem", "Src/ECS/Capabilities/TestSystem/System/TestSystem.cs", "system.test"),
        ["MouseSelectionSystem"] = new("Tools/Input", "Src/ECS/Tools/Input/MouseSelection/MouseSelectionSystem.cs", "system.mouse_selection")
    };

    public static SystemRouteMetadata Resolve(string systemId)
    {
        return _routes.TryGetValue(systemId, out var route)
            ? route
            : new SystemRouteMetadata(string.Empty, string.Empty, string.Empty);
    }
}

/// <summary>
/// 单个 System 的 AI 路由元数据。
/// </summary>
public sealed record SystemRouteMetadata(
    string Owner,
    string SourcePath,
    string ConfigRecordId);
