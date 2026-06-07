# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/ParentManager/`
- `DocsAI/ECS/Tools/NodeLifecycle/`

## Open Questions

- `ParentManager` 执行时是否直接改名为 `RuntimeMountRegistry` / `SceneMountRegistry`：默认直接改名。
- 是否新增 `DocsAI/ECS/Runtime/Mount/`：默认新增，避免把 Runtime mount 继续放 Tools。
