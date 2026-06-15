# Data 系统学习落点与重构建议

> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 上游裁决：[`../5.Data类型系统重构/09-Data系统根本裁决与重构路线.md`](../5.Data类型系统重构/09-Data系统根本裁决与重构路线.md)。

## 先修正一个误区

成熟框架没有证明“统一 Data 容器错了”，也没有证明“传统 ECS component storage 一定对”。它们证明的是：

```text
框架核心不能承担太多职责。
类型必须在明确边界固定。
动态索引用于路由，不用于长期承载业务类型系统。
数据是否共享，要有进入条件。
```

所以 SlimeAI 的下一步不是：

```text
把 Data 全部改成 QFramework Model
或把 Entity.Data 全部改成 Unity/Bevy chunk component
```

而是：

```text
继续保留 Data 作为跨功能共享的 typed runtime state protocol，
但把不该由 Data runtime 承担的职责移出去。
```

更上层的校准是：SlimeAI 第一目标不是 Data，也不是表格驱动，而是运行时功能解耦。Data 只负责其中“共享 runtime state”这一段。启动前组合、运行中启停、Capability/Profile manifest、RuntimeCommandBuffer 和 Observation 不能继续被 Data runtime 间接承包。

## Data 进入条件

建议把 Data 进入条件写成硬规则：

只有满足至少一个条件，字段才进入 Entity.Data：

- 被两个以上 Capability 读写或观察。
- 需要通过 DataOS authoring / runtime snapshot 初始化。
- 需要 modifier / computed / range / allowed values 这类数据形态 contract。
- 需要通过 generated `DataKey<T>` 给 AI、测试、debug、日志稳定追溯。
- 需要通过 Data changed event 通知 UI、Ability、Feature、Damage 等 owner。

默认不进入 Entity.Data：

- 单 Capability 内部 cache / index。
- 单帧局部计算值。
- 只服务某个算法的临时数组或集合。
- UI 展示文案、图标、分组、格式。
- “谁可以写”这种组织纪律 policy。
- 纯配置表对象，除非它会变成 Entity runtime state。

这条规则来自 QFramework 的“需要共享的数据才进 Model”，但落点是 SlimeAI 的 DataOS / Capability 边界。

## SDD-A：Data Runtime Simplification

### 目标

先让 runtime Data 变小，而不是继续修某个局部 registry。

### 采纳的成熟框架经验

- QFramework：核心规则少，runtime 不背 presentation 和组织纪律。
- Unity Entities：authoring 和 runtime 分层，runtime component 是明确类型。
- Bevy：storage lane 服务访问模式，不服务文档展示。

### 应该改的方向

```text
RuntimeDataDescriptorDto
DataDefinition
DataDefinitionCatalog
RuntimeDataSnapshotLoader
TestSystem / debug presentation 查询入口
DataOS generator / validator
```

### 设计结果

- runtime descriptor 不再投影 owner / skill / display / description / uiGroup / icon / legacy mirror 字段。
- presentation descriptor 或 sidecar manifest 服务 Debug UI / DocsAI / AI 路由。
- `write_policy` 从 runtime enforcement 核心降级为 DocsAI / skill / validator / grep gate / code review 规则。
- catalog build 输出 `DataCatalogBuildReport`；fatal 前写 structured observation，再 throw。
- `DataDefinition` 只保留 stable key、runtime id、type/codec、typed default、data kind、range/allowed/modifier/computed binding。

## SDD-B：Data Type Contract

### 目标

把类型转换从散落的 loader/storage/compute/generator 中收口。

### 采纳的成熟框架经验

- QFramework：业务字段是 `BindableProperty<T>`，类型在声明处固定。
- Unity Entities：Baker 之后 runtime 是 `IComponentData` struct。
- Bevy / Flecs / Arch：component id 可以动态，但 payload layout 是固定的。
- .NET CA1854：字典 lookup 虽快，但重复 lookup 不是零成本；热路径不应做多余查找。

### 应该改的方向

```text
DataTypeContract
DataValueCodec
DataValueConverter
DataSlot<T>
DataRuntimeStorage
DataComputeRegistry / IDataComputeResolver<T>
validate-dataos.sh
generate-data-key-handles.py
```

### 设计结果

- default typed value 只转换一次。
- record value 只在 loader/debug 边界转换一次。
- slot 类型由 catalog 决定，不由第一次写入值决定。
- generated `DataKey<T>` 类型和 descriptor type 必须有 gate。
- computed resolver output type 在 catalog build 校验，不在运行中猜。
- `SetUntyped` 只保留 loader/debug/TestSystem 边界，并通过命名和注释表达边界。

## SDD-C：Generated RuntimeId Storage

### 目标

把 hot lookup 从 stableKey string dictionary 改为 runtime id array。

### 采纳的成熟框架经验

- Bevy：Table / SparseSet 根据访问模式选择。
- Friflo / Arch：component id / archetype / query 都服务快速定位。
- .NET Dictionary：适合索引，不是高频批量读写终局。

### 应该改的方向

```csharp
public readonly record struct DataKey<T>(string StableKey, int RuntimeId);
```

runtime storage 中：

```text
IDataSlot?[] slotsByRuntimeId
Dictionary<string, int> stableKeyToRuntimeId 仅 loader/debug 使用
```

### 设计结果

- 业务 `Data.Get/Set` 优先走 runtime id。
- `StableKey` 继续服务 DataOS、日志、debug、diagnostic、AI 追溯。
- numeric lane 只在 profiler 证明热点后做，且不得形成第二事实源。

## SDD-0044 的位置

`DataComputeRegistry.Default` 和 catalog validation convergence 方向仍成立，但不应孤立优先执行。

推荐调整为：

```text
DataComputeRegistry 单例
  -> 并入 SDD-B Data Type Contract
  或作为 SDD-B 的前置子任务
```

原因：

- compute registry 的核心问题已经不是“有没有 default singleton”，而是 resolver output type、catalog build report、DataValueType switch 和 typed computed cache 的统一类型契约。
- 如果先孤立做 singleton，容易继续在旧 `DataDefinition` / `DataValueConverter` 结构上堆修补。

## Data 之后的 Runtime 解耦 SDD

Data runtime 简化完成后，建议把后续 runtime 解耦拆成两类：

1. `Capability/Profile Manifest`
   - 每个 Capability 显式声明 DataKeys、Events、Systems、Components、Dependencies、Validation、Reject list。
   - 每个游戏 profile 显式声明启用能力、启用系统、DataOS preset、资源 catalog 和 scene smoke。
   - 先支持启动前组合，不急着做运行中完整能力卸载。

2. `RuntimeCommandBuffer RFC`
   - 只覆盖 Entity spawn/destroy、Component 增删、Relationship/GodotBridge request 这些结构变化。
   - 明确 playback phase 和遍历中结构变更 guard。
   - 和 System diagnostics / lifecycle trace 连接，运行中启停有可复盘证据。

这两个方向不应塞进 Data Type Contract SDD；Data 只提供 typed state protocol。

## 不建议方向

- 不建议把 QFramework `Model + BindableProperty<T>` 当成 Entity.Data 替代品。
- 不建议立刻重写完整 archetype/chunk ECS。
- 不建议把所有字段拆回 Component 私有字段。
- 不建议继续让 Data runtime 承担 owner、presentation、write policy、legacy mirror。
- 不建议用性能焦虑提前做 numeric lane；没有 profiler 证据时先收类型和边界。
- 不建议把“填表即可组合功能”作为当前 SDD-A/B/C 的验收目标；这是底层 runtime 清晰后的上层体验。
