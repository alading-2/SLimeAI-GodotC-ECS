# Progress

## Latest Resume

- **Updated**: 2026-05-31 16:15
- **Current Task**: T1.5
- **Last Conclusion**: T1.1~T1.4 已完成。已新增 `EntityId`、`EntityRegistry`、`LifecycleLink`、`LifecycleTree` 及两个 SingleTest scene 测试，按 RED/GREEN 验证 typed identity、registry 双向索引、single-parent lifecycle、cycle guard、typed destroy policy、detach 和 snapshot 行为。最新编译证据：`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过；Godot scene smoke 未运行，因为当前 shell 无 `godot` 命令。
- **Next Action**: 从 T1.5 开始写 `EntityDestroyPipeline` RED tests，覆盖 recursive child destroy、detach child 存活、children snapshot、防重复 destroy result、owner cleanup 顺序和 component unregister 在 Data clear 前；旧 Relationship runtime 暂不删除，等 destroy/spawn/component 切片接入后再做 hard cutover 删除。
- **Open Blockers**: none
## Timeline

### P001 — 2026-05-31 15:52 — resume

- **Context**: 创建 SDD。
- **Conclusion**: 已建立任务上下文胶囊。
- **Evidence**: README、sdd.json、design、tasks、progress、bdd、notes 已生成。
- **Impact**: 后续围绕 tasks.md 和 progress.md 记录执行。
- **Resume**: 从 README 的 Current Resume 继续。

### P002 — 2026-05-31 — execution-plan-expanded

- **Context**: 用户要求生成 Entity hard cutover 执行型 SDD，而不是只保留项目设计包。
- **Conclusion**: 已将 SDD-0024 扩展为可执行 TDD 任务序：readiness baseline、EntityId/Registry、LifecycleTree、DestroyPipeline、ComponentRegistrar、SpawnPipeline、OwnedReferenceRegistry、Ability owner service、Projectile/Effect/Damage/Movement 迁移、DocsAI 和最终验证。
- **Evidence**: `README.md`、`design/main.md`、`tasks.md`、`bdd.md` 已更新。
- **Impact**: 后续实现者可以直接从本 SDD 恢复，不需要重新解释 2026-05-31 Data/Event/DocsAI 校准。
- **Resume**: 先执行 T1.1 baseline，不要直接删除 runtime 文件；每个实现切片先写 RED tests。

### P003 — 2026-05-31 15:55 — resume

- **Context**: 启动或恢复 SDD。
- **Conclusion**: SDD 已进入 active。
- **Evidence**: start command
- **Impact**: 任务可以继续推进。
- **Resume**: 按 tasks.md 的 Current 继续。

### P004 — 2026-05-31 16:07 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.1。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P005 — 2026-05-31 16:08 — finding

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.1 readiness baseline completed. Git boundary /home/slime/Code/SlimeAI/SlimeAI; worktree already dirty before this execution with Data *.uid deletions, Entity DocsAI/design edits, SDD-0024 untracked directory and pycache noise, so this run preserves existing changes. Gates run from framework root: old Relationship/DataKey.Id/InstanceId gate has many expected baseline hits across Src/ECS runtime/tests and DocsAI; EventData/EventName/EntitySpawned gate has expected hits in old DocsAI examples, Data/EventType/Global/GameEventType_Global_Entity.cs and EntityManager/HealthBarUI runtime; DocsNew/SlimeAI path gate mainly hits historical SDDs, migrated-from comments and DocsAI migration catalog. SDD validation command: python3 Workspace/SDD/sdd.py validate SDD-0024 => 0 error / 0 warning. Next action: T1.2 RED tests for EntityId and EntityRegistry.
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.1 readiness baseline completed. Git boundary /home/slime/Code/SlimeAI/SlimeAI; worktree already dirty before this execution with Data *.uid deletions, Entity DocsAI/design edits, SDD-0024 untracked directory and pycache noise, so this run preserves existing changes. Gates run from framework root: old Relationship/DataKey.Id/InstanceId gate has many expected baseline hits across Src/ECS runtime/tests and DocsAI; EventData/EventName/EntitySpawned gate has expected hits in old DocsAI examples, Data/EventType/Global/GameEventType_Global_Entity.cs and EntityManager/HealthBarUI runtime; DocsNew/SlimeAI path gate mainly hits historical SDDs, migrated-from comments and DocsAI migration catalog. SDD validation command: python3 Workspace/SDD/sdd.py validate SDD-0024 => 0 error / 0 warning. Next action: T1.2 RED tests for EntityId and EntityRegistry.

### P006 — 2026-05-31 16:11 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.3。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P007 — 2026-05-31 16:11 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.2。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P008 — 2026-05-31 16:11 — validation

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.2/T1.3 EntityId and EntityRegistry slice completed. RED: added Src/ECS/Test/SingleTest/ECS/Entity/EntityIdentityRuntimeTest.cs and dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly failed with CS0103/CS0246 for missing EntityId and EntityRegistry, confirming the test targeted absent runtime API. GREEN: added Src/ECS/Base/Entity/Core/EntityId.cs and EntityRegistry.cs plus EntityIdentityRuntimeTest.tscn; reran dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly => Build succeeded, 742 warnings, 0 errors. SDD validate currently reports 0 errors / 1 weak-latest-resume warning before resume compression. Tools/run-build.sh and Tools/run-tests.sh are absent in this repository checkout, so this slice used dotnet build as compile validation.
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.2/T1.3 EntityId and EntityRegistry slice completed. RED: added Src/ECS/Test/SingleTest/ECS/Entity/EntityIdentityRuntimeTest.cs and dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly failed with CS0103/CS0246 for missing EntityId and EntityRegistry, confirming the test targeted absent runtime API. GREEN: added Src/ECS/Base/Entity/Core/EntityId.cs and EntityRegistry.cs plus EntityIdentityRuntimeTest.tscn; reran dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly => Build succeeded, 742 warnings, 0 errors. SDD validate currently reports 0 errors / 1 weak-latest-resume warning before resume compression. Tools/run-build.sh and Tools/run-tests.sh are absent in this repository checkout, so this slice used dotnet build as compile validation.

### P009 — 2026-05-31 16:15 — change

- **Context**: 更新任务状态。
- **Conclusion**: 已完成任务 T1.4。
- **Evidence**: task command
- **Impact**: 任务进度已同步。
- **Resume**: 继续处理下一个未完成任务。

### P010 — 2026-05-31 16:15 — validation

- **Context**: 用户或 AI 追加记录。
- **Conclusion**: T1.4 LifecycleTree slice completed. RED: added Src/ECS/Test/SingleTest/ECS/Entity/LifecycleTreeRuntimeTest.cs and dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly failed with CS0246 for missing LifecycleTree. GREEN: added LifecycleLink.cs and LifecycleTree.cs; reran dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly => Build succeeded, 748 warnings, 0 errors. Godot scene smoke for EntityIdentityRuntimeTest.tscn and LifecycleTreeRuntimeTest.tscn could not run because godot is not installed in this shell (command not found).
- **Evidence**: note command
- **Impact**: 作为后续恢复上下文。
- **Resume**: T1.4 LifecycleTree slice completed. RED: added Src/ECS/Test/SingleTest/ECS/Entity/LifecycleTreeRuntimeTest.cs and dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly failed with CS0246 for missing LifecycleTree. GREEN: added LifecycleLink.cs and LifecycleTree.cs; reran dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly => Build succeeded, 748 warnings, 0 errors. Godot scene smoke for EntityIdentityRuntimeTest.tscn and LifecycleTreeRuntimeTest.tscn could not run because godot is not installed in this shell (command not found).
