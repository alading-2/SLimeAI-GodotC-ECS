# One-Shot Execution Prompt: Complete SDD-0021 Data No-Compatibility Hard Cutover

把下面整段作为新会话的一次性执行提示词使用。目标是一次性完成 SDD-0021 的全部任务，绝不把旧 Data 写法包装成兼容层继续保留。

```text
你在 /home/slime/Code/SlimeAI 工作区内工作。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行；改文件前先读相关文件；改完总结改动和验证结果。不要 push。不要随意加依赖、commit 或跨 git 边界混提交。

任务目标：
完成 SDD-0021 Data No-Compatibility Hard Cutover。Data 系统最终只允许一条链路：DataOS SQLite authoring -> generator -> runtime_snapshot.json descriptors/records -> GeneratedDataKey typed handles -> SlimeAI/Src/ECS/Base/Data -> 业务调用点。所有旧兼容入口必须删除、内化或明确迁出运行时事实源。

必须先读：
1. /home/slime/Code/SlimeAI/AGENTS.md
2. /home/slime/Code/SlimeAI/Workspace/SystemAgent/README.md
3. /home/slime/Code/SlimeAI/SDD/INDEX.md
4. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
5. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/README.md
6. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/05-Data重构运行报错根因分析.md
7. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/06-无兼容完全重构总审计/README.md
8. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/011-SDD-0021-data-no-compatibility-hard-cutover/README.md
9. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/011-SDD-0021-data-no-compatibility-hard-cutover/design/main.md
10. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/011-SDD-0021-data-no-compatibility-hard-cutover/tasks.md
11. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/011-SDD-0021-data-no-compatibility-hard-cutover/bdd.md
12. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/011-SDD-0021-data-no-compatibility-hard-cutover/progress.md
13. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/INDEX.md
14. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/ProjectState.md
15. /home/slime/Code/SlimeAI/SlimeAI/DocsNew/ECS/Data/Data系统说明.md
16. /home/slime/Code/SlimeAI/SlimeAI/Data/README.md
17. /home/slime/Code/SlimeAI/SlimeAI/Data/DataKey/README.md

Git 边界：
- SDD 文档在 /home/slime/Code/SlimeAI 根仓。
- 源码实现主要在 /home/slime/Code/SlimeAI/SlimeAI 框架仓。
- 每次 git status、git diff、commit 前必须 cd 到对应仓库目录确认边界。
- 工作区可能已有无关脏改，必须保留并避免混入。
- 不要修改 Games/BrotatoLike/SlimeAI submodule 镜像。
- 默认不要 push。

绝对禁止的最终结果：
- 不保留 `DataKey<T>` 到 `string` 的 implicit conversion。
- 不保留 generated `DataKey.Xxx = GeneratedDataKey.Xxx` compatibility alias。
- 不保留 `string_array` / `object_ref` / `modifier_list` 生成 `DataKey<string>`。
- 不保留 record field type 由 generator hardcode 决定；record type 必须来自 descriptor。
- 不保留 validator 只检查 `dataos_runtime_field_stream` 而不检查最终 `runtime_snapshot.json`。
- 不保留业务层 public string-key Data API 作为普通调用入口。
- 不保留未绑定 catalog 的 `new Data()` 运行时可访问窗口。
- 不保留 `DataMeta` / `DataRegistry` / `LegacyDataAuditReport` 在 runtime 编译面作为当前事实源。
- 不保留 `RuntimeTables` 兼容命名、兼容 README 或旧查询语义作为当前事实源。
- 不保留 FeatureDefinition/SystemConfig/SystemPreset Resource/tres authoring 作为 Data 当前事实源。
- 不保留文档继续声称 SDD-0020 已完全退出旧路径。

实施顺序必须按 SDD-0021 tasks.md：

1. T1.1 readiness baseline
   - 记录当前 `AbilityIcon` descriptor/record mismatch。
   - 记录 `dataos_runtime_field_stream` count 和 validate passed 的错层问题。
   - 记录非标量 descriptors 清单和 generated handle 当前类型。
   - 记录 `implicit operator string(DataKey`、`Compatibility aliases`、public string-key API、`new Data()`、`Get<object>(GeneratedDataKey`、`Get<List<string>`、`TargetNode` Node2D 调用、`legacyTable` / `legacyData`、过期 docs 命中。
   - 写入 SDD-0021 progress.md。

2. T1.2 generator / validator
   - 修改 `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`，record field type 来自 `data_key_descriptor.value_type`。
   - 修改 `validate-dataos.sh`，校验最终 `runtime_snapshot.json` descriptor/record 一致性。
   - `legacyTable` / `legacyData` 从 runtime snapshot 移除或迁出为独立 audit artifact。
   - 先让当前错误进入红灯，再修复到通过。

3. T1.3 非标量类型标准
   - `string_array -> string[]`。
   - `AbilityIcon object_ref` 定义资源引用契约，至少确保 record type 为 `object_ref`。
   - `TargetNode object_ref` 明确 runtime Node 引用边界，不能继续 `DataKey<string>` + `Node2D` 绕路。
   - `Feature.Modifiers modifier_list` 明确 loader-only blob 或 typed DTO。

4. T1.4 generated handle generator
   - 修改 `generate-data-key-handles.py`。
   - 删除 compatibility alias 输出。
   - 重新生成 `DataKey_Generated.cs`。

5. T1.5 删除 DataKey implicit string
   - 删除 `DataKey<T>` 隐式 string。
   - 收紧 `.Key` alias；loader/test 需要 stable key 时用显式内部方法。
   - 编译错误按真实类型修复。

6. T1.6 收紧 Data API
   - 业务 public API 只保留 typed handle。
   - string stable-key API 降为 internal/loader/test-only，方法名表达用途。

7. T1.7 删除未绑定 new Data runtime 窗口
   - Entity 构造、bootstrap、测试 fixture 改为明确 catalog 初始化路径。
   - 未绑定 Data 不能被业务读写。

8. T1.8 修已知错误调用点
   - `AvailableAnimations` 改为 `string[]` 标准。
   - `AbilityTriggerEvent` 不再 `Get<object>`。
   - `Feature.Modifiers` 不再 `Get<object>`。
   - `TargetNode` 不再靠 `DataKey<string>` 和 string overload 存取 `Node2D`。
   - `AbilityIcon` 按 object_ref/resource ref 契约读取。

9. T1.9 删除 DataMeta/DataRegistry runtime 依赖
   - 从 runtime 编译面删除或迁出。
   - 测试辅助改用 DataDefinition/DataDefinitionCatalog。

10. T1.10 文档和旧 authoring 路线收口
   - RuntimeTables 目录改名或迁出 DTO。
   - FeatureDefinition/SystemConfig/SystemPreset Resource authoring 路线删除或改为 runtime-only。
   - 更新 PRJ roadmap/progress、DocsAI ProjectState、DocsNew Data 文档、Data README、DataKey README。

11. T1.11 完整验证
   - 运行 build、DataOS validate、snapshot jq mismatch、Godot DataOS scenes、grep gates。
   - 失败就修；不能修就写 blocker，不要标记 done。

12. T1.12 回填状态
   - 勾选 tasks.md。
   - 更新 progress.md Latest Resume 和 Timeline。
   - 更新 PRJ-0002 project docs。
   - 运行 SDD validate。

关键源码/目录优先检查：
- SlimeAI/Src/ECS/Base/Data/
- SlimeAI/Src/ECS/Base/Data/DataKey.cs
- SlimeAI/Src/ECS/Base/Data/Data.cs
- SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs
- SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/
- SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh
- SlimeAI/Data/DataOS/Tools/validate-dataos.sh
- SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json
- SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql
- SlimeAI/Data/DataOS/RuntimeTables/
- SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py
- SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs
- SlimeAI/Src/ECS/AI/
- SlimeAI/Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/
- SlimeAI/Src/ECS/Base/Component/Unit/Common/AttackComponent/
- SlimeAI/Src/ECS/Base/Component/Ability/TriggerComponent/
- SlimeAI/Src/ECS/Base/System/FeatureSystem/
- SlimeAI/Src/ECS/Base/System/TestSystem/FeatureDebugService.cs
- SlimeAI/Data/Feature/Definition/
- SlimeAI/Data/Config/System/
- SlimeAI/DocsAI/
- SlimeAI/DocsNew/ECS/Data/
- SlimeAI/Data/README.md
- SlimeAI/Data/DataKey/README.md

最低验证命令：
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly

最终 snapshot mismatch gate：
cd /home/slime/Code/SlimeAI/SlimeAI
jq -r '[.descriptors[] | {key: .stableKey, type: .valueType}] as $defs | .records[] as $r | ($r.fields // {}) | to_entries[] | .key as $k | .value.type as $rt | ($defs[] | select(.key == $k) | .type) as $dt | select($rt != $dt)' Data/DataOS/Snapshots/runtime_snapshot.json

Godot DataOS 场景验证：
cd /home/slime/Code/SlimeAI/SlimeAI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataFeatureBridgeTestScene.tscn --build

最终 grep gate：
cd /home/slime/Code/SlimeAI
rg -n "implicit operator string\\(DataKey|Compatibility aliases|public static partial class DataKey" SlimeAI/Src/ECS/Base/Data SlimeAI/Data
rg -n "DataKey<string> (AbilityIcon|TargetNode|AbilityTriggerEvent|AvailableAnimations|Dependencies|EnabledSystemIds|DisabledSystemIds|FeatureModifiers)" SlimeAI/Data/DataKey/Generated
rg -n "Get<object>\\(GeneratedDataKey|Get<.*List<string>|Set\\(GeneratedDataKey\\.TargetNode|Get<Node2D>\\(GeneratedDataKey\\.TargetNode" SlimeAI/Src/ECS SlimeAI/Data
rg -n "legacyTable|legacyData" SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json SlimeAI/Data/DataOS/Tools
rg -n "class DataMeta|class DataRegistry|LegacyDataAuditReport" SlimeAI/Src/ECS/Base/Data
rg -n "DataKey\\.Xxx|new Data\\(\\).*兼容|RuntimeTables.*兼容|SDD-0020.*完全退出旧路径" SlimeAI/DocsAI SlimeAI/DocsNew SlimeAI/Data SDD/project/projects/PRJ-0002-ecs-framework-refactor -g "*.md"

允许命中只能是：
- 历史 SDD / 历史设计文档中的问题描述。
- SDD-0021 本身的 grep gate 或迁移说明。
- 明确标记为 archived/deprecated 且不参与 runtime/build/docs 推荐路径的删除说明。

SDD 验证：
cd /home/slime/Code/SlimeAI
python3 Workspace/SDD/sdd.py validate SDD-0021
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index

完成标准：
- tasks.md T1.1 至 T1.12 全部完成或有明确 blocker。
- progress.md 有 readiness baseline、迁移结论、验证摘要和 Latest Resume。
- snapshot descriptor/record mismatch 为 0。
- build / DataOS validate / Godot DataOS scenes / grep gates 有新鲜证据。
- 没有把旧写法包装成“兼容层”继续存在。
```
