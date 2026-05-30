# Data 系统现状复查与兼任问题

> 更新：2026-05-29
> 状态：复查结论，作为后续执行型 SDD 的输入。
> 立场：旧写法完全放弃，不能与 descriptor-first DataOS 路线长期兼任。

## 1. 复查结论

当前设计裁决已经明确：Data 系统目标不是“旧 C# DataMeta / DataRegistry / RuntimeTables 旁边新增 DataOS”，而是完整重构为单一路线：

```text
DataOS SQLite authoring
    -> runtime_snapshot.json
         ├── descriptors  字段定义事实源
         └── records      初始记录事实源
    -> DataDefinitionCatalog
    -> DataRuntimeStorage / DataRuntimeBootstrap
    -> Entity.Data
```

再次复查当前代码后，结论是：

- `Data/DataOS/RuntimeTables/` 仍是最大兼任残留。它被文档描述为 snapshot-backed DTO 外壳，但代码实际仍通过反射读取手写 `public static readonly` 静态实例。
- `DataMeta` / `DataRegistry` 已不再大量作为字段定义注册入口，但运行时 fallback 路径、类本身、测试辅助和部分规则文档仍在。
- 多个业务系统仍直接消费 RuntimeTables 的 `.All` / `.Get`，使手写 C# 表仍具备事实源能力。
- DataOS generator 已能从业务表生成 snapshot，但 field 投影规则仍集中在 shell SQL 中手写，`dataos_runtime_field_stream` 表尚未成为主投影输入。
- 工具、DocsAI、AGENTS/CLAUDE 规则仍混有旧 DataNew / DataOS runtime table / DataMeta 说法，会继续误导后续 AI。

因此，当前状态不能视为“旧写法已完全放弃”。它更准确地处在“descriptor-first 主链路已建立，但旧表和旧 API 仍在兼任”的中间态。

## 2. 不允许兼任的边界

后续执行必须采用强边界，不再使用“兼容保留”描述长期目标。

| 旧入口 | 当前问题 | 最终判定 |
| ---- | ---- | ---- |
| `DataMeta` | 可承载默认值、范围、computed lambda 和依赖，天然会重新变成字段事实源。 | 删除或降级为历史审计输入；运行时不再查询。 |
| `DataRegistry` | 允许注册和查询 `DataMeta`，`Data` fallback 仍调用它。 | 删除运行时依赖；不能作为 fallback。 |
| `new Data()` 未绑定 catalog | 会走旧字典 + DataRegistry 路径，允许 unknown key 和默认值 fallback。 | 禁止新代码使用；最终删除或改为必须传 catalog。 |
| `Data/DataOS/RuntimeTables/*.cs` 手写静态实例 | 与 DataOS seed / snapshot 形成第二套业务数据。 | 删除手写数据；若保留 API，必须由 snapshot 生成或运行时只读 snapshot。 |
| `DataTable.GetAll<T>()` 反射扫描静态字段 | 反射读取 C# 表，不读取 snapshot。 | 删除；替换为 snapshot record query。 |
| `EntityManager.TryResolveRecordByConfig` | 通过 config 类型名 + `Name` 推断 record，维持 RuntimeTables config 对象入口。 | 删除；Spawn 必须显式传 record table/id 或 record handle。 |
| `DataKey` compatibility alias | 生成器仍输出 `DataKey.Xxx = GeneratedDataKey.Xxx` 兼容层。 | 删除兼容 alias，统一唯一 typed handle 命名。 |
| DataConfigEditor DataNew 扫描 | 工具仍扫描旧 `Data/DataNew`。 | 删除旧工具路径或改为 DataOS authoring DB 编辑器。 |
| 文档中的 DataOS runtime table 旧说法 | 后续 AI 会继续走手写 C# 表。 | 同步清理，文档只允许 DataOS + snapshot 主链路。 |

## 3. 当前证据

### 3.1 RuntimeTables 不是生成物

复查到的生成器只有：

- `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`：生成 `Data/DataOS/Snapshots/runtime_snapshot.json`。
- `SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py`：生成 `Data/DataKey/Generated/DataKey_Generated.cs`。

没有脚本生成 `Data/DataOS/RuntimeTables/*.cs`。

相反，`SlimeAI/Data/DataOS/RuntimeTables/DataTable.cs` 明确写着：

```text
表由 C# 类型表示，例如 EnemyData / AbilityData；行由 public static readonly 静态实例表示。
扫描程序集中所有 T 的非抽象子类，收集其 public static readonly 字段值作为数据行。
```

这说明 RuntimeTables 当前仍是手写 C# 数据表，不是 snapshot-backed DTO。

### 3.2 RuntimeTables 仍被业务系统当数据源

当前命中包括：

- `SlimeAI/Src/ECS/Base/System/Spawn/SpawnSystem.cs` 使用 `EnemyData.All` 初始化生成规则。
- `SlimeAI/Src/ECS/Base/System/Core/Config/SystemConfigService.cs` 使用 `SystemData.All` 初始化系统配置。
- `SlimeAI/Src/ECS/Base/System/Core/Config/SystemPresetService.cs` 使用 `SystemPresetData.All` 初始化预设。
- `SlimeAI/Src/ECS/Base/System/TestSystem/Ability/AbilityTestService.cs` 使用 `AbilityData.All` 构建测试面板技能列表。
- `SlimeAI/Data/ResourceManagement/ResourceCatalog.cs` 使用 `EnemyData.All`、`PlayerData.All`、`TargetingIndicatorData.All`、`AbilityData.All` 构建资源目录条目。

这些不是“薄兼容 API”。这些调用让 RuntimeTables 继续决定运行时能看见哪些敌人、技能、系统和资源目录项。

### 3.3 RuntimeTables 与 DataOS 已发生漂移

`SlimeAI/Data/DataOS/Tools/validate-dataos.sh` 中有 `target_point_skill_removed` 检查：

```text
SELECT id || ':' || name FROM ability WHERE name = '位置目标' OR id = 'ability.target_point_skill';
```

但 `SlimeAI/Data/DataOS/RuntimeTables/Ability/AbilityData.cs` 仍有 `TargetPointSkill` 静态实例，`Name = "位置目标"`。

这证明“数据库真相源 + 手写 RuntimeTables”已经不是理论风险，而是现实数据漂移。

### 3.4 Entity spawn 仍保留 RuntimeTables config 入口

`EntityManager.ApplySpawnData` 当前优先支持：

```text
RuntimeDataRecord
RuntimeDataRecordTable + RuntimeDataRecordId
TryResolveRecordByConfig(config.Config)
```

其中 `TryResolveRecordByConfig` 根据 `PlayerData` / `EnemyData` / `TargetingIndicatorData` / `AbilityData` / `ChainAbilityData` 类型名映射 table，再读取 `Name` 查 snapshot record。

这条路径虽然最终写入 `DataRuntimeBootstrap.ApplyToData`，但它仍要求调用方构造 RuntimeTables config 对象。最终形态不应需要任何手写 config 对象来 spawn。

### 3.5 Data 旧 fallback 仍存在

`SlimeAI/Src/ECS/Base/Data/Data.cs` 在 `_runtimeStorage == null` 时仍走旧路径：

- `Set<T>(string key, T value)` 查询 `DataRegistry.GetMeta(key)`，并可写入 `_data` 字典。
- `Get<T>(string key, object? defaultValue = null)` 查询 `DataRegistry.GetMeta(key)`；未注册时返回类型默认值或 `_data` 里的裸 key。
- modifier、computed、reset-by-category 等旧逻辑仍依赖 `DataRegistry` / `DataMeta`。

这意味着只要有代码继续 `new Data()` 且未绑定 catalog，就可以绕过 descriptor-first catalog。

### 3.6 DataRegistry/DataMeta 还在代码和测试中

当前源码仍存在：

- `SlimeAI/Src/ECS/Base/Data/DataRegistry.cs`
- `SlimeAI/Src/ECS/Base/Data/DataMeta.cs`
- `SlimeAI/Src/ECS/Test/SingleTest/ECS/DataOS/DataSceneTestBase.cs` 的 `Meta(...)` 测试辅助仍构造 `new DataMeta`

这不等同于旧字段注册仍大量存在，但它说明旧类型还没有从运行时模型中彻底退出。

### 3.7 DataOS 投影规则仍是脚本手写

`generate-runtime-snapshot.sh` 通过大段 `UNION ALL SELECT` 把 `unit_player`、`unit_enemy`、`ability`、`system_config` 等业务表列投影到 stable key。

这比 RuntimeTables 手写数据更接近正确方向，因为源数据来自 DataOS 业务表；但它仍有两个问题：

- 每新增字段要手改 shell SQL，容易遗漏 validator / docs / generated key 同步。
- `dataos_runtime_field_stream` 表已有 schema 和 validator 检查，但当前没有成为主投影输入，导致“投影定义”不够数据化。

最终目标应至少把投影规则迁到 DataOS schema / view / projection table / generator module，而不是在 shell 脚本中无限增长。

### 3.8 文档和规则仍会误导 AI 回旧路线

当前命中包括：

- `SlimeAI/AGENTS.md` / `SlimeAI/CLAUDE.md` 仍写“新增 DataKey 用 static readonly DataMeta + DataRegistry.Register”。
- `SlimeAI/addons/DataConfigEditor/*` 仍扫描和提示 `Data/DataNew`。
- `SlimeAI/DocsAI/Modules/*`、`SlimeAI/Data/README.md`、`SlimeAI/Src/ECS/Base/System/Core/README.md` 多处仍写 `Data/DataOS runtime table` 或 RuntimeTables 入口。

这些不是纯历史文档时，会直接影响 AI 路由和后续实现选择。

## 4. 剩余问题清单

### P0：RuntimeTables 手写数据必须退出

当前最严重的兼任问题是 RuntimeTables。它同时承担：

- 业务数据静态实例。
- `.All` 枚举入口。
- `.Get(name)` 查询入口。
- Spawn record 推断入口。
- ResourceCatalog 展示入口。
- TestSystem UI 数据入口。

目标状态：

```text
旧：RuntimeTables static readonly instance -> DataTable.GetAll/GetByName -> 系统消费
新：DataOS seed -> runtime_snapshot.records -> RuntimeDataRecordQuery/Service -> 系统消费
```

如果保留 `AbilityData.Get(name)` 这类 API，只能作为 snapshot-backed facade，且文件必须由 generator 生成或仅包含无数据的类型声明。不能再手写静态实例。

### P0：Data 旧 fallback 必须删除

`Data` 必须始终绑定 `DataDefinitionCatalog`。最终目标：

- 删除无 catalog 的 `Data()` 构造，或让它只用于测试并显式标记。
- 删除 `_data` / `_modifiers` / `_cachedValues` / `_dirtyKeys` 旧存储路径。
- 删除 `DataRegistry.GetMeta` fallback。
- unknown key 必须 fail fast，不返回类型默认值。
- computed / modifier 只走 `DataRuntimeStorage` + `DataDefinitionCatalog`。

### P0：Spawn 不再接受旧 config 对象推断

`EntitySpawnConfig.Config = enemyData` 是旧数据表兼任的核心入口。最终应改为：

```text
EntitySpawnConfig.RuntimeDataRecordTable = "unit.enemy"
EntitySpawnConfig.RuntimeDataRecordId = "enemy.yuren"
```

或：

```text
EntitySpawnConfig.RuntimeDataRecord = RuntimeDataRecords.UnitEnemy("enemy.yuren")
```

不再通过 `config.GetType().Name` 和 `Name` 反射推断。

### P0：系统配置不再读 SystemData.All

SystemCore 目前仍从 RuntimeTables 读取系统配置和预设。最终应改为：

- 从 `runtime_snapshot.records` 查询 `system.config` / `system.preset`。
- 或由 DataOS generator 生成 system config snapshot DTO。
- 系统注册代码只声明 `SystemId + Factory`，其余元数据来自 DataOS snapshot。

### P1：DataKey compatibility alias 退出

`generate-data-key-handles.py` 当前同时生成：

```text
GeneratedDataKey.Xxx
DataKey.Xxx = GeneratedDataKey.Xxx
```

如果目标是绝不兼任，应删除 `DataKey` alias 输出，统一唯一调用入口。可以选择直接把生成类命名为 `DataKey`，但不能再有“旧 DataKey 调用点兼容”语义。

### P1：ResourceCatalog 改为 snapshot/resource_entry 查询

`ResourceCatalog` 当前用 RuntimeTables `.All` 构建 Unit / Ability 目录。最终应从：

- `runtime_snapshot.resources`
- `runtime_snapshot.records`
- 或 DataOS 生成的 resource catalog DTO

构建目录，不能从手写 `EnemyData.All` / `AbilityData.All` 构建。

### P1：DataConfigEditor 路线废弃或重写

旧 DataConfigEditor 扫描 `Data/DataNew` 和 C# 静态实例。最终选择只有两个：

- 删除旧插件。
- 重写为 DataOS authoring DB / seed / migration 编辑器。

不能再以 C# POCO 源码作为编辑保存目标。

### P1：DataOS 投影规则数据化

`generate-runtime-snapshot.sh` 里的 `UNION ALL` 可以作为短期生成器实现，但不是最终稳定结构。后续应评估：

- SQL view 分层。
- projection table。
- generator 模块化脚本。
- `dataos_runtime_field_stream` 作为 projection 输出，不作为手写业务源。

重点是让新增字段的变更路径可验证、可定位，而不是在单个 shell SQL 中追加分支。

### P1：文档和规则一次性纠偏

需要清理：

- `SlimeAI/AGENTS.md`
- `SlimeAI/CLAUDE.md`
- `SlimeAI/Data/README.md`
- `SlimeAI/DocsAI/Modules/DataAuthoring.md`
- `SlimeAI/DocsAI/Modules/SystemCore.md`
- `SlimeAI/DocsAI/Modules/AbilitySystem.md`
- `SlimeAI/DocsAI/Modules/FeatureSystem.md`
- `SlimeAI/DocsAI/ProjectState.md`
- `SlimeAI/Src/ECS/Base/System/*/README.md`
- `SlimeAI/addons/DataConfigEditor/*`

清理目标是：非历史文档不得再推荐 `DataMeta`、`DataRegistry.Register`、`DataNew`、手写 RuntimeTables。

## 5. 建议执行顺序

### Slice A：冻结旧入口

目标：先防止继续新增旧写法。

- 添加 grep gate：禁止新增 `DataRegistry.Register`、`new DataMeta`、`DataTable.GetAll` 新调用、`DataNew`、`LoadFromConfig`。
- 更新 AGENTS / CLAUDE / DocsAI skill 源，删除旧推荐。
- 标注 RuntimeTables 为待删除 legacy，不允许新增静态实例。

### Slice B：建立 snapshot query service

目标：替代 RuntimeTables 的 `.All` / `.Get`。

- 为 `runtime_snapshot.records` 建立按 table/id/name 查询的只读 service。
- 为 unit / ability / system / resource catalog 提供 typed projection DTO。
- DTO 从 snapshot record fields 转换，不从 C# 静态实例读取。

### Slice C：迁移业务调用点

目标：让业务系统不再依赖 RuntimeTables。

- SpawnSystem：从 snapshot query 获取 enemy spawn rule。
- SystemConfigService / SystemPresetService：从 system records 获取配置。
- AbilityTestService / FeatureDebugService：从 ability / feature records 获取展示与触发数据。
- ResourceCatalog：从 resource / record projection 构建目录。
- TargetingManager / Ability add：不再需要 RuntimeTables config 对象。

### Slice D：删除 RuntimeTables 手写数据与 Entity config 推断

目标：清掉最大兼任入口。

- 删除 `DataTable` 反射扫描。
- 删除 `AbilityData` / `EnemyData` / `PlayerData` / `SystemData` 手写静态实例。
- 删除 `EntityManager.TryResolveRecordByConfig`。
- `EntitySpawnConfig` 只接受显式 record table/id 或 record handle。

### Slice E：删除 DataRegistry/DataMeta runtime fallback

目标：Data 容器只有 descriptor-first 行为模型。

- 删除 `DataRegistry` 运行时查询。
- 删除 `DataMeta` 运行时计算和默认值 fallback。
- 删除未绑定 catalog 的 `Data` 路径。
- 更新 DataOS tests，不再构造 `DataMeta`。

### Slice F：清理工具和长期文档

目标：避免 AI 后续继续回到旧路线。

- DataConfigEditor 删除或重写为 DataOS authoring 工具。
- DocsAI / DocsNew / module README 全部同步 descriptor-first。
- 历史文档保留时必须标注“历史，不作为事实源”。

## 6. 验收 grep gate

执行型 SDD 完成前，源码主路径应满足：

```text
rg -n "DataRegistry\\.Register|new DataMeta|DataMeta\\.Compute|LoadFromConfig|DataKey\\.DefaultValue" SlimeAI/Src SlimeAI/Data
```

只允许历史文档或明确 legacy audit 文件命中，不能有运行时依赖。

```text
rg -n "DataTable\\.GetAll|EnemyData\\.All|AbilityData\\.All|SystemData\\.All|SystemPresetData\\.All" SlimeAI/Src SlimeAI/Data
```

应无运行时命中。

```text
rg -n "DataNew|Data/DataOS runtime table|DataOS runtime table 纯 C#|static readonly .*Data" SlimeAI/Src SlimeAI/Data SlimeAI/DocsAI SlimeAI/DocsNew SlimeAI/addons
```

应无非历史命中。

```text
rg -n "TryResolveRecordByConfig|RuntimeDataRecordTable|RuntimeDataRecordId" SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs
```

最终只允许显式 record table/id 或 record handle 分支，不允许 config type/name 推断分支。

## 7. 最终裁决

旧写法不再承担任何长期职责：

- 不作为字段定义事实源。
- 不作为业务数据事实源。
- 不作为运行时 fallback。
- 不作为测试事实源。
- 不作为工具编辑目标。
- 不作为文档推荐路径。

允许短期执行中读取旧文件做审计或迁移输入，但每个保留点必须有删除条件和验收 grep gate。没有删除条件的“兼容层”就是新的双系统，应直接判定为设计错误。
