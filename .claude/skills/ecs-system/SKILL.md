---
name: ecs-system
description: 修改 SlimeAI ECS Runtime System Core、SystemManager、SystemRegistry、SystemPreflight、SystemDiagnosticsSnapshot、ProjectState、运行条件、系统配置或 SystemCore 验证时使用。
---

# Runtime System 入口

## 必读入口

- `DocsAI/ECS/Runtime/System/README.md`
- `DocsAI/ECS/Runtime/System/Usage.md`
- `DocsAI/ECS/Runtime/System/SystemManifest.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md`

## 源码位置

- `Src/ECS/Runtime/System/SystemManager.cs`
- `Src/ECS/Runtime/System/SystemManager_Management.cs`
- `Src/ECS/Runtime/System/SystemManager_Query.cs`
- `Src/ECS/Runtime/System/SystemRegistry.cs`
- `Src/ECS/Runtime/System/SystemDescriptor.cs`
- `Src/ECS/Runtime/System/Config/`
- `Src/ECS/Runtime/System/Lifecycle/`
- `Src/ECS/Runtime/System/State/`
- `Src/ECS/Runtime/System/Preflight/`
- `Src/ECS/Runtime/System/Diagnostics/`
- `Src/ECS/Runtime/System/Tests/SystemCore/`
- `Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config` / `system.preset`
- `Src/ECS/Capabilities/TestSystem/System/System/`（System Info diagnostics 展示适配）

## 规则

- `SystemManager` 是当前项目唯一 Godot autoload，负责系统装载、Host 分组、启停、项目状态门禁和 `Execute` command gate。
- `SystemRegistry` 只注册 `SystemId + Factory`；不要恢复 `SystemProfile`、代码侧生命周期配置、父节点路径、run condition 或默认装载字段。
- 系统配置事实源是 DataOS runtime snapshot：`system.config` / `system.preset`。新增或修改系统配置先改 DataOS authoring，再生成 snapshot。
- 系统运行裁决固定为 `shouldRun = IsEnabled && IsStateAllowed`。命令入口必须走 `SystemManager.Execute<TSystem, TRequest, TResult>`，不要绕过 owner command handler 直接调用系统实例。
- `ProjectStateService` 使用三域状态：`GameFlowState + OverlayFlags + SimulationState`。系统观察状态变化实现 `ISystem.OnProjectStateChanged`，不要直接散订阅全局状态。
- `SystemPreflight` 只读 config / registry / preset，检查 required descriptor、active preset、dependencies、cycle 和 descriptor-only allow-list；不要让 preflight 写回配置。
- `SystemDiagnosticsSnapshot` 只读 config / registry / runtime / ProjectState；TestSystem、Godot validation 和 AI debug 应共享这个合同，不要新增第二套展示事实源。
- 自动验证和 BDD 优先断言 `SystemBlockedReasonCode`，中文 `blockedReason` 只作为日志和 UI 文案。
- `SystemLifecycleTrace` 是轻量 ring buffer，不默认长期写文件；场景验证按需 dump 到 `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json`。
- System preflight / diagnostics summary 使用 `owner=System`、`operation=SystemPreflight|SystemDiagnosticsSnapshot`；关键字段包括 config/descriptor/loaded/running/blocked/disabled/preflight error/warning counts。
- 自动验证和 AI 分析先查 `SystemPreflightIssue.RuleId`、`SystemPreflightSeverity`、`SystemBlockedReasonCode` 和 structured fields，不把中文 `blockedReason` 当机器事实源。
- 修改 Runtime System 实现、接口、生命周期、配置、preflight、diagnostics、trace 或验证方式时，必须同步 `DocsAI/ECS/Runtime/System/` 和本 skill 源。
- 不做 typed `SystemId` hard cutover，不引入第三方 ECS scheduler，不恢复旧四维 phase；这些属于单独设计范围。

## 验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0029
git diff --check -- DocsAI/ECS/Runtime/System Src/ECS/Runtime/System SDD/project/projects/PRJ-0002-ecs-framework-refactor
```

Godot 场景验证需要进入承载游戏仓：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果 runner 或 Godot CLI 不可用，记录具体 blocker，不声明 scene gate passed。
