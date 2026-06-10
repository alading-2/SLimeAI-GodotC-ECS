# Tasks

## Progress

- **Status**: done
- **Completed**: 7/7
- **Current**: done

## Task List

- [x] T1.1 建立 baseline 测试与样本
  - **Scope**: 读取 `session_adapter.py`、现有测试、8-10 日代表性 Codex JSONL；新增覆盖 SDD CLI 分类、resume boilerplate、重复 user request、tool failure recovered、stale report 的失败先行测试。
  - **Validation**: `python3 -m unittest Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py`
- [x] T2.1 重构 command category 和 schema
  - **Scope**: 用新分类替代旧 `VALIDATION_RE` / `CODE_EDIT_RE` 主导逻辑；`sdd.py validate/show/list/project-show` 不算 edit；必要时破坏性升级 index / digest schema。
  - **Validation**: 单测覆盖 command category、verification loop、schema 字段。
- [x] T3.1 重构 digest 默认入口
  - **Scope**: title / User Goal / Outcome / final_conclusion 去噪；跳过 resume boilerplate、纯 `continue`、重复消息和中间状态。
  - **Validation**: 单测覆盖生成的 `derived/ai-context.md`、`summary.md`、`user-requests.md`。
- [x] T4.1 重构 tool failure 与 stale report
  - **Scope**: `tool-failures.md` 增加 failure_category、retry_count、recovered、final_impact；新增或重构 ChatHistory 覆盖检查，报告 missing session。
  - **Validation**: 临时 `--chat-root` 样本运行后检查 failure 和 stale 输出。
- [x] T5.1 更新 Retrospective / DeepThink / GitPolicy handoff
  - **Scope**: 更新 `.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md`、`systemagent-deepthink/SKILL.md`、`Workspace/SystemAgent/Actors/Retrospective.md`、`Workspace/SystemAgent/Rules/Git.md`、相关 actor shared constraints 和 workflow governance 旧 push 表述。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。
- [x] T6.1 重建代表性 ChatHistory digest 并检查质量
  - **Scope**: 用临时 chat-root 重新生成 6/8-6/10 代表样本；确认 loops 不被 SDD validate/show 误报、标题/目标/结果可读、failure 分类可行动。
  - **Validation**: `digest-codex-month` 临时输出 + `list-digests` / stale report 输出摘要。
- [x] T7.1 收尾 SDD 和验证
  - **Scope**: 更新 README/progress/tasks/bdd/notes，记录验证摘要；如实现完成，按规则 commit/push。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0041`；必要时 `python3 Workspace/SDD/sdd.py validate --all` 并区分无关既有错误。
