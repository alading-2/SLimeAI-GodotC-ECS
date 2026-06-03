# Notes

## References

- `DocsAI/ECS/Tools/ObjectPool/README.md`
- `DocsAI/ECS/Tools/ObjectPool/Concept.md`
- `DocsAI/ECS/Tools/ObjectPool/Usage.md`
- `DocsAI/ECS/Tools/ObjectPool/Tests.md`
- `DocsAI/ECS/Capabilities/Collision/README.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/README.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md`
- `DocsAI/ECS/Capabilities/Collision/Concepts/Node2D父链约束.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/ObjectPool/`
- `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
- `Src/ECS/Capabilities/Collision/Component/`

## Open Questions

- 后续执行时是否立即把 Collision / Movement / ContactDamage guard 与 ObjectPool runtime state 放在同一批实现中；本 SDD 默认是同一批。
- 当前 BrotatoLike 承载游戏 runner 是否可用；不可用时 Godot validation 必须记录为 blocker，不能用旧游戏证据替代。

## Historical Context

旧分析已移动到 `DocsAI/ECS/Capabilities/Collision/Concepts/History/`。它们保留源码和排查证据，但不再作为默认执行事实源。
