# SDD-0029 Execution Prompt

## Role

你正在执行 `SDD-0029 System Contract Manifest And Diagnostics Hardening`。目标是把 Runtime System Core 补齐为 AI-first 可查、可诊断、可验证的合同层。

## Hard Boundary

- 不重写 `SystemManager` 生命周期。
- 不做 typed `SystemId` hard cutover。
- 不引入第三方 ECS runtime 或 scheduler。
- 不恢复 `SystemProfile`、旧四维 phase 或代码侧生命周期配置。
- 不绕过 DocsAI；实现阶段必须同步 `DocsAI/ECS/Runtime/System/`。
- 不直接改 `.codex/skills`、`.claude/skills`、`.windsurf/skills`。如需要新增 skill，只改 `.ai-config/skills/` 并运行 sync/lint。

## Reading Order

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `DocsAI/ECS/Runtime/System/README.md`
5. `DocsAI/ECS/Runtime/System/Usage.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/8.System优化/README.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/8.System优化/01-现状证据与AI-first裁决.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/8.System优化/02-目标架构与优化路线.md`
9. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/8.System优化/03-调用点迁移与验证计划.md`
10. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md`
11. `tasks.md`
12. `bdd.md`
13. `Core/progress.md`

## T1.1 Readiness Baseline

Run and record only the useful summary in `Core/progress.md`:

```bash
git rev-parse --show-toplevel
git status --short
rg -n "SystemRegistry\.Register|Register\(nameof\(|Register\(\"" Src/ECS Data -g '*.cs'
rg -n "SystemManager\.Instance|Resolve<|Execute<|TryAddSystem|TrySetSystemEnabled|CanExecute|ISystemCommandHandler" Src/ECS Data -g '*.cs'
jq -r '.records[] | select(.table=="system.config") | [.id, (.fields.SystemId.value // ""), (.fields.MountGroup.value // ""), (.fields.Tags.value // ""), (.fields.Required.value|tostring), (.fields.Priority.value|tostring), (.fields.Dependencies.value // "")] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
jq -r '.records[] | select(.table=="system.preset") | [.id, (.fields.PresetName.value // ""), (.fields.IsActive.value|tostring), (.fields.EnabledTags.value // ""), (.fields.EnabledSystemIds.value // ""), (.fields.DisabledSystemIds.value // "")] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
```

Do not clean unrelated dirty files. If existing dirty files touch the same area, read and work with them.

## Implementation Order

1. DocsAI SystemManifest first.
2. DocsAI README / Usage sync.
3. Runtime preflight report and tests.
4. Runtime diagnostics snapshot and blocked reason code.
5. TestSystem integration.
6. Lifecycle trace and JSON artifact dump.
7. Full validation and progress update.
8. Optional skill update only if the implementation introduces a new Runtime System owner skill.

## Required DocsAI Updates

At minimum:

- `DocsAI/ECS/Runtime/System/README.md`
- `DocsAI/ECS/Runtime/System/Usage.md`
- `DocsAI/ECS/Runtime/System/SystemManifest.md`

If behavior or public contract changes, update:

- `DocsAI/ECS/Runtime/System/Concept.md`
- any affected `Concepts/` history/current note.

## Validation

Required:

```bash
python3 Workspace/SDD/sdd.py validate SDD-0029
python3 Workspace/SDD/sdd.py validate --all
git diff --check -- DocsAI/ECS/Runtime/System SDD/project/projects/PRJ-0002-ecs-framework-refactor
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

Godot scene validation:

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

If the current runner or Godot CLI is unavailable, record the exact blocker in this SDD and do not mark the scene gate passed.

If `.ai-config/skills/` changes:

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

## Completion Criteria

- System manifest exists and routes every current system to owner/source/config/test.
- Preflight checks config/registry/preset/dependencies and has tests.
- Diagnostics snapshot serializes to JSON and contains stable reason code.
- TestSystem uses or adapts the diagnostics contract.
- DocsAI Runtime/System is current.
- SDD progress records validation evidence and next action.
