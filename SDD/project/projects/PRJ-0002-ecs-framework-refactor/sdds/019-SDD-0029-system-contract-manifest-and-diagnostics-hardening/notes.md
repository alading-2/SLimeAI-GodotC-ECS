# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/README.md`
- `DocsAI/ECS/Runtime/System/README.md`
- `DocsAI/ECS/Runtime/System/Usage.md`
- `Src/ECS/Runtime/System/`
- `Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.cs`
- `Src/ECS/Capabilities/TestSystem/System/System/SystemInfoService.cs`
- `Data/DataOS/Snapshots/runtime_snapshot.json`

## Open Questions

- 是否在本 SDD 内新增 Runtime System owner skill：默认等实现阶段确认后再决定。
- diagnostics artifact 是否固定为 `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json`：默认采用该路径。
- typed `SystemId` 是否进入后续 SDD：默认不进入 SDD-0029。
