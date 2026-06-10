# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI or workflow behavior.

## Scenarios

### Scenario: SDD validate is not counted as an edit

Given a Codex session contains `apply_patch`, `python3 Workspace/SDD/sdd.py validate SDD-0041`, and `python3 Workspace/SDD/sdd.py show SDD-0041`
When session-adapter builds a digest
Then `sdd.py validate/show` are categorized as validation/read, not edit
And verification loops are only counted after real edit or SDD state write

### Scenario: Resume boilerplate does not become the title

Given a Codex session starts with `A previous agent produced the plan below...`
And the real user request later names `SDD-0041`
When session-adapter renders `derived/ai-context.md`
Then the title and User Goal use the real task, not the resume boilerplate or pure `continue`

### Scenario: Tool failures explain recovery and impact

Given a tool command fails and a later command succeeds on the same target
When session-adapter renders `derived/tool-failures.md`
Then the failure has `failure_category`, `retry_count`, `recovered=yes`, and `final_impact=worked_around`

### Scenario: ChatHistory stale state is visible

Given `/home/slime/.codex/sessions/2026/06/10` has source JSONL files
And `Workspace/DocsAI/ChatHistory/index.json` has no matching digest entries
When the stale report command runs
Then it reports missing session ids and marks coverage as `stale`

### Scenario: Retrospective receives session evidence

Given a task asks for retrospective or dialogue analysis
When the `systemagent-retrospective` skill is used
Then it locates or reports the current digest coverage
And outputs `sessionEvidence` with digest path, source session, coverage, efficiency summary, and failure summary

## Validation Evidence

- `python3 -m unittest Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py` 覆盖 SDD CLI 分类、resume/continue 去噪、重复消息去重、failure recovered 和 stale report。
- `/tmp/sdd-0041-chat-v2` 临时 digest 覆盖 2026-06-08/09/10 代表样本，`list-digests` 显示 schema v4 的 loops/failure 摘要。
- `stale-report --source-root /home/slime/.codex/sessions/2026/06/10 --json` 输出 `coverage=stale`、source/digest/missing counts 和 missing session ids。
