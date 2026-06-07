# Data Snapshot-First Usage Cutover

## Goal

本 SDD 的目标是完成 Data 系统取用方式的 hard cutover：所有运行时配置、Entity 初始化记录、系统配置、测试面板展示数据和资源目录数据，都从 DataOS 生成的 runtime snapshot 或围绕 snapshot 建立的只读 query service 获取。

最终状态：

```text
DataOS SQLite authoring
  -> runtime_snapshot.json
      -> descriptors -> DataDefinitionCatalog
      -> records     -> RuntimeDataRecordQuery / typed projections
      -> resources   -> ResourceCatalog projection
  -> DataRuntimeBootstrap
  -> catalog-bound Entity.Data
```

非目标：

- 不继续维护手写 `Data/DataOS/RuntimeTables/*.cs` 静态实例。
- 不继续维护 `DataTable.GetAll<T>()` 反射扫描。
- 不继续允许 `EntitySpawnConfig.Config = EnemyData/AbilityData/...` 反射推断 snapshot record。
- 不继续让无 catalog `new Data()`、`DataRegistry`、`DataMeta` 作为 runtime fallback。
- 不修补旧 DataConfigEditor 的 `DataNew` 路线；要么删除，要么另行重写为 DataOS authoring 工具。

## Context Read

本 SDD 承接以下事实源：

- 项目级设计：`design/Runtime/2.Data系统优化/README.md`
- 完整重构裁决：`design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md`
- 现状复查：`design/Runtime/2.Data系统优化/04-Data系统现状复查与兼任问题.md`
- 现有 snapshot runtime：`SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/`
- 现有 DataOS generator：`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`
- 现有手写 RuntimeTables：`SlimeAI/Data/DataOS/RuntimeTables/`
- 现有调用点：`SpawnSystem`、`SystemConfigService`、`SystemPresetService`、`AbilityTestService`、`ResourceCatalog`、`EntityManager.ApplySpawnData`

当前主要矛盾：

1. 文档宣称 RuntimeTables 是 snapshot-backed DTO 外壳，但实现仍扫描 C# 静态实例。
2. 多个系统直接读取 `.All` / `.Get`，使 RuntimeTables 继续成为业务数据事实源。
3. `EntityManager.TryResolveRecordByConfig` 使旧 config object 继续作为 Entity 初始化入口。
4. `Data.cs` 在未绑定 catalog 时仍有 `DataRegistry/DataMeta` fallback。
5. AGENTS/CLAUDE/DocsAI/addons 仍包含旧 DataMeta、DataNew、DataOS runtime table 说法。

## Main Risks

### R1: 一次性删除 RuntimeTables 会破坏大量调用点

RuntimeTables 当前被 Spawn、SystemCore、TestSystem、ResourceCatalog、Ability、Targeting 等调用。直接删除会导致编译大面积失败。

处理策略：先建立 snapshot query/projection service，再逐桶迁移调用点，最后删除旧表。

### R2: snapshot record fields 是通用结构，业务读取需要 typed projection

业务系统不能到处手写 `record.Fields["Xxx"]`。这样会把裸字符串问题从 DataKey 转移到 snapshot fields。

处理策略：提供集中 projection API，例如：

```text
RuntimeDataRecordQuery
DataProjection.ToEnemySpawnConfig(record)
DataProjection.ToSystemConfig(record)
DataProjection.ToAbilitySummary(record)
```

projection 内部可以使用 generated DataKey stable key 或集中字段常量；调用方只消费 typed DTO。

### R3: System config 与 Entity.Data 的边界容易混淆

系统配置不一定是 Entity runtime state，不能强行写入 Entity.Data。它仍应来自 snapshot records，但由 SystemConfigService 读取 typed system DTO。

处理策略：records 是统一输入层；是否写入 Entity.Data 由 owner 决定。Entity spawn record 写入 Data；System config record 投影成 system config DTO。

### R4: DataMeta/DataRegistry 删除可能影响测试和调试 UI

旧 TestSystem 可能依赖 DataMeta 展示分类、范围、modifier 能力。

处理策略：调试 UI 改读 `DataDefinitionCatalog.Definitions` 和 descriptor presentation 字段；测试辅助改用 `DataDefinition` builder，不构造 `DataMeta`。

### R5: 文档/规则不清理会导致后续 AI 回旧路线

AGENTS/CLAUDE 和 DocsAI 是 AI 路由事实源。只改代码不改文档，会继续产生旧写法。

处理策略：本 SDD 将文档和 rule cleanup 纳入验收，而不是后置可选项。

## Options

### Option A: 保留 RuntimeTables API，底层改成 snapshot-backed

做法：保留 `AbilityData.Get(name)` / `EnemyData.All` 等 API，但移除静态实例和反射扫描，改为从 snapshot records 构造 DTO。

优点：

- 调用点迁移量小。
- 可以渐进替换底层实现。

缺点：

- API 名称仍携带旧 runtime table 概念。
- 容易被误解为允许继续手写 C# table。
- 需要额外 gate 确保没有静态实例回流。

### Option B: 删除 RuntimeTables API，新增 snapshot query/projection service

做法：新增明确的 snapshot-first query/projection API；所有调用点迁移后删除 RuntimeTables。

优点：

- 边界最清楚，不再兼任。
- API 名称表达真实事实源。
- 后续 DataOS generator 和 validator 更容易统一。

缺点：

- 初始迁移量更大。
- TestSystem / ResourceCatalog / SystemCore 都要同步改。

### Option C: 先只加 grep gate，不迁移调用点

做法：禁止新增旧写法，旧调用点留待以后。

优点：

- 风险最低，改动最少。

缺点：

- 不能解决用户明确要求的“取用改成最新 Data 系统形式”。
- 兼任状态继续存在，RuntimeTables 仍是事实源。

## Recommendation

采用 Option B。

理由：

- 用户明确要求“旧写法完全放弃，绝不兼任，完全重构”。
- 当前 RuntimeTables 已发生 DataOS 漂移，保留 API 会继续混淆事实源。
- descriptor-first DataOS 主链路已存在，真正缺的是统一取用层和调用点迁移。
- 通过 typed projection service 可以避免到处裸读 `record.Fields`。

允许的短期过渡只限执行过程内部：可以先新增 query/projection service，再迁移调用点，最后删除旧 API。任何保留点必须有删除任务和 grep gate。

## Target Architecture

### 1. RuntimeDataRecordQuery

新增围绕 `DataRuntimeBootstrap.Snapshot` 的只读查询入口：

```csharp
public sealed class RuntimeDataRecordQuery
{
    public IReadOnlyList<RuntimeDataRecordDto> GetRecords(string table);
    public RuntimeDataRecordDto GetRequired(string table, string id);
    public RuntimeDataRecordDto GetRequiredByName(string table, string name);
}
```

要求：

- 不访问 SQLite。
- 不依赖 RuntimeTables。
- 查询失败 fail fast 或返回结构化错误，调用方不 silently fallback。
- 缓存按 table/id/name 构建，避免每次线性扫描 snapshot records。

### 2. Typed projection DTO

为当前消费点建立最小 DTO：

```text
UnitSpawnDefinition      -> SpawnSystem / ResourceCatalog
AbilityDefinitionView    -> AbilityTestService / EntityManager.AddAbility
SystemConfigDefinition   -> SystemConfigService
SystemPresetDefinition   -> SystemPresetService
ResourceCatalogProjection -> ResourceCatalog
```

这些 DTO 不是 authoring 源，不保存默认值事实源，只是 snapshot record 的 typed view。

### 3. Entity spawn 显式 record

`EntitySpawnConfig` 最终只允许：

```text
RuntimeDataRecord
RuntimeDataRecordTable + RuntimeDataRecordId
```

或等价的 typed record handle。

删除：

```text
TryResolveRecordByConfig(config.Config)
Config = EnemyData / PlayerData / AbilityData / TargetingIndicatorData
```

如果仍需要给视觉或系统传局部运行时参数，应使用独立 command/context，不再塞进 Data authoring config object。

### 4. System config snapshot-first

`SystemConfigService` / `SystemPresetService` 改为读取 `system.config` / `system.preset` records。系统注册代码仍只持有 `SystemId + Factory`，运行条件、标签、优先级、依赖等来自 snapshot projection。

### 5. Spawn snapshot-first

`SpawnSystem` 不再枚举 `EnemyData.All`。它读取 `unit.enemy` records，通过 projection 获取 spawn fields：

```text
IsEnableSpawnRule
SpawnStrategy
SpawnMinWave
SpawnMaxWave
SpawnInterval
SpawnMaxCountPerWave
SingleSpawnCount
SingleSpawnVariance
SpawnStartDelay
SpawnWeight
```

生成实体时传 record table/id，而不是传 `EnemyData` object。

### 6. Ability snapshot-first

`AbilityTestService` 和 `EntityManager.AddAbility` 不再接收 `AbilityData` config object。候选方案：

```text
AddAbility(owner, RuntimeDataRecordDto abilityRecord)
AddAbility(owner, AbilityRecordId abilityId)
AddAbility(owner, RuntimeDataRecordHandle abilityRecord)
```

推荐先实现 record/id 形式，避免引入新的 id 类型阻塞。

### 7. ResourceCatalog snapshot-first

`ResourceCatalog` 不再从 RuntimeTables `.All` 构造 DataUnit/DataAbility 条目。数据来自：

- `runtime_snapshot.resources` 中的资源条目。
- 或 `unit.*` / `ability` records 的 `VisualScenePath`、`IconPath`、`EffectScene`、`ProjectileScene` 等 projection。

### 8. Data container catalog-only

`Data` 无 catalog 路径退出：

- 删除或 obsolete 无参 `Data()`。
- 删除 `_runtimeStorage == null` 下的旧 get/set/modifier/compute 路径。
- `DataRegistry` / `DataMeta` 不再被 runtime 查询。
- 旧测试辅助改为 `DataDefinition`。

## Task Strategy

本 SDD 不再拆出“兼容保留”任务。任务顺序必须保持：

1. readiness / grep baseline。
2. 建立 snapshot query + projection 层。
3. 迁移 read-only 消费点。
4. 迁移 spawn / ability 初始化点。
5. 删除 RuntimeTables / DataTable / config 推断。
6. 删除 DataRegistry/DataMeta fallback。
7. 同步 docs/rules/tools。
8. 验证和 grep gate。

## Verification Strategy

最低验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

Godot 场景验证：

```bash
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn --build
```

最终 grep gate：

```bash
rg -n "DataTable\\.GetAll|EnemyData\\.All|AbilityData\\.All|SystemData\\.All|SystemPresetData\\.All" SlimeAI/Src SlimeAI/Data
rg -n "TryResolveRecordByConfig|DataRegistry\\.GetMeta|DataRegistry\\.Register|new DataMeta|DataMeta\\.Compute|LoadFromConfig|DataKey\\.DefaultValue" SlimeAI/Src SlimeAI/Data
rg -n "DataNew|Data/DataOS runtime table|DataOS runtime table 纯 C#" SlimeAI/Src SlimeAI/Data SlimeAI/DocsAI SlimeAI/DocsNew SlimeAI/addons SlimeAI/AGENTS.md SlimeAI/CLAUDE.md
```

允许命中范围必须显式列在 progress 中，且只能是历史设计文档、归档说明或本 SDD 的 grep gate 描述。

## Must Confirm

无需再确认“是否保留旧写法”：本 SDD 默认不保留。

可能需要用户确认的只有：

- 是否删除旧 DataConfigEditor，还是另开工具 SDD 重写为 DataOS authoring editor。
- Ability 添加 API 最终是否使用 record id 字符串，还是引入 typed `AbilityRecordId`。

默认推进策略：

- 先删除旧 DataConfigEditor 运行入口；如需要编辑器，再创建独立 SDD。
- Ability API 先使用 record table/id 或 `RuntimeDataRecordDto`，不引入新 ID 类型阻塞 Data 收口。
