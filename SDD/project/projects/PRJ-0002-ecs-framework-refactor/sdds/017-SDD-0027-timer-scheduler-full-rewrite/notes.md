# Notes

## References

- `../../design/Tool/Timer/README.md`
- `../../design/Tool/Timer/01-现状证据与AI-first裁决.md`
- `../../design/Tool/Timer/02-目标架构与优化路线.md`
- `../../design/Tool/Timer/03-调用点迁移与验证计划.md`
- `Src/ECS/Tools/Timer/TimerManager.cs`
- `Src/ECS/Tools/Timer/GameTimer.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
- `DocsAI/ECS/Tools/Timer/Concept.md`
- `DocsAI/ECS/Tools/Timer/Usage.md`
- Godot source: `Resources/Engine/Engine/godot-4.6.2-stable/scene/main/scene_tree.cpp`
- Bevy source: `Resources/Engine/Engine/bevy/crates/bevy_time/src/timer.rs`
- UnityCsReference: `Resources/Engine/Engine/UnityCsReference/Modules/UIElements/Core/Scheduler.cs`
- ET Framework: `Resources/Engine/Engine/ET-Framework/Book/2.3单线程异步.md`
- Netty HashedWheelTimer docs/source reference

## Open Questions

- Timing wheel 是否需要第一轮实现：默认不需要，除非 SDD-0027 benchmark 证明 heap 成为主要瓶颈。
- `TimerOwner` 第一版是否使用 typed EntityId/ComponentId：默认用 record/enum/string 建立契约，不阻塞 Timer core；后续可 typed 化。
- `TimerClock.Fixed` 第一轮是否完整接入 `_PhysicsProcess`：设计保留，若实现范围收紧，必须在 progress 写明行为和后续切片。
