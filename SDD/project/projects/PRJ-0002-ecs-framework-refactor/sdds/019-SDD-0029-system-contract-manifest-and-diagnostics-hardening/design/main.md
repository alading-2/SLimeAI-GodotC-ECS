# System Contract Manifest And Diagnostics Hardening

## Goal

本 SDD 要把 Runtime System Core 从“人能读懂、日志可排查”推进到“AI 能通过 manifest / preflight / diagnostics / trace 自行定位和验证”。

保留现有模型：

- `SystemRegistry` 继续只注册 `SystemId + Factory`。
- `SystemConfigService` / `SystemPresetService` 继续只读 DataOS runtime snapshot。
- `SystemManager` 继续统一管理装载、启停、状态门禁和命令执行。
- `ProjectStateService` 继续使用三域状态：`GameFlowState + OverlayFlags + SimulationState`。

本 SDD 不做：

- 不重写 `SystemManager` 生命周期。
- 不引入 Bevy / Flecs / Unity DOTS scheduler API。
- 不做 typed `SystemId` hard cutover。
- 不恢复旧 `SystemProfile`、旧四维 phase 或代码侧生命周期配置。

## Context

项目级共享设计：

- `design/README.md`
- `01-现状证据与AI-first裁决.md`
- `02-目标架构与优化路线.md`
- `03-调用点迁移与验证计划.md`

当前事实源：

- DocsAI 入口：`DocsAI/ECS/Runtime/System/README.md`
- Runtime 源码：`Src/ECS/Runtime/System/`
- 系统配置：`Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config` / `system.preset`
- 调试展示：`Src/ECS/Capabilities/TestSystem/System/System/SystemInfoService.cs`
- 现有验证：`Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.cs`

已确认裁决：

- 当前 System 解耦方向正确，不做整体替换。
- 真实缺口是 AI-first 证据层：manifest、preflight、diagnostics、trace、DocsAI 同步和 scene artifact。
- DocsAI 文档必须随实现同步更新；不能只把任务写进 SDD。

## Design

### 1. SystemManifest

新增 AI 可读系统清单，默认落点：

```text
DocsAI/ECS/Runtime/System/SystemManifest.md
```

字段至少覆盖：

- `SystemId`
- owner
- source path
- `system.config` record id
- mount group / tags / required / autoload / start enabled / priority
- dependencies
- run condition
- command handlers
- tests
- risk notes

该 manifest 是文档和 AI 路由入口，不是运行时配置事实源。

### 2. SystemPreflight

新增可执行 preflight，检查：

- required system 必须有 config 和 descriptor。
- active preset 引用的 system id 必须存在 config。
- dependency 必须存在 config 和 descriptor。
- dependency graph 不允许 cycle。
- registered descriptor 无 config 时必须显式 test-only allow-list。
- `SystemId` / resource key / config record drift 输出 warning 或 error。

preflight 结果应能被测试断言，也能输出 artifact。

### 3. SystemDiagnosticsSnapshot

新增结构化 diagnostics snapshot，供 TestSystem、Godot validation 和 AI debug 共用。它只读 config / registry / runtime，不写配置。

建议字段：

- schema version
- project state
- active preset
- config / descriptor / loaded / running / blocked count
- 每个 system 的 registered / configured / loaded / enabled / stateAllowed / running
- stable blocked reason code + human message
- dependencies 和 custom stats

### 4. SystemLifecycleTrace

新增轻量 ring buffer 或等价 trace，记录：

- config/preset loaded
- descriptor registered
- bootstrap start/completed
- system add/remove/enable/disable/start/stop
- project state gate change
- command blocked/executed

默认不长期写文件；验证场景或 TestSystem 按需 dump JSON。

### 5. DocsAI 同步

必须更新：

- `DocsAI/ECS/Runtime/System/README.md`
- `DocsAI/ECS/Runtime/System/Usage.md`
- 新增 `DocsAI/ECS/Runtime/System/SystemManifest.md`
- 必要时更新 `DocsAI/ECS/Runtime/System/Concept.md`

如果实现阶段新增 Runtime System owner skill，则必须只改 `.ai-config/skills/` 源，并运行 sync/lint。

## Impact

主要影响区：

- `Src/ECS/Runtime/System/`
- `Src/ECS/Runtime/System/Tests/SystemCore/`
- `Src/ECS/Capabilities/TestSystem/System/System/`
- `DocsAI/ECS/Runtime/System/`
- `Data/DataOS/` validator 或 runtime snapshot projection 周边（仅在 preflight 需要时）
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`

不应影响：

- Ability / Damage / Movement 等 capability 业务逻辑。
- ObjectPool / Collision 当前 SDD-0028 已完成改动。
- typed DataKey / EntityId 主链路。

## Verification

文档和 SDD：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0029
python3 Workspace/SDD/sdd.py validate --all
git diff --check -- DocsAI/ECS/Runtime/System SDD/project/projects/PRJ-0002-ecs-framework-refactor
```

代码：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

Godot 场景：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果当前承载游戏 runner 或 Godot CLI 缺失，必须在 `Core/progress.md` 标为 blocked，不得把缺场景证据写成通过。
