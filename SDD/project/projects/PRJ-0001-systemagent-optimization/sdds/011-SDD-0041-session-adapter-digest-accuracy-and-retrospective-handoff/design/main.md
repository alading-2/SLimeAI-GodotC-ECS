# Session Adapter Digest Accuracy and Retrospective Handoff

## 用户原始问题

用户确认：“现在应该问题都分析完整了，可以生成 SDD 和提示词”；并补充项目级裁决：“要重构的地方完全重构绝不兼容了，session-adapter 也是，要改的地方就完全改。”

## 真实问题

PRJ-0001 二次审查已经确认三层事实：

1. `transcript.visible.md` 作为 Codex JSONL 的可见证据层方向正确。
2. digest 层仍会误导 AI：命令分类混乱、`sdd.py validate/show` 被误判为 edit、resume boilerplate 污染标题、`continue` 污染目标、最后一条 assistant message 不等于最终结论。
3. SystemAgent 消费协议不完整：Retrospective 只写“如果存在就读 efficiency.md”，没有 current digest 定位、coverage/stale/partial 判断，也没有把失败工具调用转为可执行 follow-up。

本 SDD 的目标不是给旧 digest 打补丁，而是按新契约完整重构 session-adapter 的 AI-first digest 层。旧 `index.json` / old digest schema 不作为默认读取入口；如需要读取旧产物，只能作为一次性迁移输入或只读证据。

## 设计范围

### 1. 命令分类与 schema 重建

新增或重建命令分类，不再只靠 `VALIDATION_RE` / `CODE_EDIT_RE` 两个粗粒度正则：

| Category | 含义 |
| --- | --- |
| `read` | `sed`、`rg`、`find`、`jq`、`ls`、`cat` 等读取和搜索 |
| `edit` | `apply_patch`、明确写文件、格式化或代码生成 |
| `sdd_state_write` | `sdd.py new/start/task/note/block/done/design-import/index` 等改变 SDD 状态或索引的命令 |
| `validation` | build、test、lint、`sdd.py validate`、scene validation |
| `git_inspection` | `git status`、`git diff`、`git log` 等检查，不等同测试验证 |
| `git_write` | `git add/commit/push` 等写入 |
| `external_probe` | `command -v`、`--help`、版本检查、临时探测 |
| `unknown` | 无法稳定分类 |

`verification_loop` 只从 `edit` 或 `sdd_state_write` 后的 `validation` 计算。`sdd.py validate/show/list/project-show` 不能触发 edit loop。

如现有 index schema 不适合承载分类字段，直接升级 schema version；不保留旧字段语义兼容。

### 2. Digest 默认入口重建

重建 `derived/ai-context.md`、`summary.md`、`user-requests.md`、`assistant-results.md` 的提取规则：

- 跳过 resume boilerplate 标题，例如 `A previous agent produced the plan below...`。
- 跳过纯 `continue` 作为 `User Goal`，除非没有更早目标。
- 去重相邻重复 user / assistant 事件。
- Outcome 选择最后一个 final-like assistant；找不到时写 `incomplete`，不能用中间状态伪装最终结论。
- `final_conclusion` 不再只靠“完成/验证/结果”等宽泛词命中。

### 3. Tool failure 重建

`tool-failures.md` 必须从“失败列表”升级为“失败分析入口”：

- `failure_category`：`build_failure`、`path_error`、`command_misuse`、`patch_context_mismatch`、`search_no_result`、`tool_unavailable`、`timeout`、`network_or_fetch`、`unknown`。
- `retry_count`：同类或同命令后续重试次数。
- `recovered`：`yes/no/unknown`，根据后续同目标成功、最终验证或最终结论判断。
- `final_impact`：`blocked`、`worked_around`、`not_relevant`、`unknown`。

### 4. Stale report 和覆盖判断

新增或重构 ChatHistory 覆盖检查：

- 比较 `/home/slime/.codex/sessions/YYYY/MM/DD` 原始 JSONL 数量与 `Workspace/DocsAI/ChatHistory/index.json` 对应 digest 数量。
- 输出缺失 session id、日期、source path、是否已有临时 digest。
- Retrospective / DeepThink 在分析会话记录前必须说明 coverage：`complete`、`stale`、`partial-current`、`unknown`。

### 5. Retrospective / DeepThink handoff

更新：

- `Workspace/SystemAgent/Actors/Retrospective.md`
- `.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md`
- `.ai-config/skills/systemagent-skill/systemagent-deepthink/SKILL.md`

新增 `sessionEvidence` 输出语义：digest path、source session、coverage、stale/missing、partial/current、efficiency summary、failure summary。

### 6. GitPolicy / actor 残留冲突清理

按项目规则同步修正 SystemAgent 内部旧表述：

- `Workspace/SystemAgent/Rules/Git.md` 不应继续写“默认不 push”。
- `Workspace/SystemAgent/Actors/*.md` 的 Shared constraints 不应继续硬编码“不 push”。
- `.ai-config/skills/ai/ai-feature-development/references/workflow-governance.md` 不应继续写“push 必须用户明确确认”。

新表述应指向顶层 Git Safety：AI 可按规则自动 commit/push；禁止 force push、历史改写、跨 git 边界提交和混入用户改动。

## 不做什么

- 不接常驻 hook/watch。
- 不做 Claude/OpenCode 高保真 digest。
- 不调用外部 LLM 生成摘要。
- 不删除原始 Codex JSONL。
- 不维护旧 digest / index fallback。

## 验证

完成后至少运行：

```bash
python3 -m unittest Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py
python3 -m py_compile Workspace/SystemAgent/Tools/session-adapter/session_adapter.py
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex --session <sample-jsonl> --chat-root <temp>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --chat-root <temp>
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
python3 Workspace/SDD/sdd.py validate SDD-0041
```

如修改 `.ai-config/rules` 或 `.ai-config/skills`，同步副本必须只由 `sync-ai-config.sh` 生成。
