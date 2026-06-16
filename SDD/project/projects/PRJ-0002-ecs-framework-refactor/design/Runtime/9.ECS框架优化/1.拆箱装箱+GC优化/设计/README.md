# GC 优化设计入口

> 状态：current
> 更新：2026-06-07
> 输入：用户关于 Data/Event object、字符串插值、AI-first 性能严谨性的裁决要求；用户对 `DataRuntimeValue` 多字段方案的驳回；用户确认 `DataSlot<T> + IDataSlot` 是 Data 去 object 最佳方案；`DocsAI/ECS框架与AIFirst方向决策.md`；Data/Event/Feature/ObjectPool/TargetSelector/Logger DocsAI 与源码审计；Microsoft Learn .NET boxing / GC / interpolated string handler 文档；Resources/Engine 本地框架分析和 Unity Entities / Bevy ECS 官方资料。

## DeepThink 确认包

### Goal

解决 SlimeAI ECS 框架中会破坏 AI-first 严谨性和运行时性能的装箱拆箱 / GC 问题，先形成可执行设计裁决，再由后续 SDD 实施。

2026-06-06 重新分析后，本设计入口的当前目标收窄为：在 `SDD-0031 Data Runtime Generic Slot Hard Cutover` 已完成的前提下，重新评估非 Data 切片的优先级、必要性和过度设计风险。

2026-06-07 执行结果：用户接受合并方向并要求“更新设计文档、生成 SDD、执行任务一起完成”。`SDD-0033 Non-Data GC Boundary Completion` 已完成本入口推荐的非 Data 明显问题收口：Event dynamic object 主链路、Feature / Ability raw object Execute 边界、ObjectPool manager 反射和 TargetSelector ownership facade。Logger 本轮不改，仍按 profiler / 热路径证据进入后续 P2。

本轮完成后的非目标仍然成立：不重新分析 Data 主链路；不做泛泛“全仓零 GC”口号；不为旧手写方便恢复 Event / Feature object 主链路；不把所有字符串插值、`ToArray()` 或 LINQ 都误判为 P0。

### Context Read

- Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- 已读方向源：`DocsAI/README.md`、`DocsAI/ECS框架与AIFirst方向决策.md`、`DocsAI/ECS/README.md`。
- 已读 owner 文档：`DocsAI/ECS/Runtime/Data/Data系统说明.md`、`DocsAI/ECS/Runtime/Event/Event系统说明.md`、`DocsAI/ECS/Runtime/README.md`、`DocsAI/ECS/Tools/ObjectPool/README.md`。
- 已读 SDD 源：`PRJ-0002` README、`design/INDEX.md`、`Core/roadmap.md`、`Core/progress.md`、现有 `问题/*.md`。
- 已读源码：`DataRuntimeStorage.cs`、`Data.cs`、`GameEventType_Data.cs`、`DataModifier.cs`、`DataComputeRegistry.cs`、`EventBus.cs`、`EventContext.cs`、`FeatureContext.cs`、`IFeatureHandler.cs`、`FeatureSystem.cs`、`EmitEventAction.cs`、`AbilityFeatureHandler.cs`、`AbilitySystem.cs`、`CastContext.cs`、`TriggerComponent.cs`、`ObjectPoolManager.cs`、`EntityTargetSelector.cs`、`AbilityInventoryService.cs`、`ComponentRegistrar.cs`、`Log.cs`。
- 重新分析补读源码：`EventBus.cs`、`FeatureContext.cs`、`IFeatureHandler.cs`、`EmitEventAction.cs`、`AbilitySystem.cs`、`TriggerComponent.cs`、`ObjectPoolManager.cs`、`EntityTargetSelector.cs`、`AbilityInventoryService.cs`、`ComponentRegistrar.cs`、`Log.cs`。
- 当前 Data 状态：`SDD-0031` / `SDD-0032` 已完成；Data 只作为历史背景，不再作为当前待执行 P0。
- 本地引擎资料：`Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`、`05-DefaultEcs-源码分析报告.md`、`09-Unreal-GAS-文档对照报告.md`。结论支持 typed data / typed event / Capability-owned selector，不支持把外部 ECS 运行时照搬进 SlimeAI。
- 外部资料：Microsoft Learn 确认 boxing 会把值类型包装到托管堆对象；GC 频率受分配率影响；interpolated string handler 是性能场景下避免无谓字符串构造的官方机制。Unity managed memory / GC 文档用于校准“减少每帧临时分配”和对象复用的常规性能原则；Bevy `Message` 文档用于校准 typed payload message / reader / writer 边界；本地 Resources 综合报告继续支持 typed event、少入口和 Capability-owned selector。
- 未读上下文：未做运行时 profiler；未跑 Godot 场景；未审完所有 gameplay / UI 调用点。

### Problem Shape

旧 Data/Event/Feature 设计把 `object?` 当成“方便兼任”的通用桥。Data 主链路已经通过 `SDD-0031` / `SDD-0032` 收口；Event + Feature / Ability 的 object 协议链已经通过 `SDD-0033` 收口。

这个选择在人工框架中能降低 API 设计成本，但在 AI-first 框架中会制造三个问题：

1. AI 无法从类型系统知道字段、事件和上下文 payload 的真实契约。
2. 值类型进入 `object` 会产生装箱；更重要的是事件和执行上下文会失去静态契约。
3. 兼任入口会绕过 typed event、typed handler 和 Capability-owned trigger binding 的事实源治理。

现有 `问题/` 文档中“每帧 2000+ 次 / 32KB”这类数字只能作为风险估算，不能作为事实；当前非 Data 切片的优先级应来自协议风险、源码证据和热路径判断，而不是沿用旧估算。

### Main Risks

- Event 禁 object 后，Feature 的 `EmitEventAction` 和 Ability/Feature 上下文必须同步重构，否则会产生绕路；该项已由 `SDD-0033` 完成。`TriggerComponent` 当前没有稳定 dynamic event 主链路，后续若补 OnEvent binding，应沿 typed binding id 方向继续。
- Feature / Ability 如果把完整 lifecycle 全部泛型化，会制造比 object 更难维护的 API 扩散；应只类型化 Execute 输入 / 输出。
- TargetSelector 如果只池化 List 而不定义 result ownership，会引入调用方持有已复用 buffer 的隐蔽 bug。
- 日志字符串问题容易被扩大成风格战争；实际应该以日志门禁和 handler 解决热路径求值。
- ComponentRegistrar / Entity 生命周期里的 `ToArray()` 多数是 snapshot 防修改，不应在无 profiler 证据时机械删除。
- 未 profile 前不能承诺具体 GC 数字；必须把 benchmark / scene artifact 作为执行 SDD 的验收项。

### Options

1. 小修：只缓存 `EmitDynamic` 反射、减少 LINQ、给日志加 `IsEnabled`。
   - 优点：快。
   - 缺点：Event / Feature object 协议仍在，AI-first 契约仍宽。

2. P0 协议收口：Event dynamic object removal + Feature/Ability typed Execute boundary + Trigger typed binding。
   - 优点：解决 Data 完成后剩余最影响 AI 契约的问题。
   - 缺点：需要同时触及 Event、Feature、Ability 和 TriggerComponent，必须用 SDD 和行为测试控制范围。

3. 全仓一次性零分配重构。
   - 优点：目标彻底。
   - 缺点：范围过大，容易破坏已完成 PRJ-0002 主链路和验证节奏。

### Recommendation

采用方案 2。Data 已完成后，不再继续扩大 Data；`SDD-0033` 已合并处理 Event + Feature / Ability typed execution boundary，并顺带完成 ObjectPool manager runtime interface 与 TargetQueryResult ownership facade 这两个明确 P1 小切片。Logger 仍按 owner 后续处理。

推荐顺序：

1. 已完成：Event + Feature / Ability Typed Execution Boundary（SDD-0033）。
2. 已完成：ObjectPool Manager Runtime Interface（SDD-0033 的局部切片）。
3. 已完成基础 facade：TargetQueryEngine / TargetQueryResult ownership 与 diagnostics（SDD-0033）；后续 pooled lease / allocation artifact 另起 owner SDD。
4. 后续：AbilityInventory / ComponentRegistrar 局部 cleanup（需要调用频率或 profiler 证据）。
5. 后续：Logger hot path lazy / interpolated string handler。

### Must Confirm

- 已确认并执行：下一步 SDD 使用 Event + Feature / Ability 联合切片，而不是单独做 Event 反射缓存。
- 已确认并执行：Feature 只类型化 Execute 输入 / 输出，Granted / Removed / Activated / Ended lifecycle context 暂不泛型化。
- 已确认默认方向：不保留 TriggerComponent 事件类型字符串 + object source 的主链路；后续若补 OnEvent trigger binding，应走 typed trigger binding id。

### Should Confirm

- ObjectPool 是否单独开小 SDD 去 manager 反射；默认单独处理，不混入 Event/Feature。
- TargetSelector 是否已有 TargetQueryEngine 既有设计入口可承接；默认随 TargetQueryResult ownership 一起做，不先机械池化 List。
- Logger 是否优先改 `Log.Debug/Info` handler，还是只在热路径加 `if (_log.IsDebugEnabled)`；默认先只处理热路径，不做全仓风格重写。

### Defaults I Will Use

- Event/Feature 不保旧 object API 为 AI 框架长期入口；必要时保留 `internal` 或 `[Obsolete]` 人工调试入口，并在注释中写明装箱、拆箱和契约漂移风险。
- Event 不保动态 object 兼任。
- Feature 只把 Execute 阶段改 typed；lifecycle context 保持普通对象，避免泛型扩散。
- ObjectPool 的 `ReleaseUntyped(object)` 是引用类型管理边界，不套用 Data 值类型 hot path 规则。
- TargetSelector 先定义结果 ownership，再做 buffer 复用。
- 所有具体性能数字必须通过 profiler / benchmark / scene artifact 证明。

### Not Recommended

- 不建议只优化字符串插值或 LINQ 就宣称完成 GC 优化。
- 不建议给 `EmitDynamic` 加缓存后继续让 Feature 用 object 事件。
- 不建议为了人工便利保留 Event/Feature object public API。
- 不建议把 Feature 完整 lifecycle 全部泛型化。
- 不建议在 TargetSelector 上只替换 List 为对象池而不定义 ownership。
- 不建议机械删除 ComponentRegistrar / lifecycle 的 `ToArray()` snapshot。

### Artifact Updates

本轮写入：

- `README.md`
- `00-总览与AI-first裁决.md`
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
