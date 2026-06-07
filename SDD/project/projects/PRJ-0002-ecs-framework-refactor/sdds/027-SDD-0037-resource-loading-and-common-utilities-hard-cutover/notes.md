# Notes

## References

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/01-现状证据与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/其他Tool/06-实施路线与验证门禁.md`
- `DocsAI/ECS/Tools/ResourceManagement/README.md`
- `.ai-config/skills/core/resource-path-migration/SKILL.md`

## Open Questions

- DataOS resource ref 是否未来从 `res://` 迁到 `ResourceKey + Category` 或 `uid://`：默认本 SDD 不改 schema。
- `uid://` 是否进入验证：默认只记录后续研究项。
- 游戏仓分离后 game-local catalog 输出位置：默认不由框架 ResourceGenerator 拥有游戏资源。
