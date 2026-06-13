# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 补失败先行测试，锁定旧模板、CLI timeline 写入、BDD validate 和 shared design refs 误判
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests`
- [x] T2.1 改 SDD 模板和 progress 写入，使新建 SDD 默认使用 State / Decisions / Validation 状态面板
  - **Validation**: `python3 Workspace/SDD/sdd.py new ... --root <tmp>` 生成物检查
- [x] T3.1 改 `start/block/done/note/task` 写入行为，停止默认追加 task command timeline
  - **Validation**: CLI 单元测试覆盖状态流转命令
- [x] T4.1 改 validate 规则，支持 FeatureSpec Source / Executed features / not-required reason，并放宽项目子 SDD shared refs
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0042`
- [x] T5.1 同步 SDD docs、SystemAgent docs/rules 和 `.ai-config/skills` 源
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- [x] T6.1 汇总验证并更新 SDD-0042 / PRJ-0001 状态
  - **Validation**: `python3 Workspace/SDD/sdd.py validate --all` + `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
