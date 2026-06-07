# 拆箱装箱 + GC 优化设计包

> 状态：current design package
> 更新：2026-06-07
> 范围：`Src/ECS/Runtime/Data/`、`Src/ECS/Runtime/Event/`、`Src/ECS/Capabilities/Feature/`、`Src/ECS/Capabilities/Ability/`、`Src/ECS/Tools/ObjectPool/`、`Src/ECS/Tools/TargetSelector/`、`Src/ECS/Tools/Logger/`

## 定位

本目录用于承接 SlimeAI ECS 框架的装箱拆箱与 GC 优化分析。`问题/` 保存初步问题扫描；`设计/` 保存本轮按 AI-first 方向校准后的可执行设计裁决。

## Data 完成后的重新裁决与执行结果

> 2026-06-06 重新分析：`SDD-0031 Data Runtime Generic Slot Hard Cutover` 已完成，Data 不再作为本目录当前待执行 P0。本目录后续只讨论 Event、Feature / Ability、ObjectPool、TargetSelector、Logger、Component / 生命周期分配等非 Data 切片。
>
> 2026-06-07 执行结果：`SDD-0033 Non-Data GC Boundary Completion` 已完成非 Data 明显宽口收口：Event dynamic object 主链路删除，Feature / Ability Execute 边界改 typed payload/result helper，ObjectPoolManager 改 `IObjectPoolRuntime` 非泛型管理接口，TargetSelector 新增 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics facade。Logger 本轮按用户裁决不改，仍等待 profiler 或明确热路径证据。

当前裁决：

- Event + Feature / Ability 主链路已由 SDD-0033 收口。`EventBus.EmitDynamic/OnDynamic/Action<object>` 不再作为框架事件协议入口；Feature / Ability 不再通过 `ActivationData =`、`ctx.ExecuteResult =` 或 `ExecuteResult is` 传递主链路结果。
- Event 不建议只做反射缓存。缓存 `MakeGenericMethod` 只能减少一点开销，不能解决事件 payload 契约漂移；当前已采用 typed `Emit<T>` / `On<T>` 和 typed `EmitEventAction<TEvent>`。
- Feature / Ability 不建议把完整生命周期全部泛型化。SDD-0033 只类型化 Execute 输入 / 输出；Granted、Removed、Activated、Ended 保留普通 lifecycle context，避免无谓泛型扩散。
- ObjectPool manager 反射已由 SDD-0033 局部收口。对象池生命周期、Parking、Collision guard 仍以 ObjectPool / Collision 设计包和 SDD-0028 为事实源，不用 Data value hot path 规则套用引用对象池。
- TargetSelector 已引入 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics facade。后续若要做 pooled lease、deterministic RNG 或 allocation artifact，应在 TargetQueryEngine owner 下继续，不回到旧 `EntityTargetSelector.Query` list-only 主链路。
- Logger 是 P2 / 局部热路径问题。字符串插值不是架构 P0；问题是日志 API 不能延迟求值。只在每帧热路径加门禁、lazy 或 interpolated string handler，不做全仓风格重写。
- ComponentRegistrar、Entity 生命周期 `ToArray()` 多数是防修改 snapshot 或注册 / 销毁阶段分配，默认不作为本轮性能目标；除非 profiler 证明它们进入战斗热路径。

核心结论：

- Data 已完成本轮主链路 hard cutover：`DataSlot<T> + IDataSlot` 是已采用架构；Data 残留边界（typed DataChanged、diagnostic snapshot、string-key 清理）不再阻塞非 Data 切片分析。
- Event / Feature / Ability 的主协议 object 宽口已由 SDD-0033 收口；旧 raw object 属性仅作为 `[Obsolete]` 兼容边界，不作为新代码入口。
- ObjectPool manager 反射和 TargetSelector ownership facade 已由 SDD-0033 收口；进一步生命周期 / pooled lease / deterministic allocation 优化必须走各自 owner SDD。
- Logger、Component / 生命周期分配是 P2 或 profiler 驱动项：不作为 Data 完成后的下一步默认任务。
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
