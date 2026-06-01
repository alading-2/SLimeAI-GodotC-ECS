# Notes

## References

- `../../design/6.ECS框架目录架构大重构/README.md`
- `../../design/6.ECS框架目录架构大重构/01-现状证据与AI-first裁决.md`
- `../../design/6.ECS框架目录架构大重构/02-目标目录架构与归属规则.md`
- `../../design/6.ECS框架目录架构大重构/03-迁移切片与验证门禁.md`
- `../../directory-architecture-restructure-execution-prompt.md`
- `../../../../../DocsAI/ECS框架与AIFirst方向决策.md`
- `/home/slime/Code/SlimeAI/SlimeAI-AiFirst/GameOS/Capabilities`
- `/home/slime/Code/SlimeAI/SlimeAI-AiFirst/DocsAI`

## Open Questions

- Tools 是否长期保持顶层，还是 Timer / Resource / Pool 等部分进入 Runtime，需要后续单独裁决。
- UI 是否长期保持顶层，还是建立 Presentation/UI owner，需要后续单独裁决。
- DataOS 顶层 `Data/` 是否迁入 `Src/ECS/Runtime/DataOS`，还是保持仓库级 authoring 目录，需要在 generator 和工具路径审计后裁决。
- DocsOld 迁入 Foundations 后，原 `DocsOld/` 是否删除或归档，需用户另行确认。
