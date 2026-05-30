# Data 重构运行报错根因分析

> 更新：2026-05-29  
> 状态：运行报错复盘与后续修复输入。  
> 来源：`Workspace/DocsAI/Temp/1.md` Godot 运行日志。  
> 关联 SDD：SDD-0017 Runtime Snapshot Record Apply、SDD-0018 Generated Handles、SDD-0019 Legacy Path Removal、SDD-0020 Snapshot-First Usage Cutover。  
> 立场：本文件只记录证据、根因、设计缺口和修复边界；不替代执行型 SDD 的实现任务。

## 1. 问题陈述

Data 系统完成 descriptor-first 重构后，Godot 场景运行时出现两类 Data 相关错误：

```text
Component 回调失败: UnitAnimationComponent, 错误: Data.Get 类型不匹配：AvailableAnimations expected=StringArray, actual=List`1
```

以及：

```text
DataApplyReport failed: ability/ability.dash, applied=19, errors=1
- snapshot.type_mismatch AbilityIcon: record field type 与 descriptor 不一致：string != ObjectRef
创建技能实体失败: 冲刺
```

这两个错误看起来分别发生在动画组件和技能实体创建阶段，但它们共享同一个根问题：Data 重构后，`runtime_snapshot.json.descriptors` 已经成为字段类型事实源，但生成器、generated handle、运行时代码和 validator 没有全部按同一套类型契约收敛。

当前应避免把它们当作两个局部 bug 分别打补丁。它们共同暴露了 descriptor-first Data 链路中的三个系统性缺口：

- `string_array` 的 Runtime CLR 表达没有统一。
- `object_ref` 的 descriptor 和 record projection 类型不一致。
- validator 没有校验最终 snapshot record 与 descriptor 的一致性，导致错误延迟到运行时暴露。

## 2. 预期链路

Data 系统当前目标链路应是：

```text
DataOS SQLite authoring
    -> runtime_snapshot.json
         ├── descriptors  字段定义事实源
         └── records      初始记录事实源
    -> DataDefinitionCatalog
    -> DataRuntimeStorage
    -> DataRuntimeBootstrap.ApplyRecord
    -> Entity.Data
```

在这条链路里，字段类型只能有一个权威来源：

```text
data_key_descriptor.value_type
    -> runtime_snapshot.json.descriptors[].valueType
    -> DataDefinition.ValueType
    -> DataValueConverter
    -> Data.Get / Data.Set 泛型兼容规则
```

record 只负责提供字段值，不能重新定义字段类型。如果 record 的 `fields.<key>.type` 与 descriptor 的 `valueType` 不一致，loader 必须 fail fast。

本次报错说明 fail-fast 行为本身是正确的，但 fail-fast 发生得太晚：`AbilityIcon` 的类型漂移已经进入了生成物；`AvailableAnimations` 的 CLR 调用类型和 descriptor 类型不一致也已经进入业务代码。

## 3. 复现证据摘要

### 3.1 `AvailableAnimations` 反复在组件注册阶段失败

日志中玩家和多个敌人注册 `UnitAnimationComponent` 时反复出现：

```text
Data.Get 类型不匹配：AvailableAnimations expected=StringArray, actual=List`1
```

触发位置是单位视觉场景加载后，动画组件读取 `AnimatedSprite2D.SpriteFrames`，缓存可用动画名，再立即调用 `Play(Anim.Idle)`。该路径会写入并读取 `AvailableAnimations`。

直接后果：

- `UnitAnimationComponent.OnComponentRegistered` 中抛错。
- EntityManager 仍打印组件已注册，但组件内部初始化没有完整完成。
- 后续攻击动画选择 `AttackComponent.SelectRandomAttackAnimation` 也会命中同一类型错误，因为它同样读取 `List<string>`。

### 3.2 `AbilityIcon` 阻断 `ability.dash` 实体创建

日志中主动技能添加阶段出现：

```text
DataApplyReport failed: ability/ability.dash, applied=19, errors=1
- snapshot.type_mismatch AbilityIcon: record field type 与 descriptor 不一致：string != ObjectRef
```

随后：

```text
Entity 未注册，无法注销
创建技能实体失败: 冲刺
```

这说明 `EntityManager.Spawn<AbilityEntity>` 在 `DataRuntimeBootstrap.ApplyToData` 阶段发现 snapshot record 错误，并按设计阻断实体生成。技能实体没有成功创建，主动技能栏自然没有可用技能。

## 4. 证据矩阵

| 问题 | 证据 | 说明 |
| --- | --- | --- |
| `AvailableAnimations` descriptor 是 `string_array` | `SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql` 中 `AvailableAnimations` 为 `string_array`；`runtime_snapshot.json` 中 `valueType=string_array` | descriptor 事实源已经表达它是数组语义。 |
| `AvailableAnimations` 运行时代码写入 `List<string>` | `UnitAnimationComponent` 构造 `new List<string>()` 后 `_data.Set(GeneratedDataKey.AvailableAnimations, animNames)` | 写入值 CLR 类型为 `List<string>`。 |
| `AvailableAnimations` 运行时代码读取 `List<string>` | `UnitAnimationComponent.Play` 与 `AttackComponent.SelectRandomAttackAnimation` 调用 `Get<List<string>>(GeneratedDataKey.AvailableAnimations)` | 读取泛型类型为 `List<string>`。 |
| `StringArray` 兼容规则不接受 `List<string>` | `DataValueConverter.IsCompatible` 对 `DataValueType.StringArray` 只接受 `string` 或 `string[]` | `Get<List<string>>` 必然触发类型不匹配。 |
| generated handle 对 `string_array` 生成 `DataKey<string>` | `generate-data-key-handles.py` 中 `TYPE_MAP["string_array"] = "string"` | typed handle 没有表达数组类型，削弱编译期提示。 |
| `AbilityIcon` descriptor 是 `object_ref` | `DataKeyDescriptors.seed.sql` 中 `AbilityIcon` 为 `object_ref`，`runtimeTypeId=Texture2D` | 字段语义是资源对象引用，不是普通字符串字段。 |
| `AbilityIcon` record 被生成成 `string` | `generate-runtime-snapshot.sh` 对 ability 的 `AbilityIcon` field hardcode 为 `'string'` | generator 与 descriptor 漂移。 |
| 最终 snapshot 中 `AbilityIcon` 确实不一致 | `runtime_snapshot.json` 的 `AbilityIcon` descriptor 是 `object_ref`，ability records 的 `AbilityIcon.type` 是 `string` | 运行时 loader 报错符合设计。 |
| validator 没有拦住该漂移 | `validate-dataos.sh` 校验 `dataos_runtime_field_stream`，但当前 snapshot generator 自己构造 `field_rows` CTE | 校验对象不是最终生成对象。 |

## 5. 根因一：`string_array` 的 CLR 类型契约未收敛

### 5.1 表面症状

`AvailableAnimations` 在 descriptor 中是 `string_array`，但运行时代码使用 `List<string>`。

```text
descriptor valueType = string_array
runtime write value  = List<string>
runtime read type    = List<string>
converter accepts    = string or string[]
```

因此，`DataRuntimeStorage.Get<T>` 在读取时先检查 `DataValueConverter.IsCompatible<T>(definition.ValueType)`。当 `T=List<string>` 且 `ValueType=StringArray` 时，兼容性检查失败，抛出：

```text
Data.Get 类型不匹配：AvailableAnimations expected=StringArray, actual=List`1
```

### 5.2 更深层问题

这不是单纯把 `List<string>` 加进兼容表就能完全解决的问题。真正缺口是：Data 系统没有定义 `string_array` 在四个边界上的统一表达。

| 边界 | 当前状态 | 问题 |
| --- | --- | --- |
| descriptor | `valueType=string_array` | 正确表达数组语义。 |
| generated handle | `DataKey<string>` | 丢失数组语义。 |
| snapshot record | 有些字段用逗号字符串，例如 `Dependencies` | 没有结构化 JSON array 契约。 |
| runtime code | 有些旧代码用 `string[]`，`AvailableAnimations` 用 `List<string>` | CLR 表达不统一。 |

其中 `AvailableAnimations` 又有一个特殊点：它不是 authoring 初始数据，而是由 `UnitAnimationComponent` 根据当前视觉资源运行时发现出来的状态。它被 descriptor 标为 `persisted/read_write`，短期能运行，但语义上更接近 `runtime_state`，可能不应作为持久 authoring record 字段。

### 5.3 为什么这次重构后才暴露

旧 `Data` 字典路径通常只按裸字符串 key 存 object，并在读取时做较宽松转换。`List<string>` 写入后再 `Get<List<string>>` 可以工作。

descriptor-first 后，`DataDefinitionCatalog` 先约束字段的抽象类型，再由 `DataValueConverter` 做写入和读取校验。这个变化是正确方向，但迁移没有同步清理旧调用点，所以旧的 `List<string>` 习惯被新类型系统拦下。

### 5.4 设计裁决建议

建议为 `string_array` 明确唯一 Runtime CLR 类型：

```text
DataValueType.StringArray -> string[]
```

原因：

- `string[]` 是 C# 基础类型，适合 generated handle 表达为 `DataKey<string[]>`。
- 现有 `SystemData.Dependencies`、`SystemPresetData.EnabledSystemIds` 等业务模型本来就是 `string[]`。
- `List<string>` 更适合调用点临时构造和筛选，不适合作为 Data slot 中的标准存储类型。
- 如果允许 `List<string>`、`string[]`、逗号字符串都作为等价读写类型，Data 类型系统会再次退化成宽松字典。

短期可接受的转换策略：

- 写入：允许 `string[]`；是否允许 `IEnumerable<string>` 或 `List<string>` 作为输入，需要谨慎。如果支持，也应转换成 `string[]` 存储，而不是原样存 `List<string>`。
- 读取：`Get<string[]>` 是唯一推荐 typed API；如需 List，由调用点 `new List<string>(data.Get<string[]>(key))` 自行转换。
- snapshot record：短期可继续接受逗号字符串，但 loader 应转换为 `string[]`；长期应让 record value 使用 JSON array。

### 5.5 涉及调用点

已确认直接命中：

- `SlimeAI/Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.cs`
- `SlimeAI/Src/ECS/Base/Component/Unit/Common/AttackComponent/AttackComponent.cs`
- `SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py`
- `SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs`

需要后续 SDD-0020 或专门 bugfix 检查的潜伏点：

- 所有 `DataValueType.StringArray` descriptor。
- 所有 generated `DataKey<string>` 但 descriptor 为 `string_array` 的字段，例如 `Dependencies`、`EnabledSystemIds`、`DisabledSystemIds`。
- 所有 `Data.Get<string>(GeneratedDataKey.Xxx)` 读取数组语义字段的调用点。

## 6. 根因二：`AbilityIcon` 的 projection 类型漂移

### 6.1 表面症状

`AbilityIcon` 的 descriptor 定义为：

```text
stable_key = AbilityIcon
value_type = object_ref
runtime_type_id = Texture2D
```

但 `runtime_snapshot.json.records` 中所有 ability record 的 `AbilityIcon` 都是：

```json
"AbilityIcon": {
  "type": "string",
  "value": "res://icon.svg"
}
```

`RuntimeDataSnapshotLoader.ApplyRecord` 逐字段应用 record 时，会先比较 record field type 与 descriptor value type。这里得到：

```text
string != ObjectRef
```

因此生成 `snapshot.type_mismatch`，`EntityManager.ApplySpawnData` 返回 false，`AbilityEntity` 创建失败。

### 6.2 更深层问题

`AbilityIcon` 的数据库 descriptor 已经正确升级为 `object_ref`，但是 snapshot generator 的投影规则仍旧硬编码：

```text
AbilityIcon -> string
```

这说明当前 DataOS 生成链路仍存在“局部手写字段类型”：

```text
data_key_descriptor.value_type = object_ref
generate-runtime-snapshot.sh field_rows says string
runtime_snapshot.records says string
loader rejects string != ObjectRef
```

descriptor-first 设计要求 record projection 不应重新决定字段类型。record type 应来自 descriptor，或者至少在生成阶段强制 join descriptor 并输出 descriptor type。

### 6.3 为什么 validator 没有拦住

`validate-dataos.sh` 的 record 类型校验读取的是：

```text
dataos_runtime_field_stream
```

但当前 `generate-runtime-snapshot.sh` 并不以这个表作为最终 projection 输入，而是在脚本内部用 `field_rows` CTE 重新拼出所有 records。

结果是：

```text
validator 检查的投影流      != 实际 snapshot generator 使用的投影流
```

只要 `dataos_runtime_field_stream` 为空或未覆盖 `AbilityIcon`，validator 仍然可以通过；但生成出的 `runtime_snapshot.json` 已经带着不一致进入运行时。

这就是本次 `AbilityIcon` 问题的关键设计缺口：验证对象不是最终交付物。

### 6.4 设计裁决建议

短期最小修复：

```text
generate-runtime-snapshot.sh:
AbilityIcon field type 从 string 改成 object_ref
```

但长期不能继续按字段一个个手改 hardcode。更稳的设计是：

```text
field_rows 只声明 field_key 和 value_text
JOIN data_key_descriptor
record field type = data_key_descriptor.value_type
```

这样 generator 不再维护第二套字段类型。

如果某个业务字段确实需要对外展示为字符串，但 descriptor 是 `object_ref`，转换逻辑也应该在 `DataValueConverter` 或 resource resolver 中表达，而不是在 record type 上伪装为 `string`。

## 7. 根因三：验证门禁检查错层

当前 DataOS validator 做了多项正确检查：

- 外键。
- duplicate name。
- required resource path。
- bool value。
- descriptor value type。
- descriptor dependency。
- `dataos_runtime_field_stream` field type vs descriptor type。

但这次仍然让错误进入运行时，因为关键问题发生在最终 snapshot JSON：

```text
runtime_snapshot.json.descriptors[].valueType
runtime_snapshot.json.records[].fields[*].type
```

而不是只发生在 authoring DB 表。

因此 validator 需要增加最终生成物检查：

```text
For each runtime_snapshot.records[].fields[key]:
    descriptor = descriptors[key]
    assert descriptor exists
    assert field.type == descriptor.valueType
    assert field.value can convert to descriptor.valueType
```

这项检查应该在 snapshot 生成后运行，或者作为 `generate-runtime-snapshot.sh` 的最后一步执行。只检查 DB authoring 层不足以证明 generated snapshot 正确。

## 8. 假设树

### H1：Godot 动画资源缺失导致 `UnitAnimationComponent` 报错

结论：否。

日志显示组件先成功缓存动画：

```text
缓存了 8 个可用动画: attack1, beattacked, castingidle, celebrate, dead, idle, run, skill
```

错误发生在缓存后读取 Data 时。资源存在，报错来自 Data 类型检查。

### H2：`AvailableAnimations` descriptor 类型写错，应该是普通 string

结论：不建议。

字段语义是动画名集合，`string_array` 比 `string` 更正确。真正问题是 Runtime CLR 类型和 generated handle 没有跟上。

### H3：`AbilityIcon` descriptor 写错，应该回退成 string

结论：不建议。

`AbilityIcon` 语义是 Texture2D 资源引用。`object_ref` 更符合 descriptor-first 目标。回退成 string 会掩盖 resource reference 类型，但不解决 `object_ref` record projection 的系统性问题。

### H4：RuntimeSnapshotLoader 校验太严格

结论：否。

loader 报 `snapshot.type_mismatch` 是正确的 fail-fast 行为。如果放宽校验，descriptor-first 的核心价值会被破坏，错误会进入更深的 UI 或 Resource load 阶段。

### H5：DataOS generator 和 validator 不同源导致漂移

结论：是，且是 `AbilityIcon` 的主要系统性根因。

validator 检查 `dataos_runtime_field_stream`，generator 使用内部 `field_rows` CTE，二者没有共享投影事实源。

## 9. 与 SDD-0020 的关系

本次报错与 `04-Data系统现状复查与兼任问题.md` 中的判断一致：descriptor-first 核心链路已建立，但取用层、生成层和旧兼任入口还没有硬收口。

具体对应：

| 04 文档问题 | 本次报错中的体现 |
| --- | --- |
| RuntimeTables 手写数据仍在 | `MainTest` 通过 `AbilityData.Get("冲刺")` 进入 AddAbility，仍依赖手写 C# facade 选择技能。 |
| generator projection 手写 | `AbilityIcon` 在 generator 中手写为 `string`，与 descriptor 漂移。 |
| generated handle 仍是薄 stable key | `string_array` 被生成成 `DataKey<string>`，没有暴露数组类型。 |
| Data 旧调用习惯未清理 | `UnitAnimationComponent` 和 `AttackComponent` 沿用 `List<string>` 读写。 |
| validator 缺最终 snapshot 检查 | `AbilityIcon` type mismatch 没在生成阶段失败。 |

因此，本文件建议把本次修复纳入 SDD-0020 的 P0 收口范围，或者在 SDD-0020 开始前先做一个小型 DebugFix 切片，避免后续 hard cutover 被当前运行错误阻塞。

## 10. 最小修复边界

### 10.1 必须修

1. `AbilityIcon` record type：
   - generator 输出 `object_ref`。
   - 重新生成 `runtime_snapshot.json`。
   - 增加 final snapshot descriptor/record consistency check。

2. `string_array` Runtime CLR 契约：
   - 明确 `string_array -> string[]`。
   - generated handle 输出 `DataKey<string[]>`。
   - `DataValueConverter` 写入/读取统一转换为 `string[]`。
   - `UnitAnimationComponent` / `AttackComponent` 改为 `string[]` 或局部转换 List。

3. 回归验证：
   - DataOS validate 必须能捕捉 record/descriptor 类型漂移。
   - DataSnapshotApplyTestScene 必须覆盖 repository snapshot 中 `AbilityIcon` 和至少一个 `string_array` 字段。
   - MainTest 或最小 Ability spawn 场景必须覆盖 `ability.dash` 创建成功。

### 10.2 不应作为本次最小修复

以下事项属于 SDD-0020 或更大 hard cutover，不能混入小修：

- 删除所有 RuntimeTables 手写数据。
- 删除 `DataRegistry` / `DataMeta`。
- 删除 `EntitySpawnConfig.Config` 推断入口。
- 重写 ResourceCatalog。
- 重写 DataConfigEditor。

这些确实是最终目标，但混入当前 bugfix 会扩大 blast radius，导致根因验证不清晰。

## 11. 推荐执行顺序

### Step 1：补 failing validation

先增加一个 final snapshot 一致性检查，让当前 snapshot 在修复前失败：

```text
AbilityIcon: record field type string != descriptor object_ref
```

这个检查可以放在：

- `validate-dataos.sh` 生成后读取 snapshot；或
- 新增独立 `validate-runtime-snapshot` 脚本；或
- DataSnapshotApplyTestScene 读取 repository snapshot 并全量检查 descriptors/records。

推荐至少有一个 CLI validator 和一个 runtime scene/test 双重覆盖。

### Step 2：修 generator

先修 `AbilityIcon` 最小漂移：

```text
AbilityIcon field type = object_ref
```

更稳的方案是移除 ability field_rows 中每个字段的手写 type，让它从 descriptor join 取得。但如果时间有限，先修 `AbilityIcon` 并追加最终 snapshot 校验，后续再迁 projection。

### Step 3：修 `string_array` 类型契约

按统一规则调整：

```text
TYPE_MAP["string_array"] = "string[]"
DataValueConverter.StringArray stores string[]
UnitAnimationComponent uses string[]
AttackComponent uses string[]
```

如果短期 snapshot record 仍提供逗号字符串，则 loader 可以把 `"A,B"` 转为 `string[] { "A", "B" }`。空字符串转空数组，避免 `[""]` 语义污染。

### Step 4：重新生成并验证

建议验证命令：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/build-authoring-db.sh
bash Data/DataOS/Tools/generate-runtime-snapshot.sh Data/DataOS/Authoring/slimeainew.authoring.db Data/DataOS/Snapshots/runtime_snapshot.json
python3 Data/DataOS/Tools/generate-data-key-handles.py Data/DataOS/Snapshots/runtime_snapshot.json Data/DataKey/Generated/DataKey_Generated.cs
dotnet build Brotato_my.csproj --no-restore
godot --headless --path . --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn
godot --headless --path . --scene res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn
```

如果项目已有 wrapper，应优先使用项目脚本替代裸 `godot` 命令。

## 12. 后续设计要求

### 12.1 descriptor-first 不允许 record 自定义类型

后续 generator 应遵循：

```text
record field key 决定 descriptor
descriptor 决定 field type
record 只提供 value
```

除非存在显式迁移兼容层，否则 record 不应维护第二套 `type` 字符串。

### 12.2 `string_array` 不应长期用逗号字符串表达

逗号字符串会遇到转义和空值问题，例如：

```text
"A,B" 是两个值还是一个包含逗号的值？
"" 是空数组还是一个空字符串？
```

长期 snapshot 结构应允许：

```json
{ "type": "string_array", "value": ["DamageService", "TimerManager"] }
```

SQLite authoring 内部可以仍用 TEXT 或 JSON，但生成物应尽量结构化。

### 12.3 runtime_state 字段与 persisted 字段要分清

`AvailableAnimations` 是从 `AnimatedSprite2D.SpriteFrames` 运行时发现的状态，不是稳定 authoring 初始值。后续应评估：

```text
storage_policy: runtime_state
write_policy: system_only 或 read_write
migration_policy: never
```

这不阻塞当前 bugfix，但应进入 SDD-0020 的字段语义复查。

### 12.4 validator 必须验证最终交付物

DataOS validation 至少应覆盖三层：

1. authoring DB 内部一致性。
2. generated snapshot descriptor/record/resource 一致性。
3. runtime loader 能按 repository snapshot 构建 catalog 并应用关键 records。

只验证 DB，不验证 generated JSON，不足以支撑运行时正确性声明。

## 13. 当前结论

本次运行报错的直接根因明确：

- `AvailableAnimations`：descriptor 是 `string_array`，业务代码读写 `List<string>`，而 runtime converter 不接受 `List<string>`。
- `AbilityIcon`：descriptor 是 `object_ref`，snapshot record 被 generator 生成成 `string`。

更深层根因是 Data 重构执行到 descriptor-first 核心后，类型契约没有贯穿到 generator、generated handle、业务调用点和 validator。运行时 fail-fast 暴露了这个不一致，这说明新 DataRuntimeStorage 的严格校验方向是正确的；需要修的是上游生成和调用契约，不应放宽 loader。

后续修复应优先收敛类型事实源：

```text
descriptor valueType
    -> generated handle CLR type
    -> record field type
    -> DataValueConverter storage/read type
    -> business callsite type
```

只有这条链路闭合后，SDD-0020 的 snapshot-first usage hard cutover 才能避免继续被旧 RuntimeTables、旧 Data 调用习惯和 generator 手写投影拖回兼任状态。
