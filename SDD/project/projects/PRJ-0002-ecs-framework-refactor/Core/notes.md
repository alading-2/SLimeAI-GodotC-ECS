# Notes

## References

- Context7 `/godotengine/godot-docs`：Godot `PackedScene` 加载/实例化、`SceneTree.Root.AddChild`、`SceneTree.get_nodes_in_group`。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/bevy/01-Bevy-源码分析报告.md`：模块化大框架、Relationship、Commands、CapabilityIndex 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/flecs/02-Flecs-源码分析报告.md`：`ChildOf` / lifecycle hierarchy、module scope、拒绝 pair/query DSL。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/entt/03-EnTT-源码分析报告.md`：小 Runtime kernel、拒绝 registry-like 全能力 query、resource diagnostics 观察项。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/05-DefaultEcs-源码分析报告.md`：Capability-owned selector、EntitySet lifecycle、deferred command 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`：最终综合裁决，保留 Capability-owned selector，不复制外部 ECS runtime。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/09-Unreal-GAS-文档对照报告.md`：Ability / Feature / DataKey / Modifier 分工参考；支持不新增 AttributeSet 第二套状态源。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/types/boxing-and-unboxing ：boxing/unboxing 语言事实；值类型进入 `object` 会进入托管堆对象语义。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/standard/garbage-collection/fundamentals ：.NET GC / managed heap / allocation 与 collection 基础事实。
- Microsoft Learn: https://learn.microsoft.com/en-us/dotnet/csharp/advanced-topics/performance/interpolated-string-handler ：日志等性能场景可用 interpolated string handler 避免日志关闭时构造消息。
- Unity Entities docs: https://docs.unity.cn/Packages/com.unity.entities%401.0/manual/concepts-components.html ：ECS component 保存 entity data，`IComponentData` 标记 struct component；用于校准热路径状态主链路应 typed，不代表 SlimeAI 要复制 DOTS。
- Unity Entities docs: https://docs.unity.cn/Packages/com.unity.entities%401.1/manual/components-unmanaged.html ：unmanaged component 由继承 `IComponentData` 的 struct 创建；用于校准数据热路径避免 `object` 宽口。
- Unity Manual: https://docs.unity3d.com/Manual/performance-managed-memory.html 与 https://docs.unity3d.com/Manual/performance-garbage-collector.html ：managed memory / garbage collector 基础；用于校准减少临时分配和对象复用原则，不作为 SlimeAI 全仓零 GC 目标。
- Bevy ECS docs: https://docs.rs/bevy/latest/bevy/ecs/component/trait.Component.html 与 https://docs.rs/bevy/latest/bevy/ecs/storage/index.html ：Component 有明确 storage type，ECS storage 区分 table/sparse 等数据结构；用于校准 typed storage / storage policy 方向，不复制 Rust API。
- Bevy ECS Message docs: https://docs.rs/bevy/latest/bevy/ecs/message/trait.Message.html ：Message 使用 typed `MessageWriter` / `MessageReader` / `Messages<M>`；用于校准 typed payload communication 与 schedule/ownership 边界，不复制 Rust API。
- Context7 `/godotengine/godot-docs`：Godot C# 通过 `ResourceLoader.Load<PackedScene>()` / `GD.Load<PackedScene>()` 加载并实例化资源；用于校准 `ResourceManagement` 只是 facade，底层仍是 Godot 资源系统。
- Godot docs `ResourceUID`：Godot 通过 `uid://` 维护资源唯一标识和 path 映射；用于提示 ResourceManagement 后续可验证 UID，但本轮不直接迁移 DataOS 主存储。
- Context7 `/godotengine/godot-docs`：`@export_file` 在 Godot 4.4+ 可能保存 `uid://`，Godot 4.5+ 有保留 path 行为的注解；用于说明 `uid://` 有移动资源价值，但版本、C#、snapshot、submodule 链路需要单独验证。
- Context7 `/needle-mirror/com.unity.addressables`：Unity Addressables `AssetReference` / `AssetGUID` 支持按资源引用加载；用于对照主流引擎通过 GUID/address 管理资源身份，不复制 Addressables 全套远程内容和 async handle 模型。
- Epic Unreal Asset Management docs：Unreal `Asset Manager` 管理 Primary Asset 的发现、加载、卸载和审计；用于对照 manifest / diagnostics 思想，不复制 Primary/Secondary Asset、Bundle、Chunking 全套系统。
- `/home/slime/Code/SlimeAI/SlimeAI-AiFirst/DocsAI/Framework/MultiGameLayout.md` 与 `Workspace/DocsAI/MultiGameLayout.md`：游戏仓根是 `project.godot` 所在目录，也就是 `res://` 根；框架 submodule 在游戏仓中表现为 `res://SlimeAI/...`。
- `/home/slime/Code/SlimeAI/Games/BrotatoLikeOld/Src/Validation/Game/LegacyResources/README.md`：旧资源迁移用 `ResourceLoader.Exists` 和 artifact 校验 active/missing legacy paths；可作为后续 ResourceCatalogDiagnostics 的参考。

## Open Questions

- `Tool/其他Tool` 的 Common / NodeLifecycle / TargetSelector 执行前确认项已由 2026-06-07 用户裁决关闭：Common Utilities 放 `Src/ECS/Tools/CommonUtilities/` + `DocsAI/ECS/Tools/CommonUtilities/`；NodeLifecycle 迁到 `Src/ECS/Runtime/NodeLifecycle/`；TargetSelector 不做 `EntityTargetSelector.Query(query)` 兼容桥。
- Data GC hard cutover 实施前需确认 `Data.Get/Set<T>(string)`、`SetUntyped(... object?)`、`GetAll(): Dictionary<string, object>` 是否删除、internal 化或标 `[Obsolete]` debug-only。
- Data GC hard cutover 架构已确认：执行 SDD 名称使用 `Data Runtime Generic Slot Hard Cutover`，方案使用 `DataSlot<T> + IDataSlot`；`DataRuntimeValue` 多字段 union 已被用户否定并在设计中废弃，不作为默认折中。
- Data GC hard cutover 实施前仍需确认 public object API 删除/internal/obsolete debug-only 的具体处置，以及 `PropertyChanged(object?)` 是否改 typed/domain event + debug snapshot。
- Event / Feature / Ability 非 Data GC hard cutover 已由 SDD-0033 完成；后续不要恢复 `EmitDynamic` / `OnDynamic` / `Action<object>` 主链路，也不要恢复 `object? OnExecute`。
- ObjectPool manager 反射与 TargetSelector ownership 基础 facade 已由 SDD-0033 完成；后续 pooled lease / deterministic RNG / allocation artifact 必须从 TargetQueryEngine owner 继续。
- Logger 本轮明确不改；只有 profiler 或明确热路径证据出现后再进入 Logger lazy / interpolated string handler 小切片。
- ResourceManagement DeepThink 已由 2026-06-07 用户校准：`res://` 本身不是问题；问题是无 owner 裸加载、路径移动后缺自动替换和 diagnostics。统一加载工具可以简化为 ResourceLoading facade，路径移动由 `resource-path-migration` skill + `ResourceGenerator` + `rg` 残留检查处理。
- Resource Loading Hard Cutover 前需确认 DataOS resource ref 是否未来迁到 `ResourceKey + Category` 或 `uid://`；默认先不改 DataOS schema。
- Godot `uid://` 是否纳入下一阶段验证；默认只作为研究项，不作为当前主存储。注意它不是 `.cs.uid` 文件。
- Resource loading 失败策略需确认：默认 structured result + preflight fail-fast，不在 gameplay 热路径到处抛未捕获异常。
- 未来框架/游戏仓分离后，框架 ResourceGenerator 不默认拥有游戏资源；游戏仓需要 game-local catalog 或 generator `--project-root` / `--output` 能力。
