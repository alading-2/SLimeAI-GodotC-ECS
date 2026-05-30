# Data 重构后剩余框架问题深度复查

> 日期：2026-05-30  
> 范围：`SlimeAI/Src/ECS/Base/Data/`、`SlimeAI/Data/DataOS/`、`SlimeAI/Data/DataKey/Generated/`、`SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs`、`SlimeAI/DocsNew`、`SlimeAI-AiFirst/GameOS/Runtime/Data/`。  
> 关系：本文件是 SDD-0021 收口后的当前状态复查，承接历史审计结论，但只记录当前仍然成立的问题。  

## 0. 总结论

当前 Data 主链路已经不是 `06` 文档描述的最坏状态。旧 `DataMeta` / `DataRegistry` runtime 源码、generated `DataKey.Xxx` 兼容 alias、`Data/Data`、`Data/DataNew`、`RuntimeTables` 等大头兼容入口已经退出，`runtime_snapshot.json` 里的 descriptor / record 类型也已对齐。

所以现在再说“完全重构还是在兼容”不够精确。更准确的判断是：

```text
旧兼容入口大头已清掉；
但 Data 的中层契约仍然太软：
generator 投影仍手写、
runtime projection 仍手写字段名、
runtime 写入失败仍缺结构化诊断、
object_ref 语义仍混用、
同程序集 internal string API 与旧文档仍会把裸字符串写法带回来。
```

这会造成一个体感：每次改 Data 好像又炸一片。根因不是 DataOS / descriptor-first 方向错，而是 Data 在 SlimeAI 旧 ECS 中是全局控制面，任何字段定义变化都会穿过 authoring、generator、snapshot、typed handle、Entity spawn、ResourceCatalog、Feature bridge、AI/Movement/Ability 调用点、测试场景和文档。旧对象字典能吞掉的漂移，现在 descriptor-first 会 fail-fast 暴露出来。

## 1. 为什么 AiFirst 看起来没那么多问题

### 1.1 直接证据

`SlimeAI-AiFirst/GameOS/Runtime/Data/Data.cs` 的主 API 是：

- `Set<T>(DataKey<T> key, T value)`
- `Get<T>(DataKey<T> key)`
- `TryGet<T>(DataKey<T> key, out T value)`

它没有把 public string-key API 放在业务主入口。`DataCatalog` 也通过 builder 构造 catalog，对外呈现为运行时合约对象。

但是 AiFirst 并不是“没有同类问题”，而是问题面小很多：

- `RuntimeDataSnapshot.ApplyRecordWithReport()` 仍然把 snapshot descriptor 作为 mirror 检查到 C# `DataKey` 类型上，而不是纯 descriptor-first。
- `DataCatalog.TryResolve(string stableKey, out IDataKey key)` 仍会回落到 `DataKeyRegistry.TryResolve()`，说明 catalog 边界仍不是完全封闭。
- `RuntimeDataField.TryToObject()` 当前只对 bool/int/float/double/string 做显式分支，其他类型靠 raw text + `key.TryConvert()`，复杂类型压力还没充分进入运行时。

### 1.2 推断

AiFirst 目前更干净，主要因为它是 clean-room 框架切片：

- 没有旧 Godot C# ECS 的历史组件、测试场景、`.tres` 文档和 DataKey 文档债。
- 没有旧 `EntityManager.Spawn` 的 object config / reflection / snapshot record 混用路径。
- 没有大量既有业务系统同时读写同一批 Data 字段。
- 没有“旧对象字典宽松行为”向“descriptor-first 严格行为”切换时的迁移爆炸。

因此 AiFirst 只能证明方向是合理的，不能证明 SlimeAI 旧 ECS 可以只复制 AiFirst Data 代码就安全完成重构。SlimeAI 的难点在历史集成面和生成链路治理，不在单个 `Data.cs` 容器。

## 2. 当前已修正的旧问题

以下是当前已不应再作为 P0 执行依据的问题：

| 项 | 当前证据 | 结论 |
| --- | --- | --- |
| `DataMeta` / `DataRegistry` runtime 源码 | `rg "class DataMeta|class DataRegistry|public static implicit operator string" SlimeAI/Src/ECS/Base/Data SlimeAI/Data -g '!*.uid'` 无命中 | runtime 编译面已退出。 |
| generated `DataKey.Xxx` compatibility alias | `Data/DataKey/Generated/DataKey_Generated.cs` 只暴露 `GeneratedDataKey` | alias 大头已删除。 |
| 非标量 handle 类型 | `AbilityIcon` 为 `DataKey<ResourceRef>`，`AvailableAnimations` 为 `DataKey<string[]>`，`Feature.Modifiers` 为 `FeatureModifierEntryData[]`，`TargetNode` 为 `Godot.Node2D` | `06` 中的 `string` handle 回归已修正。 |
| `Data/Data` / `Data/DataNew` | `find SlimeAI/Data -maxdepth 3 -type d` 未见旧目录 | 旧 Data 输入目录已不在当前目录结构。 |
| `RuntimeTables` | 当前为 `RuntimeModels` | 旧目录名已退出。 |
| DataOS authoring 校验 | `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db` 通过 | 基础 DB 校验当前通过。 |
| snapshot descriptor/record 类型 | jq gate 无输出 | 当前 `runtime_snapshot.json` 没有 descriptor / record type mismatch。 |

这说明后续不能再照抄 `06` 的所有 P0 清单执行。真正要处理的是下面这些仍然存在的结构性问题。

## 3. 仍然成立的问题分级

### P0-1：snapshot 投影仍是手写中层事实源

证据：

- `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh:34` 开始的 `field_rows AS (...)` 仍手写每个业务表到 Data field 的映射。
- `generate-runtime-snapshot.sh:180-193` 会 join descriptor 并使用 `d.descriptor_type AS value_type`，这修掉了 record type 漂移，但没有修掉“字段投影仍靠脚本手写”的问题。
- `SlimeAI/Data/DataOS/Schema/core.sql:281` 存在 `dataos_runtime_field_stream`，但当前 DB 查询结果是 `dataos_runtime_field_stream|0`，说明这个结构还不是实际投影事实源。

判断：

这不是兼容残留，而是“generator 仍是第二事实源”。新增字段或改业务表时，AI 必须同时理解 schema、seed、descriptor、shell SQL 投影和 runtime projection，任何一处漏改都会在 runtime 才暴露。

建议：

- 把 `field_rows` 迁移为 schema/view/table 驱动的投影定义，例如 `runtime_field_projection` 或启用 `dataos_runtime_field_stream`。
- generator 只消费结构化 projection stream，不再维护大段 union SQL。
- validator 必须检查 projection stream 与最终 `runtime_snapshot.json` 一致。

最低门禁：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '(.descriptors | map({key: .stableKey, value: .valueType}) | from_entries) as $d | .records[] | .table as $table | .id as $id | .fields | to_entries[] | select(($d[.key] // "__missing__") != .value.type) | [$table,$id,.key,($d[.key] // "missing_descriptor"),.value.type] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
```

第二条命令必须无输出。

### P0-2：runtime projection 仍手写 stable key 字符串

证据：

- `SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataRecordQuery.cs:215-232` 的 `ToUnitSpawnDefinition()` 直接读 `"Name"`、`"VisualScenePath"`、`"SpawnInterval"` 等字符串。
- `RuntimeDataRecordQuery.cs:238-249` 的 ability view projection 也直接读 `"Name"`、`"AbilityFeatureGroup"`、`"FeatureHandlerId"` 等字符串。
- `RuntimeDataRecordQuery.cs:255-290` 的 system config / preset projection 继续手写字段列表。

判断：

这层已经不是旧 Data 兼容，但它仍让字段名在 C# projection 中再出现一次。descriptor-first 的 DataKey handle 已生成，projection 却没有消费 generated handle 或 generated projection contract。

风险：

- 改 descriptor stable key 后，Data 容器能 fail-fast，但 projection 字符串不会编译期失败。
- AI 新增系统级 record 时容易只改 DB / snapshot，忘记同步 C# projection。
- projection 错误会表现为 spawn、system mount、test panel 等跨模块失败。

建议：

- 为常用 runtime view 生成 projection DTO reader，或至少用 `GeneratedDataKey.Xxx.StableKey` 替代裸字符串。
- projection 层要有独立测试：每个 runtime view 从 snapshot 读完整 record，并覆盖 missing field / wrong type 诊断。

### P0-3：runtime 写入失败没有结构化诊断

证据：

- `SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs:921-925` 调用 `DataValueConverter.TryApplyWritePolicies(..., out _)`，但丢弃 error，只返回 `false`。
- `SlimeAI/Src/ECS/Base/Data/Data.cs:124-128` 的 typed `Set<T>` 最终也是 bool 结果。
- snapshot apply 已有 `DataApplyReport`，但普通 runtime `Set` / modifier 写入没有同级报告。

判断：

descriptor-first 变严格后，“失败返回 false”对 AI 不够。AI 需要知道是 unknown key、wrong CLR type、write_policy、range_policy、computed/runtime_only 还是 conversion 失败。否则每次改 Data 都会退回人工 grep 和日志猜测。

建议：

- 增加 `DataWriteReport` / `DataWriteError`，至少给 `TrySet` / `SetUntyped` 提供可选 report 或 `out string error`。
- 对 runtime 写入失败统一记录 stable key、expected type、actual type、source、policy、raw value。
- 测试断言错误 code，而不是只断言 false。

### P0-4：`object_ref` 同时表达资源引用和运行时对象引用

证据：

- `DataValueConverter.IsCompatible()` 中 `ObjectRef` 接受 `ResourceRef` 或任何非值类型且非 string 的 CLR 类型。
- generated handle 中 `AbilityIcon` 是 `DataKey<ResourceRef>`，`TargetNode` 是 `DataKey<Godot.Node2D>`；两者都落在 `object_ref`。
- `ConvertObjectRef()` 对 string 会转 `ResourceRef`，对运行时对象又允许直接保留引用。

判断：

`object_ref` 现在混合了两类完全不同的生命周期：

- authoring / snapshot 可序列化的资源路径。
- runtime only 的 Godot Node / Node2D 引用。

这会让 descriptor 的 `runtimeTypeId` 很难成为真正约束。资源路径字段和运行时对象字段共享同一个 value type，validator、snapshot loader、DataKey generator 都只能靠约定理解它们。

建议：

- 拆分成 `resource_ref` 与 `runtime_ref` / `node_ref`，或至少在 `object_ref` 上强制校验 `runtimeTypeId`。
- snapshot records 中只允许写 `resource_ref`，runtime-only Node 引用禁止从 snapshot 注入。
- `TargetNode` 这类字段应明确 `storage_policy = runtime_only`，并由 runtime API 写入。

### P1-1：`EntitySpawnConfig.Config` 仍是 object，视觉注入仍有反射回退

证据：

- `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs:22-35` 中 `Config` 是 `required object`，同时并存 `RuntimeDataRecord` / `RuntimeDataRecordTable` / `RuntimeDataRecordId`。
- `EntityManager.cs:343-371` 的 `InjectVisualScene()` 优先读 `RuntimeDataRecordDto`，之后仍通过 `config.GetType().GetProperty(GeneratedDataKey.VisualScenePath.StableKey)` 反射读取 `VisualScenePath`。

判断：

这已经不是通过 config 推断 snapshot record 的旧路径，但仍保留了“局部 object config 可以携带 Data 字段”的门。它会让 spawn API 在类型上无法表达：哪些参数是 runtime override，哪些参数来自 snapshot record。

建议：

- 拆成强类型 `EntitySpawnConfig<TLocalOptions>`，或把视觉覆盖从 `Config` 中彻底独立为 `VisualSceneOverride` / `VisualScenePathOverride`。
- 禁止通过反射读取 Data stable key。
- spawn 必须只从 `RuntimeDataRecord` 或 explicit override 写 Data。

### P1-2：`new Data()` 隐式绑定全局 default bootstrap

证据：

- `SlimeAI/Src/ECS/Base/Data/Data.cs:23-26` 中默认构造函数直接绑定 `DataRuntimeBootstrap.Default.Catalog`。
- `DocsNew/ECS/Data/Data系统说明.md` 当前也说明 `new Data()` 会绑定默认 catalog。

判断：

这比“未绑定 Data fallback 到旧 DataRegistry”安全很多，但它把实体构造、默认 snapshot 文件读取、全局 bootstrap 生命周期绑在了一起。测试隔离和多 profile catalog 会更难判断。

建议：

- 生产实体由 `EntityManager` 或工厂显式注入 catalog/bootstrap。
- `new Data()` 只保留给最外层默认框架 profile，测试必须显式传 `DataDefinitionCatalog`。
- 如果保留默认构造，至少在文档中标为 convenience，不作为新系统推荐入口。

### P1-3：internal string-key API 仍能在同程序集回流

证据：

- `SlimeAI/Src/ECS/Base/Data/Data.cs` 仍有 internal `Set<T>(string key, T value)`、`Get<T>(string key)`、`Has(string)`、`Remove(string)`、`SetUntyped(string, ...)`、`AddModifier(string, ...)` 等。
- `SlimeAI/Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.cs`、`DataSnapshotApplyTestScene.cs` 等测试大量用裸字符串验证。
- 生产侧仍能看到 `GetRequiredByName()` 和部分稳定字符串入口；同一 Godot C# 项目下 internal 不等于架构硬边界。

判断：

这些 API 对迁移测试很有用，但它们继续存在会让 AI 在同程序集里轻易写回裸字符串。编译器不会强制业务代码只能走 `DataKey<T>`。

建议：

- internal string API 集中到测试 helper 或 `DataRuntimeStorage` 的底层 private/internal 边界，不暴露在业务 `Data` 容器上。
- production 目录加 grep gate，禁止 `Data.Get<T>("...")` / `Data.Set("...")`。
- 测试也尽量改为 generated handle；确实测试 unknown key 时使用专用 helper。

### P1-4：`DataDefinitionCatalog` 校验后没有真正冻结

证据：

- `SlimeAI/Src/ECS/Base/Data/DataDefinitionCatalog.cs:53-65` 的 `Register()` 仍 public。
- `DataDefinitionCatalog.cs:105-146` 的 `ValidateAndBuildIndexes()` 会重建依赖索引并做校验，但没有 `_isFrozen` 或 builder-only 约束。

判断：

注释上说“索引冻结”，代码上没有冻结。catalog 被运行时 Data 容器使用后仍可注册新 definition，索引与 storage slot 的一致性只能靠调用约定。

建议：

- 改为 `DataDefinitionCatalogBuilder.Build()` 生成不可变 catalog。
- 或至少 `ValidateAndBuildIndexes()` 后设置 frozen，后续 `Register()` 直接 throw。
- 对 default catalog 构建完成后禁止二次写入。

### P1-5：display name 仍可作为 record 查询入口

证据：

- `RuntimeDataRecordQuery.cs:76-84` 提供 `GetRequiredByName(table, name)`。
- 生产/测试调用包括 `TargetingManager.cs`、`SpawnTestModule.cs`、`MainTest.cs`、多个 SingleTest 场景。

判断：

显示名适合 UI，不适合作为稳定运行时 identity。中文显示名一旦重命名，record 查找会失败；AI 也会被鼓励用 display name 写测试和业务胶水。

建议：

- 生产代码只用 table/id。
- `GetRequiredByName()` 降为 editor/test helper，或改名为 `GetRequiredByDisplayNameForDebug()`。
- 测试中也优先断言 id。

### P1-6：JSON array / modifier list 的未来输入形态不稳

证据：

- `RuntimeDataSnapshotLoader.NormalizeRecordValue()` 和 `RuntimeDataRecordQuery.NormalizeRecordValue()` 对 JSON array 使用 `GetRawText()`。
- `DataRuntimeStorage.ConvertStringArray()` 只接受 `string[]` 或逗号分隔 string。
- `ConvertModifierList()` 只接受 `FeatureModifierEntryData[]`，或空字符串 / `"[]"`。

判断：

当前 generator 多数把 string_array 写成逗号字符串，所以门禁通过。但如果未来 snapshot 真的输出 JSON array，string_array 会被当成 raw JSON 文本再按逗号拆；非空 modifier JSON array 也不能被解析成 `FeatureModifierEntryData[]`。

建议：

- 明确 snapshot 的数组字段标准：要么 records 只输出 JSON array，要么只输出逗号字符串，但不能两者都靠 converter 猜。
- loader 中对 `string_array` 和 `modifier_list` 按 descriptor 类型显式解析 JSON array。
- validator 增加非空数组 fixture。

### P2-1：旧 DocsAI 已删除，当前文档事实源回到 Src 旁文档

证据：

- `SlimeAI/DocsNew/ECS/Data/Data系统说明.md` 当前主链路描述基本正确，已明确不再用 `DataMeta` / `DataRegistry` 作为事实源。
- 用户已裁决 `SlimeAI/DocsAI` 也是旧入口，并且目录已经删除；不要恢复或继续局部修补，不再作为当前 Data 文档同步目标。
- 旧模块 README 仍有大量 `DataKey.Xxx`、`DataMeta`、`.tres`、旧本地 file URL 和旧 DataKey 文件路径示例。
- 典型位置包括 `Src/ECS/Base/System/TestSystem/README.md`、`Src/ECS/Base/Entity/Core/EntityManager.md`、`Src/ECS/Base/Component/Component规范.md`、`Src/ECS/Base/Component/Ability/CostComponent/README.md`。

判断：

这不是 runtime bug，但会直接影响 AI-first 工作流。AI 读到旧 README 或旧 DocsAI 后，会继续生成旧 DataKey / DataMeta / `.tres` 方案。

建议：

- `SlimeAI/DocsAI` 不再修补，也不再恢复。
- 当前需要先修的是 `Src/ECS/Base/**` 下已经恢复为主入口的旁文档。
- 加文档 grep gate，禁止非 historical 文档推荐 `DataMeta`、`DataRegistry`、`DataKey.XXX.Key`、`.tres` authoring、`new Data()` 作为主流程。

### P2-2：`ApplyDataAsModifiers()` / `GetAll()` 仍保留旧对象字典味道

证据：

- `Data.cs:432-452` 的 `ApplyDataAsModifiers()` 遍历 `sourceData.GetAll()`。
- `Data.cs:544` 仍暴露 `GetAll()`，返回 `Dictionary<string, object>`。

判断：

这类 API 适合 debug / migration / bulk copy，不适合长期业务能力。它们会绕过 generated handle，并把 Data 再次降级成 string-object 字典。

建议：

- 标为 debug/migration helper，或迁入测试/迁移工具。
- 长期业务改为显式 modifier grant / revoke 管线。

## 4. 为什么“一改 Data 全是问题”

### 4.1 Data 是全局控制面，不是普通容器

一次字段变化会穿过：

```text
DataOS schema / seed
  -> generator projection
  -> runtime_snapshot descriptors / records
  -> generated DataKey<T>
  -> DataDefinitionCatalog
  -> DataRuntimeStorage converter / policy / modifier / computed
  -> EntityManager.Spawn
  -> ResourceCatalog / Feature / Ability / AI / Movement / TestSystem
  -> Godot 场景测试和文档路由
```

任何一层仍靠手写字符串或旧 object 约定，都会把“字段变化”放大成跨模块问题。

### 4.2 旧系统宽松，新系统严格

旧对象字典能接受：

- key 不存在时默认值兜底。
- string / enum / object 混写。
- List / array / string 混用。
- config / resource / DataOS 多源并存。

descriptor-first 的 Data 正确地把这些不一致暴露出来。报错变多本身不是坏事；坏的是缺少足够结构化的门禁和诊断，导致错误晚暴露、难定位。

### 4.3 “完全重构”目前清的是入口，不是所有中层契约

当前已经清掉了旧入口大头，但还有这些中层事实源：

- `generate-runtime-snapshot.sh` 的手写 `field_rows`。
- `RuntimeDataRecordProjection` 的手写 field key。
- `EntitySpawnConfig.Config` 的 object / reflection 边界。
- `Data` 的 internal string API。
- `DataDefinitionCatalog` 的可变 register。
- 旧文档对 AI 的误导。

这些不一定叫“兼容”，但效果和兼容一样：它们让一条 descriptor-first 链路旁边继续存在弱契约入口。

## 5. 建议下一步不要再叫“兼容清理”

下一步建议单独开一个 SDD，名称可以是：

```text
Data Projection / Diagnostics Contract Hardening
```

目标不是再做一轮大删除，而是把 Data 的中层契约硬化：

1. **Projection hardening**：用结构化 projection stream 替代 `field_rows` 手写 SQL。
2. **Generated projection / stable key hardening**：runtime projection 不再裸写 field key。
3. **Diagnostics hardening**：runtime write / modifier / policy failure 输出结构化 error。
4. **Reference type hardening**：拆分或强约束 `object_ref`。
5. **Spawn boundary hardening**：`EntitySpawnConfig.Config` 不再承载 Data 字段，不再反射读取 stable key。
6. **Catalog immutability**：catalog build 后不可变。
7. **Docs gate**：非 historical 文档不得推荐旧 Data 路线。

## 6. 推荐验收门禁

### 6.1 DataOS / snapshot 门禁

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '(.descriptors | map({key: .stableKey, value: .valueType}) | from_entries) as $d | .records[] | .table as $table | .id as $id | .fields | to_entries[] | select(($d[.key] // "__missing__") != .value.type) | [$table,$id,.key,($d[.key] // "missing_descriptor"),.value.type] | @tsv' Data/DataOS/Snapshots/runtime_snapshot.json
```

### 6.2 旧入口门禁

```bash
cd /home/slime/Code/SlimeAI
rg -n "class DataMeta|class DataRegistry|public static implicit operator string|DataKey_Compatibility|RuntimeTables|Data/DataNew|Data/Data/" SlimeAI/Src/ECS/Base/Data SlimeAI/Data SlimeAI/DocsNew -g '!*.uid'
```

当前期望：无输出。

### 6.3 裸字符串门禁

```bash
cd /home/slime/Code/SlimeAI
rg -n 'Data\.(Get|Set|Has|Remove)<[^>]+>\("|Data\.(Get|Set|Has|Remove)\("' SlimeAI/Src/ECS/Base SlimeAI/Data -g '!*.md' -g '!*.uid'
```

当前建议：允许测试 helper 和底层 storage，禁止 production 业务层新增。

### 6.4 文档门禁

```bash
cd /home/slime/Code/SlimeAI
rg -n "DataMeta|DataRegistry|DataKey\\.XXX\\.Key|DataKey\\.[A-Za-z]|\\.tres|DataNew|RuntimeTables|new Data\\(" SlimeAI/DocsNew SlimeAI/Src/ECS/Base -g '*.md'
```

当前预期：会命中大量历史文档。下一步不是全部删除，而是区分 current / historical，并让 current 入口只指向 descriptor-first。

## 7. 结论裁决

Data 框架方向没有错。现在真正的问题是：

- 旧兼容入口已经删掉一批，但中层投影和运行时诊断还没有工程化。
- AiFirst 示例更干净，但它没有承受旧 ECS 的历史集成面，不能直接作为“SlimeAI 不该出问题”的证据。
- 下一轮应该从“清兼容”切到“硬化 contract”：projection、diagnostics、object_ref、spawn boundary、catalog immutability、docs gate。

如果只继续做零散修 bug，Data 每改一次仍会沿着 generator / projection / spawn / docs 四条线扩散。只有把这些中层事实源收口，DataOS descriptor-first 才会真正变成 AI-first，而不是“看起来只有一个事实源，实际到处还有手写投影”。

## 8. 落地文档

本文件是当前 residual 问题总览。继续落地时，请接着看：

- [`05-Data残余问题代码修复分解.md`](./05-Data残余问题代码修复分解.md)
- [`06-Data文档更新与门禁清单.md`](./06-Data文档更新与门禁清单.md)

`05` 负责代码怎么改，`06` 负责文档怎么改和门禁怎么跑。
