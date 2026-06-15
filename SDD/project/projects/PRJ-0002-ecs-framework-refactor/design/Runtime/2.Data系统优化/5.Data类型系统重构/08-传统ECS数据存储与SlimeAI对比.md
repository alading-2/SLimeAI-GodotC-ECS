# 传统 ECS 数据存储与 SlimeAI Data 对比

> 本轮原始问题：见 [`0.Prompt.md`](./0.Prompt.md) 第二段。用户要求单独说明传统 ECS 如何存数据，为什么它能避免当前 Data 的类型转换问题，并判断 SlimeAI 是否应该改成传统 ECS 的单独变量 / component storage。  
> 本文目标：把“传统 ECS / 成熟框架的数据结构”与 “SlimeAI 统一 Data 容器”拆开比较，避免把 AI-first 误解为某一种固定运行时形态。

## 先给结论

传统 ECS 的类型问题更轻，不是因为它没有类型转换，而是因为它把类型转换放在 authoring / baking / import 边界，运行时数据已经是组件字段、数组、chunk、table 或 sparse set 中的真实类型。

SlimeAI 现在的问题也不是“用了字典就必然错”，而是：

```text
DataOS / snapshot / generated DataKey<T> / catalog / slot
都在表达字段类型
但运行时仍反复通过 DataValueType + object 做恢复和防御
```

所以推荐路线不是立刻改成传统 ECS，而是：

```text
短期：Dictionary<string, IDataSlot> 只作为 stableKey 索引
中期：DataKey<T>(stableKey, runtimeId) + runtimeId -> IDataSlot?[]
长期：只对 profiler 证明的热点字段做 numeric lane / typed sparse lane
```

AI-first 不绑定某种存储形式。QFramework、Unity ECS、Bevy、Flecs、Godot C# 框架都可以通过清楚的 DocsAI、规则、测试和验证变成 AI-friendly 框架。SlimeAI 选择统一 Data 的理由，是它想让 AI 从 DataOS / generated key / owner docs 一路追溯字段，而不是让字段散落到多个 component struct 和 system query 里。

## 传统 ECS 通常怎么存数据

### Unity Entities：archetype + chunk + component struct

Unity Entities 的基本形状是：

```csharp
public struct Health : IComponentData
{
    public float Current;
    public float Max;
}
```

系统处理时查询 `Health` 这类 component。官方文档说明，`IComponentData` 用来标记 component 类型，component 最好只是纯数据；同一组 component 的 entity 形成 archetype，component data 按 archetype 存在 16KiB chunk 中。

这套结构的重点是：

```text
字段类型在 C# struct 里
系统 query 决定读写哪类 component
运行时按 chunk 批量迭代
```

它不会在每次读 `Health.Current` 时问“这个字段是 float 还是 string”。转换早在 Baker / authoring / import 阶段完成。

### Bevy：Table / SparseSet

Bevy 的 component 也是真实 Rust 类型。`StorageType` 提供两种典型存储：

```rust
#[derive(Component)]
#[component(storage = "SparseSet")]
struct A;
```

官方文档对 `Table` 和 `SparseSet` 的取舍很直接：

```text
Table：迭代快、缓存友好，增删组件慢一些；默认存储。
SparseSet：增删组件快，迭代慢一些。
```

这说明成熟 ECS 不是只用一种结构，而是按访问模式选择 storage lane。

### Flecs / EnTT / Arch：component id + typed payload + query

这些框架各自实现不同，但共同点是：

```text
component 类型注册成 id
storage 知道 id 对应的 payload layout
系统通过 view/query/group 迭代匹配的实体
动态 id / registry 只做寻址和元数据
payload 仍然按真实类型存
```

这对 SlimeAI 的启发是：动态索引可以存在，但类型系统不能放在热路径里猜。

## QFramework 不是 ECS，但给了另一个答案

QFramework 的主线不是 ECS storage，而是应用层 MVC/CQRS 架构。它的数据一般放在 `Model` 的 C# 字段里，例如：

```csharp
public interface ICounterAppModel : IModel
{
    BindableProperty<int> Count { get; }
}

public class CounterAppModel : AbstractModel, ICounterAppModel
{
    public BindableProperty<int> Count { get; } = new BindableProperty<int>();
}
```

`BindableProperty<T>` 内部保存 `T mValue`，变更时触发 `EasyEvent<T>`。它也使用字典，但字典用途是 `Type -> instance`、`Type -> event`、`string -> event` 这类注册表和事件表，不负责把任意字段恢复成类型。

这说明一个成熟框架可以完全不走 SlimeAI 统一 Data 形式。它通过强类型 Model 字段 + Command/Query/System 约束访问路径，获得简单清晰的代码。

但这不是 SlimeAI 可以直接照搬的原因：

- QFramework 没有 DataOS descriptor / runtime snapshot / generated DataKey / modifier / computed / per-entity Data。
- 它的 Model 更适合应用级共享状态，不适合 SlimeAI 当前每个 Entity 都有一套可追溯字段的协议。
- 它的简洁来自“字段在代码里”，SlimeAI 的目标是“字段可由 DataOS 和 AI 生成、检查、追踪”。

详见 Resources 专项报告：`/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/07-QFramework数据结构与SlimeAIData对比深化.md`。

## Dictionary 为什么不是“脏结构”

.NET 官方文档说明，`Dictionary<TKey,TValue>` 是 hash table，用 key 取值很快，接近 O(1)。所以 `Dictionary<string, IDataSlot>` 不是天然糟糕，也不是“不整齐”。

它的问题在访问模式：

```text
单个实体偶发读写某个字段：
  Dictionary 很合适，stableKey 对 AI、日志、debug 也友好。

每帧对大量实体批量读写同一个字段：
  Dictionary 会重复 hash string、比较 key、追 entry 链、再走 IDataSlot 接口；
  数组 / table / chunk 可以按连续内存或连续索引扫描。
```

也就是说，字典不是错，但它不适合做高频批量数值运算的终局结构。

### string key 的真实成本

`string` 的问题不是“有字符串就慢”，而是同一帧大量读写时会重复：

```text
计算 hash
定位 bucket
比较 key
取 entry.value
从 IDataSlot 做接口分派
再进入 DataSlot<T>
```

如果 generated key 携带 `runtimeId`：

```csharp
public readonly record struct DataKey<T>(string StableKey, int RuntimeId);
```

热路径就可以变成：

```text
slots[key.RuntimeId] -> DataSlot<T>
```

`StableKey` 仍保留给日志、debug、DataOS、DocsAI 和 snapshot；运行时读写不必每次用它查字典。

这不是推翻 Data，而是把 stable key 从 hot lookup key 降级为 human/AI/debug identity。

## 字典、数组、chunk 的对比

| 结构 | 适合 | 不适合 | 对 SlimeAI 的位置 |
| --- | --- | --- | --- |
| `Dictionary<string, IDataSlot>` | 字段稀疏、debug、loader、按 stableKey 追溯 | 大量实体同字段批量读写 | 短期协议层 |
| `runtimeId -> IDataSlot?[]` | 每个实体按字段 id 快速读写 | 跨实体同字段连续扫描仍一般 | 中期主存储 |
| typed sparse lane | 稀疏热点字段、频繁增删 | 纯批量迭代不如 table | 后续热点优化 |
| numeric array lane | HP/Speed/Attack 这类热点数值 | 复杂对象、低频字段 | profiling 后局部采用 |
| archetype/chunk | 大量 entity 同组件批量系统迭代 | Godot Node 桥接、DataOS 字段协议、AI 简单修改 | 不作为当前默认方向 |

## 单独变量能不能保持解耦

可以。传统 ECS 的解耦是：

```text
Component 存数据
System 处理数据
系统通过 query 组合数据
组件之间不直接互调
```

这种方式非常成熟。它的优点是代码类型强、IDE 友好、性能模型清晰。

但对 SlimeAI 来说，它会把 AI 的理解路径改成：

```text
字段在哪个 component struct
哪个 authoring source 生成它
哪个 system query 读写它
哪个 test 覆盖它
```

统一 Data 的理解路径是：

```text
stableKey
  -> DataOS descriptor
  -> GeneratedDataKey<T>
  -> owner docs / skill
  -> runtime snapshot record
  -> scene/test diagnostics
```

这两种都能 AI-first。区别不是“哪个更像 AI 框架”，而是 SlimeAI 当前更想优化哪条路径。

## SlimeAI 应该吸收什么

### Adopt Now

- 把 DataOS generator 当成 Unity Baker：runtime 不回头猜 authoring text。
- 把 `DataKey<T>` 当成业务唯一 typed handle：业务代码不新增 string/object Data 访问。
- 把 `Dictionary<string, IDataSlot>` 明确定义为协议索引：它不承担类型系统。
- 把 `write_policy` 这类权限约束降级，不让 runtime 替人维护“谁能写”的组织纪律。
- 保留真正影响数据形态的契约：类型、默认值、computed、modifier、enum/allowed values、range。

### Adopt Later

- `DataKey<T>(stableKey, runtimeId)`。
- `runtimeId -> IDataSlot?[]`。
- 高频 numeric lane。
- DataTypeContract / DataValueCodec manifest 化，减少 Python/C# 双写。

### Reject

- 第一阶段重写为 Unity/Bevy/Flecs/Arch 的完整 archetype/chunk ECS。
- 把字段拆回各 Capability 私有字段作为默认方式。
- 用 QFramework `Model + BindableProperty<T>` 替代 Entity.Data 主链路。
- 让 AI 直接手写 storage/query DSL。

## 当前判断的不确定项

- 还没有 SlimeAI 自己的 profiler 数据证明字典查找是瓶颈。
- 不清楚未来游戏规模是否会让 `Data.Get/Set` 成为每帧主热点。
- `runtimeId` 的稳定性需要 catalog/profile 规则保证，否则不同 snapshot 可能产生 id 漂移。
- numeric lane 一旦引入，必须防止与 `DataSlot<T>` 形成双事实源。

## 外部证据

- .NET `Dictionary<TKey,TValue>` 官方文档：key lookup 很快，接近 O(1)，因为它是 hash table。  
  `https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.dictionary-2`
- .NET CA1854：`ContainsKey` 后再用 indexer 会做两次 lookup，推荐 `TryGetValue`。这说明字典 lookup 虽快，但不是零成本。  
  `https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1854`
- Unity Entities components：`IComponentData`、archetype、16KiB chunk。  
  `https://docs.unity3d.com/Packages/com.unity.entities@1.4/manual/concepts-components.html`
- Bevy ECS `StorageType`：`Table` 快速且缓存友好迭代，`SparseSet` 增删快但迭代慢。  
  `https://docs.rs/bevy_ecs/latest/bevy_ecs/component/enum.StorageType.html`
- Context7 资料源：`/websites/unity3d_packages_com_unity_entities_1_4`、`/dotnet/docs`。
