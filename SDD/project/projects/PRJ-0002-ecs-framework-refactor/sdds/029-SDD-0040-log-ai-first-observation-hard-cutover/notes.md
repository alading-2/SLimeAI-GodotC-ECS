# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/01-现状分析与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/02-目标架构与数据契约.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/03-控制面与CLI设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/04-测试统一与Observation接入.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/05-调用点迁移与验证计划.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/06-功能OwnerLog文档与分析流程.md`
- `DocsAI/ECS/Tools/Logger/README.md`
- `.ai-config/skills/godot/godot-scene-test/SKILL.md`

## Open Questions

- 第一版 `logctl` 最终落地语言和目录位置需在 T1.1 baseline 中结合现有工具结构确认；默认不引入第三方依赖。
- 第一批 owner flow 的实际代码接入范围可按风险裁剪，但 owner `Log.md` 必须覆盖设计包列出的高价值 owner。
- 如果 Godot runner 或 Godot CLI 仍不可用，最终场景验证应记录 blocker，不能把 stdout fallback 当作结构化验证通过。
