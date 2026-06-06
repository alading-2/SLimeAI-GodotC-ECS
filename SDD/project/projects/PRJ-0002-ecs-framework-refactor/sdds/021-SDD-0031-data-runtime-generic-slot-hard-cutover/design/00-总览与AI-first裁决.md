# 总览与 AI-first 裁决

## 一句话结论

SlimeAI 的 GC 优化不应从“全仓找字符串和 LINQ”开始，而应从 Data / Event / Feature 这些 AI-first ECS 基础协议去 object 化开始。

`object?` 最初很可能是为了兼任和开发方便：Data 可以装任何值，Event 可以动态发任何 payload，Feature 可以把任意子系统上下文塞进 `ActivationData`。这在小框架阶段能快速跑通，但和当前 `DocsAI/ECS框架与AIFirst方向决策.md` 的方向冲突：AI-first ECS 要事实源少、契约明确、类型可查、验证可复盘，而不是靠运行时转型和人工记忆。

## 证据摘要

| 区域 | 当前证据 | 裁决 |
| --- | --- | --- |
| Data runtime | `DataSlot.Value object?`、`GetEffectiveValue(): object?`、`SetUntyped(... object?)`、`DataChangeRecord object?`、computed cache `Dictionary<string, object?>` | P0 hard cutover，已确认最终架构为 `DataSlot<T> + IDataSlot` / typed field / typed policy / typed resolver |
| Event runtime | `Emit<T>(in T)` 泛型路径已存在；`EmitDynamic(object)` 仍反射调用；`OnDynamic(Type, Action<object>)` 与 `Action<object>` 分支仍存在 | P0 禁 dynamic object，泛型事件保留 |
| Feature / Ability | `FeatureContext.ActivationData/ExecuteResult object?`、`IFeatureHandler.OnExecute(): object?`、Ability 用 `CastContext` 转型桥接 | P0/P1，同步类型化，避免绕过 Event 禁 object |
| ObjectPool | `ObjectPoolManager._pools Dictionary<string, object>`，归还、统计、清理通过反射调用 | P1，改非泛型 pool interface |
| TargetSelector / AbilityInventory / ComponentRegistrar | 存在 `new List`、`GetRange`、`new Random()`、`Where().ToList()`、`ToArray()` | P1，随 TargetQueryEngine / Component owner 优化 |
| Logger / 字符串 | `Log.Debug(object)` / `Log.Info(object)` 调用前 `$"..."` 会先求值；Warn/Error 内部已有级别判断 | P1，不把字符串插值等同字符串拼接；改日志 API |

## 为什么 Data/Event 最高优先级

Data 和 Event 是所有 Capability 的底层。Ability、Damage、Movement、AI、UI、TestSystem 都会读 Data 或发事件。这里的 `object?` 不只是“装箱问题”，还是契约问题：

- AI 看 `object?` 不知道应该传 `float`、`EntityId`、`ResourceRef` 还是 `AbilityExecutedResult`。
- 运行时靠 `Convert.ChangeType`、`Activator.CreateInstance`、反射或 `as` 转型补契约，会把错误推迟到运行中。
- 值类型写进 object 是装箱；Data 每帧读写频繁，风险比低频工具统计更大。

外部资料只用来校准底层事实和成熟框架方向：.NET boxing 会把值类型包装成托管堆对象；提高分配率会提高 GC 压力；Unity Entities / Bevy 这类 ECS 都把热路径状态放在 typed component / typed storage 上，managed/dynamic 能力处在受约束边界；interpolated string handler 是官方提供的性能场景优化机制。SlimeAI 的具体裁决仍以本仓 DocsAI/SDD/源码事实为准。

## 对用户观点的裁决

### 字符串插值不是 P0

用户判断“字符串差值不是问题，字符串拼接才是问题”需要细分：

- 插值不是原罪，它比手写 `+` 更可读，现代 C# 对插值也有 handler 机制。
- 但当前 `Log.Debug(object message)` 这类 API 接收的是已构造参数；调用点写 `$"..."` 时，日志方法被调用前就会构造字符串。
- 所以问题不是“插值语法”，而是“日志 API 无法延迟求值”。

裁决：不禁止 `$"..."`；只禁止热路径把高成本日志消息直接传给无法延迟的日志 API。后续 Logger 设计用 `IsEnabled`、`Func<string>` 或 interpolated string handler 收口。

### Data object 必须改

Data 当前已经完成 descriptor-first / snapshot-first / generated typed handle，但 runtime storage 仍把 typed handle 值转到 `object?`。这说明之前只是完成了事实源和调用侧 typed 化，没有彻底完成运行时值存储 typed 化。

裁决：这次应彻底去掉 AI 框架主链路 object，不再为旧便利写法保留 public object API。需要 loader/debug 的未类型化输入时，必须是边界 API，不能被业务热路径调用。

补充裁决：用户已确认 `DataSlot<T> + IDataSlot` 是 Data 去 object 的最终方案。不采用 `DataRuntimeValue` 多字段 union 作为默认方案。SlimeAI 已有 `DataKey<T>` 和 `Data.Get/Set<T>`，运行时应沿用泛型信息进入 `DataSlot<T>`、typed policy 和 `IDataComputeResolver<T>`；通用 value union 会制造冗余字段和新一层动态分发，违背本次 hard cutover 的目标。

### Event object 必须禁止

Event 当前泛型 payload 模型是对的。`EmitDynamic`、`OnDynamic` 和 `Action<object>` 是遗留的宽口能力。

裁决：Event 不需要兼任人工 object 入口。Feature 如果需要数据驱动发事件，应有 typed action registry 或 code-generated action，不允许把 object payload 塞进 EventBus。

## 推荐 SDD 拆分

| 顺序 | SDD 建议 | 目标 |
| --- | --- | --- |
| 1 | Data Runtime Generic Slot Hard Cutover | 按已确认的 `DataSlot<T> + IDataSlot` 最终架构替换 `DataSlot object?`、typed policy、typed computed、typed changed event；保留 loader/debug 边界但不作为业务入口 |
| 2 | Event Dynamic Object Removal | 删除/禁用 `EmitDynamic`、`OnDynamic`、`Action<object>`；Feature event action 改 typed registry |
| 3 | Feature Ability Typed Execution Context | `FeatureContext` / `IFeatureHandler` 类型化，Ability 的 `CastContext` / `AbilityExecutedResult` 不再经 object 返回 |
| 4 | ObjectPool Manager Untyped Interface | `_pools` 改非泛型接口，去掉管理器反射调用 |
| 5 | TargetQuery Allocation Hardening | 结合 `TargetQueryEngine` 设计处理 List、Random、LINQ 和 diagnostics |
| 6 | Logger Lazy Message API | 日志级别门禁前不构造消息，文档明确插值使用边界 |

## 其他方案复核

用户指出 Data `DataRuntimeValue` 方案冗余后，本设计包按同一标准复核其它方案：

| 方案 | 复核结论 | 是否要重构 |
| --- | --- | --- |
| Data `DataRuntimeValue` 多字段 union | 不推荐。它绕开已有 `DataKey<T>` / `Data.Get/Set<T>`，把宽口从 `object` 换成自定义 union，且每加一种 Data 类型都要膨胀字段和分发逻辑。 | 是，改为 `DataSlot<T>` |
| Data 多个 `Dictionary<string, float/int/bool>` | 不推荐作为默认方案。它只优化存储局部，不自然覆盖 computed、modifier、changed event、string array、ResourceRef、EntityId 等 typed policy。 | 是，除非未来 profiler 指向 source-generated typed storage |
| Event dynamic object | 不推荐。Event 是协议，不需要人工兼任入口；缓存反射也不能解决契约漂移和装箱。 | 是，删除或禁用 |
| Feature `ActivationData/ExecuteResult object?` | 不推荐。它会在 Event 去 object 后成为新的绕路。 | 是，改 typed execution context |
| ObjectPool `ReleaseUntyped(object)` | 可以保留为低频引用类型管理边界。ObjectPool 管的是 class/Node，不是 Data 值类型热路径；但必须用 `IObjectPoolRuntime` 删除反射和字符串方法名。 | 局部重构，不套用 Data 规则 |
| TargetQueryResult / buffer lease | 不是同类问题。它解决集合 ownership 和分配，不引入通用 value union。 | 保留方向，执行时以 allocation artifact 验证 |
| Logger lazy / handler | 不是同类问题。它解决日志关闭时消息构造，不改变 Data/Event 契约。 | 保留方向，P1 |

## 验证原则

执行阶段至少需要：

- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`
- Data 纯 C# microbenchmark 或 Godot Data scene artifact，覆盖 `Get/Set/Modifier/Computed/Changed` 分配变化。
- Event grep gate：`EmitDynamic|OnDynamic|Action<object>` 不再出现在框架 Runtime Event 主链路。
- Data grep gate：业务代码不调用 `SetUntyped(string, object?)` / `GetAll(): Dictionary<string, object>` 这类 object API。

## Must Confirm

- 是否允许 Data public object API 删除或降为 internal / obsolete debug-only。
- 是否允许 Event 彻底删除 dynamic object 入口，而不是只做反射缓存优化。

## Confirmed Decisions

- Data 去 object 最终采用 `DataSlot<T> + IDataSlot`，不再比较 `DataRuntimeValue` union 或多字典拆分作为同级方案。
- `IDataSlot` 是跨类型 slot 管理和 diagnostics 边界，不保存业务值，不暴露 `object? Value`。
- 后续第一个执行型 SDD 名称使用 `Data Runtime Generic Slot Hard Cutover`。
