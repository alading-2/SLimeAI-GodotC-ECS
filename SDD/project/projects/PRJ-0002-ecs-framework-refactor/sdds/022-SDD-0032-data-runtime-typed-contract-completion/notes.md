# Notes

## References

- `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/ECS框架优化/0.ECS框架的思考/01-Data作为ECS框架核心的概念复盘与方案批判.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/progress.md`
- `.ai-config/skills/ecs/ecs-data/SKILL.md`
- `.ai-config/skills/data/data-authoring/SKILL.md`

## Open Questions

- `Data.Get<T>(string)` 仍为 internal 兼容入口；本轮用 grep gate 限制业务复制，不做大范围旧测试迁移。
- `LifecycleComponent` 的 `PropertyChanged` 监听一并迁移，因为它是业务组件，不属于 diagnostic 例外。
