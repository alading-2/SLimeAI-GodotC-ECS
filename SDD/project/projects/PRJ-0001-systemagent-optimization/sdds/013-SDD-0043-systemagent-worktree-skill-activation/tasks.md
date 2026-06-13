# Tasks

## Progress

- **Status**: done
- **Completed**: 5/5
- **Current**: done

## Task List

- [x] T1.1 补 SDD 入口、项目级 FeatureSpec 和 worktree record
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0043`
- [x] T2.1 新增 `systemagent-worktree` skill 源，覆盖 create/list/status/switch/merge/clean
  - **Validation**: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [x] T3.1 同步 SystemAgent docs / registry / skill 副本
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- [x] T4.1 更新 PRJ-0001 roadmap / progress / project metadata
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all`
- [x] T5.1 汇总验证、关闭 SDD-0043 并准备提交
  - **Validation**: `git diff --check`
