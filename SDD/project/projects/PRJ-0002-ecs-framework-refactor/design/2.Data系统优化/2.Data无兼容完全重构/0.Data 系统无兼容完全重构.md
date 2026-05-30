# Data 系统无兼容完全重构总审计

> 更新：2026-05-29  
> 状态：新任务总审计结论，作为后续执行型 SDD / 修复任务的唯一输入账本。  
> 立场：Data 系统只允许一条链路；迁移材料可以读，运行时和新代码不允许保留兼容入口。  
> 覆盖范围：`SlimeAI/Src/ECS/Base/Data/`、`SlimeAI/Data/DataOS/`、`SlimeAI/Data/DataKey/`、`SlimeAI/Data/Feature/`、`SlimeAI/Data/Config/`、`SlimeAI/DocsAI`、`SlimeAI/DocsNew`、`Workspace/DocsAI/Temp/2-SDD0020后Data类型回归分析.md`、本目录 `01~05` 设计文档。  

## 0. 最直接的结论

Data 系统的思路没有错。正确流程非常简单：

```text
DataOS SQLite authoring
    -> 脚本生成
        -> Data/DataOS/Snapshots/runtime_snapshot.json
        -> Data/DataKey/Generated/DataKey_Generated.cs
    -> SlimeAI/Src/ECS/Base/Data 负责加载、校验、转换、读写
    -> 其他 Component / System / Entity 只消费 Data
```

现在问题多，不是因为这条路复杂，而是因为这条路没有硬收口。

用通俗的话说：数据库已经在说“字段是什么类型”，但后面的生成脚本、生成的 DataKey、业务调用点、文档还在偷偷按旧写法理解它。旧系统原来很宽松，能把这些不一致吞掉；SDD-0020 后 `Data` 开始严格按 descriptor 检查，所以这些旧兼容残留不再被吞，直接变成运行时报错。

真正根因不是“需要再兼容一下”，而是“兼容门还没关干净”。

## 1. 当前报错的直接根因

### 1.1 `AbilityIcon` 为什么报 `string != ObjectRef`

数据库 / descriptor 说：

```text
AbilityIcon = object_ref, runtimeTypeId = Texture2D
```

但 `generate-runtime-snapshot.sh` 仍然手写：

```text
AbilityIcon -> string
```

所以最终 `runtime_snapshot.json` 里出现了这种矛盾：

```text
descriptors: AbilityIcon = object_ref
records:     AbilityIcon = string
```

运行时 loader 严格检查 record field type 必须等于 descriptor type，于是拒绝创建 `ability.dash`。这是正确的失败；错的是生成物已经错了，validator 没有提前拦住。

对应证据：

- `SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql`：`AbilityIcon` 是 `object_ref`。
- `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`：`AbilityIcon` hardcode 为 `string`。
- `SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json`：当前有 10 条 `AbilityIcon` record/descriptor mismatch。
- `SlimeAI/Data/DataOS/Tools/validate-dataos.sh`：检查 `dataos_runtime_field_stream`，但该表当前 count 为 `0`，不是最终 snapshot。

### 1.2 `AvailableAnimations` 为什么报 `StringArray != List<string>`

数据库 / descriptor 说：

```text
AvailableAnimations = string_array
```

但生成和调用点变成了三套理解：

```text
GeneratedDataKey.AvailableAnimations = DataKey<string>
UnitAnimationComponent 写入 List<string>
AttackComponent / UnitAnimationComponent 读取 List<string>
DataValueConverter 只接受 string 或 string[]
```

所以 `Data.Get<List<string>>(GeneratedDataKey.AvailableAnimations)` 必然失败。

这不是 `List<string>` 少加一个兼容分支的问题。根本问题是 `string_array` 在 Data 系统里没有统一成唯一 CLR 类型，生成器还把它降级成了 `string`，业务层继续用旧对象字典的习惯写 `List<string>`。

无兼容裁决：

```text
string_array 的唯一运行时标准类型应收口为 string[]
```

调用点如需 `List<string>`，应自己把 `string[]` 转成 `List<string>`，不能让 Data slot 同时接受 `string`、`string[]`、`List<string>` 三种事实。

### 1.3 为什么 validator passed 但运行时炸

当前验证检查的是：

```text
dataos_runtime_field_stream
```

但真实生成 snapshot 的脚本是自己在 shell SQL 里拼 `field_rows`。也就是说：

```text
validator 检查的对象 != runtime_snapshot.json 最终交付物
```

所以 validator passed 不能证明 runtime snapshot 正确。无兼容重构后，最终门禁必须检查 `runtime_snapshot.json` 本身：descriptor、record、generated handle、runtime apply 都要一致。

## 2. 不是流程错，是事实源没有只剩一个

目标流程应该只有一个事实源方向：

```text
SQLite 业务表 / descriptor
    -> snapshot descriptors / records
    -> generated typed handle
    -> DataDefinitionCatalog / DataRuntimeStorage
    -> Entity.Data
```

当前残留的问题可以归成一句话：

```text
字段定义、字段类型、字段值、字段访问入口没有全部听 descriptor 的。
```

具体表现：

- snapshot record 还自己携带并决定 field type。
- handle generator 把 `string_array` / `object_ref` / `modifier_list` 降级成 `string`。
- `DataKey<T>` 可以隐式转 `string`，错误类型的 key 仍能进入 string API。
- `Data` 还暴露大量 public string-key API。
- docs 和 roadmap 仍写 SDD-0020 已完成旧路径退出。
- DocsNew 仍把 `DataKey.Xxx` alias、`new Data()`、RuntimeTables 兼容 API 当成可见事实。

## 3. 当前 04 / 05 文档的整合裁决

本文件覆盖 `04-Data系统现状复查与兼任问题.md` 和 `05-Data重构运行报错根因分析.md` 的当前判断。

`05` 的运行报错根因仍然成立：`AbilityIcon`、`AvailableAnimations` 暴露的是 descriptor-first 链路没有端到端统一。

`04` 的大方向仍然成立：不能兼任。但部分具体证据已经过期，需要按当前代码修正：

| 旧判断 | 当前修正 |
| --- | --- |
| `Data` 未绑定 catalog 时仍 fallback 到 `DataRegistry` | 当前 `Data.cs` 已经直接 throw `Data 容器未绑定 DataDefinitionCatalog`，不再 fallback；但 `new Data()` 构造、public string API 仍是兼容形态，必须继续删除/内化。 |
| RuntimeTables 仍有大量 Ability / Unit 静态实例 | 当前 `RuntimeTables` 只剩 System / Feature DTO 和 README；大量静态实例已退出。但目录名、README、DTO 命名和 `legacyTable` 输出仍保留旧表语义。 |
| Entity spawn 通过 config 类型推断 snapshot record | 当前 record 推断已删除，必须显式传 `RuntimeDataRecord` 或 table/id。但 `EntitySpawnConfig.Config` 仍是 required object，`InjectVisualScene` 仍用反射读 `Config.VisualScenePath`，这是残留兼容面。 |
| ResourceCatalog 从 RuntimeTables `.All` 构建目录 | 当前已从 `runtime_snapshot.records` 构建 Unit / Ability 目录；保留的是 `ResourceCategory.DataAbility` 这类分类兼容。 |

因此，后续不能再以 04 的旧证据直接执行删除，也不能因为部分证据过期就认为问题结束。当前真正要删的是下面这份新账本。

## 4. P0 旧兼容 / 兼任清单

### P0-1：snapshot record type 仍是第二事实源

位置：

- `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`
- `SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json`

问题：

- `field_rows` 里每个字段仍手写 `value_type`。
- `active_fields` 虽然 join 了 descriptor，但输出 records 时仍用 `f.value_type`。
- `AbilityIcon` 已经证明 record type 会和 descriptor 漂移。
- snapshot 里还输出 `legacyTable` / `legacyData`，继续把旧 RuntimeTables 名字写进运行时交付物。

目标：

```text
record 只提供 field_key + value
record field type 必须来自 data_key_descriptor.value_type
legacyTable / legacyData 从 runtime snapshot 移除；如需迁移追踪，写到单独 audit artifact。
```

### P0-2：validator 没检查最终 snapshot

位置：

- `SlimeAI/Data/DataOS/Tools/validate-dataos.sh`

问题：

- 当前验证 `dataos_runtime_field_stream`。
- 当前 DB 里 `dataos_runtime_field_stream` count 为 `0`。
- `validate-dataos.sh` passed，但 `runtime_snapshot.json` 已经有 10 条 type mismatch。

目标：

```text
validate-dataos 必须生成或读取最终 runtime_snapshot.json，然后校验：
1. 每个 record field key 必须存在 descriptor
2. 每个 record field type 必须等于 descriptor valueType
3. generated DataKey<T> 的 T 必须等于 descriptor valueType 的标准 CLR 映射
4. snapshot records 能完整 Apply 到 DataRuntimeBootstrap
```

### P0-3：generated handle 把非标量类型降级成 string

位置：

- `SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py`
- `SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs`

当前错误映射：

```text
string_array  -> string
modifier_list -> string
object_ref    -> string
```

已确认受影响字段：

| 字段 | descriptor type | 当前 generated type | 风险 |
| --- | --- | --- | --- |
| `AbilityIcon` | `object_ref` | `DataKey<string>` | 资源引用被当字符串，和 loader strict type 冲突。 |
| `TargetNode` | `object_ref` | `DataKey<string>` | AI 代码实际 `Get<Node2D>` / `Set(Node2D)`，靠隐式 string 绕过 typed handle。 |
| `AbilityTriggerEvent` | `string_array` | `DataKey<string>` | 业务代码 `Get<object>`，后续必炸或变成弱类型。 |
| `AvailableAnimations` | `string_array` | `DataKey<string>` | 当前已报 `List<string>` mismatch。 |
| `Dependencies` | `string_array` | `DataKey<string>` | System projection 已用 `string[]` view，handle 仍错。 |
| `EnabledSystemIds` | `string_array` | `DataKey<string>` | System preset view 用 `string[]`，handle 仍错。 |
| `DisabledSystemIds` | `string_array` | `DataKey<string>` | System preset view 用 `string[]`，handle 仍错。 |
| `Feature.Modifiers` | `modifier_list` | `DataKey<string>` | Feature 代码 `Get<object>`，modifier blob 无标准 CLR 类型。 |

目标：

```text
string_array  -> DataKey<string[]>
object_ref    -> 不能再假装 string；必须定义唯一 ObjectRef/NodeRef/ResourceRef 契约
modifier_list -> 不能再假装 string；要么 loader-only typed blob，要么 FeatureModifierEntry[] 契约
```

### P0-4：`DataKey<T>` 隐式转 string

位置：

- `SlimeAI/Src/ECS/Base/Data/DataKey.cs`

当前残留：

```text
public string Key => StableKey
public static implicit operator string(DataKey<T> key) => key.StableKey
```

问题：

`TargetNode` 当前 generated 是 `DataKey<string>`，但业务代码可以写：

```text
Data.Get<Node2D>(GeneratedDataKey.TargetNode)
Data.Set(GeneratedDataKey.TargetNode, node2D)
```

按类型安全设计，这本来应该编译失败。但因为 `DataKey<T>` 能隐式转 `string`，调用会绕到 `Data.Get<T>(string)` / `Data.Set<T>(string, T)`。于是错误从编译期拖到运行期。

目标：

```text
删除 implicit operator string
删除或收紧 Key alias
业务层只允许 Data.Get/Set(DataKey<T>)
需要 stable key 字符串的 loader/test 内部入口单独命名，例如 GetByStableKeyForLoader
```

### P0-5：`Data` public string-key API 仍是旧入口形状

位置：

- `SlimeAI/Src/ECS/Base/Data/Data.cs`

当前仍公开：

```text
Set<T>(string key, T value)
Get<T>(string key, object? defaultValue = null)
GetBase<T>(string key)
TryGetValue<T>(string key)
Has(string key)
Remove(string key)
Add<T>(string key)
Multiply<T>(string key)
SetMultiple(Dictionary<string, object>)
AddModifier(string key)
GetModifiers(string key)
GetAll()
```

注意：这些 API 当前不再 fallback 到 `DataRegistry`，未绑定 catalog 会 throw。这是进步。但它们仍让业务代码绕过 `DataKey<T>`，也让 `DataKey<T> -> string` 的隐式转换有落点。

目标：

```text
业务 public API 只保留 typed handle：
Data.Get<T>(DataKey<T>)
Data.Set<T>(DataKey<T>, T)
Data.Has<T>(DataKey<T>)
Data.Remove<T>(DataKey<T>)
Data.Add<T>(DataKey<T>, T)
Data.AddModifier<T>(DataKey<T>, DataModifier)

string stable key 入口降为 internal/loader/test-only，并用名字表明危险用途。
```

### P0-6：`new Data()` / `new Data(this)` 仍允许未绑定窗口

位置：

- `SlimeAI/Src/ECS/Base/Data/Data.cs`
- 多个 Entity 构造函数和测试

当前 `Data(IEntity? owner = null)` 仍能创建未绑定 catalog 的容器。EntityManager spawn 后会通过 `DataRuntimeBootstrap.ApplyToData` 绑定 catalog，这是当前运行主路。但只要对象在绑定前访问 Data，就会报未绑定错误；测试里也还有多个 `new Data()`。

目标：

```text
长期删除 public new Data()
Entity 构造时由 DataRuntimeBootstrap / EntityManager 提供 catalog
测试必须显式使用 catalog fixture
```

如果 Godot Entity 构造阶段确实拿不到 catalog，也要把“未初始化 Data”做成明确状态，不允许它暴露旧式可访问 API。

## 5. P1 旧兼容 / 兼任清单

### P1-1：DataKey compatibility aliases

位置：

- `SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py`
- `SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs`

生成器明确写着：

```text
Compatibility aliases for callsites that still use DataKey.Xxx during migration.
```

这和“绝不兼容”冲突。最终只能二选一：

```text
方案 A：唯一入口叫 GeneratedDataKey.Xxx，删除 DataKey.Xxx alias。
方案 B：唯一入口叫 DataKey.Xxx，删除 GeneratedDataKey 这层名字。
```

不能同时存在“新 GeneratedDataKey”和“旧 DataKey 兼容别名”。

### P1-2：RuntimeTables 目录和 README 仍保留兼容语义

位置：

- `SlimeAI/Data/DataOS/RuntimeTables/README.md`
- `SlimeAI/Data/DataOS/RuntimeTables/System/SystemData.cs`
- `SlimeAI/Data/DataOS/RuntimeTables/System/SystemPresetData.cs`
- `SlimeAI/Data/DataOS/RuntimeTables/Feature/FeatureDefinitionData.cs`

当前代码已经比 04 文档时收口：这里主要是 DTO，不再是大量手写静态表。但目录名和 README 仍写：

```text
RuntimeTables
按名字查询的运行时便利 API
少量迁移期静态行
根据 RuntimeTables 类型和 Name 推断 snapshot record
```

目标：

```text
如果只是 DTO，移到 RuntimeSnapshot/Projection 或 DataOS/GeneratedViews，删除 RuntimeTables 命名。
README 中所有“兼容 API / 迁移期静态行 / RuntimeTables 推断”删除。
```

### P1-3：EntitySpawnConfig 仍保留 object Config 和视觉反射 fallback

位置：

- `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs`

当前 record 推断已经删除，这是正确的。但 `EntitySpawnConfig` 仍有：

```text
public required object Config { get; init; }
```

`InjectVisualScene` 仍会：

```text
config.GetType().GetProperty(DataKey.VisualScenePath)
```

目标：

```text
Spawn 数据只来自 RuntimeDataRecord / table + id / 显式 VisualSceneOverride
删除通过任意 Config object 反射读取 VisualScenePath
Config 如保留，只能改名为 RuntimeArgs，并声明不参与 Data / snapshot / visual path 推断
```

### P1-4：Godot Resource authoring 旧入口还在

位置：

- `SlimeAI/Data/Feature/Definition/FeatureDefinition.cs`
- `SlimeAI/Data/Feature/Definition/FeatureModifierEntry.cs`
- `SlimeAI/Data/Config/System/System/SystemConfig.cs`
- `SlimeAI/Data/Config/System/Preset/SystemPreset.cs`
- `SlimeAI/Data/Config/System/**/Resource/*.tres`

问题：

- `FeatureDefinition : Resource` 仍通过 `[DataKey(nameof(DataKey.Xxx))]` 和 `[DataKey("Feature.Modifiers")]` 表达 Data 映射。
- `SystemConfig` / `SystemPreset` Resource 和 `.tres` 文件仍存在，虽然运行服务已从 snapshot 读取。
- 这些文件容易让后续 AI 认为 Godot Resource 仍是 authoring 事实源。

目标：

```text
Feature / System / Preset authoring 只进 DataOS DB / seed / migration。
旧 Resource 类如仅用于运行时临时对象，必须改名并移出 authoring 路径。
.tres 旧配置文件从资源索引和文档事实源退出。
```

### P1-5：`DataMeta` / `DataRegistry` / `LegacyDataAuditReport` 仍在 runtime 源码

位置：

- `SlimeAI/Src/ECS/Base/Data/DataMeta.cs`
- `SlimeAI/Src/ECS/Base/Data/DataRegistry.cs`
- `SlimeAI/Src/ECS/Base/Data/LegacyDataAuditReport.cs`

当前主路不再 fallback 到它们，但类还在 runtime 源码里。只要还在，就会被 AI 或旧测试重新拿来当“可用入口”。

目标：

```text
DataMeta / DataRegistry 从 runtime 源码删除。
如果需要迁移对照，移动到 SDD audit script / test fixture / archived migration input，不参与运行编译面。
LegacyDataAuditReport 若保留，不能引用运行时 DataMeta。
```

### P1-6：旧测试和示例大量使用 `DataKey.Xxx` / `new Data()`

位置示例：

- `SlimeAI/Src/ECS/Test/SingleTest/ECS/System/ActiveSkillInputTest/ActiveSkillInputTest.cs`
- `SlimeAI/Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/AbilitySystemPipelineTest.cs`
- `SlimeAI/Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.cs`
- `SlimeAI/Src/ECS/Test/SingleTest/Tools/TargetSelector/TargetSelectorTest.cs`
- 多个 `README.md` 示例

问题：

测试如果继续用旧 API，后续删除兼容层会让测试大面积失败；如果为了测试继续保留兼容层，就又回到兼任。

目标：

```text
测试也按新主路重写：
catalog fixture -> RuntimeDataRecord -> DataRuntimeBootstrap -> GeneratedDataKey typed handle
不要用 DataKey.Xxx alias
不要用裸 new Data()
```

### P1-7：文档事实源互相打架

位置：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
- `SlimeAI/DocsAI/ProjectState.md`
- `SlimeAI/DocsNew/ECS/Data/Data系统说明.md`
- `SlimeAI/Data/README.md`
- `SlimeAI/Data/DataKey/README.md`
- `SlimeAI/DocsAI/Modules/Data.md`
- `SlimeAI/DocsAI/Modules/AI.md`
- `SlimeAI/DocsAI/Modules/TestSystem.md`
- `SlimeAI/Src/ECS/Base/System/*/README.md`

问题：

- roadmap 和 ProjectState 写 SDD-0020 已完成旧路径退出，但当前 snapshot 和 generated handle 证明并未彻底退出。
- DocsNew 仍写 `DataKey.Xxx` 兼容别名、`new Data()` 迁移期入口、RuntimeTables 兼容 API。
- 部分模块文档仍写 `DataKey.TargetNode` 是特殊 const/string，不符合 descriptor-first。

目标：

```text
06 本文是当前 Data 无兼容审计事实源。
roadmap / ProjectState 必须改为“SDD-0020 完成了主路切换，但无兼容收口未完成”。
非历史文档不得再推荐 DataMeta、DataRegistry、DataKey alias、new Data、RuntimeTables、DataNew、Resource/tres authoring。
```

## 6. 非标量类型必须一次性定标准

当前所有非标量 descriptor：

| stable key | descriptor type | runtimeTypeId | storage/write |
| --- | --- | --- | --- |
| `AbilityIcon` | `object_ref` | `Texture2D` | `persisted/read_write` |
| `TargetNode` | `object_ref` | `Godot.Node2D` | `runtime_only/system_only` |
| `AbilityTriggerEvent` | `string_array` |  | `persisted/read_write` |
| `AvailableAnimations` | `string_array` |  | `persisted/read_write` |
| `Dependencies` | `string_array` |  | `persisted/read_write` |
| `EnabledSystemIds` | `string_array` |  | `persisted/read_write` |
| `DisabledSystemIds` | `string_array` |  | `persisted/read_write` |
| `Feature.Modifiers` | `modifier_list` | `Godot.Collections.Array<FeatureModifierEntry>` | `authoring_blob/loader_only` |

无兼容裁决：

| descriptor type | 唯一目标表达 |
| --- | --- |
| `string_array` | `string[]`。snapshot 可以短期从逗号字符串转换，但 Data slot 和 generated handle 必须是 `string[]`。 |
| `object_ref` persisted resource | 定义 `ObjectRef` / `ResourceRef` / `ResourcePathRef` 之一，不再用裸 `string` 冒充。`AbilityIcon` 至少必须让 record type 为 `object_ref`。 |
| `object_ref` runtime Node | `TargetNode` 不能继续是 `DataKey<string>`。要么有专门 `NodeRef` 契约，要么移出 Data slot 到 AI runtime blackboard。不能靠 string API 存 `Node2D`。 |
| `modifier_list` | 如果是 authoring blob，不应被业务 `Data.Get<object>` 读取；如果 runtime 要用，就定义 `FeatureModifierEntry[]` 或专门 DTO。 |

重点：不要把 `object_ref` / `modifier_list` 继续兼容成 `string` 或 `object`。这会把 descriptor 类型系统重新打回弱类型字典。

## 7. 必须增加的红灯门禁

下面这些门禁应该成为无兼容重构完成条件。结果必须为 0 或白名单明确。

```bash
# 1. 最终 snapshot 不允许 descriptor/record type 漂移
jq -r '[.descriptors[] | {key: .stableKey, type: .valueType}] as $defs | .records[] as $r | ($r.fields // {}) | to_entries[] | .key as $k | .value.type as $rt | ($defs[] | select(.key == $k) | .type) as $dt | select($rt != $dt)' SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json

# 2. 不允许 DataKey<T> 隐式转 string
rg -n "implicit operator string\\(DataKey" SlimeAI/Src/ECS/Base/Data SlimeAI/Data

# 3. 不允许 generated compatibility alias
rg -n "Compatibility aliases|public static partial class DataKey" SlimeAI/Data/DataOS/Tools SlimeAI/Data/DataKey/Generated

# 4. 不允许非标量 descriptor 生成 DataKey<string>
rg -n "DataKey<string> (AbilityIcon|TargetNode|AbilityTriggerEvent|AvailableAnimations|Dependencies|EnabledSystemIds|DisabledSystemIds|FeatureModifiers)" SlimeAI/Data/DataKey/Generated

# 5. 不允许业务层把 descriptor key 当 string API 使用
rg -n "Data\\.Get<.*>\\(\\\"|Data\\.Set\\(\\\"|\\.Get<.*>\\(DataKey\\.|\\.Set\\(DataKey\\." SlimeAI/Src/ECS SlimeAI/Data

# 6. 不允许当前已知错误调用继续存在
rg -n "Get<object>\\(GeneratedDataKey|Get<.*List<string>|Set\\(GeneratedDataKey\\.TargetNode|Get<Node2D>\\(GeneratedDataKey\\.TargetNode" SlimeAI/Src/ECS SlimeAI/Data

# 7. runtime snapshot 不再输出旧表字段
rg -n "legacyTable|legacyData" SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json SlimeAI/Data/DataOS/Tools

# 8. 旧字段定义源不在 runtime 编译面
rg -n "class DataMeta|class DataRegistry|LegacyDataAuditReport" SlimeAI/Src/ECS/Base/Data
```

最终验证还必须包含：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
Tools/run-dataos-validate.sh
```

以及旧 ECS DataOS 场景：

```bash
GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path /home/slime/Code/SlimeAI/SlimeAI --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path /home/slime/Code/SlimeAI/SlimeAI --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path /home/slime/Code/SlimeAI/SlimeAI --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path /home/slime/Code/SlimeAI/SlimeAI --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataFeatureBridgeTestScene.tscn
```

## 8. 推荐执行顺序

### 第一批：先让错误在生成/验证阶段变红

目的不是先修 UI 或组件，而是把“错误生成物”挡在 runtime 之前。

1. `validate-dataos.sh` 增加最终 snapshot descriptor/record mismatch 校验。
2. generator record field type 改为来自 descriptor，不再用 `field_rows.value_type` 决定。
3. `AbilityIcon` mismatch 先通过门禁变红，再修生成器。
4. handle generator 增加 descriptor type 到 CLR type 的严格映射校验。

### 第二批：切断 typed handle 绕路

1. 删除 `DataKey<T>` 隐式转 string。
2. 删除 generated `DataKey.Xxx` compatibility alias。
3. 业务代码统一改到唯一入口。
4. `Data` public string-key API 降级为 internal loader/test-only。

### 第三批：统一非标量类型

1. `string_array -> string[]`：修 generator、converter、snapshot apply、调用点。
2. `object_ref`：先定义 `AbilityIcon` resource reference 和 `TargetNode` runtime reference 的边界，不能继续共用 `string`。
3. `modifier_list`：明确 `Feature.Modifiers` 是 authoring blob 还是 runtime typed DTO。

### 第四批：删除旧 authoring / DTO / 文档误导面

1. RuntimeTables 目录改名或迁移到 generated view / projection DTO。
2. FeatureDefinition / SystemConfig / SystemPreset Resource authoring 路线裁决删除或改名为 runtime-only。
3. `legacyTable` / `legacyData` 从 runtime snapshot 移除。
4. roadmap、ProjectState、DocsAI、DocsNew、Data README 一次性改口。

### 第五批：测试重建

1. 所有旧 `new Data()` 测试改为 catalog fixture。
2. 所有 `DataKey.Xxx` 测试改为唯一 generated handle。
3. 增加“无兼容红灯门禁”脚本。
4. Godot DataOS 场景和主场景 smoke 确认不再出现 Data 类型错误。

## 9. 给后续执行者的一句话

不要再把这次问题理解成“某几个字段类型没兼容好”。真正的问题是：Data 系统已经想走“数据库生成 snapshot，再由 Base/Data 严格执行”这条清晰路线，但代码里还留着让旧写法继续混进来的门。

这次重构要做的是关门：

```text
没有 DataKey -> string
没有 DataKey.Xxx 兼容别名
没有 record 自己定义字段类型
没有 validator 只查中间表
没有 public string-key 业务入口
没有 Resource/tres authoring 事实源
没有 RuntimeTables 兼容语义
没有文档继续说 SDD-0020 已完全退出旧路径
```

关完这些门，Data 系统才会回到最初那条很简单的路：数据库负责定义和数据，脚本生成，Base/Data 执行，其他地方直接用。
