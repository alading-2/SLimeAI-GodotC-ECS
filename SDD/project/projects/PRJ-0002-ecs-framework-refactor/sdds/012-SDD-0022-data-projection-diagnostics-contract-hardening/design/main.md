# Data Projection Diagnostics Contract Hardening

## Goal

完成 Data no-compat 后的中层契约硬化，使 DataOS descriptor-first 链路不仅能生成和加载，还能稳定驱动玩家移动、敌人移动和技能创建/触发。

本 SDD 的完成标准是：

```text
DataOS authoring
  -> generator projection
  -> runtime_snapshot descriptors/records
  -> GeneratedDataKey<T>
  -> RuntimeDataSnapshotLoader.ApplyRecord
  -> EntityManager.Spawn/RegisterComponents
  -> Movement / Ability 首帧行为
```

每一层都有单一事实源、结构化诊断和可重复门禁。

## Context

SDD-0021 已完成旧兼容入口大头删除，包括 generated alias、`DataKey<T>` 隐式 string、旧 DataMeta/DataRegistry runtime 编译面、RuntimeTables 命名、旧 Resource/tres authoring 当前入口和 final snapshot type mismatch。

新增 4 份 Data residual 文档指出：当前问题已经不是“旧兼容入口还没清完”，而是 Data 中层契约仍然太软：

- generator `field_rows` 仍手写业务字段投影和历史类型信息。
- runtime projection 仍手写 stable key 字符串。
- runtime write / modifier / policy 失败缺少结构化错误。
- `object_ref` 同时表达资源引用和运行时对象引用。
- `EntitySpawnConfig.Config` 和 visual injection 仍存在 object/反射边界。
- `DataDefinitionCatalog` 逻辑冻结但代码上还能继续 `Register()`。
- display name 仍能作为运行时 record identity 查询入口。
- current 文档仍可能把 AI 带回 `DataMeta`、`DataRegistry`、`.tres`、`DataKey.Xxx.Key`、`new Data()` 等旧写法。

移动与施法失败的直接根因是 record completeness 与初始化时序缺失：`DefaultMoveMode` 是 Movement 组件注册期字段，但 `unit.player` / `unit.enemy` records 未前置表达，旧 Entity/Pool 兜底又已被 no-compat 删除或晚于 `RegisterComponents()`。

## Design

### 1. Record Completeness Contract

为 final `runtime_snapshot.json` 增加业务 record 完整性约束，而不是只检查“出现的字段类型正确”。

最低规则：

- `unit.player` 必须包含 `Name`、`Team`、`EntityType`、`DeathType`、`MoveSpeed`、`DefaultMoveMode = PlayerInput`。
- `unit.enemy` 必须包含 `Name`、`Team`、`EntityType`、`DeathType`、`MoveSpeed`、`DetectionRange`、`DefaultMoveMode = AIControlled`。
- `ability` 必须包含 `Name`、`AbilityType`、`AbilityTriggerMode`、`AbilityFeatureGroup`、`FeatureHandlerId`；手动技能必须能被 manual ability query 发现。

短期可在 generator 中按 table 固定投影 `DefaultMoveMode`；长期应把默认移动模式表达为 unit authoring 内容。

### 2. Initialization Timing Contract

组件注册期字段必须在 `RegisterComponents()` 前通过 snapshot/apply/spawn bootstrap 写入。

`OnPoolAcquire()` 只允许处理对象池复用状态，不承担 `DefaultMoveMode` 这类注册期配置写入职责。`EntityMovementComponent` 可以 fail-fast 日志诊断，但不能恢复旧 fallback。

### 3. Projection Single-Source Contract

业务投影只提供 field key、value 和来源；field value type 只来自 descriptor。`generate-runtime-snapshot.sh` 不应让 `field_rows` 再维护一个人工 `value_type` 事实源。

runtime projection 层优先使用 generated handle / generated projection reader；过渡阶段至少把裸字符串 stable key 改为 `GeneratedDataKey.Xxx.StableKey`，并为 projection view 增加 missing field / wrong type 测试。

### 4. Diagnostics Contract

新增 `DataWriteReport` / `DataWriteError` 或等价结构，让 runtime writes、modifier writes 和 policy failures 返回结构化错误：

- stable key
- error code
- expected type
- actual type
- source
- policy
- raw value 摘要

旧 bool API 可保留给既有调用点，但新调试和测试必须能断言错误 code。

### 5. Reference And Array Type Contract

`object_ref` 必须区分资源引用和 runtime object 引用：

- snapshot 可注入资源引用，例如 `ResourceRef`。
- runtime-only Node/Node2D 引用必须通过 `runtimeTypeId` / storage policy 明确约束，禁止从 snapshot 注入。

`string_array` 和 `modifier_list` 需要确定唯一输入形态和 runtime 标准类型；loader / validator 必须覆盖非空数组 fixture，避免 JSON array 与逗号字符串混用继续靠 converter 猜。

### 6. Spawn Boundary And Catalog Immutability

`EntitySpawnConfig.Config` 不再作为 Data 字段载体。visual scene 覆盖只能来自 `RuntimeDataRecordDto` 或显式 `VisualScenePathOverride`，删除通过 `GeneratedDataKey.VisualScenePath.StableKey` 反射读取 object config 的回退。

`DataDefinitionCatalog` 在 `ValidateAndBuildIndexes()` 后必须 frozen；默认 bootstrap 构建完成后禁止二次 `Register()`。

### 7. Query And Docs Gate

运行时生产链路使用 table/id 查询 record；display name lookup 降级为 debug/editor/test helper，并改名体现风险。

current 文档只保留 descriptor-first / snapshot-first / generated handle / catalog-bound Data 事实源。旧 DocsAI 不恢复；`Src/ECS/Base/**` 旁文档和 `DocsNew` 需要区分 current、historical、bug/follow-up。

## Verification

最低验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/generate-runtime-snapshot.sh Data/DataOS/Authoring/slimeainew.authoring.db Data/DataOS/Snapshots/runtime_snapshot.json
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '(.descriptors | map({key: .stableKey, value: .valueType}) | from_entries) as $d | .records[] | .table as $table | .id as $id | .fields | to_entries[] | select(($d[.key] // "__missing__") != .value.type) | [$table,$id,.key,($d[.key] // "missing_descriptor"),.value.type] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

行为验证：

- runtime apply 测试覆盖 `unit.player/player.deluyi`、代表性 `unit.enemy`、`ability.dash`。
- Godot 场景证明玩家进入 `PlayerInput` strategy、敌人进入 `AIControlled` strategy、技能实体创建成功且手动技能列表可发现预期技能。

文档和旧入口门禁：

```bash
cd /home/slime/Code/SlimeAI
rg -n "class DataMeta|class DataRegistry|public static implicit operator string|RuntimeTables|DataKey\\.XXX\\.Key|const string TargetNode|new Data\\(" SlimeAI/DocsNew SlimeAI/Src/ECS/Base -g '*.md'
rg -n 'Data\\.(Get|Set|Has|Remove)<[^>]+>\\("|Data\\.(Get|Set|Has|Remove)\\("' SlimeAI/Src/ECS/Base SlimeAI/Data -g '!*.md' -g '!*.uid'
```
