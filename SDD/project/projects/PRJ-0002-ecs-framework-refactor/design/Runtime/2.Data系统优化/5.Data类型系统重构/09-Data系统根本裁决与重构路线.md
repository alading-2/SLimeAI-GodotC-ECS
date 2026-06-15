# Data 系统根本裁决与重构路线

> 原始问题：见 [`0.Prompt.md`](./0.Prompt.md)，尤其是 2026-06-15 用户追加裁决。  
> 本文目标：把这轮“可以推倒重来”的授权落成长期设计裁决，避免后续继续在过度复杂的 Data 形态上小修小补。

## 结论

用户判断成立：SlimeAI 当前 Data 系统确实走偏了。它的问题不是“用了字典”这么简单，而是把 **功能解耦** 误实现成了 **数据形态解耦**。

SlimeAI 真正要解耦的是：

```text
Ability / Feature / Damage / AI / UI / Movement 等功能之间不要互相持有实现细节
```

不需要解耦成：

```text
所有数据都必须进入同一个动态 descriptor / policy / object / converter 管线
```

当前 DataOS descriptor -> runtime snapshot -> generated key -> catalog -> slot 的链路不是完全错误，但它已经把太多 authoring、presentation、policy、validator、runtime storage、debug、AI routing 的职责塞进 Data runtime。结果是 Data 这个框架核心变得比业务功能还复杂，类型恢复、默认值、policy、computed、modifier 和诊断互相缠绕。

新的根本裁决是：

```text
功能解耦优先，数据结构直接优先。

Data 只保留为“跨功能共享的 typed runtime state protocol”。
不是所有字段、所有配置、所有临时状态、所有权限规则都必须进入 Data。
```

## SlimeAI 到底要什么

SlimeAI 的目标不是做一个传统 ECS 复刻，也不是做一个万能动态数据容器。更准确的目标是：

```text
AI 能稳定理解项目结构
功能 owner 边界清楚
运行时协议强类型
数据输入可验证
错误可观察
实现代码简单直接
```

这些目标不要求 Data 形态统一到极致。

传统 ECS 也能解耦：Component 存数据，System 处理数据，系统通过 query 组合数据，功能之间不直接互调。QFramework 也能解耦：Model 持有强类型状态，Command / Query / Event 约束访问路径。它们都不是 AI-first 的反面。

所以 SlimeAI 后续不能再把“统一 Data”当成默认正确答案。统一 Data 只有在它真的降低功能耦合、提高 AI 追溯和验证效率时才保留；一旦它让核心代码更复杂、更弱类型、更难验证，就必须拆掉或降级。

## 当前 Data 的真实根因

### 1. DataDefinition 变成了新 DataMeta

旧 `DataMeta` 的问题是大而全。当前 `DataDefinition` 删除了旧 registry，却又把 owner、presentation、policy、migration、runtime、computed、modifier 混进一个 runtime 对象，形成了新的大而全。

这不是“字段多一点”的问题，而是职责错位：

- owner / skill / description 服务 AI 路由和文档。
- display / ui / icon 服务展示。
- write policy 服务组织纪律。
- range / allowed / modifier / computed 服务数据形态。
- stable key / type / default 服务 runtime storage。

它们不应该都进入 `DataRuntimeStorage` 的核心对象。

### 2. 类型被描述了很多次，却没有真正固定

当前类型信息至少在 DataOS、snapshot DTO、generated `DataKey<T>`、catalog、`DataValueConverter`、`DataSlot<T>` 中重复表达。结果不是更安全，而是 loader、storage、record query、compute registry、handle generator、validator 各自维护映射。

正确方式是：

```text
生成期确定类型
catalog build 绑定 CLR Type / codec / typed default
runtime slot 只保存 T
业务 API 只接 DataKey<T>
```

运行时不应该继续靠 `DataValueType + object` 反复猜。

### 3. policy 过度设计

`write_policy` 这类权限规则把 Data runtime 变成“谁能写”的裁判。这个方向收益很低，成本很高。

SlimeAI 的 AI-first 规则应该主要靠：

```text
DocsAI / owner skill / validator / test / code review / grep gate
```

而不是每次 `Data.Set` 都进入权限系统。保留数据形态 contract，例如 computed、modifier、range、allowed values；降级 runtime 权限 policy。

### 4. 字典不是原罪，但不能承担类型系统

`.NET Dictionary<TKey,TValue>` 本身不是脏结构。它适合 stable key 索引、loader、debug 和稀疏字段访问。

问题是当前 Data 让字典、`IDataSlot`、`object`、`DataValueType` 一起承担了类型恢复和跨功能协议。长期应把 stable key 降级为人和 AI 的 identity，把热路径查找改为 generated runtime id：

```text
DataKey<T>(stableKey, runtimeId)
slots[runtimeId] -> DataSlot<T>
```

但这应排在类型契约收口之后，不应第一步就做性能结构重写。

## 新边界

### Data 应该保存什么

Data 只保存跨功能共享、需要 AI/validator/diagnostic 追溯的运行时状态，例如：

- HP、Mana、Attack、MoveSpeed 这类共享属性。
- Ability / Feature / Damage 共同需要读取或修改的数值。
- runtime record 初始化后需要被多个功能观察的字段。
- 需要进入 DataOS authoring / generated key / scene test 的字段。

### Data 不应该保存什么

默认不进 Data：

- 单个 Capability 内部临时缓存。
- 一帧内的局部计算值。
- 只服务某个系统内部算法的索引、数组、查询缓存。
- UI 展示文案、图标、分组等 presentation metadata。
- “谁可以写”这类组织纪律 policy。
- 纯配置表对象，如果它不会成为 Entity runtime state。

这些可以放在 owner service、component、system config、generated projection、cache 或 manifest。解耦靠 owner API、事件和测试，不靠把所有东西塞进 Data。

## 推荐架构

### Authoring 层

DataOS 保留，但定位改清楚：

```text
业务表优先
descriptor 只描述共享 runtime field
presentation / owner / docs routing 进入 sidecar 或 manifest
validator 在生成前发现类型和数据问题
```

不要把 DataOS descriptor 变成万能 EAV，也不要把每个展示字段、权限字段都投影进 runtime definition。

### Generated Contract 层

generated key 不再只是 thin handle。中期目标：

```csharp
public readonly record struct DataKey<T>(string StableKey, int RuntimeId);
```

`StableKey` 服务 DataOS、日志、AI、debug；`RuntimeId` 服务热路径。业务代码只看 `GeneratedDataKey.Xxx`。

### Runtime Definition 层

把当前 `DataDefinition` 瘦身为 runtime 必需字段：

```text
stableKey
runtimeId
clrType / codec
typedDefault
dataKind
range? / allowedValues? / modifier? / computeBinding?
```

不进入 runtime definition：

```text
ownerDomain
ownerCapability
ownerSkill
displayName
description
uiGroup
unit
format
iconPath
writePolicy
legacy mirror fields
```

### Runtime Storage 层

第一阶段可继续用：

```text
Dictionary<string, IDataSlot>
```

但必须明确只是协议索引。中期改为：

```text
IDataSlot?[] slotsByRuntimeId
Dictionary<string, int> stableKeyToRuntimeId 仅 loader/debug 使用
```

高频 numeric lane 只能在 profiler 证明后做，且不能形成第二事实源。

## 方案取舍

| 方案 | 裁决 | 理由 |
| --- | --- | --- |
| 继续当前 descriptor-first 大 Data | 拒绝 | 能跑但复杂度继续堆高，类型和 policy 问题不会自然消失。 |
| 小修 `DataDefinition` 字段 | 不够 | 只能缓解观感，不能解决职责错位和类型重复。 |
| 保留 DataOS + 大幅瘦身 runtime Data | 推荐 | 保留 AI 追溯和 validator 收益，同时让 runtime 变简单。 |
| 完整改传统 ECS component storage | 可作为未来大分支，不作为第一步 | 能解决类型和性能，但会重建 Entity/Query/Schedule/GodotBridge/DocsAI 路由，范围过大。 |
| QFramework Model / BindableProperty 替代 Entity.Data | 拒绝 | 它适合应用层状态，不适合 SlimeAI per-entity DataOS / snapshot / modifier / computed 主链路。 |

## 执行路线

### SDD-A：Data Runtime Simplification Hard Cutover

目标：先让 Data runtime 变小。

范围：

- `RuntimeDataDescriptorDto`
- `DataDefinition`
- `RuntimeDataSnapshotLoader`
- `DataDefinitionCatalog`
- DataOS snapshot generator / validator
- TestSystem / debug presentation 查询入口

验收：

- runtime descriptor 不再投影 owner/presentation/legacy mirror 字段。
- `write_policy` 不再作为新 runtime 架构核心。
- presentation descriptor 或 manifest 可以供 debug/UI/AI 查询说明。
- catalog build 产生 report；fatal 前写 structured log，再 throw。

### SDD-B：Data Type Contract Hard Cutover

目标：把类型转换收口到生成期和 catalog build。

范围：

- `DataTypeContract`
- `DataValueCodec`
- `DataValueConverter`
- `DataSlot<T>`
- `DataRuntimeStorage`
- `DataComputeRegistry` / computed output type 校验
- `validate-dataos.sh` / `generate-data-key-handles.py`

验收：

- default typed value 只转换一次。
- record value 只在 loader/debug 边界转换一次。
- slot 类型由 catalog 决定，不由第一次 boundary value 决定。
- `DataComputeRegistry` 不再自己 switch `DataValueType` 推断输出类型。
- generated handle type gate 作为强门禁。

### SDD-C：Generated RuntimeId Storage

目标：把 hot lookup 从 string dictionary 改成 id array。

范围：

- `DataKey<T>`
- generated key handle
- catalog runtime id 稳定规则
- `DataRuntimeStorage`
- 调用点和测试

验收：

- `DataKey<T>` 携带 stable key + runtime id。
- runtime storage 优先通过 runtime id 查 slot。
- stable key 仍用于日志、DataOS、debug、diagnostic。

### SDD-D：是否进入传统 ECS 分支

只有在 SDD-A/B/C 后仍证明 Data 是主要复杂度或性能瓶颈，才考虑完整传统 ECS 分支。

触发条件：

- profiler 证明 Data.Get/Set 或 modifier/computed 是主热点。
- capability owner 缓存已经不足以解决热点。
- DataOS/generator 仍无法让 runtime 代码保持简单。

否则不提前重写 world/query/chunk。

## 对 SDD-0044 的影响

`SDD-0044 DataComputeRegistry Singleton And Catalog Validation Convergence` 的局部方向仍然成立：registry 应该变成轻量 resolver table，catalog build 负责验证，fatal 前要有 report/log。

但它不应再作为孤立小修优先执行。新的优先级是：

```text
先冻结 Data runtime 简化总方向
再把 SDD-0044 内容并入 SDD-B 或作为 SDD-B 的前置子任务
```

原因：如果先做 registry 单例，却不瘦身 `DataDefinition` 和类型 contract，Data 复杂度只会从一个局部移动到另一个局部。

## 外部资料采纳

externalResources:

```yaml
enabled:
  - engine-framework
  - official-docs
scope:
  - Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/07-QFramework数据结构与SlimeAIData对比深化.md
  - Resources/Engine/Engine/QFramework
  - Context7 Bevy ECS StorageType
  - .NET Dictionary / CA1854
  - Unity Entities component/chunk docs
reason: 判断成熟框架如何处理数据结构、类型边界、字典和 runtime storage。
expires: current-task
copiedCodeOrAssets: none
```

Evidence:

- QFramework 用 `Model` / `BindableProperty<T>` / Command / Query / Event 组织状态；字典主要做 IOC、事件表、注册表，不承担字段类型恢复。
- Bevy ECS 官方文档把 component storage 分为 Table / SparseSet，说明成熟 ECS 会按访问模式选择 typed storage。
- Unity Entities 的 component 是 `IComponentData` struct，authoring/baking 后 runtime 读取 typed component。
- .NET Dictionary 是 hash table，key lookup 很快但不是零成本；CA1854 也提示避免重复 lookup。

Inference:

- 成熟框架不是没有动态索引，而是把动态索引用于 registry / routing / storage metadata，把业务 payload 留在明确类型中。
- SlimeAI 可以保留 stable key 和 DataOS，但 runtime 不应继续靠 dynamic value pipeline 保持解耦。

Unknown:

- 当前没有 SlimeAI profiler 证明 dictionary lookup 是主瓶颈。
- 完整改传统 ECS 的收益和成本需要单独 SDD 评估，不能在本设计中假设一定更好。

## 默认假设

- 用户已授权重大重构，不保兼容。
- 后续 Data 改动采用 hard cutover，不做长期 adapter。
- 不立即放弃 DataOS；先把 DataOS 降回 authoring / validator / generator 边界。
- 不立即重写完整 ECS world/query/chunk；先让 Data runtime 简化。
- 功能解耦由 Capability owner、事件、typed API、DocsAI、测试和验证保证，不由动态 Data 形态保证。
