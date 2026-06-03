# Runtime System Manifest

> 状态：current
> 更新：2026-06-03
> 范围：当前 `Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config` / `system.preset` 与 `SystemRegistry` 常规注册点。

## 定位

本 manifest 是 AI-first 路由入口，帮助 AI 直接定位 System owner、源码、配置、运行条件、命令和测试。

它不是运行时配置事实源；运行时配置仍只来自 DataOS runtime snapshot，代码注册仍只保存 `SystemId + Factory`。

## 当前 Preset

| Preset | Active | EnabledTags | EnabledSystemIds | DisabledSystemIds |
| --- | --- | --- | --- | --- |
| `Default` | true | `Core, Gameplay, Combat, UI, Roguelike, Runtime` | `TestSystem`, `MouseSelectionSystem` | - |

## AI 使用规则

1. 修改具体业务系统时，先按 `Owner` 进入对应 `DocsAI/ECS/Capabilities/`、`DocsAI/ECS/Tools/` 或 `DocsAI/ECS/UI/`。
2. 修改注册、装载、运行条件、preflight、diagnostics、trace 或 `SystemManager.Execute` 时，进入 `DocsAI/ECS/Runtime/System/` 和 `Src/ECS/Runtime/System/`。
3. 不要把本 manifest 当成 DataOS 配置源；新增或修改配置先改 DataOS authoring，再生成 runtime snapshot。
4. 新增系统必须同步：DataOS `system.config`、`SystemRegistry.Register`、本 manifest、SystemPreflight 覆盖、相关 owner DocsAI 和测试入口。

## 系统清单

| SystemId | Owner | Source | Config | Group | Tags | Required | AutoLoad | StartEnabled | Priority | Dependencies | RunCondition | CommandHandlers | Tests | Risk Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `ObjectPoolInit` | Tools/ObjectPool | `Src/ECS/Tools/ObjectPool/Management/ObjectPoolInit.cs` | `system.object_pool_init` | Base | Core, Runtime | true | true | true | 0 | - | unrestricted | - | `Src/ECS/Tools/ObjectPool/Tests/` | Required，不能被 preset 或 TestSystem 禁用/移除；对象池行为改动需跑 ObjectPool 专项验证。 |
| `TimerManager` | Tools/Timer | `Src/ECS/Tools/Timer/TimerManager.cs` | `system.timer_manager` | Base | Core, Runtime | true | true | true | 1 | - | unrestricted | - | `Src/ECS/Tools/Timer/Tests/` | Required，全局 timer 服务；不要用 Godot timer 绕过。 |
| `ProjectStateBridge` | Runtime/System | `Src/ECS/Runtime/System/State/ProjectStateBridge.cs` | `system.project_state_bridge` | Base | Core, Runtime | true | true | true | 2 | - | unrestricted | - | `Src/ECS/Runtime/System/Tests/SystemCore/` | Required，负责把全局流程事件同步到 `ProjectStateService`；不要改成全局状态散写。 |
| `EntityManager` | Runtime/Entity | `Src/ECS/Runtime/Entity/Components/EntityManager_Component_Init.cs` | `system.entity_manager` | Base | Core, Runtime | true | true | true | 5 | - | unrestricted | - | `Src/ECS/Runtime/Entity/Tests/` | Required，实体生命周期入口；实体创建/销毁必须走 EntityManager 协议。 |
| `DamageService` | Damage | `Src/ECS/Capabilities/Damage/System/DamageService.cs` | `system.damage_service` | Combat | Core, Combat, Runtime | false | true | true | 10 | - | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | `DamageProcessRequest -> DamageProcessResult` | `Src/ECS/Capabilities/Damage/Tests/DamageSystemTest.cs`、`Src/ECS/Runtime/System/Tests/SystemCore/` | 命令必须经 `SystemManager.Execute`，前台或暂停态应返回 `FlowStateMismatch` / overlay / simulation reason code。 |
| `DamageStatisticsSystem` | Damage | `Src/ECS/Capabilities/Damage/System/DamageStatisticsSystem.cs` | `system.damage_statistics` | Combat | Core, Combat, Runtime | false | true | true | 11 | `DamageService` | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | - | Damage tests / SystemCore preflight | 依赖 `DamageService`；preflight 必须检查依赖 config + descriptor。 |
| `RecoverySystem` | Unit | `Src/ECS/Capabilities/Unit/System/RecoverySystem/RecoverySystem.cs` | `system.recovery` | Combat | Core, Combat, Runtime | false | true | true | 12 | - | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | `RecoveryRegisterRequest -> RecoveryRegistrationResult`; `RecoveryUnregisterRequest -> RecoveryRegistrationResult` | Unit / SystemCore smoke | RecoveryComponent 注册/注销必须走 `SystemManager.Execute`。 |
| `SpawnSystem` | Spawn | `Src/ECS/Capabilities/Spawn/System/SpawnSystem.cs` | `system.spawn` | Gameplay | Gameplay, Runtime | false | true | true | 13 | - | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | `SpawnBatchRequest -> SpawnBatchResult`; `StartWaveRequest -> StartWaveResult`; `KillAllEnemiesRequest -> KillAllEnemiesResult` | `Src/ECS/Capabilities/Spawn/Tests/`、TestSystem Spawn module | 局内流程系统；不要在 FrontEnd 直接执行生成命令。 |
| `TargetingManagerRuntime` | Ability/Targeting | `Src/ECS/Capabilities/Ability/System/TargetingSystem/TargetingManagerRuntime.cs` | `system.targeting_manager` | Combat | Core, Combat, Runtime | false | true | true | 14 | - | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | - | Ability / Targeting tests | 目标查询应走 Targeting/TargetSelector 协议，不手写全局距离扫描。 |
| `PauseMenuSystem` | UI | `Src/ECS/UI/PauseMenu/PauseMenuSystem.cs` | `system.pause_menu` | UI | UI, Runtime | false | true | true | 20 | - | Flow=`SessionPlaying`, Simulation=`Any` | - | UI / SystemCore smoke | 可在暂停模拟时运行；访问项目状态应经 `SystemManager.ProjectState`。 |
| `UIManager` | UI | `Src/ECS/UI/Core/UIManager.cs` | `system.ui_manager` | UI | Core, UI, Runtime | false | true | true | 21 | - | unrestricted | - | UI smoke | UI runtime 入口；不要让 UI 变成 Entity Component。 |
| `DamageNumberRuntimeBridge` | UI | `Src/ECS/UI/UI/DamageNumberUI/DamageNumberRuntimeBridge.cs` | `system.damage_number_bridge` | UI | Combat, UI, Runtime | false | true | true | 22 | - | Flow=`SessionPlaying`, BlockedOverlays=`Blocking`, Simulation=`Running` | - | UI / Damage smoke | 监听伤害事件显示 UI；运行条件应与战斗模拟一致。 |
| `TestSystem` | TestSystem | `Src/ECS/Capabilities/TestSystem/System/TestSystem.cs` | `system.test` | Test | Debug, Test | false | false | true | 100 | - | unrestricted | - | `Src/ECS/Capabilities/TestSystem/System/` | 仅通过 Default preset 显式启用；System Info 模块读取 `SystemDiagnosticsSnapshot`。 |
| `MouseSelectionSystem` | Tools/Input | `Src/ECS/Tools/Input/MouseSelection/MouseSelectionSystem.cs` | `system.mouse_selection` | Debug | Debug, Test | false | false | true | 101 | - | unrestricted | - | MouseSelection / TestSystem | Debug/Test 系统，仅通过 Default preset 显式启用；不要被 Debug tag 自动常驻策略误加载。 |

## Contract Layer

| 合同 | 源码 | 说明 |
| --- | --- | --- |
| `SystemPreflight` | `Src/ECS/Runtime/System/Preflight/` | 检查 config、registry、preset、dependencies、cycle 和 descriptor-only allow-list。 |
| `SystemDiagnosticsSnapshot` | `Src/ECS/Runtime/System/Diagnostics/` | 合并 config / registry / runtime / ProjectState，输出 stable `blockedReasonCode`。 |
| `SystemLifecycleTrace` | `Src/ECS/Runtime/System/Diagnostics/SystemLifecycleTrace.cs` | 轻量 ring buffer，记录 bootstrap、add/remove、enable/disable、state gate 和 command events。 |
| `SystemCoreRuntimeTest` artifact | `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json` | Godot 场景验证输出的 JSON artifact，包含标准答案字段和 diagnostics snapshot。 |

## 当前 Preflight 规则

| Rule | 级别 | 含义 |
| --- | --- | --- |
| `SYS-PF-001` | error | `system.config` 必须有非空 `SystemId` 和 `Description`。 |
| `SYS-PF-002` | error | Required system 必须有 `SystemRegistry` descriptor。 |
| `SYS-PF-003` | error | active preset `EnabledSystemIds` 必须存在 config。 |
| `SYS-PF-004` | error | active preset 不允许禁用 Required system。 |
| `SYS-PF-005` | error | dependency 必须存在 config 和 descriptor。 |
| `SYS-PF-006` | error | dependency graph 不允许 cycle。 |
| `SYS-PF-007` | warning/error | descriptor-only system 必须显式进入 test-only allow-list；默认 warning。 |
| `SYS-PF-010` | warning | priority 冲突只警告，排序使用 SystemId 稳定补充。 |

## 稳定 Blocked Reason Code

`SystemManager.Execute`、`SystemDiagnosticsSnapshot` 和 TestSystem System Info 共享以下稳定 reason code：

```text
None
Disabled
FlowStateMismatch
MissingRequiredOverlay
BlockedOverlay
SimulationStateMismatch
NotLoaded
NotRunning
NotRegistered
MissingConfig
DependencyMissing
CommandTargetMissing
Unknown
```

中文 `blockedReason` 只作为人类说明；自动验证和 BDD 应优先断言 `blockedReasonCode`。
