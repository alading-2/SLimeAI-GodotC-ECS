# One-Shot Execution Prompt: Complete SDD-0020 Data Snapshot-First Usage Cutover

把下面整段作为新会话的一次性执行提示词使用。目标是一次性完成 SDD-0020 的全部任务，而不是继续做兼容补丁。

```text
你在 /home/slime/Code/SlimeAI 工作区内工作。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行；改文件前先读相关文件；改完总结改动和验证结果。不要 push。不要随意加依赖、commit 或跨 git 边界混提交。

任务目标：
完成 SDD-0020 Data Snapshot-First Usage Cutover。旧 Data 写法必须完全放弃，绝不作为长期兼任或 runtime fallback 保留。最终运行时配置、Entity 初始化记录、系统配置、测试面板展示数据和资源目录数据都从 DataOS 生成的 runtime_snapshot.json、DataRuntimeBootstrap、snapshot query/projection 和 catalog-bound Data 获取。

必须先读：
1. /home/slime/Code/SlimeAI/AGENTS.md
2. /home/slime/Code/SlimeAI/Workspace/SystemAgent/README.md
3. /home/slime/Code/SlimeAI/SDD/INDEX.md
4. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md
5. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/README.md
6. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/03-完全重构范围与TDD测试计划.md
7. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/04-Data系统现状复查与兼任问题.md
8. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/010-SDD-0020-data-snapshot-first-usage-cutover/README.md
9. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/010-SDD-0020-data-snapshot-first-usage-cutover/design/main.md
10. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/010-SDD-0020-data-snapshot-first-usage-cutover/tasks.md
11. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/010-SDD-0020-data-snapshot-first-usage-cutover/bdd.md
12. /home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/010-SDD-0020-data-snapshot-first-usage-cutover/progress.md
13. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/INDEX.md
14. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/DataOS/Overview.md
15. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/GameOS/Contracts.md
16. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/GameOS/ApiIndex.md
17. /home/slime/Code/SlimeAI/SlimeAI/DocsAI/ProjectState.md

Git 边界：
- SDD 文档在 /home/slime/Code/SlimeAI 根仓。
- 源码实现主要在 /home/slime/Code/SlimeAI/SlimeAI 框架仓。
- 每次 git status、git diff、commit 前必须 cd 到对应仓库目录确认边界。
- 不要修改 Games/BrotatoLike/SlimeAI submodule 镜像。
- 工作区可能已有无关脏改，必须保留并避免混入。
- 默认不要 push。

绝对禁止的长期结果：
- 不保留手写 Data/DataOS/RuntimeTables 作为业务数据事实源。
- 不保留 DataTable.GetAll<T>() / GetByName 反射扫描作为 runtime 数据入口。
- 不保留 EnemyData.All、AbilityData.All、SystemData.All、SystemPresetData.All 作为运行时取用入口。
- 不保留 EntityManager.TryResolveRecordByConfig 或 Config = EnemyData/AbilityData/SystemData 等旧 config object 推断 record。
- 不保留 DataRegistry/DataMeta 作为 Data runtime fallback。
- 不保留未绑定 catalog 的 new Data() runtime 事实源路径。
- 不保留 DataNew / 旧 DataConfigEditor 路线作为当前推荐入口。
- 不在业务代码中散落 record.Fields["..."] 裸字符串读取；集中在 query/projection 层。

允许的短期过程：
- 可以先新增 snapshot query/projection，再迁移调用点，最后删除旧 API。
- 可以保留无数据、无事实源能力的 DTO 类型，但不能保留静态实例、All/Get 反射表或兼容读取 API。
- 如果某个旧命中只能暂时保留，必须写入 progress.md 的 blocker，并明确删除任务；不要把它标记为 done。

实施顺序必须按 SDD-0020 tasks.md：

1. T1.1 readiness / grep baseline
   - 在 /home/slime/Code/SlimeAI/SlimeAI 中固定当前 RuntimeTables、DataMeta/DataRegistry fallback、config 推断、DataNew/tool/docs 旧路线命中。
   - 把 baseline 分类写入 SDD-0020 progress.md。

2. T1.2 建立 RuntimeDataRecordQuery 和 typed projection 基础层
   - 围绕 DataRuntimeBootstrap.Snapshot 建立只读 query。
   - 支持 table/id/name 查询、缓存和 missing record fail-fast。
   - 建立当前消费点需要的 typed projection DTO/helper，例如 UnitSpawnDefinition、AbilityDefinitionView、SystemConfigDefinition、SystemPresetDefinition、ResourceCatalogProjection。
   - 调用方不能散落读取 record.Fields。
   - 加纯 C# 测试覆盖 query、missing、类型转换和 projection 错误报告。

3. T1.3 迁移 SystemConfigService / SystemPresetService
   - system.config / system.preset 改从 snapshot records 投影。
   - 删除 SystemData.All / SystemPresetData.All 运行时依赖。
   - 跑 SystemCore 相关 runtime tests。

4. T1.4 迁移 SpawnSystem
   - unit.enemy 生成规则从 snapshot projection 获取。
   - 生成实体时传 explicit record handle/table/id，不传 EnemyData object。
   - 覆盖波次规则筛选验证。

5. T1.5 迁移 Ability/TestSystem/FeatureDebug
   - 技能列表、测试面板、EntityManager.AddAbility 改用 ability record id / record projection。
   - 不再消费 AbilityData.All 或 AbilityData config object。
   - Ability API 可先用 record table/id 或 RuntimeDataRecordDto，避免引入新 ID 类型阻塞。

6. T1.6 迁移 ResourceCatalog
   - ResourceCatalog 从 runtime_snapshot.resources 或 unit/ability records projection 构建 DataUnit/DataAbility 条目。
   - 不从 RuntimeTables .All 构建资源目录。

7. T1.7 删除 EntityManager config type/name 推断
   - 删除 TryResolveRecordByConfig。
   - EntitySpawnConfig 只接受 explicit RuntimeDataRecord 或 table/id。
   - Entity spawn 相关测试全部改用 explicit record。

8. T1.8 删除 RuntimeTables 手写数据和 DataTable 反射扫描
   - 删除或清空 Data/DataOS/RuntimeTables 的手写静态实例。
   - 删除 DataTable.GetAll/GetByName 反射入口。
   - 如保留 DTO 类型，必须无数据、无事实源能力。

9. T1.9 删除 DataRegistry/DataMeta runtime fallback
   - Data 改为 catalog-bound only。
   - 删除 _runtimeStorage == null 下旧字典/registry/computed/modifier 路径。
   - 测试辅助改用 DataDefinition / DataDefinitionCatalog。
   - unknown key 必须 fail fast，不回退。

10. T1.10 清理 DataConfigEditor、AGENTS/CLAUDE、DocsAI 和模块文档旧路线
   - 删除或隔离 DataNew editor 路线；如仍需要编辑器，另开 SDD 重写为 DataOS DB editor。
   - 非历史文档不得推荐 DataMeta/DataRegistry/DataNew/手写 RuntimeTables。
   - 如果修改 .ai-config/skills 或 rules 源，必须运行：
     bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
     并运行对应 skill-test / lint。

11. T1.11 运行完整验证与 grep gate
   - 在框架仓运行 build、DataCatalogTdd、DataOS validate、Godot DataOS scenes。
   - 运行最终 grep gates。
   - 失败项必须修复；不能修复时写 blocker，不要假装完成。

12. T1.12 回填状态
   - 更新 SDD-0020 tasks.md checkbox、progress.md Latest Resume 和 Timeline。
   - 更新 PRJ-0002 roadmap/progress/status board。
   - 更新 SlimeAI/DocsAI/ProjectState.md。
   - 运行 SDD validate。

关键源码/目录优先检查：
- SlimeAI/Src/ECS/Base/Data/
- SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/
- SlimeAI/Data/DataOS/RuntimeTables/
- SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh
- SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs
- SlimeAI/Src/ECS/Base/System/
- SlimeAI/Src/ECS/Systems/SpawnSystem.cs
- SlimeAI/Src/ECS/Systems/Ability/
- SlimeAI/Src/ECS/Test/
- SlimeAI/Data/ResourceManagement/
- SlimeAI/addons/DataConfigEditor/
- SlimeAI/DocsAI/
- SlimeAI/DocsNew/
- SlimeAI/AGENTS.md
- SlimeAI/CLAUDE.md

最终 grep gate：
cd /home/slime/Code/SlimeAI
rg -n "DataTable\\.GetAll|EnemyData\\.All|AbilityData\\.All|SystemData\\.All|SystemPresetData\\.All" SlimeAI/Src SlimeAI/Data
rg -n "TryResolveRecordByConfig|DataRegistry\\.GetMeta|DataRegistry\\.Register|new DataMeta|DataMeta\\.Compute|LoadFromConfig|DataKey\\.DefaultValue" SlimeAI/Src SlimeAI/Data
rg -n "DataNew|Data/DataOS runtime table|DataOS runtime table 纯 C#" SlimeAI/Src SlimeAI/Data SlimeAI/DocsAI SlimeAI/DocsNew SlimeAI/addons SlimeAI/AGENTS.md SlimeAI/CLAUDE.md

允许命中只能是：
- 历史 SDD / 历史设计文档中的问题描述。
- 本 SDD 的 grep gate 或迁移说明。
- 明确标记为 archived/deprecated 且不参与 runtime/build/docs 推荐路径的删除说明。

最低验证命令：
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db

Godot DataOS 场景验证：
cd /home/slime/Code/SlimeAI/SlimeAI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataFeatureBridgeTestScene.tscn --build

SDD 验证：
cd /home/slime/Code/SlimeAI
python3 Workspace/SDD/sdd.py validate SDD-0020
python3 Workspace/SDD/sdd.py validate --all
python3 Workspace/SDD/sdd.py index

完成标准：
- tasks.md T1.1 至 T1.12 全部完成或有明确 blocker。
- progress.md 有 readiness baseline、迁移结论、验证摘要和 Latest Resume。
- build / DataCatalogTdd / DataOS validate / Godot DataOS scenes / grep gates 有新鲜证据。
- RuntimeTables、DataTable、Entity config 推断、DataRegistry/DataMeta fallback、DataNew 推荐路线不再是 runtime 或文档推荐事实源。
- 没有把旧写法包装成“兼容层”继续存在。
```
