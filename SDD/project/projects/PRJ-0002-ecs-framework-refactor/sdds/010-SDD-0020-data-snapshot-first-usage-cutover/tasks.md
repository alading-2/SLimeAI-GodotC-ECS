# Tasks

## Progress

- **Status**: done
- **Completed**: 12/12
- **Current**: complete

## Task List

- [x] T1.1 执行 snapshot-first cutover readiness gate
  - **Scope**: 固定当前 RuntimeTables、DataMeta/DataRegistry fallback、config 推断、DataNew/tool/docs 旧路线命中基线；确认 SDD-0012~SDD-0019 作为历史输入，不再按其“完成”结论跳过本轮收口。
  - **Validation**: readiness 记录进入 progress；grep baseline 包含命中分类和允许/禁止清单。
- [x] T1.2 建立 RuntimeDataRecordQuery 和 snapshot projection 基础层
  - **Scope**: 为 runtime snapshot records 建立 table/id/name 查询索引和 typed projection helper；调用方不直接散落读取 `record.Fields["..."]`。
  - **Validation**: 纯 C# 测试覆盖 table/id/name 查询、missing record fail-fast、类型转换和 projection 错误报告。
- [x] T1.3 迁移 SystemConfigService / SystemPresetService
  - **Scope**: `system.config` / `system.preset` 改从 snapshot records 投影，删除 `SystemData.All` / `SystemPresetData.All` runtime 依赖。
  - **Validation**: SystemCore runtime test 通过；grep 无 `SystemData.All` / `SystemPresetData.All` 运行时命中。
- [x] T1.4 迁移 SpawnSystem 敌人规则读取
  - **Scope**: `unit.enemy` 生成规则从 snapshot projection 获取；生成敌人时传 explicit record handle/table/id，不传 `EnemyData` object。
  - **Validation**: Spawn 场景或单测覆盖当前波次规则筛选；grep 无 `EnemyData.All` 运行时命中。
- [x] T1.5 迁移 Ability/TestSystem/FeatureDebug 读取
  - **Scope**: 技能列表、测试面板和 `EntityManager.AddAbility` 改用 ability record id / record projection；不再消费 `AbilityData.All` 或 `AbilityData` config object。
  - **Validation**: Ability 相关 runtime tests 和 TestSystem smoke 通过；grep 无 `AbilityData.All` 运行时命中。
- [x] T1.6 迁移 ResourceCatalog 和资源展示入口
  - **Scope**: ResourceCatalog 从 runtime snapshot resources / records projection 构建 DataUnit/DataAbility 条目；不从 RuntimeTables `.All` 构建。
  - **Validation**: ResourceCatalog 查询测试或相关 Godot smoke 通过；grep 无 RuntimeTables `.All` 资源目录命中。
- [x] T1.7 删除 EntityManager config type/name 推断
  - **Scope**: 删除 `TryResolveRecordByConfig` 和 `Config = RuntimeTables object` 初始化路径；`EntitySpawnConfig` 只接受 explicit record 或 table/id。
  - **Validation**: Entity spawn 相关测试改用 explicit record；grep 无 `TryResolveRecordByConfig`。
- [x] T1.8 删除 RuntimeTables 手写数据和 DataTable 反射扫描
  - **Scope**: 删除或清空 `Data/DataOS/RuntimeTables` 手写静态实例；删除 `DataTable.GetAll/GetByName` 反射入口；如保留 DTO 类型则必须无数据、无事实源能力。
  - **Validation**: grep 无 `public static readonly .*Data = new` 形式的 runtime table 静态数据；build 通过。
- [x] T1.9 删除 DataRegistry/DataMeta runtime fallback
  - **Scope**: `Data` 改为 catalog-bound only；删除 `_runtimeStorage == null` 下旧字典/registry/computed/modifier 路径；测试辅助改用 `DataDefinition`。
  - **Validation**: DataCatalogTdd + DataOS Godot scenes 通过；grep 无 `DataRegistry.GetMeta` / `new DataMeta` runtime 命中。
- [x] T1.10 清理 DataConfigEditor、AGENTS/CLAUDE、DocsAI 和模块文档旧路线
  - **Scope**: 删除或隔离 DataNew editor 路线；同步规则和文档，非历史文档不得推荐 DataMeta/DataRegistry/DataNew/手写 RuntimeTables。
  - **Validation**: 文档 grep gate 通过；如修改 `.ai-config/skills` 源，运行 sync 和 skill-test。
- [x] T1.11 运行完整验证与 grep gate
  - **Scope**: 运行 build、DataCatalogTdd、DataOS validate、DataOS Godot scenes、最终 grep gates。
  - **Validation**: 验证摘要进入 progress；失败项必须有 blocker 或后续 SDD。
- [x] T1.12 回填项目状态、DocsAI ProjectState 和归档准备
  - **Scope**: 更新 PRJ-0002 roadmap/progress/status board、DocsAI ProjectState、相关 SDD resume；确认 SDD-0020 可恢复或可 done。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0020` 和 `validate --all` 0 error。
