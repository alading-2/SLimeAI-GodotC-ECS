# Tasks

## Progress

- **Status**: done
- **Completed**: 12/12
- **Current**: done

## Task List

- [x] T1.1 建立 readiness baseline
  - **Goal**: 记录 SDD-0021 后仍成立的问题，不再把已删除旧兼容入口列为 P0。
  - **Read**: `design/03-*`、`design/04-*`、`design/05-*`、`design/06-*`。
  - **Checks**: 当前 `DefaultMoveMode` record 覆盖、`field_rows` 投影、runtime projection 裸 key、write failure bool-only、`object_ref` 类型语义、catalog freeze、display name query、current docs 旧入口命中。
  - **Validation**: baseline 结论写入 `Core/progress.md`，并附关键 grep / jq 摘要。

- [x] T1.2 前移 Movement 注册期必需字段
  - **Goal**: `unit.player` / `unit.enemy` records 在 `RegisterComponents()` 前提供正确 `DefaultMoveMode`。
  - **Files**: `SlimeAI/Data/DataOS/Authoring/*`、`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`、`SlimeAI/Src/ECS/Base/Component/Movement/EntityMovementComponent.cs`、`PlayerEntity.cs`、`EnemyEntity.cs`。
  - **Rule**: 不恢复 Player/Enemy 旧兜底；`OnPoolAcquire()` 不写注册期配置。
  - **Validation**: final snapshot 中 player/enemy records 有正确 `DefaultMoveMode`，runtime apply 测试可用 generated key 读取。

- [x] T1.3 增加 final snapshot completeness validator
  - **Goal**: validator 检查 record 必需字段和 table-specific expected value。
  - **Files**: `SlimeAI/Data/DataOS/Tools/validate-dataos.sh` 及必要测试 fixture。
  - **Rules**: 覆盖 `unit.player`、`unit.enemy`、`ability`；检查 final `runtime_snapshot.json`，不是只检查中间 stream。
  - **Validation**: 缺 `DefaultMoveMode` / ability 必需字段时红灯，修复后通过。

- [x] T1.4 消除 generator 投影重复 value_type
  - **Goal**: `field_rows` 不再手写字段类型，record field type 只来自 descriptor。
  - **Files**: `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`，必要时启用或替换 `dataos_runtime_field_stream`。
  - **Validation**: snapshot descriptor/record mismatch jq gate 无输出。

- [x] T1.5 收紧 runtime projection stable key
  - **Goal**: `RuntimeDataRecordQuery` 不再靠裸字符串长期维护 projection contract。
  - **Files**: `SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataRecordQuery.cs` 及 projection tests。
  - **Rule**: 优先 generated projection reader；短期至少使用 `GeneratedDataKey.Xxx.StableKey` 并覆盖 missing/wrong type 诊断。
  - **Validation**: projection tests 覆盖 unit spawn、ability view、system config / preset。

- [x] T1.6 增加 runtime write diagnostics
  - **Goal**: `Set` / `SetUntyped` / modifier write / policy failure 能返回结构化错误。
  - **Files**: `SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs`、`SlimeAI/Src/ECS/Base/Data/Data.cs`。
  - **Output**: `DataWriteReport` / `DataWriteError` 或同等结构。
  - **Validation**: 测试断言 unknown key、wrong CLR type、range/write policy、computed/runtime_only 等错误 code。

- [x] T1.7 硬化 `object_ref` / array / modifier list 类型契约
  - **Goal**: 资源引用、runtime Node 引用、数组、modifier blob 的 snapshot 表达和 runtime 类型单一化。
  - **Files**: `DataRuntimeStorage.cs`、`generate-data-key-handles.py`、`validate-dataos.sh`、`DataKey_Generated.cs`。
  - **Rules**: snapshot 只注入可序列化资源引用；runtime-only Node 引用必须 storage policy 明确；非空 JSON array fixture 必须被 validator/loader 覆盖。
  - **Validation**: `AbilityIcon`、`TargetNode`、`AvailableAnimations`、`Feature.Modifiers` typed handle 与 converter 行为一致。

- [x] T1.8 清理 spawn boundary 的 Data 反射回退
  - **Goal**: `EntitySpawnConfig.Config` 不再承载 Data 字段或被反射读取 stable key。
  - **Files**: `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs`。
  - **Rule**: visual scene 只来自 runtime record 或显式 override。
  - **Validation**: grep 无 `GetProperty(GeneratedDataKey.*.StableKey)` 类 Data 反射回退；spawn tests 通过。

- [x] T1.9 冻结 DataDefinitionCatalog
  - **Goal**: catalog build/index 后不可变。
  - **Files**: `SlimeAI/Src/ECS/Base/Data/DataDefinitionCatalog.cs`。
  - **Rule**: `ValidateAndBuildIndexes()` 后 `Register()` throw；默认 bootstrap catalog 不允许二次注册。
  - **Validation**: catalog freeze 测试覆盖 build 后 register failure。

- [x] T1.10 收口 display name record query
  - **Goal**: 生产链路只用 table/id，display name lookup 降级 debug/editor/test helper。
  - **Files**: `RuntimeDataRecordQuery.cs`、`DataRuntimeBootstrap.cs`、`TargetingManager.cs`、`SpawnTestModule.cs`、相关测试场景。
  - **Rule**: 如果保留，命名必须显式表达 `DisplayNameForDebug` 风险。
  - **Validation**: 生产调用点不再依赖 display name 作为稳定 identity。

- [x] T1.11 更新 current docs 和门禁
  - **Goal**: current 文档不再推荐旧 Data 路线，并指向 residual contract hardening 事实源。
  - **Files**: `SlimeAI/Src/ECS/Base/System/TestSystem/README.md`、`System/Core/README.md`、`Component/Component规范.md`、`Entity/Entity规范.md`、`Entity/Core/EntityManager.md`、`System/Movement/*`、`SlimeAI/DocsNew/ECS/Data/Data系统说明.md`。
  - **Rule**: 不恢复 `SlimeAI/DocsAI`；历史文档必须明确 historical。
  - **Validation**: 文档 grep gate 只有 historical / SDD 问题描述允许命中。

- [x] T1.12 完整验证与 SDD 回填
  - **Goal**: 记录新鲜验证证据，更新 SDD 和 PRJ 状态。
  - **Validation**: DataOS generate/validate、snapshot jq mismatch、dotnet build、runtime tests、Godot movement/ability smoke、grep gates、`python3 Workspace/SDD/sdd.py validate SDD-0022`、`python3 Workspace/SDD/sdd.py validate --all`。
