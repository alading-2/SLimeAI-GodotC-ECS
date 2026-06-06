# 拆箱装箱 + GC 优化设计包

> 状态：current design package
> 更新：2026-06-06
> 范围：`Src/ECS/Runtime/Data/`、`Src/ECS/Runtime/Event/`、`Src/ECS/Capabilities/Feature/`、`Src/ECS/Capabilities/Ability/`、`Src/ECS/Tools/ObjectPool/`、`Src/ECS/Tools/TargetSelector/`、`Src/ECS/Tools/Logger/`

## 定位

本目录用于承接 SlimeAI ECS 框架的装箱拆箱与 GC 优化分析。`问题/` 保存初步问题扫描；`设计/` 保存本轮按 AI-first 方向校准后的可执行设计裁决。

核心结论：

- Data 是 P0：当前 `DataSlot.Value object?`、`DataChangeRecord object?`、computed resolver `object?` 仍在 Runtime Data 热路径中，必须改为 `DataSlot<T> + IDataSlot`、typed runtime field、typed policy 和 typed computed resolver，不继续把 `object` 当 AI 框架主链路。
- Data 架构已确认：`DataSlot<T> + IDataSlot` 是最终方案；上一版 `DataRuntimeValue` 多字段 union 废弃。SlimeAI 已有 `DataKey<T>` 与 `Data.Get/Set<T>`，继续引入带多个候选字段的通用 value 结构会冗余、扩散分发逻辑，并削弱泛型契约。
- Event 是 P0：泛型 `EventBus.Emit<T>` 可以保留，但 `EmitDynamic` / `OnDynamic` / `Action<object>` 不应继续作为框架事件协议入口；Event 完全由 AI 写，必须禁止 object 兼任。
- Feature / Ability 是 P0/P1 交界：`FeatureContext.ActivationData/ExecuteResult object?` 和 `IFeatureHandler.OnExecute object?` 是当前 Ability 接入 Feature 的宽口桥，必须类型化，否则 Event 禁 object 后仍会从 Feature 绕回 object。
- ObjectPool、TargetSelector、Logger 是 P1：确有反射、集合分配和日志字符串求值问题，但优先级低于 Data/Event object 热路径，应随对应 owner hard cutover 或性能 SDD 分批处理。
- 字符串插值不是和 Data object 同级的问题。日志 API 接收 `object` / `string` 时，调用点的插值表达式会先求值；这应通过日志门禁或 interpolated string handler 解决，不把所有 `$"..."` 一概禁止。

## 阅读顺序

1. `设计/README.md`
2. `设计/00-总览与AI-first裁决.md`
3. `设计/01-Data运行时object去除设计.md`
4. `设计/02-EventBus动态object禁用设计.md`
5. `设计/03-FeatureAbility上下文类型化设计.md`
6. `设计/04-ObjectPool反射管理接口设计.md`
7. `设计/05-TargetSelector集合分配与LINQ设计.md`
8. `设计/06-Logger字符串与诊断分配设计.md`
9. `问题/00-总览.md`

## 非目标

- 不在本设计阶段直接改源码。
- 不以“兼容人手写方便”为理由保留 AI 框架热路径 object。
- 不用未验证的每帧分配估算当作完成证据；估算只能作为优先级输入，最终以 profiler / benchmark / scene artifact 为准。
- 不复制 Unity DOTS、Bevy 或第三方 ECS API；只采纳“热路径 typed data / typed event / 降低分配率 / 可验证”的原则。
