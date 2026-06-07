# Notes

## References

- PRJ-0002 project design: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/ECS框架优化/1.拆箱装箱+GC优化/`
- Runtime Event docs: `DocsAI/ECS/Runtime/Event/Event系统说明.md`
- Feature docs: `DocsAI/ECS/Capabilities/Feature/README.md`
- Ability docs: `DocsAI/ECS/Capabilities/Ability/README.md`
- ObjectPool docs: `DocsAI/ECS/Tools/ObjectPool/`
- TargetSelector docs: `DocsAI/ECS/Tools/TargetSelector/`
- Owner skills: `.ai-config/skills/ecs/ecs-event/SKILL.md`, `.ai-config/skills/gameos/feature-system/SKILL.md`, `.ai-config/skills/gameos/ability-system/SKILL.md`, `.ai-config/skills/core/tools/SKILL.md`

## Open Questions

- Logger 是否进入后续 SDD：本轮默认不进入，等待 profiler 或明确热路径证据。
- TargetQueryResult 是否立刻引入 pooled lease：本轮默认不引入，只建立 ownership / diagnostics，避免先池化再补语义。
- Event dynamic API 是否完全删除：本轮默认先 obsolete/禁用业务主链路；如果构建面证明没有外部使用，可进一步删除。
