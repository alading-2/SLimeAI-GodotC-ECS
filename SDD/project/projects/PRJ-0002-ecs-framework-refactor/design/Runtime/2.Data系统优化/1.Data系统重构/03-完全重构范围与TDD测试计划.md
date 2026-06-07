# Data 系统完全重构范围与 TDD 测试计划

> 更新：2026-05-28
> 状态：执行前设计裁决，后续执行型 SDD 必须遵守。
> 范围：旧 ECS Data 全链路，包括 `SlimeAI/Src/ECS/Base/Data/`、`SlimeAI/Data/DataKey/`、`SlimeAI/Data/Data/`、`SlimeAI/Data/DataNew/`、Data 相关配置映射、Data 单测和 Godot 测试场景。
> 非目标：本文不直接修改源码；执行时必须另开执行型 SDD，并按 TDD 小步落地。

## 1. 总裁决：不是兼容迁移，而是完整重构

本轮 Data 系统优化不再按“旧系统旁边新增 descriptor-first 通道，再长期兼容旧路径”的方式描述。新的执行裁决是：

```text
旧 Data 系统相关事实源全部重构。
旧 DataMeta / DataRegistry / DataKey 静态定义不再作为长期入口。
旧 Data/Data 与 DataNew 两套配置路径不再保留。
旧 Data 测试场景不做修补，改为基于新 Data 系统的 TDD 测试矩阵重建。
```

这里的“完全重构”不是指立刻无验证地删除文件，而是指执行型 SDD 的目标状态必须是：

- **单一字段定义事实源**：DataOS descriptor / `runtime_snapshot.json.descriptors`。
- **单一运行时定义索引**：`DataDefinitionCatalog`。
- **单一 Data 容器行为模型**：typed handle + catalog + slot + policy + compute resolver。
- **单一测试事实源**：新 Data 系统 TDD 测试，而不是旧 `DataMeta` 行为回归。
- **无旧配置路径兼容负担**：`SlimeAI/Data/Data`、`SlimeAI/Data/DataNew` 不再作为新系统输入。

## 2. 必须删除或改造的旧范围

### 2.1 `SlimeAI/Data/Data/`

裁决：**执行型 SDD 中应删除或清空其 Data 运行时输入职责**。

原因：

- 它承载旧 Godot Resource / 配置类映射语义。
- 它依赖 `[DataKey]`、`Data.LoadFromConfig`、`DataKey.DefaultValue` 等旧入口。
- 它会让 AI 同时面对 DataOS descriptor、Resource 配置类和 C# DataKey 三套事实源。

目标：

```text
旧：SlimeAI/Data/Data/*.cs 或 *.tres -> Data.LoadFromConfig -> Data
新：DataOS 业务表 -> runtime_snapshot.json.records -> RuntimeDataSnapshotLoader.ApplyRecord -> Data
```

如果仍有非 Data 系统专属资源，应迁到对应业务目录，并明确不再作为 Data 字段定义或 Data records 事实源。

### 2.2 `SlimeAI/Data/DataNew/`

裁决：**执行型 SDD 中应删除**。

原因：

- `DataNew` 本质是旧系统迁移尝试中的 DTO 层。
- 它大量读取 `DataKey.Xxx.DefaultValue`，会继续把 C# DataKey 变成事实源。
- 它会制造“新 DataOS”和“DataNew DTO”双新系统并存。

目标：

```text
旧：DataNew/Unit/EnemyData.cs、PlayerData.cs、AbilityData.cs 等 POCO/DTO 默认值
新：DataOS schema + seed + generator + snapshot descriptor/records
```

### 2.3 `SlimeAI/Data/DataKey/`

裁决：**不再手写长期字段定义**。

目标：

- 第一批执行型 SDD 可以读取旧 `DataKey_*.cs` 作为迁移输入清单。
- 迁移完成后，不再允许新增 `DataRegistry.Register(new DataMeta { ... })`。
- 最终如果 C# 业务需要 typed handle，应由 descriptor codegen 生成薄 `DataKey<T>`。
- 生成的 `DataKey<T>` 只包含 `stable_key` 和泛型类型，不包含默认值、范围、分类、computed、modifier 策略等定义信息。

### 2.4 `SlimeAI/Src/ECS/Base/Data/`

裁决：**保留 Data 系统位置，但内部完全重构**。

该目录不应删除，因为它是运行时 Data 容器的合理位置；但内部职责要重建：

```text
Data.cs                 新 Data API、事件通知、owner 绑定
DataSlot.cs             单字段值、modifier、computed cache
DataDefinition.cs       字段定义 core + policy + presentation
DataDefinitionCatalog.cs 启动时冻结索引
DataComputeRegistry.cs  compute_id -> resolver
DataValueConverter.cs   strict convert / range policy / allowed values
RuntimeSnapshot/*       snapshot DTO + loader + apply report
```

旧 `DataMeta`、`DataRegistry`、反射 `LoadFromConfig`、隐式 string key 机制不再作为新系统核心。

### 2.5 `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/`

裁决：**旧 Data 测试场景完全重构，不做修补**。

现有测试场景问题：

- 以 `new Data()` 和旧 `DataRegistry` 为前提。
- 测试 `DataMeta.GetDefaultValue`、`IsPercentage`、`Options`、`Compute` lambda 等旧行为。
- 测试 `DataNew` 默认值和 `[DataKey]` 映射回退。
- `TestDataKeyMapping` 明确验证“无标签按名映射兼容”，这与新系统禁止隐式兼容入口冲突。

目标：

```text
旧测试：验证 DataMeta / DataRegistry / DataNew / LoadFromConfig 是否还能工作。
新测试：用 TDD 验证 DataDefinitionCatalog / DataSlot / policy / compute resolver / snapshot apply 是否正确。
```

## 3. 执行时删除规则

完全重构不等于无序删除。删除应遵守：

1. **先 TDD 写红灯测试**：先写新 Data 系统失败测试，证明目标行为未实现。
2. **再实现新核心**：按最小闭环让测试变绿。
3. **迁移调用点**：业务系统改到新 API。
4. **删除旧路径**：只有当新测试覆盖并调用点迁移完成后，删除旧目录和旧入口。
5. **验证无引用**：使用 grep / build / test 确认无 `DataNew`、`Data/Data/`、`LoadFromConfig`、旧 `DataMeta.Compute` 依赖。

禁止：

- 为了编译临时保留一条旧兼容 API 却不记录删除条件。
- 新旧两套 Data 输入长期共存。
- 把旧测试改到勉强通过，却仍验证旧行为。
- 让 `DataKey.DefaultValue` 继续成为任何 DTO 默认值来源。

## 4. 新 Data 系统 TDD 测试矩阵

### 4.1 TDD 总流程

每个功能按以下顺序执行：

```text
RED：写一个失败测试，描述新 Data 行为。
GREEN：写最小实现让测试通过。
REFACTOR：整理 Data / Slot / Catalog / Loader 结构，确保测试仍绿。
```

执行型 SDD 必须在任务中记录每个切片的 RED/GREEN/REFACTOR 证据。

### 4.2 Catalog 小测试

这些测试应是纯 C# 小测试，不依赖 Godot 场景树：

```text
BuildCatalog_ShouldRejectDuplicateStableKey
BuildCatalog_ShouldRejectEmptyStableKey
BuildCatalog_ShouldRejectUnknownValueType
BuildCatalog_ShouldConvertDescriptorDefaultValue
BuildCatalog_ShouldRejectInvalidDefaultValue
BuildCatalog_ShouldBuildDefinitionIndexByStableKey
BuildCatalog_ShouldBuildDependentComputedIndex
BuildCatalog_ShouldRejectUnknownDependency
BuildCatalog_ShouldRejectComputeCycle
BuildCatalog_ShouldRejectComputedWithoutComputeId
BuildCatalog_ShouldRejectMissingComputeResolver
BuildCatalog_ShouldValidateStoragePolicy
BuildCatalog_ShouldValidateWritePolicy
BuildCatalog_ShouldValidateRangePolicy
BuildCatalog_ShouldValidateModifierPolicy
BuildCatalog_ShouldValidateMigrationPolicy
```

### 4.3 Data core 小测试

```text
Data_Get_ShouldReturnDescriptorDefault
Data_Set_ShouldStoreTypedValue
Data_Set_ShouldRejectUnknownKey
Data_Set_ShouldRejectWrongType
Data_Set_ShouldRespectWritePolicy
Data_Set_ShouldRejectComputedReadonlyKey
Data_Set_ShouldApplyRangePolicyValidate
Data_Set_ShouldApplyRangePolicyClampRuntime
Data_Set_ShouldApplyRangePolicyRejectRuntime
Data_Set_ShouldValidateAllowedValues
Data_Get_ShouldNotCreateUnknownKeyImplicitly
Data_Remove_ShouldRespectWritePolicy
Data_Clear_ShouldResetAllowedRuntimeStateOnly
```

### 4.4 Modifier 小测试

```text
Data_AddModifier_ShouldRejectNonNumericKey
Data_AddModifier_ShouldRejectKeyWithoutModifierPolicy
Data_AddModifier_ShouldApplyAdditive
Data_AddModifier_ShouldApplyMultiplicative
Data_AddModifier_ShouldApplyFinalAdditive
Data_AddModifier_ShouldApplyOverrideByPriority
Data_AddModifier_ShouldApplyCapAfterOtherModifiers
Data_RemoveModifierBySource_ShouldRemoveOnlyMatchingSource
Data_ModifierChange_ShouldMarkDependentComputedDirty
```

### 4.5 Compute 小测试

```text
Data_GetComputed_ShouldUseResolver
Data_GetComputed_ShouldUseDescriptorDependencies
Data_GetComputed_ShouldPassComputeParams
Data_GetComputed_ShouldCacheUntilDependencyChanges
Data_GetComputed_ShouldInvalidateTransitiveDependents
Data_GetComputed_ShouldRejectResolverSideEffectContractViolation
Data_GetComputed_ShouldReportMissingResolver
Data_SetComputed_ShouldFail
```

### 4.6 Snapshot apply 小测试

```text
ApplyRecord_ShouldApplyPersistedFields
ApplyRecord_ShouldRejectUnknownFieldKey
ApplyRecord_ShouldRejectFieldTypeMismatch
ApplyRecord_ShouldRejectValueConversionFailure
ApplyRecord_ShouldRejectComputedRecordWrite
ApplyRecord_ShouldRejectRuntimeOnlyRecordWrite
ApplyRecord_ShouldReportAllErrorsWithoutPartialCrash
ApplyRecord_ShouldReturnStructuredReportWithTableAndRecordId
```

### 4.7 Feature 数据接入测试

Feature 不替代 compute，但 Feature 相关 Data 必须纳入新定义体系：

```text
FeatureModifiers_ShouldBeRepresentedAsAuthoringBlobDefinition
FeatureModifiers_ShouldValidateTargetKeyExists
FeatureModifiers_ShouldRejectTargetKeyWithoutModifierPolicy
FeatureModifiers_ShouldRejectNonNumericTargetKey
FeatureGranted_ShouldApplyModifiersToOwnerData
FeatureRemoved_ShouldRemoveModifiersBySource
FeatureModifierChange_ShouldInvalidateComputedDependents
```

这些测试可以分为纯 C# service 测试和少量 Godot 场景 smoke。不要用旧 `FeatureModifiers` 裸 `const string` 作为长期测试入口。

### 4.8 Godot 单场景测试重建

`SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/` 下旧场景应替换为新场景包：

```text
DataCatalogTestScene        验证 catalog 启动、descriptor 加载、错误报告展示
DataRuntimeTestScene        验证 Data.Get/Set、policy、modifier、computed
DataSnapshotApplyTestScene  验证 record apply 和结构化错误
DataFeatureBridgeTestScene  验证 Feature.Modifiers 与新 Data modifier policy 连接
```

场景测试只做集成 smoke，不替代纯 C# 小测试。核心行为必须先由小测试 TDD 覆盖。

## 5. 必须清理的旧引用

执行型 SDD 结束前必须让以下搜索无结果，或只出现在历史文档 / 删除说明中：

```text
SlimeAI/Data/DataNew
SlimeAI/Data/Data/
DataNew
LoadFromConfig
DataKey.DefaultValue
DataRegistry.Register(new DataMeta
DataMeta.Compute
Data.Get<T>("裸字符串业务 key")
TestDataKeyMapping
无标签按名映射
```

注意：`SlimeAI/Data/DataKey/` 可在执行中作为迁移输入被扫描，但最终不能作为字段定义事实源。

## 6. 文档同步要求

完全重构执行型 SDD 完成后必须同步：

- `SlimeAI/Docs/框架/ECS/Data/DataSystem_Design.md`：改成新 Data 系统唯一主说明。
- `SlimeAI/Data/README.md`：删除 `Data/Data`、`DataNew` 作为推荐路径的说明。
- `SlimeAI/Src/ECS/**` 旁 Data 相关文档、DocsNew 和 SDD design：同步 descriptor-first、TDD 测试入口和删除旧路径；`SlimeAI/DocsAI/` 已删除，不再作为同步目标。
- `.ai-config/skills` 中 Data 相关 skill：如修改 Data 系统接口/流程，必须同步 skill 源并运行 AI config sync。

## 7. 执行拆分建议

建议后续执行型 SDD 不再叫“兼容迁移”，而叫：

```text
SDD: Data System Full Rewrite
```

推荐拆分：

1. **RED 测试骨架**：先建立新 Data 小测试工程/测试场景，写 catalog 与 Data core 红灯测试。
2. **Core Catalog**：实现 `DataDefinition`、policy enum、catalog、validator。
3. **DataSlot Runtime**：实现 typed value、range、allowed values、write policy。
4. **Modifier Runtime**：实现 modifier policy 和完整 modifier pipeline。
5. **Compute Runtime**：实现 resolver、cache、依赖图、循环检测。
6. **Snapshot Apply**：实现 descriptor/record loader 和结构化报告。
7. **Feature Bridge**：让 `Feature.Modifiers` 正式接入新 Data modifier policy。
8. **Delete Old Paths**：删除 `SlimeAI/Data/Data`、`SlimeAI/Data/DataNew`，重写 `SingleTest/ECS/Data`。
9. **Docs/Skill Sync**：更新 Data 长期文档、测试文档、skill。

## 8. 最终验收标准

完成不是“新 Data 能跑”，而是同时满足：

- 新 Data 所有核心能力由 TDD 测试覆盖。
- 旧 `Data/Data` 和 `DataNew` 不再作为项目路径存在，或目录只剩明确迁移说明且无源码参与编译。
- 旧 Data 测试场景被替换为新测试场景。
- `DataMeta` 不再是字段定义事实源。
- `DataRegistry` 不再是运行时 catalog。
- `LoadFromConfig` 不再是 Data 输入通道。
- `DataKey.DefaultValue` 不再被业务 DTO 默认值引用。
- Feature 继续保留生命周期/Modifier 能力，但 computed 只走 Data compute resolver。
- `dotnet build`、Data 小测试、Godot Data 场景 smoke 全部通过。
