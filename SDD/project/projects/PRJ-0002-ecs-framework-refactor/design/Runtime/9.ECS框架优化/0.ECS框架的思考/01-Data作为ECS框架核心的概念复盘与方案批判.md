# Data 作为 ECS 框架核心的概念复盘与方案批判

> 状态：current concept review
> 更新：2026-06-06
> 关联设计：`../1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`
> 关联执行：`../../../../sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/`

## 一句话结论

`01-Data运行时object去除设计.md` 的大方向是正确的：Data 必须从 `object?` 热路径切到 `DataSlot<T> + IDataSlot`，否则 `DataKey<T>`、DataOS descriptor 和 AI-first 契约都会在运行时被 `object` 宽口抵消。

但这个方案还需要补得更清楚：它不是“泛型槽位一改就完事”，而是要把 SlimeAI 的 Data 定位成 **类型化运行时状态协议**。`DataSlot<T>` 只解决存储层第一刀，后续还必须收口 typed policy、typed computed、typed change、field id/index、边界 API 和高频能力专用索引。否则框架会出现“表面 typed、内部仍动态”的半成品状态。

我的判断不是“这个方案有根本问题”。更准确地说：

- **方向正确**：`DataSlot<T> + IDataSlot` 是当前最适合 SlimeAI 的 Data 去 object 主方案。
- **表达还不够完整**：文档里已经提到 typed policy / computed / changed event，但需要把“哪些必须本轮做、哪些可后续做、哪些不能做”说得更硬。
- **风险需要显式写出**：不要让实现者以为保留 `SetUntyped`、`PropertyChanged(object?)`、`IDataComputeResolver.Compute(): object?` 没问题；这些只能是边界或后续债务，不能继续作为业务主链路。

## SDD-0031 后补充结论：object 仍存在，但性质不同

`SDD-0031 Data Runtime Generic Slot Hard Cutover` 完成后，结论要重新表述：

- Data runtime 的主存储问题已经解决：旧的 `DataSlot.Value object?` 和 `_computedCache Dictionary<string, object?>` 不再是业务值存储主链路。
- `object?` 仍然存在，但分成三类：输入/诊断边界、后续需要 typed 化的协议债务、少量真实业务误用。
- 不能把“全仓还有 object”理解为 SDD-0031 失败；也不能把“主链路已 typed”理解为 Data 终局完成。

从开发者角度看，正确问题不是“要不要完全消灭 object”，而是：

```text
这个 object 是否承载高频业务状态？
这个 object 是否让 AI 或调用方失去 typed contract？
这个 object 是否会把错误从编译期推迟到运行时？
这个 object 是否只是 loader / snapshot / debug / diagnostic 的边界格式？
```

如果答案是前三个之一，就值得改；如果只是第四个，通常不值得为了形式纯洁去改。

### SDD-0031 后 object 残留分级

| 残留位置 | 当前性质 | 是否必须改 | 理由 |
| --- | --- | --- | --- |
| `RuntimeDataSnapshot` / DTO 的 `object? Value`、`DefaultValue` | 输入边界 | 不急 | JSON / SQLite / snapshot 本来就是未泛型输入；边界进入 runtime 后必须转换即可。 |
| `DataValueConverter.TryConvert(object?)`、`SetUntyped(... object?)` | loader/debug/TestSystem 边界 | 保留但收窄 | loader 需要 untyped 入口；业务代码不应调用。 |
| `IDataSlot.GetEffectiveValueForDiagnostics()` / `GetAll()` | 诊断边界 | 改名/收口，不急删 | TestSystem、debug dump 需要跨类型视图；不应作为业务查询。 |
| `DataChangeRecord(object? OldValue/NewValue)` / `PropertyChanged(object?)` | Event / diagnostic 协议债务 | 需要后续改 | 业务监听如果依赖 object event，会继续装箱和 runtime cast。 |
| `IDataComputeResolver.Compute(): object?` | computed registry 协议债务 | 需要后续改 | computed 是 Data 核心能力，不应长期把输出类型推迟到运行时转换。 |
| `DataModifier.Source object?` / `RemoveModifiersBySource(object)` | source identity 宽口 | 建议后续改 | 不是 Data value slot，但会让 Feature / Buff / Entity 来源缺少稳定 typed id。 |
| 业务代码 `Data.Get<T>(string)` / `Data.Add(string)` | 真实业务误用 | 必须优先改 | 会绕过 generated `DataKey<T>`，重新打开 string/untyped 热路径。 |

### 自身开发者角度：哪些真的值得继续改

作为框架开发者，我会按“契约风险优先，性能证据其次，形式洁癖最后”的顺序判断。

必须改：

1. **业务层 string/untyped Data 调用**  
   例如 Ability Cost 按 `resourceKey string` 读写资源。这个问题不只是装箱，而是绕过 DataOS descriptor 和 generated `DataKey<T>`。AI 以后看到这种代码会继续复制裸字符串。它应该改成 typed resource key resolver，未定义的 `CurrentEnergy` / `CurrentAmmo` 要么补 descriptor，要么明确不支持并 fail-fast。

2. **Data changed 业务事件 typed 化**  
   `PropertyChanged(object?)` 可以保留为 debug snapshot，但不能当业务订阅主协议。UI、Damage、Recovery、Feature 这类业务应监听 typed/domain event，例如 `HealthChanged(float oldHp, float newHp)` 或后续 `DataChanged<T>`。

3. **computed resolver typed 化**  
   computed 是 Data 核心，不是调试边界。当前 computed cache 已进入 typed slot，但 resolver 返回 `object?` 仍会把类型错误延后。长期应改 `IDataComputeResolver<T>`，并在 registry/catalog build 阶段验证输出类型。

4. **默认值 typed cache / typed runtime field**  
   当前 `DataDefinition.DefaultValue object?` 作为 DTO 输入可以接受，但 slot 未写入时不应长期每次从 object default 转换。先做 slot 级 typed default cache，再考虑 `DataFieldDefinition<T>`。

不急改：

1. **snapshot / authoring DTO 的 object**  
   外部输入天然是动态数据。只要进入 runtime 时 fail-fast 并转换为 typed slot，就没有必要强行把 DTO 也做成泛型对象树。

2. **diagnostic dump 的 object 或 string 化**  
   TestSystem、debug UI、日志展示需要跨类型查看。关键是命名和文档必须写清楚它不是 gameplay API。

3. **ObjectRef runtime Node 的边界 object**  
   `TargetNode` 这种 runtime-only Godot Node 引用确实需要边界特殊处理。更重要的问题不是 object 本身，而是它是否有清晰的 `DataKey<Godot.Node2D>`、write policy 和 owner 生命周期约束。

### 不建议追求“全仓零 object”

`object` 是 C# 和 Godot bridge 中不可完全避免的边界工具。真正危险的是把它作为框架主协议。

如果追求“全仓零 object”，会出现三个反效果：

- loader / snapshot / debug 代码被迫引入复杂泛型包装，成本高于收益。
- Event / Feature / Data 的公共 API 会为了纯类型化制造过多泛型参数，AI 更难使用。
- 注意力会从真实业务宽口转移到无害 DTO 边界。

推荐目标应写成：

```text
业务热路径和 AI 可调用协议零 object；
loader / snapshot / diagnostic 边界允许 object；
允许 object 的 API 必须命名、注释和 grep gate 证明它不是业务主入口。
```

## Goal

本轮概念复盘要解决三个问题：

1. SlimeAI 这种 Data/Event 做底层、Component/System 做功能的框架是否成立。
2. Data runtime 去 object 的方案是否足以支撑这个框架。
3. 与传统 ECS 和成熟引擎框架相比，SlimeAI 应该承认哪些性能差距，并用什么架构边界补救。

非目标：

- 不写具体 C# 实现步骤。
- 不替代 SDD-0031 的任务拆分。
- 不讨论 Event、Feature、Ability 的具体代码迁移，只说明它们和 Data 的概念关系。

## Context Read

已读事实源：

- `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- `Src/ECS/Runtime/Data/DataRuntimeStorage.cs`
- `Src/ECS/Runtime/Data/Data.cs`
- `Src/ECS/Runtime/Data/DataComputeRegistry.cs`
- `Src/ECS/Runtime/Data/Events/GameEventType_Data.cs`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/1.拆箱装箱+GC优化/设计/00-总览与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/design/main.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/` 下 Bevy、Flecs、EnTT、Unity Entities 相关报告
- Context7 返回的 Bevy / Unity Entities 官方文档片段

未读或未完全验证：

- 没有重新跑完整 profiler / benchmark。本文件的性能判断是架构推断，最终仍需要 SDD-0031 或后续性能基线验证。
- 没有全量审计所有 Capability 对 `Data.Get<T>(string)` / `SetUntyped` 的调用。本文件只给设计裁决。

Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。

## Problem Shape

### 1. 你的框架不是传统 ECS，但不能无视传统 ECS 的底层原则

传统 ECS 的核心通常是：

```text
Entity = id
Component = typed data
System = logic over typed component storage
```

SlimeAI 当前的核心更像：

```text
Entity = identity + Data + Events
Data = typed runtime state protocol
Event = typed message protocol
Component/System/Capability = 功能拼装、Godot bridge、业务服务
DataOS = authoring truth source
```

这不是错。它更适合 AI-first，因为 AI 更容易维护 DataOS descriptor、generated `DataKey<T>`、Capability 文档和验证入口，而不是直接维护一套完整 query/archetype/storage DSL。

但传统 ECS 有一个必须借鉴的原则：**热路径数据必须是 typed storage，不应是任意 object 容器**。Bevy 的 table/sparse set、Unity Entities 的 unmanaged `IComponentData`、Flecs 的 table/archetype、EnTT 的 sparse set/storage，本质都在做同一件事：运行时状态尽量落在明确类型和可预测存储结构上。

SlimeAI 可以不复制它们的 API，但不能继续用 `Dictionary<string, object>` 当运行时核心。

### 2. Data 是框架核心，不只是一个属性包

Data 当前承担四个角色：

- 字段定义消费端：消费 DataOS descriptor / runtime snapshot。
- Entity 运行时状态容器：HP、速度、攻击、目标引用、资源引用等。
- Feature/Ability 的修改目标：modifier 改输入，computed 算输出。
- AI 可观察/可编辑协议：generated `DataKey<T>` 给 AI 明确字段入口。

因此 Data 的质量直接决定整个框架质量：

- Data 是 `object`，Capability 就会把 `object` 当正常协议继续扩散。
- Data 是 string key 主链路，AI 就会继续手写裸字符串。
- Data 的 computed 不 typed，Feature/Ability 就会继续靠 runtime cast。
- Data changed event 是 object 宽口，UI/TestSystem/业务监听就会继续以“调试方便”为理由绕过 typed contract。

这就是为什么 Data 不是 P1 优化，而是 P0 架构问题。

### 3. 当前最大误解：把“解耦”误解成“动态 object”

SlimeAI 要的是功能解耦、数据驱动和 AI 可拼接，不是运行时所有值都动态化。

正确解耦应该是：

```text
DataOS descriptor 解耦字段定义
Generated DataKey<T> 解耦业务调用与裸字符串
DataSlot<T> 解耦存储类型与通用管理
IDataSlot 解耦 diagnostics/loader 与业务热路径
Capability service 解耦功能组合与底层 storage
```

错误解耦是：

```text
object? value
Dictionary<string, object>
Event payload object
FeatureContext.ActivationData object
```

后者看起来灵活，实际会让 AI 和人都失去契约边界。框架越大，这种“灵活”越会变成不可验证。

## 对 `01-Data运行时object去除设计.md` 的详细批判

### 正确点 1：废弃 `DataRuntimeValue` union 是对的

文档明确拒绝 `DataRuntimeValue` 多字段 union，这是正确裁决。

原因不是 union 不能优化装箱，而是它会引入新的概念债：

- 每个值都携带多个候选字段，结构冗余。
- 每新增一个 Data 类型，都要改 union、converter、diagnostics 和分发逻辑。
- 它会绕开现有 `DataKey<T>` / `Data.Get/Set<T>`，让 AI 继续写“通用值处理器”。
- 它仍然是动态值容器，只是从 CLR `object` 换成项目自定义 `object-like`。

推荐保持当前裁决：**不再把 union、多字典、object slot 作为同级候选；默认就是 `DataSlot<T> + IDataSlot`。**

### 正确点 2：`Dictionary<string, IDataSlot>` 可以保留为管理边界

很多高性能 ECS 不会这么做，但 SlimeAI 的目标不是复制 chunk ECS。`Dictionary<string, IDataSlot>` 作为 slot 管理边界可以接受，因为它换来：

- descriptor-first 的动态字段集合。
- AI 可通过 stable key 和 generated handle 找字段。
- loader/debug/TestSystem 可以统一管理不同类型 slot。
- 不需要一次性生成所有实体的固定布局代码。

但必须写清楚：这个字典只是 **跨类型管理边界**，不是业务值存储边界。业务值不能再通过 `object? Value` 存进去。

### 正确点 3：typed policy / typed computed / typed event 必须纳入终局

文档已经把 `DataValuePolicy<T>`、`IDataComputeResolver<T>`、`DataChanged<T>` 写进目标形状，这是正确的。

但这里需要加一句更硬的裁决：

> 如果本轮 SDD-0031 因范围控制只做到部分 typed，剩余 object 边界必须标成后续债务，不能在 DocsAI 中写成最终完成态。

否则风险是：代码只把 `DataSlot.Value object?` 换成 `DataSlot<T>`，但 `TrySet<T>` 仍先装箱进 `TryApplyWritePoliciesWithReport(object?)`，computed 仍返回 `object?`，事件仍发 `object?`，最后只是“存储字段表面泛型化”。

### 问题 1：方案没有把“概念定位”讲透

`01-Data运行时object去除设计.md` 主要是实现设计，但它还不够回答“SlimeAI 框架是不是可行”。

应该补充的概念定位：

- Data 不是传统 ECS Component 的完全替代品。
- Data 是 SlimeAI 的 runtime state protocol。
- Component/System/Capability 不应私藏共享业务状态；共享状态进 Data。
- 但高频算法内部可以有 Capability-owned index/cache，不能强迫所有计算都每帧查 Data 字典。

推荐写入规则：

```text
Entity.Data 是跨 Capability 共享状态和 AI 可编辑状态的事实源。
Capability 内部为了性能维护的缓存、索引、临时数组不是 Data 的替代事实源；
它们必须有明确 invalidation、Observation dump 或验证入口。
```

### 问题 2：typed hot path 的边界还不够硬

文档说 `SetUntyped` 只留 loader/debug/TestSystem，但需要更明确：

- `Data.Set<T>(DataKey<T>, T)` 不应调用 `SetUntyped`。
- `Data.Get<T>(DataKey<T>)` 不应调用 `ConvertForRead(object?)`。
- typed 写策略不应返回 `object? finalValue`，否则值类型仍可能装箱。
- `Data.Get<T>(string)` / `Data.Set<T>(string, T)` 不应作为业务 API。

推荐裁决：

| API | 推荐裁决 |
| --- | --- |
| `Data.Get<T>(DataKey<T>)` | public 主入口 |
| `Data.Set<T>(DataKey<T>, T)` | public 主入口 |
| `Data.TrySet<T>(DataKey<T>, T, out report)` | public 或 internal，视调用需求 |
| `Data.Get<T>(string)` | internal test/helper only，后续 obsolete |
| `Data.Set<T>(string, T)` | internal migration only，后续删除 |
| `Data.SetUntyped` | loader/debug/TestSystem boundary，禁止业务新增 |
| `Data.GetAll()` | 改名为 diagnostic snapshot，不作为业务查询 |

### 问题 3：DataDefinition 仍是 `object? DefaultValue`，需要分层说明

`DataDefinition.DefaultValue object?` 作为 DTO / descriptor 输入可以保留，因为 snapshot、JSON、SQLite 输入本来是边界数据。

但 runtime catalog 最好有 typed projection：

```text
RuntimeDataDescriptorDto / DataDefinition
  -> DataFieldDefinition<T>
     -> DataValuePolicy<T>
     -> DataSlot<T>
```

如果暂时不实现 `DataFieldDefinition<T>`，也要在文档里写清楚：

- `DataDefinition.DefaultValue object?` 属于 descriptor/input 边界。
- slot 创建后应把 default 投影成 `T`。
- 热路径读取 default 不应每次都从 `object?` 转换。

推荐优先级：

- P0：`DataSlot<T>` 内部缓存 typed default 或第一次读取后缓存 typed base。
- P1：Catalog build 阶段生成 typed runtime field。
- P2：generated field id / array-backed storage。

### 问题 4：computed resolver 分成“本轮可接受”和“终局必须改”

`IDataComputeResolver.Compute(): object?` 目前可以作为 SDD-0031 的兼容边界，但不能算最终设计。

本轮可接受：

```text
resolver.Compute() 返回 object?
DataRuntimeStorage 立即按 field T 转换
computed cache 存在 DataSlot<T>
```

终局推荐：

```text
IDataComputeResolver<T>.Compute(...) -> T
DataComputeRegistry 按 computeId + output T 校验
Catalog build 时发现 resolver 输出类型不匹配就 fail fast
```

原因：

- computed 是 Data 的核心能力，不是 debug 边界。
- computed 读写频率可能很高。
- resolver 返回 `object?` 会继续让错误推迟到运行时。

推荐将 typed compute 拆成 SDD-0031 内的后续任务或独立 SDD，不要永远停在 object resolver。

### 问题 5：Data changed event 不能长期保持 object 主协议

`PropertyChanged(string, object?, object?)` 可以暂留给 diagnostics，但不应继续作为业务主事件。

推荐分层：

```text
DataChanged<T>(DataKey<T> key, T oldValue, T newValue)
  -> Runtime / Capability 内部 typed 监听

HealthChanged(float oldHp, float newHp)
  -> 领域事件，给 UI / Damage / Recovery 等业务使用

DataDiagnosticChange(stableKey, valueType, oldText, newText)
  -> TestSystem / debug dump 使用
```

这样可以避免 UI 或业务系统继续订阅 object payload 后自己 cast。

本轮如果不改事件，文档必须标注：

```text
PropertyChanged(object?) 是 Event/diagnostic 边界，不是 Data runtime typed 化完成证明。
```

### 问题 6：性能差距要承认，不要用 typed slot 过度承诺

完成 `DataSlot<T>` 后，SlimeAI 仍不会等同传统高性能 ECS。

仍存在的成本：

- 字符串 stable key 字典查找。
- `IDataSlot` 接口分发。
- descriptor policy / modifier / computed 的分支。
- Godot C# runtime 和 Node bridge 的成本。

这不是否定方案，而是需要明确策略：

- 通用 AI-first 状态走 Data。
- 高频批量算法走 Capability-owned cache/index。
- 超热固定字段未来可用 generated field id / array-backed storage 优化。

推荐写入框架原则：

```text
Data 是共享状态事实源，不是所有算法的唯一内存布局。
Capability 可以为 Movement、Collision、TargetSelector、AI 感知维护派生索引；
派生索引必须可重建、可失效、可诊断，不能成为第二事实源。
```

### 问题 7：缺少“何时不放 Data”的规则

当前设计强调共享状态进 Data，但还需要反向规则，避免 Data 变成万能容器。

不应放 Data：

- 每帧临时计算值，例如 steering 临时向量、碰撞 broadphase 临时候选。
- Godot Node / Resource 实例，除非是明确 `runtime_only object_ref` 且有 owner 边界。
- 大批量只读曲线、配置数组、空间索引。
- Capability 内部缓存，例如 target query result buffer。
- Event payload 历史记录。

应放 Data：

- 跨 Capability 共享的 entity runtime 状态。
- AI/配置需要显式修改的数值、枚举、引用 id。
- Feature/Ability modifier 的输入字段。
- computed 字段输出，前提是 resolver 纯计算且依赖清晰。

## Options

### 方案 A：继续 `DataSlot<T> + IDataSlot`，并补足边界裁决

这是推荐方案。

优点：

- 延续当前 DataOS descriptor 和 generated `DataKey<T>` 成果。
- 不引入传统 ECS query/storage DSL。
- 兼顾 AI-first 可维护性和运行时 typed hot path。
- 可以分阶段完成，不需要一次性推翻框架。

代价：

- 性能不如 chunk/table ECS。
- 需要长期维护 typed boundary，防止 `object` 重新扩散。
- 需要 benchmark / grep gate / DocsAI 共同约束。

### 方案 B：改成传统 ECS：Component 存数据，System 只处理逻辑

不推荐作为 SlimeAI 主线。

优点：

- 性能模型更接近成熟 ECS。
- 查询和内存布局更清晰。

问题：

- 会推翻当前 DataOS / DataKey / Capability 的 AI-first 主线。
- AI 会面对更复杂的 component storage、query、archetype、system order DSL。
- Godot C# bridge 和游戏功能拼装会大幅重写。
- 不符合“拼功能 + 改数据 = 新游戏”的主要目标。

可采纳的是原则，不是 API：typed data、明确存储、边界动态、专用索引。

### 方案 C：保留 object，提高 converter/cache 性能

不推荐。

优点：

- 改动最小。
- loader/debug 方便。

问题：

- 不能解决 Data 作为框架核心协议的契约问题。
- 装箱拆箱、runtime cast、错误延迟仍存在。
- AI 仍会看到 object 宽口并继续误用。

## Recommendation

采用方案 A，并把 `01-Data运行时object去除设计.md` 的推荐补成以下明确路线：

1. **P0：Data runtime typed hot path**
   - `Data.Get/Set<T>(DataKey<T>)` 不经过 untyped boundary。
   - `DataSlot<T>` 保存 typed value 和 typed default。
   - modifier pipeline 对 `int/float/double` 返回 typed T。

2. **P0：untyped API 边界化**
   - `SetUntyped` 只给 loader/debug/TestSystem。
   - `GetAll` 改 diagnostic snapshot 语义。
   - 新业务调用点禁止 string/untyped API。

3. **P1：computed typed 化**
   - 短期允许 resolver object 边界，但 cache 必须 typed。
   - 后续改 `IDataComputeResolver<T>`，catalog build fail fast。

4. **P1：Data changed 分层**
   - 业务高频监听走 typed/domain event。
   - object `PropertyChanged` 只留 diagnostics。

5. **P1：field id / typed runtime field**
   - generated handle 除 stable key 外提供 field id 或 runtime descriptor id。
   - 减少 string dictionary lookup，给 P2 source-generated storage 留入口。

6. **P2：高频 Capability 派生索引**
   - Movement、Collision、TargetSelector、AI 感知可维护专用缓存。
   - 缓存不是事实源，必须有 invalidation 和 debug dump。

## Must Confirm

下面问题比上一轮要更具体。若进入实现或修改 `01-Data运行时object去除设计.md`，建议按这些问题确认：

1. 是否同意把 `Data.Get<T>(string)` / `Data.Set<T>(string, T)` 定义为 migration/test/internal 入口，禁止业务新增调用？
   - 推荐：同意。否则 AI 会继续绕过 generated `DataKey<T>`。

2. 是否同意 `SetUntyped` 只保留给 loader/debug/TestSystem，且命名或注释必须体现 boundary？
   - 推荐：同意。可以保留 API，但不允许它是普通 runtime 写入口。

3. 是否同意 `PropertyChanged(object?)` 不作为终局业务事件，只作为 diagnostics 兼容边界？
   - 推荐：同意。业务事件应使用 `DataChanged<T>` 或领域事件。

4. 是否同意 computed resolver 最终改为 typed resolver，但允许 SDD-0031 先只把 computed cache typed 化？
   - 推荐：同意。这样能控制范围，又不把 object resolver 写成最终形态。

5. 是否同意 Data 是共享状态事实源，但不是所有高频算法的唯一内存布局？
   - 推荐：同意。否则 Data 会被迫承担 spatial index、target cache、physics candidate buffer 等不该承担的职责。

## Should Confirm

1. generated `DataKey<T>` 是否在下一阶段增加 field id / descriptor id？
   - 默认建议：先设计，不急着本轮实现。

2. 是否为 Data hot path 建 microbenchmark？
   - 默认建议：需要。否则只能证明行为正确，不能证明 GC/装箱改善。

3. `DataModifier.Source object?` 是否改成 typed source id？
   - 默认建议：后续改。它不是 Data value slot，但仍是 object 宽口。

## Defaults I Will Use

如果用户不再补充，后续相关文档和 SDD 默认采用：

- `DataSlot<T> + IDataSlot` 是 Data runtime 去 object 的唯一主方案。
- 不恢复 `DataRuntimeValue` union。
- `SetUntyped`、`GetAll`、`PropertyChanged(object?)` 只作为边界/债务，不作为终局业务协议。
- SlimeAI 不改成传统纯 ECS，但采纳成熟 ECS 的 typed hot path 原则。
- 高频 Capability 可以维护派生索引，但 Data 仍是共享状态事实源。

## Not Recommended

- 不建议把 SlimeAI 改成 Bevy/Unity Entities/Flecs/EnTT 形态的完整 ECS。
- 不建议把 Data 做成万能 JSON/object blackboard。
- 不建议为了 debug 方便长期保留 public object API。
- 不建议用“现在能跑”作为 Data 设计完成标准；Data 是框架核心，必须用 typed contract 和验证门禁收口。

## Artifact Updates

本文件是概念层补充，应作为 `ECS框架优化/0.ECS框架的思考` 的长期入口之一。

建议后续同步：

- 在 `../1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md` 中补充本文的 Must Confirm 和 Recommendation。
- 在 SDD-0031 结束前确认 DocsAI 没有把 object 边界写成最终完成态。
- 后续 Event / Feature / Ability typed 化 SDD 应引用本文的“Data 是 typed runtime state protocol，不是 object blackboard”裁决。
