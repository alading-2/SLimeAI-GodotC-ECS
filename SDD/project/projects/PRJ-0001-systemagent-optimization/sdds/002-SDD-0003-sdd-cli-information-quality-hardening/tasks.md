# Tasks

## Progress

- **Status**: done
- **Completed**: 12/12
- **Current**: done

## Task List

- [x] T1.1 为 README 覆盖 BUG 写失败测试
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0003`
- [x] T1.2 拆分保存职责，README 改为字段级 patch
  - **Validation**: `python3 -m unittest Workspace.SDD.tests.test_sdd_cli.SDDCliTests.test_readme_summary_survives_state_changes`
- [x] T1.3 验证 start/task/note/block/done 不覆盖手写 README 摘要
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests`
- [x] T2.1 为 done 继承 Latest Resume 写失败测试
  - **Validation**: `python3 -m unittest Workspace.SDD.tests.test_sdd_cli.SDDCliTests.test_done_inherits_latest_resume_when_no_conclusion_is_given`
- [x] T2.2 增加 done --conclusion 和 --next-action
  - **Validation**: `python3 Workspace/SDD/sdd.py done --help`
- [x] T2.3 验证 done 记录 validation 但保留核心结论
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests`
- [x] T3.1 增加模板残留、弱 README、弱 resume、弱 validation 检查
  - **Validation**: `python3 -m unittest Workspace.SDD.tests.test_sdd_cli.SDDCliTests.test_validate_reports_quality_warnings_and_template_errors`
- [x] T3.2 增加 Key Files、artifacts、notes、progress 冗余风险 warning
  - **Validation**: `python3 -m unittest Workspace.SDD.tests.test_sdd_cli.SDDCliTests.test_validate_reports_redundancy_warnings`
- [x] T3.3 更新 SDDFormat、CLI、ValidationRules 文档
  - **Validation**: `git diff --check -- Workspace/SDD/docs`
- [x] T4.1 更新 `.ai-config/skills/sdd` 源 skill
  - **Validation**: `git diff --check -- .ai-config/skills/sdd`
- [x] T4.2 运行 ai-config sync 和 skill-test
  - **Validation**: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all`
- [x] T5.1 运行最终验证并归档本轮 SDD 证据
  - **Validation**: `python3 -m unittest discover Workspace/SDD/tests && python3 Workspace/SDD/sdd.py validate --all`
