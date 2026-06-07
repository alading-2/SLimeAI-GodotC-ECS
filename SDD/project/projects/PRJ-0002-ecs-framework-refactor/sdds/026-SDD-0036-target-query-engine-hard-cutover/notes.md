# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/05-TargetSelector查询契约.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/TargetSelector/`
- `SDD-0035 Runtime Mount And Node Lifecycle Hard Cutover`

## Open Questions

- 是否引入空间索引：默认不引入，除非 profiling 或场景 artifact 证明需要。
- 是否新增 pooled lease：默认不混入本 SDD，后续按 TargetSelector 性能证据另开小切片。
