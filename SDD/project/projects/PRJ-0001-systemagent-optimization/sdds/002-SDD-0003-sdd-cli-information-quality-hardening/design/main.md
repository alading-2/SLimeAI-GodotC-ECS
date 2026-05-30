# SDD CLI Information Quality Hardening

## Goal

让 SDD CLI 从“能维护结构”提升到“能保护高价值信息”。本轮重点不是让 SDD 记录更多内容，而是修复会破坏上下文胶囊可信度的写入边界，并让 `validate` 能提醒空壳完成、弱证据和冗余膨胀。

## Context

- 完整设计统一引用项目级共享设计 `../../design/SDD/SDD-CLI信息质量加固设计.md`，本文件只保留任务级摘要。
- 当前 `save_instance()` 会整体重建 `README.md`，导致人工摘要和恢复点被覆盖。
- 当前 `done` 写入固定结论“SDD 已进入 done。”，会稀释或替换真正的完成结论。
- 当前 `validate` 主要检查结构一致性，缺少模板残留、弱摘要、弱 validation、done 追溯入口和冗余风险提醒。
- 约束：不引入第三方依赖，不大改目录结构，不实现可选 `repair-readme`、`strict-quality` 或 evidence 子命令。

## Design

### README 写入边界

`new` 仍负责创建初始 README。后续保存不再整体调用 `build_readme()` 覆盖文件，而是拆分为 `save_metadata()`、`update_tasks_header()` 和 `patch_readme_fields()`：只更新 `Status`、`Updated`、`Current Task`、`Open Blockers` 等 CLI 拥有字段，保留 `What This SDD Is About` 与人工维护的正文。

### Done 结论策略

`done` 追加 validation 记录并移动状态，但默认继承当前 `Latest Resume` 的 `Last Conclusion` 与合理的 `Next Action`。新增 `--conclusion`、`--next-action` 参数用于显式覆盖最终结论；没有参数时不制造泛化结论。

### Validate 信息质量

新增 `SDD015` 至 `SDD024`。结构错误仍为 error；done 状态保留模板残留为 error；弱 README、弱 Latest Resume、弱 validation、done 缺追溯入口和冗余风险先作为 warning，避免把 SDD 推向形式主义。

### 文档与 skill

更新 `SDDFormat.md`、`CLI.md`、`ValidationRules.md` 说明核心证据、核心文件和 README 边界。更新 `.ai-config/skills/sdd` 源 skill，强调 readiness gate、progress 只写核心结论、完成前必须有新鲜验证证据。

## Verification

- `python3 -m unittest discover Workspace/SDD/tests`
- `python3 Workspace/SDD/sdd.py validate --all`
- `python3 -m py_compile Workspace/SDD/sdd.py Workspace/SDD/tests/test_sdd_cli.py`
- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all`
- `git diff --check`
