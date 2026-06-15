# Data 存储结构与 AI-first 裁决

> 本轮原始问题：见 [`0.Prompt.md`](./0.Prompt.md)。用户追问 `Dictionary<string, IDataSlot>` 是否只是临时解决，`runtimeId -> IDataSlot[]` 是否更合理，传统 ECS/QFramework 是否说明 SlimeAI 当前 Data 方向错了。  
> 本文目标：只给裁决和路线图；传统 ECS 与 QFramework 的细节证据见 [`08-传统ECS数据存储与SlimeAI对比.md`](./08-传统ECS数据存储与SlimeAI对比.md) 和 Resources QFramework 专项报告。

## 裁决

`Dictionary<string, IDataSlot>` 可以作为短期协议层，但不能作为 SlimeAI Data 的长期性能存储层。

更准确地说：

```text
短期：
stableKey -> IDataSlot
slot 内部保存 typed T
业务只走 GeneratedDataKey<T>
字典只做字段寻址，不承担类型系统

中期：
GeneratedDataKey<T> 携带 stableKey + runtimeId
DataRuntimeStorage 内部优先 runtimeId -> IDataSlot?[]
stableKey 字典只留给 loader/debug/diagnostic

长期：
只对 profiler 证明的高频 numeric 字段做 generated numeric lane / typed sparse lane
外部仍通过 DataKey<T> 和 DataOS descriptor 暴露
```

不建议第一阶段改成 Unity Entities / Bevy / Flecs / Arch 那种完整 archetype/chunk ECS，也不建议把字段拆回各 Capability 私有变量。那会解决一部分性能焦虑，但会把 SlimeAI 现在最有价值的 DataOS、generated key、DocsAI 路由、snapshot、validator 和 scene test 追溯链打散。

## AI-first 的关键修正

用户这轮提醒是正确的：AI-first 不等于必须把所有数据放进统一字典。一个传统 ECS、QFramework 这种 MVC/CQRS 框架，甚至更常规的 Godot C# 框架，只要有足够清楚的 DocsAI、规则、测试和验证，也可以对 AI 友好。

所以这里的选择不应被表述成：

```text
统一 Data = AI-first
传统 ECS = 非 AI-first
```

真正的问题是 SlimeAI 要优化哪条理解路径。

传统 ECS 的路径是：

```text
component struct
  -> system query
  -> authoring/baker/import
  -> tests/profiler
```

SlimeAI 当前选择的路径是：

```text
stableKey
  -> DataOS descriptor
  -> GeneratedDataKey<T>
  -> owner docs / skill
  -> runtime snapshot record
  -> runtime diagnostics / scene test
```

两条都能解耦，也都能 AI-first。SlimeAI 继续保留统一 Data 的理由不是形式正确，而是当前项目已经围绕 DataOS 和 generated key 建了事实源、验证链和 AI 路由；现在重写成 component schema-first 会把问题扩大成 Runtime 世界模型重建。

## `runtimeId -> IDataSlot[]` 为什么更适合作为中期结构

字典并不“乱”。`.NET Dictionary<TKey,TValue>` 是 hash table，key lookup 接近 O(1)。`Dictionary<string, IDataSlot>` 看起来也很整齐：每个 key 对应一个 slot。

它不适合作为终局，是因为 SlimeAI 的高频访问模式可能是：

```text
每帧大量实体读取 BaseHp / CurrentHp / MoveSpeed / Cooldown
```

这时每次通过 string 做 hash lookup，再经过 `IDataSlot` 接口，会比数组索引更重。`runtimeId` 的价值是让热路径变成：

```text
slots[key.RuntimeId] -> DataSlot<T>
```

同时保留：

```text
key.StableKey -> 日志、DataOS、debug、AI、snapshot
```

这比完整 archetype/chunk 更小，也更符合 SlimeAI 当前 DataOS-first 的边界。

## 方案对比

| 方案 | 优点 | 主要问题 | 裁决 |
| --- | --- | --- | --- |
| `Dictionary<string, IDataSlot>` | 最小改动；stableKey 对 AI、日志、DataOS 友好 | string lookup、每实体 dictionary、批量访问缓存局部性差 | 短期保留 |
| `runtimeId -> IDataSlot?[]` | 热路径数组索引；保留 stableKey 诊断；不重写 ECS | catalog id 稳定性、snapshot/profile 规则需要设计 | 中期推荐 |
| generated numeric lane | 高频数值可走专用数组/缓存 | 容易形成双事实源；需要 profiler 证明 | Adopt Later |
| 完整 archetype/chunk ECS | 批量迭代性能最好；类型最强 | 重建 world/query/schedule/bridge，AI 路由换中心 | 当前拒绝 |
| QFramework Model 字段 | 代码最直接；类型天然清楚 | 不支持 SlimeAI per-entity DataOS/snapshot/modifier/computed 主链路 | 不替代 Data |

## 当前最应该解决的不是字典

当前 Data 最大问题仍然是类型契约没有前移：

```text
default_value_text / JsonElement / object?
  -> loader 转换
  -> DataDefinition.DefaultValue object?
  -> DataSlot<T> 再转换
  -> SetUntyped 再转换
  -> policy / allowed values / computed 又各自判断类型
```

在这个问题没收口前，直接做 `runtimeId` 或 numeric lane 会把复杂度搬到新结构里。

合理顺序是：

```text
1. DataDefinition / RuntimeDataDescriptorDto 瘦身
2. default / record / enum / object_ref 类型检查前移到 validator/generator/catalog build
3. DataSlot<T> 固定 typed default/current，boundary object 只留 loader/debug
4. GeneratedDataKey<T> 增加 runtimeId，storage 改数组索引
5. profiler 证明热点后，再做 numeric lane
```

## Policy 的同步裁决

用户指出 `write_policy` 这类硬性读写权限冗余，这个方向成立。

AI-first 解耦更适合：

```text
规则、DocsAI、owner skill、代码审查和测试约束“谁应该写”
而不是 runtime 用 write_policy 替人维护组织纪律
```

但不能把所有 policy 都一起删除。需要区分：

| 类型 | 裁决 |
| --- | --- |
| `write_policy` 这类权限约束 | 降级或删除 runtime enforcement，保留为文档/validator/report 可选提示 |
| `storage_policy=computed/runtime_only` | 保留或改名为 data kind，因为它影响字段是否存基础值 |
| `modifier_policy` | 保留为数据形态契约，决定是否能挂 modifier |
| `range/allowed_values` | 保留为数据质量契约，尽量在生成期检查，runtime 只做防御 |
| `migration_policy` | 暂留 authoring/manifest，不进 runtime hot definition |

这会同步到 [`06-DataOS字段与Policy决策说明.md`](./06-DataOS字段与Policy决策说明.md)。

## 本文依赖的证据

- 当前代码：`Src/ECS/Runtime/Data/DataDefinition.cs`、`DataRuntimeStorage.cs`、`DataValueType.cs`、`RuntimeDataDescriptorDto.cs`。
- DataOS：`Data/DataOS/Schema/core.sql`、`Data/DataOS/Authoring/DataKeyDescriptors.seed.sql`、`Data/DataOS/Tools/generate-runtime-snapshot.sh`。
- 传统 ECS 对比：[`08-传统ECS数据存储与SlimeAI对比.md`](./08-传统ECS数据存储与SlimeAI对比.md)。
- QFramework 对比：`/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/07-QFramework数据结构与SlimeAIData对比深化.md`。
- 外部资料：.NET Dictionary、.NET CA1854、Unity Entities component/chunk、Bevy `StorageType`，见 08 文档。
