# Tasks

## Progress

- **Status**: done
- **Completed**: 10/10
- **Current**: done

## Task List

- [x] T1.1 Readiness baseline
  - 记录 workflow、must-read、git boundary、dirty baseline、Component Preset 调用点和当前实现范围。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0030`
- [x] T1.2 TDD RED: code composition contract
  - 在 `EntitySpawnPipelineRuntimeTest` 增加 composition provider 测试：profile 在 register 前创建组件、typed options 已注入、注册回调只执行一次。
  - **Validation**: 运行目标测试或 `dotnet build`，预期先因缺少 composition contract / API 失败。
- [x] T1.3 Runtime composition implementation
  - 新增 `IComponentCompositionProvider`、`ComponentCompositionProfile`、`ComponentCompositionEntry`、`ComponentComposer`，并接入 `EntitySpawnPipeline`。
  - **Validation**: T1.2 测试转绿。
- [x] T1.4 EntityOrientation typed options
  - 移除 `EntityOrientationComponent` 的 `[Export] Sink`，新增 `EntityOrientationComponentOptions` 与 `Configure`。
  - **Validation**: `rg -n "\\[Export\\]" Src/ECS/Capabilities/*/Component Src/ECS/Runtime/Component -g '*.cs'` 不命中 current component。
- [x] T1.5 Owner profiles
  - 让 `PlayerEntity`、`EnemyEntity`、`TargetingIndicatorEntity`、`AbilityEntity` 实现代码化 profile，复刻 UnitCore / Player / Enemy / Ability Preset 组件集合。
  - **Validation**: profile 组件集合测试或运行时 spawn 测试覆盖核心集合。
- [x] T1.6 Preset dependency cutover
  - 从对应 Entity `.tscn` 移除 Component Preset instance，保留 root scene、Component 容器和非 preset 组件。
  - **Validation**: `rg -n "Capabilities/.*/Presets/.*Preset\\.tscn" Src/ECS/Capabilities/*/Entity -g '*.tscn'` 不命中。
- [x] T1.7 Component manifest and DocsAI sync
  - 新增 `DocsAI/ECS/Runtime/Component/ComponentManifest.md`，更新 Component README / Concepts 必要入口。
  - **Validation**: `rg -n "ComponentManifest|ComponentComposer|IComponentCompositionProvider|SDD-0030" DocsAI/ECS/Runtime/Component`
- [x] T1.8 Owner skill sync
  - 更新 `.ai-config/skills/ecs/ecs-component/SKILL.md` 源并运行 AI config sync / skill-test。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` + `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [x] T1.9 Full validation
  - 运行构建、DataOS、SDD validate、grep gate；Godot runner 可用时补 ComponentRegistrar / EntitySpawnPipeline 场景。
  - **Validation**: 记录各命令结果；不可运行项说明原因。
- [x] T1.10 SDD closeout
  - 更新 PRJ-0002 roadmap/progress/README，完成 SDD progress/tasks/bdd，必要时 `done SDD-0030`。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0030`
