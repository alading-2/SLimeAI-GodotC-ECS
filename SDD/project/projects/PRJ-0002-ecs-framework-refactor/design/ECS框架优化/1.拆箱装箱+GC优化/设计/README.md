# GC 优化设计入口

> 状态：current
> 更新：2026-06-06
> 输入：用户关于 Data/Event object、字符串插值、AI-first 性能严谨性的裁决要求；用户对 `DataRuntimeValue` 多字段方案的驳回；用户确认 `DataSlot<T> + IDataSlot` 是 Data 去 object 最佳方案；`DocsAI/ECS框架与AIFirst方向决策.md`；Data/Event/Feature/ObjectPool/TargetSelector/Logger DocsAI 与源码审计；Microsoft Learn .NET boxing / GC / interpolated string handler 文档；Resources/Engine 本地框架分析和 Unity Entities / Bevy ECS 官方资料。

## DeepThink 确认包

### Goal

解决 SlimeAI ECS 框架中会破坏 AI-first 严谨性和运行时性能的装箱拆箱 / GC 问题，先形成可执行设计裁决，再由后续 SDD 实施。

本轮非目标：不直接改源码；不做泛泛“全仓零 GC”口号；不为旧手写方便保留 Data/Event object 主链路；不把所有字符串插值都误判为 P0。

### Context Read

- Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- 已读方向源：`DocsAI/README.md`、`DocsAI/ECS框架与AIFirst方向决策.md`、`DocsAI/ECS/README.md`。
- 已读 owner 文档：`DocsAI/ECS/Runtime/Data/Data系统说明.md`、`DocsAI/ECS/Runtime/Event/Event系统说明.md`、`DocsAI/ECS/Runtime/README.md`、`DocsAI/ECS/Tools/ObjectPool/README.md`。
- 已读 SDD 源：`PRJ-0002` README、`design/INDEX.md`、`roadmap.md`、`progress.md`、现有 `问题/*.md`。
- 已读源码：`DataRuntimeStorage.cs`、`Data.cs`、`GameEventType_Data.cs`、`DataModifier.cs`、`DataComputeRegistry.cs`、`EventBus.cs`、`EventContext.cs`、`FeatureContext.cs`、`IFeatureHandler.cs`、`FeatureSystem.cs`、`EmitEventAction.cs`、`AbilityFeatureHandler.cs`、`AbilitySystem.cs`、`CastContext.cs`、`TriggerComponent.cs`、`ObjectPoolManager.cs`、`EntityTargetSelector.cs`、`AbilityInventoryService.cs`、`ComponentRegistrar.cs`、`Log.cs`。
- 本地引擎资料：`Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`、`05-DefaultEcs-源码分析报告.md`、`09-Unreal-GAS-文档对照报告.md`。结论支持 typed data / typed event / Capability-owned selector，不支持把外部 ECS 运行时照搬进 SlimeAI。
- 外部资料：Microsoft Learn 确认 boxing 会把值类型包装到托管堆对象；GC 频率受分配率影响；interpolated string handler 是性能场景下避免无谓字符串构造的官方机制。Unity Entities / Bevy ECS 资料用于校准成熟 ECS 的热路径状态主链路是 typed component / typed storage，动态能力只留在受约束边界。
- 未读上下文：未做运行时 profiler；未跑 Godot 场景；未审完所有 gameplay / UI 调用点。

### Problem Shape

旧 Data/Event/Feature 设计把 `object?` 当成“方便兼任”的通用桥。这个选择在人工框架中能降低 API 设计成本，但在 AI-first 框架中会制造三个问题：

1. AI 无法从类型系统知道字段、事件和上下文 payload 的真实契约。
2. 值类型进入 `object` 会产生装箱，Data/Event/Feature 又处于高频基础层。
3. 兼任入口会绕过 DataOS descriptor、generated handle 和 typed event 的事实源治理。

现有 `问题/` 文档中“每帧 2000+ 次 / 32KB”这类数字只能作为风险估算，不能作为事实；但 Data/Event object 热路径本身已被源码证实。

### Main Risks

- Data 改动面大：`DataValueConverter`、computed resolver、modifier、Data changed event、snapshot apply、测试场景都会被影响。
- Event 禁 object 后，Feature 的 `EmitEventAction`、`TriggerComponent` 事件触发、Ability/Feature 上下文需要同步重构，否则会产生绕路。
- 直接替换为 `DataSlot<T>` 可能引入大量泛型容器和 API 扩散；需要用 descriptor `DataValueType` 做稳定分发。
- 日志字符串问题容易被扩大成风格战争；实际应该以日志门禁和 handler 解决热路径求值。
- 未 profile 前不能承诺具体 GC 数字；必须把 benchmark / scene artifact 作为执行 SDD 的验收项。

### Options

1. 小修：只缓存 `EmitDynamic` 反射、减少 LINQ、给日志加 `IsEnabled`。
   - 优点：快。
   - 缺点：Data object 主问题仍在，AI-first 契约仍宽。

2. P0 hard cutover：Data runtime generic slot + Event 禁 dynamic object + Feature/Ability typed context。
   - 优点：解决最底层、最高频、最影响 AI 契约的问题。
   - 缺点：实施面大，需要 SDD 和强验证。

3. 全仓一次性零分配重构。
   - 优点：目标彻底。
   - 缺点：范围过大，容易破坏已完成 PRJ-0002 主链路和验证节奏。

### Recommendation

采用方案 2。先做 Data/Event/Feature 三个 P0/P1 执行 SDD，再把 ObjectPool、TargetSelector、Logger 按 owner 后续处理。

推荐顺序：

1. Data Runtime Generic Slot Hard Cutover。
2. Event Dynamic Object Removal。
3. Feature / Ability Typed Execution Context。
4. ObjectPool Manager Untyped Interface。
5. TargetQueryEngine 分配控制。
6. Logger lazy / interpolated string handler。

### Must Confirm

- Data 是否接受一次性 hard cutover：允许删除或 internal 化 `Data.SetUntyped(string, object?)`、`Data.GetAll(): Dictionary<string, object>` 等当前 AI 框架不应调用的宽口 API。
- Event 是否接受删除 `EmitDynamic` / `OnDynamic` / `Action<object>`，并把 Feature 的动态事件动作改成 typed event action registry，而不是缓存反射后继续保留。
- Feature / Ability 是否接受把 `FeatureContext` 从通用 `object? ActivationData/ExecuteResult` 改成 typed execution context 或 typed adapter，哪怕会要求每个 Capability 显式注册自己的上下文类型。

### Should Confirm

- Data changed event 是否需要保留通用 `PropertyChanged` 给 TestSystem / UI 调试；如果保留，应只做 debug/diagnostic snapshot，不进入运行时热监听。
- 是否为 Data 性能补一个纯 C# microbenchmark，再补 Godot scene artifact；默认两者都做。
- Logger 是否优先改 `Log.Debug/Info` handler，还是只在热路径加 `if (_log.IsDebugEnabled)`；默认先做 handler/facade，减少调用点噪音。

### Defaults I Will Use

- Data/Event/Feature 不保旧 object API 为 AI 框架长期入口；必要时保留 `internal` 或 `[Obsolete]` 人工调试入口，并在注释中写明装箱、拆箱和 GC 风险。
- Event 不保动态 object 兼任。
- Data 主链路最终确认使用 `DataFieldDefinition<T>`、`DataSlot<T>`、`IDataSlot`、`DataValuePolicy<T>` 和 `IDataComputeResolver<T>`。`Dictionary<string, IDataSlot>` 只作为跨类型 slot 管理边界，不暴露 `object? Value`。
- 不采用上一版 `DataRuntimeValue` 多字段 union；它会制造冗余字段和新一层动态分发，违背 `DataKey<T>` 已经建立的泛型契约。
- 所有具体性能数字必须通过 profiler / benchmark / scene artifact 证明。

### Not Recommended

- 不建议只优化字符串插值或 LINQ 就宣称完成 GC 优化。
- 不建议给 `EmitDynamic` 加缓存后继续让 Feature 用 object 事件。
- 不建议为了人工便利保留 Data/Event object public API。
- 不建议把 Data 值全部拆成多个 `Dictionary<string, float/int/bool>` 而不处理 computed、modifier、changed event 和 DataValueConverter；那只是局部存储优化。
- 不建议使用 `DataRuntimeValue` 多字段 union 替代泛型 slot；这会从 `object` 宽口变成自定义宽口。

### Artifact Updates

本轮写入：

- `README.md`
- `00-总览与AI-first裁决.md`
- `01-Data运行时object去除设计.md`
- `02-EventBus动态object禁用设计.md`
- `03-FeatureAbility上下文类型化设计.md`
- `04-ObjectPool反射管理接口设计.md`
- `05-TargetSelector集合分配与LINQ设计.md`
- `06-Logger字符串与诊断分配设计.md`

项目级同步：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/notes.md`
