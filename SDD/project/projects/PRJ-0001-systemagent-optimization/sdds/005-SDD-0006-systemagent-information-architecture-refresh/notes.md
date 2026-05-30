# Notes

## References

- `../../design/SystemAgent问题清单.md`
- `../../design/02-Workflow与Skill触发优化方案.md`
- `../../design/06-实施路线图.md`
- `../../design/09-WorkflowSkillRole分层模型.md`
- `../../design/10-Subagent使用场景与采纳策略.md`
- `Workspace/SystemAgent/README.md`
- `Workspace/SystemAgent/INDEX.md`
- `Workspace/SystemAgent/Catalog/manifest.yaml`

## Open Questions

- T1.1 执行时确认是否新增 `Workspace/SystemAgent/Capabilities/`，还是继续通过 `Skills/` 承载 capability 说明；当前推荐新增 `Capabilities/`，保留 `Skills/` 只描述 wrapper policy。
- T1.1 执行时确认 Subagent 边界是否写入 `Policies/SubagentPolicy.md`，还是整合进现有 `DocumentationManagement.md` / `AIConfigBoundary.md`；当前推荐单独 policy，避免散落。
