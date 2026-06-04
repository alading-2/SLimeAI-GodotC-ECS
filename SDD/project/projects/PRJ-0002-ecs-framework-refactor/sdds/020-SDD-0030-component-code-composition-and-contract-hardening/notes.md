# Notes

## Reference Inputs

- Project shared design: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/7.Component/`
- Runtime Component docs: `DocsAI/ECS/Runtime/Component/`
- Runtime Component source: `Src/ECS/Runtime/Component/`
- Entity component registrar: `Src/ECS/Runtime/Entity/Components/`
- Spawn pipeline: `Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs`
- Current Component Preset inputs:
  - `Src/ECS/Capabilities/Unit/Presets/UnitCorePreset.tscn`
  - `Src/ECS/Capabilities/Unit/Presets/PlayerPreset.tscn`
  - `Src/ECS/Capabilities/Unit/Presets/EnemyPreset.tscn`
  - `Src/ECS/Capabilities/Ability/Presets/AbilityPreset.tscn`

## Open Follow-Ups Not In This SDD

- 统一 `EntityManager.Destroy` 与 `EntityDestroyPipeline` 销毁路径。
- 删除 legacy Component Preset `.tscn` 与 `ResourceCategory.Preset` 记录。
- 收口 `EntityManager.GetComponent<T>()` documented exceptions。

## References

- 无。

## Open Questions

- 无。
