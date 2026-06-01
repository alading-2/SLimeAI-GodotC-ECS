# One-Shot Execution Prompt: Complete SDD-0022 Data Projection Diagnostics Contract Hardening

把下面整段作为新会话的一次性执行提示词使用。目标是完成 SDD-0022 的全部任务，把 Data no-compat 后暴露的中层软契约硬化为可验证的 projection、diagnostics、record completeness、spawn boundary 和 docs gate。

```text
你在 /home/slime/Code/SlimeAI 工作区内工作。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行；改文件前先读相关文件；改完总结改动和验证结果。不要 push。不要随意加依赖、大重构或跨 git 边界混提交。

任务目标：
完成 SDD-0022 Data Projection Diagnostics Contract Hardening。不要把这个任务理解成“再清兼容入口”；SDD-0021 已完成旧兼容大头删除。当前目标是把 Data 中层契约硬化，让 DataOS descriptor-first 链路能端到端驱动玩家移动、敌人移动和技能创建/触发，并在失败时给 AI 可定位的结构化诊断。

必须先读：
1. /home/slime/Code/SlimeAI/AGENTS.md
2. /home/slime/Code/SlimeAI/Workspace/SystemAgent/README.md
3. /home/slime/Code/SlimeAI/SDD/INDEX.md
4. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
5. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md
6. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md
7. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/README.md
8. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/main.md
9. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md
10. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/04-BUG:Data无兼容重构后移动与施法失败根因说明.md
11. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/05-Data残余问题代码修复分解.md
12. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/06-Data文档更新与门禁清单.md
13. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/tasks.md
14. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/bdd.md
15. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/progress.md

Git 边界：
- SDD 文档在 /home/slime/Code/SlimeAI 根仓。
- 源码实现主要在 /home/slime/Code/SlimeAI/SlimeAI 框架仓。
- 每次 git status、git diff、commit 前必须 cd 到对应仓库目录确认边界。
- 工作区可能已有无关脏改，必须保留并避免混入。
- 不要修改 Games/BrotatoLike/SlimeAI submodule 镜像。
- 默认不要 push。

核心裁决：
- 不恢复 PlayerEntity / EnemyEntity 旧兜底作为长期方案。
- 不放宽 RuntimeDataSnapshotLoader 类型校验。
- 不让 generator 和 descriptor 继续各自表达字段类型。
- 不让 OnPoolAcquire 写入组件注册期配置。
- 不让 EntitySpawnConfig.Config 继续承载 Data 字段。
- 不把 display name 当生产运行时稳定 identity。
- 不恢复 SlimeAI/DocsAI；当前文档事实源是 Src/ECS/Base 旁文档、DocsNew 和 SDD design。
- 可以保留必要底层 helper，但必须命名和边界表达 loader/test/debug 用途。

实施顺序必须按 SDD-0022 tasks.md：

1. T1.1 readiness baseline
   - 记录 `unit.player` / `unit.enemy` records 是否有 `DefaultMoveMode`。
   - 记录 `generate-runtime-snapshot.sh` 的 `field_rows` 是否仍手写 value_type。
   - 记录 `RuntimeDataRecordQuery.cs` 的裸 stable key projection。
   - 记录 `DataRuntimeStorage` / `Data.cs` 写入失败是否仍只返回 bool。
   - 记录 `object_ref` 当前同时表达 ResourceRef 和 Godot Node/Node2D 的证据。
   - 记录 `DataDefinitionCatalog.Register()` 是否 build 后仍可调用。
   - 记录 `GetRequiredByName()` 生产调用点。
   - 记录 current docs 旧入口命中。
   - 写入 SDD-0022 progress.md。

2. T1.2 Movement 注册期字段前移
   - DataOS authoring / generator 为 `unit.player` 输出 `DefaultMoveMode = PlayerInput`。
   - DataOS authoring / generator 为 `unit.enemy` 输出 `DefaultMoveMode = AIControlled`。
   - `EnemyEntity.OnPoolAcquire()` 不再承担默认移动模式写入职责。
   - `PlayerEntity` 不恢复旧兜底。
   - `EntityMovementComponent` 可增加明确错误日志，但不补写 fallback。

3. T1.3 final snapshot completeness validator
   - validator 检查 `unit.player`、`unit.enemy`、`ability` 必需字段。
   - validator 检查 `DefaultMoveMode` 的 table-specific expected value。
   - validator 检查 final `runtime_snapshot.json`，不是只检查中间 stream。

4. T1.4 generator projection 单一事实源
   - `field_rows` 只输出 table/id/field/value/source，不再手写 value_type。
   - record field type 只来自 `data_key_descriptor.value_type`。
   - snapshot descriptor/record mismatch jq gate 必须无输出。

5. T1.5 runtime projection stable key 硬化
   - `RuntimeDataRecordQuery` 优先生成 projection reader。
   - 如果短期不生成，至少用 `GeneratedDataKey.Xxx.StableKey` 替代裸字符串。
   - 为 unit spawn、ability view、system config / preset projection 加 missing/wrong type 测试。

6. T1.6 runtime write diagnostics
   - 增加 `DataWriteReport` / `DataWriteError` 或等价结构。
   - `TryApplyWritePolicies(..., out error)` 的错误不能被丢弃。
   - `Set` / `SetUntyped` / modifier write 至少提供可选诊断入口。
   - 测试断言错误 code：unknown key、wrong CLR type、write policy、range policy、computed/runtime_only、conversion failed。

7. T1.7 reference / array / modifier 类型契约
   - 明确 `AbilityIcon` 等资源引用走 `ResourceRef`。
   - 明确 `TargetNode` 等 runtime Node 引用不能从 snapshot 注入，并受 `runtimeTypeId` / storage policy 约束。
   - `string_array` 标准 runtime 类型是 `string[]`。
   - `modifier_list` 标准 runtime 类型是 `FeatureModifierEntryData[]`。
   - loader / validator 覆盖非空 JSON array fixture 或明确禁止 JSON array 形态。

8. T1.8 spawn boundary 去反射化
   - 删除 `EntityManager.InjectVisualScene()` 从 `object Config` 反射读取 `GeneratedDataKey.VisualScenePath.StableKey` 的回退。
   - visual scene 只来自 runtime record 或显式 `VisualScenePathOverride`。
   - `Config` 如保留，只承载非 Data 的局部运行参数。

9. T1.9 catalog freeze
   - `DataDefinitionCatalog.ValidateAndBuildIndexes()` 后设置 frozen。
   - frozen 状态 `Register()` 直接 throw。
   - 默认 bootstrap build 完成后禁止二次注册。

10. T1.10 display name query 收口
   - 生产代码只用 table/id 查询 record。
   - `GetRequiredByName()` 降为 debug/editor/test helper，或改名为 `GetRequiredByDisplayNameForDebug()`。
   - 迁移 `TargetingManager.cs`、`SpawnTestModule.cs` 等生产/测试调用点。

11. T1.11 current docs 和门禁
   - 更新 `SlimeAI/Src/ECS/Base/System/TestSystem/README.md`。
   - 更新 `SlimeAI/Src/ECS/Base/System/Core/README.md`。
   - 更新 `SlimeAI/Src/ECS/Base/Component/Component规范.md`。
   - 更新 `SlimeAI/Src/ECS/Base/Entity/Entity规范.md`。
   - 更新 `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.md`。
   - 更新 `SlimeAI/Src/ECS/Base/System/Movement/README.md` 和 `EntityMovementComponent说明.md`。
   - 更新 `SlimeAI/DocsNew/ECS/Data/Data系统说明.md` residual 入口。
   - 不恢复 `SlimeAI/DocsAI`。

12. T1.12 完整验证和 SDD 回填
   - 勾选 tasks.md。
   - 更新 progress.md Latest Resume 和 Timeline。
   - 更新 PRJ-0002 roadmap/progress/project.json/README 当前状态。
   - 运行 SDD validate。

关键源码/目录优先检查：
- SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh
- SlimeAI/Data/DataOS/Tools/validate-dataos.sh
- SlimeAI/Data/DataOS/Authoring/
- SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json
- SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py
- SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs
- SlimeAI/Src/ECS/Base/Data/Data.cs
- SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs
- SlimeAI/Src/ECS/Base/Data/DataDefinitionCatalog.cs
- SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataRecordQuery.cs
- SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/DataRuntimeBootstrap.cs
- SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs
- SlimeAI/Src/ECS/Base/Component/Movement/EntityMovementComponent.cs
- SlimeAI/Src/ECS/Base/Entity/Unit/Player/PlayerEntity.cs
- SlimeAI/Src/ECS/Base/Entity/Unit/Enemy/EnemyEntity.cs
- SlimeAI/Src/ECS/Base/System/AbilitySystem/
- SlimeAI/Src/ECS/Base/System/TargetingSystem/TargetingManager.cs
- SlimeAI/Src/ECS/Base/System/TestSystem/Spawn/SpawnTestModule.cs
- SlimeAI/Src/ECS/Base/**.md
- SlimeAI/DocsNew/ECS/Data/Data系统说明.md

最低验证命令：
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/generate-runtime-snapshot.sh Data/DataOS/Authoring/slimeainew.authoring.db Data/DataOS/Snapshots/runtime_snapshot.json
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '(.descriptors | map({key: .stableKey, value: .valueType}) | from_entries) as $d | .records[] | .table as $table | .id as $id | .fields | to_entries[] | select(($d[.key] // "__missing__") != .value.type) | [$table,$id,.key,($d[.key] // "missing_descriptor"),.value.type] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly

行为验证建议：
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-godot-scene.sh run res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs

最终 grep gate：
cd /home/slime/Code/SlimeAI
rg -n "class DataMeta|class DataRegistry|public static implicit operator string|RuntimeTables|DataKey\\.XXX\\.Key|const string TargetNode|new Data\\(" SlimeAI/DocsNew SlimeAI/Src/ECS/Base -g '*.md'
rg -n 'Data\\.(Get|Set|Has|Remove)<[^>]+>\\("|Data\\.(Get|Set|Has|Remove)\\("' SlimeAI/Src/ECS/Base SlimeAI/Data -g '!*.md' -g '!*.uid'
rg -n "GetRequiredByName\\(|GetProperty\\(GeneratedDataKey\\..*\\.StableKey\\)|TryApplyWritePolicies\\([^\\n]+out _" SlimeAI/Src/ECS/Base SlimeAI/Data -g '!*.md' -g '!*.uid'

允许命中只能是：
- 历史 SDD / 历史设计文档中的问题描述。
- SDD-0022 本身的 grep gate 或迁移说明。
- 明确标记为 historical/debug/test-only 且不参与生产推荐路径的说明。

SDD 验证：
cd /home/slime/Code/SlimeAI
python3 Workspace/SDD/sdd.py validate SDD-0022
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index

完成标准：
- tasks.md T1.1 至 T1.12 全部完成或有明确 blocker。
- progress.md 有 readiness baseline、关键决策、验证摘要和 Latest Resume。
- `unit.player` / `unit.enemy` / representative ability record completeness 被 validator 覆盖。
- 玩家/敌人 Movement 默认策略和 ability 创建链路有行为证据。
- runtime write failure 有结构化诊断。
- snapshot descriptor/record mismatch 为 0。
- build / DataOS validate / runtime or Godot behavior / grep gates 有新鲜证据。
- 没有用旧 Entity fallback 或宽松 loader 掩盖 DataOS record 问题。
```
