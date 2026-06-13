# Notes

## References

- `../../design/优化/Worktree激活设计.md`
- `../../design/优化/Worktree激活.FeatureSpec.md`
- `../../design/1/04-Git与Worktree策略.md`
- `Workspace/SystemAgent/Rules/Git.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`

## Open Questions

- SDD CLI 是否需要 `start --worktree` / `done --merge-worktree`，留给 Phase 2 单独设计。
- Hook 是否提示 worktree 使用，留给 Hook 重启 SDD；本轮不启用。
