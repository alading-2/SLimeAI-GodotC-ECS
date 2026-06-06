# Notes

## References

- Context7 `/godotengine/godot-docs`：Godot `PackedScene` 加载/实例化、`SceneTree.Root.AddChild`、`SceneTree.get_nodes_in_group`。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/bevy/01-Bevy-源码分析报告.md`：模块化大框架、Relationship、Commands、CapabilityIndex 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/flecs/02-Flecs-源码分析报告.md`：`ChildOf` / lifecycle hierarchy、module scope、拒绝 pair/query DSL。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/entt/03-EnTT-源码分析报告.md`：小 Runtime kernel、拒绝 registry-like 全能力 query、resource diagnostics 观察项。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/05-DefaultEcs-源码分析报告.md`：Capability-owned selector、EntitySet lifecycle、deferred command 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`：最终综合裁决，保留 Capability-owned selector，不复制外部 ECS runtime。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing ：boxing/unboxing 语言事实；值类型进入 `object` 会进入托管堆对象语义。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals ：.NET GC / managed heap / allocation 与 collection 基础事实。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/interpolated-string-handler ：日志等性能场景可用 interpolated string handler 避免日志关闭时构造消息。

## Open Questions

- `Tool/其他Tool` 实施前需确认 Common Utilities 最终目录：`Src/ECS/Common/Utilities/` 还是 `Src/ECS/Runtime/Common/`。
- `NodeLifecycle` 是否从 `Src/ECS/Tools/NodeLifecycle/` 迁到 `Src/ECS/Runtime/NodeLifecycle/`。
- `EntityTargetSelector.Query(query)` 是否只作为执行期临时桥，切片结束前删除或 internal 化。
- Data GC hard cutover 实施前需确认 `Data.Get/Set<T>(string)`、`SetUntyped(... object?)`、`GetAll(): Dictionary<string, object>` 是否删除、internal 化或标 `[Obsolete]` debug-only。
- Event GC hard cutover 实施前需确认 `EmitDynamic` / `OnDynamic` / `Action<object>` 是否彻底删除，而不是只缓存反射。
- Feature/Ability GC hard cutover 实施前需确认 `FeatureContext` Execute 阶段是否改 typed generic contract。
