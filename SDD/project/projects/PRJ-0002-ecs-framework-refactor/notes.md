# Notes

## References

- Context7 `/godotengine/godot-docs`：Godot `PackedScene` 加载/实例化、`SceneTree.Root.AddChild`、`SceneTree.get_nodes_in_group`。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/bevy/01-Bevy-源码分析报告.md`：模块化大框架、Relationship、Commands、CapabilityIndex 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/flecs/02-Flecs-源码分析报告.md`：`ChildOf` / lifecycle hierarchy、module scope、拒绝 pair/query DSL。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/entt/03-EnTT-源码分析报告.md`：小 Runtime kernel、拒绝 registry-like 全能力 query、resource diagnostics 观察项。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/05-DefaultEcs-源码分析报告.md`：Capability-owned selector、EntitySet lifecycle、deferred command 参考。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`：最终综合裁决，保留 Capability-owned selector，不复制外部 ECS runtime。

## Open Questions

- `Tool/其他Tool` 实施前需确认 Common Utilities 最终目录：`Src/ECS/Common/Utilities/` 还是 `Src/ECS/Runtime/Common/`。
- `NodeLifecycle` 是否从 `Src/ECS/Tools/NodeLifecycle/` 迁到 `Src/ECS/Runtime/NodeLifecycle/`。
- `EntityTargetSelector.Query(query)` 是否只作为执行期临时桥，切片结束前删除或 internal 化。
