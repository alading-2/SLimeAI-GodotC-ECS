# Notes

## References

- `../../design/优化/SDD精简设计.md`
- `../../design/优化/FeatureSpec-功能实现规格设计.md`
- `../../design/优化/SDD精简与FeatureSpec集成.FeatureSpec.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`
- `Workspace/SDD/docs/ValidationRules.md`
- `Workspace/SystemAgent/Docs/06-SDD系统详解.md`
- `Workspace/SystemAgent/Docs/11-FeatureSpec功能实现规格.md`

## Validation Commands

- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py validate SDD-0042`
- `python3 Workspace/SDD/sdd.py validate --all`
- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

## Open Questions

- TDD Data 试点与 Log/Validation evidence plane 的落地顺序留给后续 SDD 决定。
- Worktree skill 不进入本 SDD，后续单独创建执行任务。
