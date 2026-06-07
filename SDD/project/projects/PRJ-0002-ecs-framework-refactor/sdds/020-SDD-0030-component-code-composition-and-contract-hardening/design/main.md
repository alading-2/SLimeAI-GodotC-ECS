# Component Code Composition And Contract Hardening

## Goal

把 Component 默认组合从 `.tscn` Preset 迁到 C# profile / composer，并补齐 Runtime Component 的 AI-first 合同入口。

本 SDD 完成后：

- `PlayerEntity`、`EnemyEntity`、`TargetingIndicatorEntity`、`AbilityEntity` 的默认 Component 组合由代码 profile 提供，不再依赖 `UnitCorePreset`、`PlayerPreset`、`EnemyPreset`、`AbilityPreset` 场景实例。
- `EntitySpawnPipeline` 在 `ComponentRegistrar.RegisterComponents` 前调用代码化组合，确保 typed options 可先于 `OnComponentRegistered` 注入。
- `EntityOrientationComponent.Sink` 改为 `EntityOrientationComponentOptions` 注入，不再使用 `[Export]` / Inspector 默认参数。
- `DocsAI/ECS/Runtime/Component` 提供 Component manifest 和当前组合入口说明。
- `.ai-config/skills/ecs/ecs-component/SKILL.md` 同步最新代码化组合规则，并通过 sync / skill-test 验证。

## Context

### Selected Workflow

- Workflow: `NewFeature`
- Task size: large
- SDD strategy: 使用项目子 SDD `SDD-0030`
- Owner skill: `ecs-component`
- Git Boundary: `/home/slime/Code/SlimeAI/SlimeAI`
- Worktree: none；用户直接要求在当前仓推进，且当前 dirty workspace 中存在大量既有 `.uid` 删除和 `__pycache__`，本轮只在当前边界内追加 Component/SDD/DocsAI/skill 改动，不清理无关状态。
- Submodule Boundary: 不涉及游戏仓 submodule 指针；Godot 场景验证如需运行，进入承载游戏仓只执行验证脚本，不改游戏仓业务代码。

### Must Read Status

已读：

- `Workspace/SystemAgent/README.md`
- `Workspace/SystemAgent/Routes/NewFeature.md`
- `Workspace/SystemAgent/Actors/Planner.md`
- `Workspace/SystemAgent/Actors/TestDesigner.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `Workspace/SDD/README.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`
- `Workspace/SDD/docs/ValidationRules.md`
- `DocsAI/README.md`
- `DocsAI/ECS/README.md`
- `DocsAI/ECS/Runtime/Component/README.md`
- `.codex/skills/ecs-component/SKILL.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/*`
- `Src/ECS/Runtime/Component/IComponent.cs`
- `Src/ECS/Runtime/Component/TemplateComponent.cs`
- `Src/ECS/Runtime/Entity/Components/ComponentRegistrar.cs`
- `Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs`
- `Src/ECS/Runtime/Entity/Tests/ComponentRegistrarRuntimeTest.cs`
- `Src/ECS/Runtime/Entity/Tests/EntitySpawnPipelineRuntimeTest.cs`
- 当前 Unit / Ability Entity 场景和 Component Preset 场景

### Current Facts

- Runtime Component 最小契约是 `IComponent.OnComponentRegistered(Node)` / `OnComponentUnregistered()`。
- `ComponentRegistrar` 只维护内部 owner index，不恢复 `ENTITY_TO_COMPONENT` relationship 图。
- `EntitySpawnPipeline` 当前顺序是：create node -> AddToSceneTree -> Data apply -> visual -> transform -> registry -> component register -> lifecycle -> activate。
- 当前 Entity `.tscn` 仍 instance Component Preset：
  - `PlayerEntity.tscn`: `UnitCorePreset` + `PlayerPreset` + `ContactDamageComponent`
  - `EnemyEntity.tscn`: `EnemyPreset` + `UnitCorePreset`
  - `TargetingIndicatorEntity.tscn`: `UnitCorePreset` + `TargetingIndicatorControlComponent`
  - `AbilityEntity.tscn`: `AbilityPreset`
- `UnitCorePreset.tscn` 中 `EntityOrientationComponent.Sink = 1` 是唯一 Inspector override，目标改为 typed options 注入。
- `ActiveSkillInputTest` 和 Ability pipeline tests 仍手工 `EntityManager.AddComponent`；这类测试动态组件可保留，不作为默认组合事实源。

## Design

### Runtime Composition Contract

新增 Runtime 通用组合契约：

```text
IComponentCompositionProvider
  GetComponentCompositionProfile()

ComponentCompositionProfile
  Entries: ComponentCompositionEntry[]

ComponentCompositionEntry
  NodeName
  CreateNode()
  Configure(Node component)

ComponentComposer.Compose(entity, profile)
  -> ensure Component container
  -> skip existing child with same NodeName
  -> create node
  -> configure typed options
  -> AddChild to container
```

Runtime 只认识通用 contract，不引用 Unit / Ability 具体组件类型。具体 profile 由对应 Entity 类提供：

- `PlayerEntity`: UnitCore + Player profile
- `EnemyEntity`: Enemy + UnitCore profile
- `TargetingIndicatorEntity`: UnitCore profile；现有 `TargetingIndicatorControlComponent` 仍保留在 Entity root scene
- `AbilityEntity`: Ability profile

`EntitySpawnPipeline` 在 registry register 之后、`ComponentRegistrar.RegisterComponents` 之前调用 `ComponentComposer.Compose`。这样组件结构参数已经注入，`OnComponentRegistered` 仍是唯一 Entity/Data/Event 初始化入口。

### Preset Cutover

本轮停止 Entity root scene 依赖 Component Preset instance：

- 移除 Entity `.tscn` 里 `UnitCorePreset`、`PlayerPreset`、`EnemyPreset`、`AbilityPreset` 的 `ext_resource` 和节点 instance。
- 保留 Entity root `.tscn`、`Component` 容器、`ContactDamageComponent`、`TargetingIndicatorControlComponent`、碰撞 shape、Camera 等非 Component Preset 节点。
- 暂不删除旧 Preset 文件和 `ResourceCategory.Preset` 记录；它们作为 legacy 对照输入保留到后续清理切片，避免 ResourceGenerator / 资源目录一次性漂移。

### Options Boundary

`EntityOrientationComponent.Sink` 改为：

```text
EntityOrientationComponentOptions(OrientationSink Sink)
EntityOrientationComponent.Configure(EntityOrientationComponentOptions)
```

默认 `RootRotation`，UnitCore profile 显式注入 `VisualFlipX`。`Sink` 不进入 DataOS，不新增 DataKey。

### Manifest / Skill

新增 `DocsAI/ECS/Runtime/Component/ComponentManifest.md`，内容包含：

- 当前组件 owner、源码、Node 类型、Process hook、订阅/Timer/Godot signal 风险。
- 当前代码化 profile 的组件集合。
- `GetComponent<T>()` 当前 documented exception。
- 旧 Preset 的 legacy 状态和后续清理条件。

更新 `.ai-config/skills/ecs/ecs-component/SKILL.md` 源，再运行 sync 和 skill-test。

### Out of Scope

- 不重写 `IComponent` 方法签名。
- 不把 Component 改成纯数据 ECS storage。
- 不删除全部 Component `.tscn` 资源；本轮只删除 Entity root 对 Component Preset 的依赖。
- 不合并 `EntityManager.Destroy` 与 `EntityDestroyPipeline`，该项保留为后续 SDD。
- 不迁移 `TargetingManager` / `HealthExecutionProcessor` 的 `GetComponent<T>()` 调用，只在 manifest 中标注例外。

## Verification

### Owner Verification

- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`
- `python3 Workspace/SDD/sdd.py validate SDD-0030`
- `python3 Workspace/SDD/sdd.py validate --all`

### Contract Checks

- TDD RED/GREEN：`EntitySpawnPipelineRuntimeTest` 覆盖 composition 在 register 前执行、typed options 注入、profile 组件集合。
- Grep gate：
  - `rg -n "\\[Export\\]" Src/ECS/Capabilities/*/Component Src/ECS/Runtime/Component -g '*.cs'`
  - `rg -n "Capabilities/.*/Presets/.*Preset\\.tscn" Src/ECS/Capabilities/*/Entity -g '*.tscn'`
  - `rg -n "EntityRelationshipType\\.ENTITY_TO_COMPONENT|ENTITY_TO_COMPONENT" Src/ECS/Runtime/Component Src/ECS/Runtime/Entity/Components DocsAI/ECS/Runtime/Component -g '*.cs' -g '*.md'`

### Config / Docs Verification

- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

### Godot Scene Verification

若承载游戏 runner / Godot CLI 可用：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/ECS/Runtime/Entity/Tests/EntitySpawnPipelineRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Src/ECS/Runtime/Entity/Tests/ComponentRegistrarRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果 Godot runner 不可用，必须在 `Core/progress.md` 记录缺口，不用 smoke 替代专项验收。
