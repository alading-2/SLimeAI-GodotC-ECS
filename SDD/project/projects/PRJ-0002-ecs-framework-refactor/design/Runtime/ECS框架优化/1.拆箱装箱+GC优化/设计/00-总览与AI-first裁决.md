# 总览与 AI-first 裁决

## 一句话结论

SlimeAI 的 GC 优化不应从“全仓找字符串和 LINQ”开始，也不应在 Data 已完成后继续围着 Data 打转。`SDD-0031 Data Runtime Generic Slot Hard Cutover` 已把 Data 主链路切到 `DataSlot<T> + IDataSlot`；`SDD-0033 Non-Data GC Boundary Completion` 已把非 Data 明显宽口收口到 typed / owner 边界：Event + Feature / Ability typed execution、ObjectPool manager runtime interface、TargetQueryEngine / TargetQueryResult ownership facade。

`object?` 最初很可能是为了兼任和开发方便：Data 可以装任何值，Event 可以动态发任何 payload，Feature 可以把任意子系统上下文塞进 `ActivationData`。这在小框架阶段能快速跑通，但和当前 `DocsAI/ECS框架与AIFirst方向决策.md` 的方向冲突：AI-first ECS 要事实源少、契约明确、类型可查、验证可复盘，而不是靠运行时转型和人工记忆。

当前重新裁决与执行结果：

- Data 不再作为本目录当前待执行 P0；Data 残留边界留给 Data follow-up，不阻塞其他切片。
- Event dynamic object 和 Feature / Ability object bridge 是同一条协议链，已由 SDD-0033 同批处理；只删 Event 不改 Feature 会让 object 从 Feature 绕回去，因此不得恢复拆开执行。
- ObjectPool manager 反射已按 P1 小切片处理；对象池生命周期、parking strategy 和碰撞验证仍归 ObjectPool / Collision owner。
- TargetSelector 已引入 TargetQueryEngine / TargetQueryResult ownership 与 diagnostics；后续 pooled lease、deterministic RNG 和 allocation artifact 继续走 TargetSelector owner，不回到 list-only facade。
- Logger、ComponentRegistrar、Entity 生命周期 snapshot 分配是 P2 或 profiler 驱动项，不作为下一步默认任务。

## 证据摘要

| 区域 | 当前证据 | 裁决 |
| --- | --- | --- |
| Data runtime | `SDD-0031` 已完成 typed `DataKey<T>` 主链路、modifier effective value 和 computed cache 的 `DataSlot<T> + IDataSlot` 切换；`SetUntyped/GetAll/PropertyChanged(object?)` 仍是边界或后续 Event/Data follow-up | 已完成主链路 hard cutover；本轮不再重新分析 Data |
| Event runtime | `Emit<T>(in T)` 泛型路径已存在；旧 `EmitDynamic(object)` / `OnDynamic(Type, Action<object>)` / `Action<object>` 分支已由 SDD-0033 删除 | 已完成；保留 typed emit / subscribe |
| Feature / Ability | `FeatureContext` 已新增 typed activation payload / execution result helper；`IFeatureHandler.OnExecute` 已改 `void`，Ability 通过 typed `CastContext` / `AbilityExecutedResult` 桥接 | 已完成；只类型化 Execute 输入 / 输出，不把完整 lifecycle 过度泛型化 |
| TriggerComponent | 当前无稳定 dynamic event 主链路；旧 `SourceEventData object?` 相关主链路不作为本轮执行目标 | 后续若补 OnEvent binding，走 typed trigger binding id，不恢复事件类型字符串 + object |
| ObjectPool | `ObjectPoolManager` 已改 `Dictionary<string, IObjectPoolRuntime>`，归还、统计、清理不再通过反射调用 | 已完成 manager 边界；不重写生命周期 |
| TargetSelector / AbilityInventory | `TargetQueryEngine` / `TargetQueryResult` 已提供 read-only items 与 diagnostics；`AbilityInventoryService.GetManualAbilities` 仍按调用频率另行裁决 | 已完成基础 ownership facade；后续 allocation hardening 另起 owner SDD |
| ComponentRegistrar / Entity lifecycle | 存在 `Distinct().ToArray()`、`GetComponents().ToArray()`、destroy child snapshot 等 | P2；多数是防修改 snapshot 或生命周期阶段，不默认优化 |
| Logger / 字符串 | `Log.Debug(object)` / `Log.Info(object)` 调用前 `$"..."` 会先求值；`Trace/Debug` 有 `[Conditional("DEBUG")]`，Warn/Error 内部已有级别判断 | P2；只在热路径加 `IsEnabled` / lazy / handler，不全仓重写插值 |

## 为什么 SDD-0033 合并 Event + Feature / Ability

Data 已经完成主链路 hard cutover，因此 Data 后的最高风险不再是 Data，而是 Event 和 Feature / Ability 之间残留的 object 协议链。SDD-0033 必须合并处理这里的 `object?`，因为它不只是“装箱问题”，还是契约问题：

- AI 看 `object?` 不知道应该传哪个事件 payload、`CastContext`、`AbilityExecutedResult` 还是其他调用方上下文。
- 运行时靠 `Convert.ChangeType`、`Activator.CreateInstance`、反射或 `as` 转型补契约，会把错误推迟到运行中。
- EventBus 删除 dynamic object 后，如果 Feature / Ability 继续用 `ActivationData/ExecuteResult object?`，object 宽口只是换了入口，并没有消失。SDD-0033 已把 Ability 主链路切到 typed helper。
- TriggerComponent 的 OnEvent 目前仍是 TODO 空壳，说明 dynamic event 还不是稳定依赖，没必要为了它保留 EventBus object 能力。

外部资料只用来校准底层事实和成熟框架方向：.NET boxing 会把值类型包装成托管堆对象；提高分配率会提高 GC 压力；Unity Entities / Bevy 这类 ECS 都把热路径状态放在 typed component / typed storage 上，managed/dynamic 能力处在受约束边界；interpolated string handler 是官方提供的性能场景优化机制。SlimeAI 的具体裁决仍以本仓 DocsAI/SDD/源码事实为准。

## 对用户观点的裁决

### 字符串插值不是 P0

用户判断“字符串差值不是问题，字符串拼接才是问题”需要细分：

- 插值不是原罪，它比手写 `+` 更可读，现代 C# 对插值也有 handler 机制。
- 但当前 `Log.Debug(object message)` 这类 API 接收的是已构造参数；调用点写 `$"..."` 时，日志方法被调用前就会构造字符串。
- 所以问题不是“插值语法”，而是“日志 API 无法延迟求值”。

裁决：不禁止 `$"..."`；只禁止热路径把高成本日志消息直接传给无法延迟的日志 API。后续 Logger 设计用 `IsEnabled`、`Func<string>` 或 interpolated string handler 收口。

### Data object 已完成主链路 cutover

Data 曾经的问题判断成立，但当前状态已经改变：`SDD-0031` 已完成 Runtime Data 主链路从 `DataSlot.Value object?` 到 `DataSlot<T> + IDataSlot` 的 hard cutover。后续 Data follow-up 可以继续处理 typed DataChanged、diagnostic snapshot、string-key 边界，但这不应阻塞 Event / Feature / Ability 的重新设计。

裁决：本目录后续不再把 Data 作为当前待办 P0，也不再用 Data 估算数字驱动其他 owner 过度优化。

保留历史裁决：不采用 `DataRuntimeValue` 多字段 union，不采用多字典作为默认方案。

### Event object 必须禁止

Event 当前泛型 payload 模型是对的。`EmitDynamic`、`OnDynamic` 和 `Action<object>` 是遗留的宽口能力。

裁决：Event 不需要兼任人工 object 入口。Feature 如果需要数据驱动发事件，应有 typed action registry 或 code-generated action，不允许把 object payload 塞进 EventBus。

### Feature / Ability 不要过度泛型化

Feature 当前用 `ActivationData object?` 和 `ExecuteResult object?` 达成与 Ability 解耦。问题不在“Feature 不依赖 Ability”，而在“用 object 实现解耦”。

裁决：只把 Execute 阶段改成 typed 输入 / 输出，例如 typed executor / typed adapter；Granted、Removed、Activated、Ended 继续使用普通 lifecycle context，避免把整个 Feature 生命周期泛型化导致 API 扩散。

### TargetSelector 不能只换池化 List

TargetSelector 的 `new List`、`GetRange`、`new Random` 是真实热路径问题，但只把 List 换成对象池会引入 ownership 风险：调用方如果长期持有结果，池化 buffer 复用后会产生隐蔽 bug。

裁决：TargetSelector 应随 `TargetQueryEngine + TargetQueryResult/Lease` 一起做，先明确结果 ownership，再谈分配优化。

## 推荐 SDD 拆分

| 顺序 | SDD 建议 | 目标 |
| --- | --- | --- |
| 0 | Data Runtime Generic Slot Hard Cutover | 已由 `SDD-0031` 完成；不再作为当前待办 |
| 1 | Event + Feature/Ability Typed Execution Boundary | 已由 `SDD-0033` 完成：删除 `EmitDynamic`、`OnDynamic`、`Action<object>` 主链路；Feature event action 改 typed wrapper；Ability Feature Execute 改 typed helper |
| 2 | ObjectPool Manager Runtime Interface | 已由 `SDD-0033` 完成：`_pools` 改非泛型管理接口，去掉 manager 反射调用；不改对象池生命周期策略 |
| 3 | TargetQueryEngine Allocation Hardening | `SDD-0033` 已完成基础 `TargetQueryResult` ownership / diagnostics；pooled lease、Random、allocation artifact 后续另起 TargetSelector owner SDD |
| 4 | AbilityInventory / ComponentRegistrar Local Cleanup | 只在调用频率或 profiler 证明后处理 `Where().ToList()`、`ToArray()` 等局部问题 |
| 5 | Logger Hot Path Lazy Message API | 仅热路径日志加级别门禁、lazy 或 interpolated string handler，不做全仓字符串风格重写 |

## 其他方案复核

用户指出 Data `DataRuntimeValue` 方案冗余后，本设计包按同一标准复核其它方案：

| 方案 | 复核结论 | 是否要重构 |
| --- | --- | --- |
| Data `DataRuntimeValue` 多字段 union | 不推荐。它绕开已有 `DataKey<T>` / `Data.Get/Set<T>`，把宽口从 `object` 换成自定义 union，且每加一种 Data 类型都要膨胀字段和分发逻辑。 | 是，改为 `DataSlot<T>` |
| Data 多个 `Dictionary<string, float/int/bool>` | 不推荐作为默认方案。它只优化存储局部，不自然覆盖 computed、modifier、changed event、string array、ResourceRef、EntityId 等 typed policy。 | 是，除非未来 profiler 指向 source-generated typed storage |
| Event dynamic object | 不推荐。Event 是协议，不需要人工兼任入口；缓存反射也不能解决契约漂移和装箱。 | 是，删除或禁用 |
| Feature `ActivationData/ExecuteResult object?` | 不推荐。它会在 Event 去 object 后成为新的绕路。 | 是，只改 Execute typed adapter，不把所有 lifecycle 泛型化 |
| Trigger event type string + object source | 不推荐。当前 OnEvent 还是 TODO，说明 dynamic event 不是必须保留的稳定主链路。 | 是，改 typed trigger binding id |
| ObjectPool `ReleaseUntyped(object)` | 可以保留为低频引用类型管理边界。ObjectPool 管的是 class/Node，不是 Data 值类型热路径；但必须用 `IObjectPoolRuntime` 删除反射和字符串方法名。 | 局部重构，不套用 Data 规则 |
| TargetQueryResult / buffer lease | 不是同类问题。它解决集合 ownership 和分配，不引入通用 value union。 | 保留方向，执行时以 allocation artifact 验证 |
| Logger lazy / handler | 不是同类问题。它解决日志关闭时消息构造，不改变 Data/Event 契约。 | 保留方向，P1 |
| ComponentRegistrar / lifecycle `ToArray()` | 多数是 snapshot 防修改或生命周期阶段，不是默认热路径。 | 默认不改，profiler 或具体 owner SDD 证明后再改 |

## 验证原则

执行阶段至少需要：

- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`
- Event + Feature grep gate：`EmitDynamic|OnDynamic|Action<object>|ActivationData|ExecuteResult|object? OnExecute|SourceEventData` 不再出现在框架主执行链路。
- Trigger binding 行为测试：OnEvent trigger 能订阅 / 取消 typed event，并生成 typed `CastContext`。
- ObjectPool grep gate：`GetMethod("Release"|"GetStats"|"Cleanup"|"Clear")` 不再出现在 manager 主链路。
- TargetQuery allocation artifact：多次查询不随调用次数线性增长 managed allocations，并证明 result ownership 不泄漏。

## Must Confirm

- 已确认并执行：Event + Feature / Ability 合成一个 SDD，而不是先单独删 Event 后再修 Feature 绕路。
- 已确认并执行：Feature 只类型化 Execute 输入 / 输出，lifecycle context 暂不泛型化。
- 已确认默认方向：TriggerComponent 后续不使用事件类型字符串 + object source 作为主链路；如补 OnEvent binding，应切到 typed trigger binding id。

## Confirmed Decisions

- Data 去 object 最终采用 `DataSlot<T> + IDataSlot`，不再比较 `DataRuntimeValue` union 或多字典拆分作为同级方案。
- `IDataSlot` 是跨类型 slot 管理和 diagnostics 边界，不保存业务值，不暴露 `object? Value`。
- `SDD-0031 Data Runtime Generic Slot Hard Cutover` 已完成；`SDD-0033 Non-Data GC Boundary Completion` 也已完成。
- 后续不再从本设计包重复创建 Event dynamic / Feature Execute / ObjectPool manager / TargetQueryResult 基础切片；只在 profiler、allocation artifact 或 owner 设计证明后继续 Logger、TargetQuery pooled lease、AbilityInventory / ComponentRegistrar 局部 cleanup。
