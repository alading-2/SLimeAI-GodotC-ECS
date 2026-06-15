# DataDefinition 瘦身与分层方案

## 先裁决：DataDefinition 不应继续是“大而全 descriptor”

旧 `DataMeta` 的根本问题是把 runtime contract、展示信息、计算 lambda 和编辑器 metadata 放在一个对象里。当前 `DataDefinition` 虽然删除了 C# lambda 和旧 registry，但又把 DataOS descriptor 的大部分字段搬进 runtime 对象，形成了新的混合对象。

推荐把字段定义拆成三类：

```text
DataAuthoringDescriptor
  DataOS / snapshot 输入事实源，字段最多，可以服务 AI、工具、展示、裁剪和验证。

DataRuntimeDefinition
  runtime catalog 使用，字段最少，只保留 Data.Get/Set/Modifier/Computed 必须内容。

DataPresentationDescriptor
  TestSystem / debug UI / 文档 / AI 解释字段用，不进入 DataRuntimeStorage 热路径。
```

短期可以不立刻新增三个 C# 类型，但设计和 generator 必须按这三层输出，不再把所有字段都塞进 `DataDefinition`。

## 建议保留在 runtime 的字段

`DataRuntimeDefinition` 建议只保留。下表中的 `DataKind` 是目标设计名，现有代码可先用收窄后的 `DataStoragePolicy` 承接，后续再决定是否重命名：

| 字段 | 保留理由 |
| --- | --- |
| `StableKey` | 字段稳定协议 key，runtime catalog 索引必需。 |
| `ValueType` 或 `RuntimeType` | 用于 loader 防御、slot 创建、report。长期建议从 `DataValueType` 升级为 runtime type descriptor。 |
| `DefaultValue` typed | 未写入时的玩法默认值。必须是已转换的 typed 值，不应长期是 text/object。 |
| `DataKind` / 收窄后的 `StoragePolicy` | 区分普通值、runtime_only、computed、authoring_blob；这是字段形态，不是权限规则。 |
| `RangePolicy`、`MinValue`、`MaxValue` | 条件字段。优先在生成期检查，runtime 只保留必要防御。 |
| `ModifierPolicy` | 决定字段是否允许 modifier。 |
| `AllowedValues` | 对 enum/string 这类值域有运行时写入保护价值；为空时不应分配复杂结构。 |
| `ComputeId`、`Dependencies` | computed resolver 与 dirty graph 必需。 |
| `ComputeParams` | 只有 resolver 真实使用时保留；当前为空，可先留在 authoring，runtime 按需投影。 |

可选保留：

| 字段 | 条件 |
| --- | --- |
| `RuntimeTypeId` | 仅当 `valueType=enum/object_ref/modifier_list` 且 generator 需要 CLR/Godot 类型补充时保留。普通 `float/int/bool/string` 不需要。 |
| `MigrationPolicy` | 如果当前没有 Entity profile 迁移读取它，先留 authoring；等迁移功能真实使用时进入 runtime。 |
| `WritePolicy` | 降级为 authoring/report/规则提示；computed 字段由 `DataKind=computed` 自然禁止写基础值，不建议作为 runtime hot path 权限系统。 |

这里的关键不是把所有条件字段都删掉，而是避免“每个字段都携带全部能力”。runtime definition 可以按能力拆小对象：

```text
DataRuntimeDefinition
  StableKey
  RuntimeType
  TypedDefaultValue
  DataKind
  RangePolicy?        仅 numeric/ranged 字段存在
  ModifierPolicy?     仅 modifier 字段存在
  AllowedValues?      仅 enum/string 白名单字段存在
  ComputeBinding?     仅 computed 字段存在
```

这样 `DataDefinition` 不需要用一堆空字符串、空数组和默认枚举值模拟“未使用”。

## 建议移出 runtime 的字段

| 字段 | 建议归属 | 原因 |
| --- | --- | --- |
| `OwnerDomain` | authoring / docs / generator | `StableKey` 命名和 owner 文档已能表达归属；runtime 读写不需要。 |
| `OwnerCapability` | authoring / capability trimming / generated manifest | 对 DataOS 裁剪有价值，但 DataRuntimeStorage 不需要每个字段携带。 |
| `OwnerSkill` | authoring / AI 路由文档 | 对 AI 有价值，不是 runtime contract。 |
| `DisplayName` | presentation descriptor | TestSystem/UI 可读，不参与 Data.Get/Set。 |
| `Description` | presentation descriptor / DocsAI | AI 理解字段需要，但不应在 runtime hot object 里。 |
| `UiGroup` | presentation descriptor | UI 分组，不是运行时策略。 |
| `ResetGroup` | reset/profile 系统单独 manifest | 若要做 reset，应由 reset profile 消费，不应混进 Data runtime。 |
| `Unit`、`Format` | presentation descriptor | 替代旧 `isPercentage` 的展示语义。 |
| `IconPath` | presentation/resource catalog | 当前基本为空；后续如使用也属于展示资源。 |

## 旧 mirror 字段处理

当前 DataOS schema 和 snapshot 里还有旧字段：

| 旧字段 | 裁决 |
| --- | --- |
| `category` | 删除 runtime 投影。需要 UI 分组时用 `ui_group`；需要 owner 时用 `owner_capability` 或 stable key 命名。 |
| `is_percentage` / `isPercentage` | 删除 runtime 投影。展示用 `unit=percent` / `format`，计算由 resolver 明确处理。 |
| `supports_modifiers` / `supportsModifiers` | 删除 runtime 投影。唯一事实源是 `modifier_policy`。 |
| `is_computed` / `isComputed` | 删除 runtime 投影。唯一事实源是 `storage_policy=computed` 或 `compute_id` 非空。 |
| `options_json` / `options` | 删除或迁到 `allowed_values_json`。不要双事实源。 |
| `min_value` / `max_value` text | authoring 可存 text，但 snapshot/runtime 应输出 typed number，且只保留一套 `minValue/maxValue`。 |

这些字段如果短期还要留在 SQLite 表里作为迁移账本，应标记为 legacy authoring input，不再进入 `RuntimeDataDescriptorDto`。

当前 `dataos_runtime_field_stream` 在 schema 中存在，但本轮审计到的库内记录数为 `0`，generator 实际使用的是 shell SQL 内部 `field_rows` CTE。这说明 validator / generator / snapshot projection 之间还有事实源残留，后续瘦身时应顺手确认 stream 表是删除、补齐还是降级为 authoring view，避免再产生一套 record 类型投影。

## `RuntimeTypeId` 的具体裁决

用户提出 “`RuntimeTypeId` 只需保留 `StableKey` 即可” 的直觉部分成立：大多数字段确实不需要 `RuntimeTypeId`。但完全删除会破坏三类字段：

1. `enum`：`valueType=enum` 需要知道 generated handle 是 `AbilityType`、`Team` 还是 `UnitRank`。只靠 stable key 可以推断，但那会把类型规则藏进命名约定。
2. `object_ref`：资源引用和运行时 Godot Node 引用都叫 object_ref，但 `ResourceRef` 与 `Godot.Node2D` 生命周期不同。
3. `modifier_list`：`FeatureModifierEntryData[]` 这类复杂结构需要明确 CLR 类型。

推荐改名/改形：

```text
runtime_type_id -> clr_type_hint
```

并规定：

- 标量类型禁止填写。
- enum 必填。
- runtime object_ref 必填。
- resource object_ref 可为空，默认映射 `ResourceRef`。
- generator 和 validator 使用它；runtime catalog 只在需要创建 slot 或报告 expected type 时保留。

## `OwnerCapability` 是否还有必要

有必要，但不是 runtime 必要。

它当前至少有三类价值：

- DataOS `capability_manifest` 裁剪 descriptors / records。
- AI 改字段时知道 owner skill / owner docs。
- 数据质量报告可以按 Capability 聚合。

但 `DataRuntimeStorage.Get/Set` 不需要它。建议：

```text
DataOS authoring 保留 owner_capability / owner_skill
runtime snapshot 可在 manifest 或 sidecar presentation catalog 中保留
DataRuntimeDefinition 默认不携带
```

如果以后需要按 owner dump runtime catalog，可从 generated manifest 或 presentation descriptor 查询，不要让每个 slot 都背一份 owner 字符串。

## 默认值存储策略

数据库或知识库中默认值可以继续存 text，但必须明确这是 authoring 表达，不是 runtime 表达。

推荐链路：

```text
default_value_text + value_type
  -> DataOS validator 校验可解析
  -> snapshot generator 输出 typed JSON value
  -> RuntimeDataSnapshotLoader 只做防御性 parse
  -> DataRuntimeDefinition<T>.DefaultValue 或 DataSlot<T>._defaultValue
```

重点是：进入 runtime slot 后，默认值就是 `T`。不要每次 `Get<T>` 都从 text/object 恢复类型。

## 最小 runtime 字段集草案

如果本轮要先做小切片，建议新建或等价实现：

```csharp
public sealed class DataRuntimeDefinition
{
    public required string StableKey { get; init; }
    public required DataValueType ValueType { get; init; }
    public required object? TypedDefaultValueForBoundary { get; init; }
    public Type ValueClrType { get; init; }
    public DataKind DataKind { get; init; }
    public DataRangePolicy RangePolicy { get; init; }
    public float? MinValue { get; init; }
    public float? MaxValue { get; init; }
    public DataModifierPolicy ModifierPolicy { get; init; }
    public IReadOnlyList<DataAllowedValue> AllowedValues { get; init; }
    public string ComputeId { get; init; }
    public IReadOnlyList<string> Dependencies { get; init; }
}
```

更理想的终局是泛型化：

```text
DataRuntimeDefinition<T>
  StableKey
  DefaultValue: T
  ValuePolicy<T>
  ModifierPolicy<T>
  ComputeBinding<T>
```

但这会影响 catalog 构建、slot 创建、loader 和测试，建议作为第二阶段，不要和字段瘦身硬塞进同一个 SDD。
