# 拆箱装箱 + GC 优化设计包

> 状态：current design package
> 更新：2026-06-06
> 范围：`Src/ECS/Runtime/Data/`、`Src/ECS/Runtime/Event/`、`Src/ECS/Capabilities/Feature/`、`Src/ECS/Capabilities/Ability/`、`Src/ECS/Tools/ObjectPool/`、`Src/ECS/Tools/TargetSelector/`、`Src/ECS/Tools/Logger/`

## 定位

本目录用于承接 SlimeAI ECS 框架的装箱拆箱与 GC 优化分析。`问题/` 保存初步问题扫描；`设计/` 保存本轮按 AI-first 方向校准后的可执行设计裁决。

## Data 完成后的重新裁决

> 2026-06-06 重新分析：`SDD-0031 Data Runtime Generic Slot Hard Cutover` 已完成，Data 不再作为本目录当前待执行 P0。本目录后续只讨论 Event、Feature / Ability、ObjectPool、TargetSelector、Logger、Component / 生命周期分配等非 Data 切片。

当前裁决：

- Event + Feature / Ability 是下一批 P0。`EventBus.EmitDynamic/OnDynamic/Action<object>` 与 `FeatureContext.ActivationData/ExecuteResult object?` 是同一条宽口协议链，不能只删 Event 而让 Feature 继续用 object 绕回去。
- Event 不建议只做反射缓存。缓存 `MakeGenericMethod` 只能减少一点开销，不能解决事件 payload 契约漂移；应删除或禁用框架主链路 dynamic object，并用 typed event action / typed trigger binding 替代。
- Feature / Ability 不建议把完整生命周期全部泛型化。真正需要 typed 的是 Execute 输入 / 输出；Granted、Removed、Activated、Ended 可保留普通 lifecycle context，避免无谓泛型扩散。
- ObjectPool 是 P1 小切片，只去掉 manager 反射和字符串方法名。它管理 class / Node 引用，不能套用 Data 的值类型热路径规则，也不应借机重写对象池生命周期。
- TargetSelector 是 P1 性能切片，应随 TargetQueryEngine / TargetQueryResult ownership 一起做。不要只把 `new List` 换成池化列表后继续让调用方长期持有，避免引入 buffer 生命周期 bug。
- Logger 是 P2 / 局部热路径问题。字符串插值不是架构 P0；问题是日志 API 不能延迟求值。只在每帧热路径加门禁、lazy 或 interpolated string handler，不做全仓风格重写。
- ComponentRegistrar、Entity 生命周期 `ToArray()` 多数是防修改 snapshot 或注册 / 销毁阶段分配，默认不作为本轮性能目标；除非 profiler 证明它们进入战斗热路径。

核心结论：

- Data 已完成本轮主链路 hard cutover：`DataSlot<T> + IDataSlot` 是已采用架构；Data 残留边界（typed DataChanged、diagnostic snapshot、string-key 清理）不再阻塞非 Data 切片分析。
- Event 是 P0：泛型 `EventBus.Emit<T>` 可以保留，但 `EmitDynamic` / `OnDynamic` / `Action<object>` 不应继续作为框架事件协议入口；Event 完全由 AI 写，必须禁止 object 兼任。
- Feature / Ability 是 P0：`FeatureContext.ActivationData/ExecuteResult object?` 和 `IFeatureHandler.OnExecute object?` 是当前 Ability 接入 Feature 的宽口桥，必须与 Event 一起收口，否则 Event 禁 object 后仍会从 Feature 绕回 object。
- ObjectPool、TargetSelector 是 P1：确有反射、集合分配问题，但处理方式必须保持 owner 边界；ObjectPool 只补非泛型管理接口，TargetSelector 走 TargetQueryResult ownership。
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
