# Progress

## Latest Resume

- **Updated**: 2026-05-29 19:25
- **Current Task**: complete
- **Last Conclusion**: SDD-0020 已完成 Data snapshot-first usage hard cutover。运行时配置、系统配置/预设、敌人生成规则、Ability/TestSystem/FeatureDebug、ResourceCatalog 和 Entity spawn 初始化记录已切到 `runtime_snapshot.json` / `DataRuntimeBootstrap` / `RuntimeDataRecordQuery` / typed projection / catalog-bound `Data`。旧 RuntimeTables 手写数据、`DataTable` 反射扫描、Entity config 推断、DataRegistry/DataMeta runtime fallback、DataConfigEditor 和旧文档推荐路线已退出当前运行时事实源。
- **Next Action**: PRJ-0002 可继续进入 Entity / Relationship hard cutover SDD；Data 侧如后续需要编辑器，应新开 DataOS DB editor 任务，不复活旧 DataConfigEditor。
- **Open Blockers**: none
- **Known Drift**: 执行提示词中的 `Tools/DataCatalogTdd/DataCatalogTdd.csproj` 在当前框架仓不存在，命令返回 `The provided file path does not exist`。本轮用现有四个 Godot DataOS 场景覆盖 catalog/runtime/snapshot apply/feature bridge 行为，并把路径漂移记录为历史 prompt/仓库结构不一致。
- **Git Boundary**: SDD 文档在 `/home/slime/Code/SlimeAI` 根仓；实现改动在 `/home/slime/Code/SlimeAI/SlimeAI` 框架仓。
- **Submodule Boundary**: 未修改 `Games/BrotatoLike/SlimeAI` submodule 镜像。

## Readiness Baseline

- RuntimeTables 旧事实源：`Data/DataOS/RuntimeTables/DataTable.cs`、unit/ability 静态 RuntimeTables 数据和 DataConfigEditor 旧编辑器在本轮删除；`SystemData` / `SystemPresetData` 仅保留无静态数据 DTO。
- 禁止入口 baseline：`DataTable.GetAll<T>()`、`EnemyData.All`、`AbilityData.All`、`SystemData.All`、`SystemPresetData.All`、`EntityManager.TryResolveRecordByConfig`、`DataRegistry.GetMeta/Register` runtime fallback、`new DataMeta`、`DataMeta.Compute`、`LoadFromConfig`、`DataKey.DefaultValue` 均作为禁止运行时命中。
- 文档/工具旧路线：`DataNew`、`Data/DataOS runtime table`、`DataOS runtime table 纯 C#` 和扩展后的 `DataOS runtime table` 描述已从当前推荐路径清理。
- 允许保留：历史 SDD/历史设计问题描述、无运行时读取能力的 DTO、迁移审计类 `LegacyDataAuditReport` 和已标记不再作为事实源的兼容类型声明。

## Migration Summary

- 新增 `RuntimeDataRecordQuery` 和 typed projection，集中处理 table/id/name 查询、missing record fail-fast、字段类型转换与 projection 错误报告；业务调用点不再散落 `record.Fields["..."]`。
- `SystemConfigService` / `SystemPresetService` 从 snapshot `system.config` / `system.preset` projection 加载，`SystemData.All` / `SystemPresetData.All` 不再存在。
- `SpawnSystem` 从 `unit.enemy` projection 读取规则，`SpawnBatchRequest` 改用 `UnitSpawnDefinition`，Entity spawn 必须显式传 `RuntimeDataRecord` 或 table/id。
- Ability/TestSystem/FeatureDebug 改用 `AbilityDefinitionView`；测试面板技能目录和 AddAbility 不再消费 `AbilityData.All` 或 `AbilityData` config object。
- `ResourceCatalog` 从 runtime snapshot resources / records projection 构建 DataUnit/DataAbility 条目，不从 RuntimeTables `.All` 构建。
- `EntityManager.TryResolveRecordByConfig` 已删除；`EntitySpawnConfig` 不再接受旧 config object 推断 record。
- `Data` 已改为 catalog-bound only；未绑定 `DataDefinitionCatalog` 的访问 fail-fast，旧 `_data/_modifiers/_cachedValues/_dirtyKeys` fallback 与 DataMeta 计算路径已移除；modifier 全量清理改由 `DataRuntimeStorage` 承担。
- DataConfigEditor 旧插件目录删除；DocsAI / DocsNew / AGENTS / CLAUDE / 模块 README 中当前推荐路线已改为 runtime snapshot / DataOS authoring / catalog-bound Data。

## Validation Summary

- PASS: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`，最终结果 `Build succeeded`，0 errors。
- PASS: `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`。
- DRIFT: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 因项目路径不存在失败；当前仓库实际没有 `Tools/DataCatalogTdd`，`Tools/` 下仅有 `ResourceGenerator`。
- PASS: `DataCatalogTestScene.tscn`，runner `status=passed`、`firstError=null`。
- PASS: `DataRuntimeTestScene.tscn`，runner `status=passed`、`firstError=null`。
- PASS: `DataSnapshotApplyTestScene.tscn`，runner `status=passed`、`firstError=null`。
- PASS: `DataFeatureBridgeTestScene.tscn`，runner `status=passed`、`firstError=null`。
- NOTE: 四个 Godot 场景 stderr 仍有既有 `LightningLineEffect` UID warning、component warmup warning 和 Godot exit RID/resource leak `ERROR` 行；runner 均判定 passed 且 `firstError=null`，本轮未把这些既有退出噪声作为 Data cutover 阻塞。
- PASS: 最终 grep gates 无命中：
  - `DataTable.GetAll|EnemyData.All|AbilityData.All|SystemData.All|SystemPresetData.All`
  - `TryResolveRecordByConfig|DataRegistry.GetMeta|DataRegistry.Register|new DataMeta|DataMeta.Compute|LoadFromConfig|DataKey.DefaultValue`
  - `DataNew|Data/DataOS runtime table|DataOS runtime table 纯 C#|DataOS runtime table`
  - `Data.cs` 旧 fallback 内部残留：`_runtimeStorage == null|_data|_modifiers|GetComputedValueBoxed|GetModifiedValueBoxed|CalculateFinalValue(` 无命中。

## Timeline

### P001 — 2026-05-29 — sdd-created

- **Context**: 用户指出 Data 要改的地方很多，必须把取用改成最新 Data 系统形式，并要求生成 SDD 任务、深度思考详细分析。
- **Conclusion**: 已创建 SDD-0020，作为 SDD-0012~SDD-0019 后的 Data 使用层 hard cutover 任务；目标不是继续兼容旧 RuntimeTables，而是用 snapshot query/projection 统一替代所有旧取用点。
- **Evidence**: `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`notes.md`、`progress.md` 已写入 SDD-0020。
- **Impact**: 后续 Data 实施应先完成 SDD-0020，再继续 Entity/Event 大改，避免旧 Data 兼任问题扩散到后续系统。
- **Resume**: 从 T1.1 readiness gate 开始。

### P002 — 2026-05-29 19:25 — snapshot-first-cutover-done

- **Context**: 按 `execution-prompt.md` 完成 T1.1~T1.12。
- **Conclusion**: SDD-0020 已完成。Data 运行时取用收口到 runtime snapshot query/projection 和 catalog-bound Data，旧 RuntimeTables / DataTable / config 推断 / DataRegistry fallback / DataConfigEditor 不再作为当前运行时事实源。
- **Evidence**: build、DataOS validate、四个 Godot DataOS scenes、最终 grep gates 均有新鲜证据；`DataCatalogTdd` 命令仅因项目路径不存在失败，已记录为仓库/提示词漂移。
- **Impact**: Data 使用层不再阻塞 Entity / Relationship hard cutover；后续 Entity SDD 可以假定 Entity spawn 必须显式携带 snapshot record handle。
- **Resume**: 从 PRJ-0002 的 Entity / Relationship hard cutover 设计包继续。
