# ChatHistory AI-first 整理与 Digest Gate 设计

> 状态：current
> 日期：2026-06-09
> 任务来源：用户检查 `Workspace/DocsAI/ChatHistory` 后确认原始 transcript 噪声过多，要求面向 AI 额外生成整理文档、按会话建文件夹、标记用户提问 / AI 回答 / 工具调用 / 代码修改；随后裁决过滤逻辑要简化：短会话默认跳过不生成 digest，中断会话是否跳过做成可选项；工具调用成功 / 失败必须区分，失败单独记录。
> 设计裁决：在 `SDD-0039` 的 `visible-transcript` 之上新增 AI-first digest 层；不删除原始来源，不让 AI 默认硬读全量 Markdown。

## Goal

本设计要解决的问题：

- 把现有 `Workspace/DocsAI/ChatHistory` 从“原始可见 transcript 仓库”升级为 AI 可快速筛选、恢复和复盘的 session digest ledger。
- 每个 session 建独立文件夹，保存 raw transcript、结构化事件、摘要、digest gate 结果和 AI 默认阅读入口。
- 让 AI 和脚本能稳定筛选：用户提问、AI 回答、工具调用、工具输出、工具失败、代码修改信号、验证信号、最终结论、中断 / 回滚、噪声。
- 生成 digest 前先做简单 `Digest Gate`：短会话默认跳过不生成整理文档；中断会话默认可选择跳过；保留 source locator 和 index skip reason。
- 给后续执行型 SDD 提供明确 schema、目录结构、Digest Gate 规则、迁移边界和验证口径。

非目标：

- 不删除 Claude Code / Codex / OpenCode 原始 session。
- 不删除现有 `visible-transcript` Markdown；迁移和归档单独确认。
- 不还原或伪造隐藏推理 / encrypted content。
- 不把 ChatHistory 变成 SystemAgent 规则事实源；它只是证据、恢复和复盘入口。
- 第一阶段不接自动 hook / watcher，不接 dashboard，不引入外部 LLM 摘要 API。
- 第一阶段不要求 Claude / OpenCode 达到 Codex JSONL 的同等保真度。

## Context Read

已读事实源：

- `Workspace/SystemAgent/Tools/session-adapter/session_adapter.py`
- `Workspace/SystemAgent/Tools/session-adapter/README.md`
- `Workspace/DocsAI/ChatHistory/index.json`
- `Workspace/DocsAI/ChatHistory/2026/06/**` 抽样 transcript
- `SDD/project/projects/PRJ-0001-systemagent-optimization/sdds/010-SDD-0039-cross-agent-session-adapter/`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/06-功能OwnerLog文档与分析流程.md`

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 本设计只更新 PRJ-0001 design 文档，不修改 `Workspace/SystemAgent/Tools/session-adapter` 实现。

## Evidence / Search Coverage

本地证据：

- `Workspace/DocsAI/ChatHistory/index.json` 当前有 64 条 entry。
- 其中 `source_lines < 100` 的短会话有 11 条，`source_lines >= 1000` 的长会话有 20 条；最短 9 行，最长 2372 行。
- Codex 2026/06 原始 JSONL 中统计到 `turn_aborted` 66 次、`thread_rolled_back` 27 次。
- 47 个 Codex session 文件包含 `turn_aborted` 或 `thread_rolled_back` 信号。
- 一个 18 行短会话仍有 113KB，因为 `session_meta.base_instructions`、开发者规则、AGENTS 注入和 skill 列表占了大量体积。
- 长会话中 `session_meta`、`turn_context`、AGENTS、skill 列表和工具输出会挤占大量 AI 阅读预算。
- Codex 原始 JSONL 已天然区分 `user_message`、`agent_message`、`function_call`、`function_call_output`、`turn_context`、`turn_aborted`、`thread_rolled_back`，应直接作为结构化输入。

外部方向校准（上一轮已完成，本设计只引用结论）：

- OpenTelemetry Logs Data Model 支持把日志视作结构化 record，字段包括 severity、attributes、trace/span correlation。
- .NET logging 的 high-performance / source-generated logging 强调强类型、结构化、减少关闭日志时的无用构造。
- OpenAI Agents SDK tracing 把 agent run 中的 tool call、handoff、LLM span 等作为 trace 事件。
- LangGraph memory 管理建议对长对话做 trim / summarize，而不是无限塞完整历史。

本设计采纳这些思想，但不引入外部平台或新依赖。

## Problem Reality Check

问题真实存在：

- 现有 ChatHistory 已经解决“能导出可见 transcript”，但还没有解决“AI 怎样快速判断这次会话值不值得读”。
- `source_lines/source_bytes` 只能粗略表示体积，不能表示有效工作量、是否中断、是否有工具失败、是否有代码改动、是否有验证和是否有结论。
- 短会话可能只是启动后中断、回滚或两三句话结束；直接生成 digest 会继续制造文档噪声。
- 长会话如果不拆分，AI 仍会从系统注入、工具输出和大段上下文里硬找结论。
- 当前文件名的 `HHmm` 可读性弱，且 slug 来自首段文本时经常变成路径、skill 名或过长任务描述，难以判断主题。

不是问题或不应过度处理的点：

- 原始可见 transcript 本身仍有证据价值，不应删除。
- 中断不一定代表无价值。长会话中断后继续推进、或中断前已有代码改动 / 验证证据，仍可能重要。
- 文件大小大不等于价值高；工具输出很大但没有结论的会话不应仅靠体积进入 AI 默认阅读入口。

## Idea Check

用户当前思路成立：

- 每个记录新建文件夹是正确方向。一个 session 不应只有一个 Markdown，应有 manifest、raw、derived digest 和可筛选事件。
- 标记用户提问、AI 回答、工具调用、代码修改能显著降低 AI 复盘成本。
- 过滤短会话必须简单，优先作为“是否生成 digest”的前置 gate，而不是复杂价值评分。
- “是否中断”必须能参与 gate，尤其是短会话中断通常是低价值信号；但中断过滤应是可选开关，避免误跳过有结果的长会话。
- Log 设计包的 AI-first 思想可借鉴，但必须简化，不能把完整 runtime log 管道搬到 ChatHistory。

需要校正：

- 代码修改不能只靠消息文本判断。第一阶段只能标记为 `explicit` / `inferred` / `unknown`：
  - `explicit`：有 `apply_patch`、文件写入命令、明确 git diff/status 输出证明。
  - `inferred`：工具命令或 assistant 消息提到修改，但缺少直接证据。
  - `unknown`：无法判断。
- AI 回答不应全部进入摘要。只保留用户可见的阶段更新、最终结论、关键裁决和恢复提示。
- 工具输出不应完整进入 `ai-context.md`，应进入 `raw/` 或 `derived/tools.md`，摘要入口只保留失败、验证和关键路径。
- 工具调用必须区分成功、失败和未知。失败工具调用单独写入 `derived/tool-failures.md`，并在 `index.json` 里保留计数。

## Target Shape

ChatHistory 目标结构：

```text
Workspace/DocsAI/ChatHistory/
  index.json
  2026/
    06/
      09/
        2026-06-09-15-28-codex-log-hard-cutover-sdd-019eab48a8077/
          manifest.json
          raw/
            transcript.visible.md
            source-locator.md
          derived/
            events.jsonl
            ai-context.md
            summary.md
            user-requests.md
            assistant-results.md
            tools.md
            tool-failures.md
            files.md
            validation.md
            interruptions.md
            noise.md
```

命名规则：

```text
YYYY-MM-DD-HH-MM-<agent>-<topic-slug>-<short-session-id>/
```

规则：

- 小时和分钟中间使用 `-`，例如 `2026-06-09-15-28`。
- `short-session-id` 使用去掉分隔符后的前 13 位。实测 2026/06 Codex 历史里存在同一分钟、同前 8 位 session id 的连续会话；8 位会导致 folder 覆盖。
- `topic-slug` 不直接使用完整首句；优先从用户目标、显式文件路径、SDD id、owner 名、最终摘要中提取。
- slug 太弱时使用 fallback：`session`、`deepthink`、`sdd-0040-log`、`chat-history-digest`。
- 旧 `YYYY-MM-DD-HHmm-...md` 文件可保留；迁移阶段由 `manifest.json.legacy_paths` 记录旧路径。

## Raw / Derived Boundary

| 层 | 文件 | 职责 | AI 默认读取 |
| --- | --- | --- | --- |
| Source locator | `raw/source-locator.md` | 原始 JSONL、sha256、bytes、session id、不可读 encrypted payload 说明 | 否 |
| Raw visible transcript | `raw/transcript.visible.md` | 保留现有可见 Markdown transcript，不做摘要截断 | 否，除非需要审查证据 |
| Structured events | `derived/events.jsonl` | 一行一条结构化事件，供脚本和 AI 精查 | 按需 |
| AI context | `derived/ai-context.md` | AI 默认恢复入口，只保留高价值摘要和下一步 | 是 |
| Summary | `derived/summary.md` | 会话目标、结果、时间线、digest gate 和 priority 解释 | 是 |
| Topic files | `derived/*.md` | 用户请求、工具、文件、验证、中断、噪声分桶 | 按需 |

AI 默认阅读顺序：

1. `Workspace/DocsAI/ChatHistory/index.json`
2. 目标 session 的 `derived/ai-context.md`
3. `derived/summary.md`
4. 相关 `derived/tools.md` / `files.md` / `validation.md` / `interruptions.md`
5. 必要时才读 `raw/transcript.visible.md`

## Structured Event Schema

`derived/events.jsonl` 每行一个 event。

基础字段：

```json
{
  "schema_version": 1,
  "session_id": "019eab48-a807-7a53-8517-8113be876303",
  "event_index": 42,
  "source_record_type": "response_item",
  "source_payload_type": "function_call",
  "role": "assistant",
  "event_kind": "tool_call",
  "phase": "implementation",
  "summary": "sed -n 读取 Log README",
  "text_ref": "raw/transcript.visible.md#000042",
  "tool_name": "exec_command",
  "tool_status": "success",
  "evidence_level": "explicit"
}
```

`event_kind` 枚举：

```text
session_meta
system_context
user_request
assistant_message
assistant_final
tool_call
tool_output
code_edit_signal
validation_signal
file_reference
sdd_reference
interruption
rollback
noise
unknown
```

`phase` 枚举：

```text
bootstrap
requirements
deepthink
planning
research
implementation
validation
final
interrupted
unknown
```

`evidence_level` 枚举：

```text
explicit
inferred
weak
unknown
```

`tool_status` 枚举：

```text
success
failed
unknown
not_applicable
```

第一版成功 / 失败判定：

- `failed`：tool output 中能解析到非零 exit code、`Process exited with code <non-zero>`、明确 error / exception / failure，或工具调用自身返回错误。
- `success`：tool output 明确 `Process exited with code 0`、验证通过、CLI 返回成功，或工具协议有成功状态。
- `unknown`：没有 output、output 被截断、工具类型没有稳定状态字段，或只能看到调用参数。

失败工具调用不代表任务失败；它只是复盘时必须优先看的证据。

## Digest Gate

### 目标

过滤逻辑必须简单。第一版先判断“是否生成 digest 文件夹和整理文档”，再对已生成 digest 做轻量优先级。不要先实现复杂分数模型。

### 默认 gate 规则

默认只生成两类输出：

- `digest`：生成 per-session folder、`manifest.json` 和 `derived/*`。
- `locator-only`：不生成整理文档，只在 `index.json` 保留 source locator、基础 metadata 和 skip reason。

默认跳过短会话：

```text
meaningful_user_turns < 2
AND tool_calls < 5
AND code_edit_signals == 0
AND validation_signals == 0
AND final_conclusion == false
```

满足上面条件时：

```json
{
  "digest_status": "locator-only",
  "skip_reasons": ["too_short"]
}
```

短会话不删除，不生成 `derived/*`。如后续用户要查，仍可通过 source locator 回到 raw session。

### 可选中断 gate

执行命令应提供开关：

```bash
--skip-interrupted
--include-interrupted
```

默认建议：批量整理历史时使用 `--skip-interrupted`；单个指定 session 时默认 `--include-interrupted`。

中断信号来源：

- Codex JSONL `payload.type == "turn_aborted"`
- Codex JSONL `payload.type == "thread_rolled_back"`
- user message 中的 `<turn_aborted>`
- assistant final 缺失且最后事件停在 tool call / reasoning / aborted
- source 中存在 rollback 或 resume ghost 相关事件

使用 `--skip-interrupted` 时，满足以下条件默认不生成 digest：

```text
interrupted == true
AND final_conclusion == false
AND code_edit_signals == 0
AND validation_signals == 0
```

skip reason：

```json
{
  "digest_status": "locator-only",
  "skip_reasons": ["interrupted", "no_final_conclusion"]
}
```

如果中断后仍有代码修改、验证或最终结论，不应自动跳过；只在 `derived/interruptions.md` 记录风险。

### 轻量优先级

对已经生成 digest 的会话，只保留简单优先级，不做复杂 0-100 分：

```text
priority: low | normal | high
```

建议规则：

- `high`：有文件修改或 SDD/DocsAI owner 变更，且有验证或最终结论。
- `normal`：有明确目标和最终结论，或有多个工具调用但没有长期事实源改动。
- `low`：有 digest 价值但缺验证、缺最终结论或噪声很高。

如确实需要数值，可后续再从样本校准，不作为第一版范围。

### Gate 输出字段

```json
{
  "digest_status": "digest",
  "priority": "normal",
  "skip_reasons": [],
  "meaningful_user_turns": 3,
  "tool_calls": 18,
  "tool_success_count": 15,
  "tool_failed_count": 2,
  "tool_unknown_count": 1,
  "interrupted": false
}
```

`locator-only` entry 不要求存在 `folder_path` / `digest_path`；必须保留 `source_path`、`source_sha256`、`source_bytes`、`source_lines`、`skip_reasons`。

### 工具失败记录

工具调用失败必须集中记录：

```json
{
  "tool_failed_count": 2,
  "tool_failure_path": "Workspace/DocsAI/ChatHistory/.../derived/tool-failures.md"
}
```

失败工具摘要不替代 raw output，只保存命令 / 工具名、失败原因、exit code 或错误摘要、raw ref、是否已恢复。

## Index Schema v3

`Workspace/DocsAI/ChatHistory/index.json` 建议升级到 schema v3。

新增字段：

```json
{
  "schema_version": 3,
  "entries": [
    {
      "id": "codex:019eab48-a807-7a53-8517-8113be876303",
      "source_tool": "codex",
      "session_id": "019eab48-a807-7a53-8517-8113be876303",
      "title": "SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log",
      "topic_slug": "sdd-0040-log-hard-cutover",
      "folder_path": "Workspace/DocsAI/ChatHistory/2026/06/09/2026-06-09-15-28-codex-sdd-0040-log-hard-cutover-019eab48a8077",
      "digest_path": "Workspace/DocsAI/ChatHistory/2026/06/09/2026-06-09-15-28-codex-sdd-0040-log-hard-cutover-019eab48a8077/derived/ai-context.md",
      "raw_transcript_path": "Workspace/DocsAI/ChatHistory/2026/06/09/2026-06-09-15-28-codex-sdd-0040-log-hard-cutover-019eab48a8077/raw/transcript.visible.md",
      "legacy_paths": [
        "Workspace/DocsAI/ChatHistory/2026/06/09/2026-06-09-1528-codex-sddprojectprojectsprj-0002-ecs-framework-refacto-019eab48a8077.md"
      ],
      "digest_status": "digest",
      "priority": "normal",
      "skip_reasons": [],
      "digest_reasons": [
        "has_tool_calls",
        "has_sdd_reference",
        "has_final_conclusion",
        "has_validation_signal"
      ],
      "meaningful_user_turns": 1,
      "assistant_messages": 15,
      "tool_calls": 60,
      "tool_outputs": 60,
      "tool_success_count": 55,
      "tool_failed_count": 3,
      "tool_unknown_count": 2,
      "tool_failure_path": "Workspace/DocsAI/ChatHistory/2026/06/09/2026-06-09-15-28-codex-sdd-0040-log-hard-cutover-019eab48a8077/derived/tool-failures.md",
      "turn_aborted_count": 0,
      "thread_rolled_back_count": 0,
      "interrupted": false,
      "code_edit_signals": {
        "explicit": 1,
        "inferred": 0,
        "unknown": 0
      },
      "validation_signals": 3,
      "final_conclusion": true,
      "duration_seconds": 622,
      "noise_ratio_estimate": 0.42,
      "recommended_reading_order": [
        "derived/ai-context.md",
        "derived/summary.md",
        "derived/validation.md",
        "raw/transcript.visible.md"
      ],
      "topics": [
        "systemagent",
        "session-adapter",
        "log",
        "sdd"
      ],
      "sdd_ids": [
        "SDD-0040"
      ],
      "files_mentioned": [
        "SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log"
      ]
    }
  ]
}
```

## Digest Files

### `manifest.json`

职责：

- 固定 session identity。
- 记录 source path、sha256、bytes、lines。
- 记录 legacy paths 和生成器版本。
- 记录 digest 生成时间和 schema。

### `derived/ai-context.md`

AI 默认入口，长度应严格控制。

建议结构：

```markdown
# <topic>

## Verdict

- digest_status:
- priority:
- skip_reasons:
- why_read:
- why_skip:

## User Goal

## Outcome

## Key Decisions

## Files / SDD / DocsAI

## Tool And Validation Evidence

## Interruptions

## Resume Prompt
```

规则：

- 不复制完整 tool output。
- 不复制 system/developer/AGENTS。
- 中断必须明确写：次数、位置、是否影响最终结论。
- 没有最终结论必须写 `Outcome: no final conclusion`。

### `derived/summary.md`

面向人工和 Reviewer 的复盘摘要：

- 会话目标。
- 时间线。
- 关键行动。
- 结果。
- Digest Gate / priority 解释。
- 未解决问题。

### `derived/tools.md`

工具摘要：

- tool call 计数。
- 成功 / 失败 / 未知计数。
- 失败命令。
- 验证命令。
- 大输出截断说明。
- 需要读 raw 的工具输出定位。

### `derived/tool-failures.md`

工具失败摘要。只要 `tool_failed_count > 0` 就必须生成。

建议结构：

```markdown
# Tool Failures

## Summary

- failed:
- unknown:

## Failures

| Event | Tool | Command / Action | Exit / Error | Why It Matters | Raw Ref |
| --- | --- | --- | --- | --- | --- |
```

规则：

- 失败工具调用单独集中，AI 排查时优先读这里。
- 非零 exit code、异常、超时、验证失败都进入本文件。
- 如果失败是预期或已恢复，`Why It Matters` 写明 `recovered`。
- 不复制完整输出，只给摘要和 raw ref。

### `derived/files.md`

文件和修改信号：

- 明确修改文件。
- 可能修改文件。
- 只读取文件。
- git status / diff 证据。
- `explicit` / `inferred` / `unknown` 标记。

### `derived/validation.md`

验证摘要：

- 命令。
- 结果。
- error/warning。
- 未验证原因。
- 受既有工作区状态影响的边界。

### `derived/interruptions.md`

中断摘要：

- `turn_aborted_count`
- `thread_rolled_back_count`
- first / last interrupted event index
- 是否短会话中断
- 是否中断后恢复并完成
- 对 `digest_status` / `priority` 的影响

### `derived/noise.md`

噪声摘要：

- bootstrap / system / developer / AGENTS / skill 注入估算占比。
- 重复工具输出。
- 过长 tool output。
- encrypted payload 占位。
- 建议跳过的 raw ranges。

## Noise Rules

默认噪声：

- `session_meta.base_instructions`
- developer instructions
- AGENTS.md full injection
- `<permissions instructions>`
- `<collaboration_mode>`
- `<skills_instructions>`
- `<skill>` full body injection
- `turn_context` 中重复的环境上下文
- encrypted reasoning placeholder
- 超大 function_call_output 的完整输出

噪声不等于删除。处理方式：

- raw 保留。
- events 标记为 `noise` 或 `system_context`。
- ai-context 不复制。
- index 记录 `noise_ratio_estimate`。

## Code Edit Signal Detection

第一版检测规则：

`explicit`：

- `apply_patch` 成功。
- `exec_command` 中出现明确写入命令且输出证明成功。
- `git status --short` / `git diff --stat` / `git diff --name-only` 在会话中显示目标文件变化。
- SDD CLI 写操作输出创建 / 更新路径。

`inferred`：

- assistant 消息说“我已修改 / 已生成 / 已更新”，但没有工具输出支撑。
- 工具命令可能写入，例如 `python3 Workspace/SDD/sdd.py task ...`，但没有后续 status 证据。

`unknown`：

- 只有用户要求修改。
- 只有读取命令。
- 只有计划。

## Final Conclusion Detection

`final_conclusion` 为 true 需要满足任一条件：

- transcript 中存在最后 assistant final message。
- assistant 消息包含明确完成/阻塞结论，并列出产物或验证。
- SDD progress / tasks 有对应完成记录。

以下不算最终结论：

- 中间 `agent_message` 状态更新。
- `update_plan` 输出。
- tool output 最后一行成功但 assistant 没总结。
- 会话以 `turn_aborted` / `thread_rolled_back` 结束。

## Recommended Implementation Phases

### Phase 0：Design Freeze

交付：

- 本设计文档。
- 更新 `会话记录适配器参考设计/INDEX.md`。
- 更新 PRJ-0001 `design/INDEX.md`、`Core/roadmap.md`、`Core/progress.md`。

### Phase 1：Codex Digest Prototype

范围：

- 只处理 Codex JSONL。
- 新增命令建议：`digest-codex --session <id>`、`digest-codex-month --source-root ...`。
- 默认短会话不生成 digest，只写 `locator-only` index entry。
- 批量历史整理默认允许 `--skip-interrupted`，单 session 默认允许 `--include-interrupted`。
- 生成 folder + manifest + derived files。
- 工具调用必须统计 success / failed / unknown；失败写入 `derived/tool-failures.md`。
- 不删除旧 Markdown。

验证：

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex --session <id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex-month --source-root /home/slime/.codex/sessions/2026/06 --limit 3 --skip-interrupted
python3 -m py_compile Workspace/SystemAgent/Tools/session-adapter/session_adapter.py
```

### Phase 2：Index v3 和筛选命令

范围：

- `index.json` 升级 schema v3。
- 新增筛选命令建议：

```bash
session_adapter.py list-digests --status digest
session_adapter.py list-digests --status locator-only
session_adapter.py list-digests --priority high
session_adapter.py list-digests --failed-tools
session_adapter.py list-digests --interrupted
session_adapter.py list-digests --topic log
```

### Phase 3：历史迁移 / 归档策略

范围：

- 迁移现有 64 个 visible transcript 到 folder。
- 旧 Markdown 是否移动到 `raw/` 或保留 legacy path 需用户确认。
- 不在 Phase 1 自动删除任何历史文件。

### Phase 4：Claude / OpenCode Digest

范围：

- 基于各自可见导出能力补结构化 events。
- 字段不足时标记 `source_confidence=partial`。

## Options

### Option A：只补 index gate，不建文件夹

优点：

- 改动最小。
- 能快速跳过短会话和中断会话。

缺点：

- AI 仍要读大 Markdown。
- 没有分桶 digest 文件。
- 无法解决 raw 和 derived 混在一起的问题。

适合：临时止血。

### Option B：每个 session 建 folder + digest，但保留旧 Markdown

优点：

- 解决 AI 默认阅读入口。
- 迁移安全，不删除旧证据。
- 可逐步处理 Codex，再处理 Claude / OpenCode。
- 与 Log 设计的 `raw / analysis / ai-context` 思路一致，但复杂度可控。

缺点：

- 会新增较多 derived 文件。
- 需要 index v3 和迁移策略。

适合：推荐方案。

### Option C：引入 tracebase / dashboard 级系统

优点：

- 分析能力强，可能支持 scorecard、context waste、redaction。

缺点：

- 范围过大。
- 本地 clone 当前不是零依赖可运行。
- 会把当前问题从“整理 ChatHistory”扩大为“引入 trace 平台”。

适合：后续独立研究，不适合当前执行。

## Recommendation

采用 Option B：

```text
Codex first folder digest
  + index v3 digest_status / priority
  + simple digest gate
  + optional interrupted skip
  + tool success/failure split
  + raw/derived 分层
  + 旧 Markdown 保留为 legacy evidence
```

原因：

- 符合 AI-first：AI 默认读 digest，不读 raw。
- 符合用户新增要求：过滤逻辑简单，短会话默认不生成整理文档；中断跳过可选；工具失败单独记录。
- 与 SDD-0039 兼容：它完成 visible transcript，本设计新增 digest 层。
- 与 Log 设计兼容：借鉴分桶、summary、noise、ai-context，但不搬完整 log 系统。

## DesignCritic Review

Assumptions：

- Codex JSONL 的 event type 足够支撑第一版结构化事件。
- 用户接受第一版 Codex first，不要求 Claude / OpenCode 同时达到同等保真。
- 旧 transcript 可保留，不需要立即删除或移动。
- Digest Gate 用于判断是否生成整理文档；priority 只表达 AI 阅读优先级，不判断任务质量或 agent 能力。

Missing Context：

- Claude Code 和 OpenCode 的中断/回滚事件结构尚未实测。
- 旧 Markdown 是否迁移为 `raw/transcript.visible.md` 需要用户最终裁决。
- 工具失败识别第一版依赖 exit code / 明确错误文本 / 工具协议状态，无法覆盖所有语义失败。

Design Defects to Avoid：

- 把 `source_bytes` 当主要 gate 条件。
- 把中断作为硬删除条件。
- 为了排序做过度复杂模型，导致第一版难以落地。
- 只记录工具调用次数，不区分成功 / 失败。
- AI 生成自由摘要而没有结构化证据引用。
- 生成大量 digest 文件但不更新 index，导致更难找。
- 让 `ai-context.md` 复制完整 transcript，重新制造噪声。

Better Option Checked：

- 只更新 `index.json`：更小，但解决不了 AI 硬读全文。
- 直接删除短会话：危险，会丢失 source locator 和偶发重要短任务；应写 locator-only。
- 引入 tracebase：过重，不适合当前 ChatHistory 体积治理。

Recommendation：

- 新建执行型 SDD，例如 `ChatHistory AI-first Session Digest`。
- Phase 1 只做 Codex digest prototype。
- 过滤逻辑用 `Digest Gate`，不是复杂 `value_score`。
- 默认保留旧 Markdown，生成新 folder，确认后再迁移旧路径。

## Must Confirm

### 思路问题

- 暂无。当前“在 visible transcript 之上新增 digest 层”的方向成立。

### 信息缺口

- Claude / OpenCode 是否要在第一版同时做 digest gate？为什么问：两者事件结构未实测，强行一起做会扩大 parser 风险。默认值：第一版 Codex first，Claude / OpenCode 只保留后续任务。

### 决策未定

- 旧 `YYYY-MM-DD-HHmm-...md` 是否迁移进新 folder 的 `raw/transcript.visible.md`？为什么问：迁移会产生大量路径变化。默认值：不移动旧文件；新 folder 先记录 `legacy_paths`。
- 是否创建新 SDD 执行本设计？为什么问：这是跨工具、索引、历史导出结构和文档治理的中型任务。默认值：创建 PRJ-0001 子 SDD，标题为 `ChatHistory AI-first Session Digest`。

## Should Confirm

- 短会话是否默认不生成 digest，仅保留 locator-only index entry？默认值：是。
- 批量整理历史时是否默认跳过中断且无结论/无代码/无验证的会话？默认值：是；单个 session 指定时默认包含。
- 工具失败是否必须生成 `derived/tool-failures.md`？默认值：是。
- folder 命名是否采用 `YYYY-MM-DD-HH-MM-<agent>-<topic>-<id>`？默认值：接受。

## Defaults I Will Use

用户不补充时，后续执行采用：

- 新建 PRJ-0001 子 SDD，不直接修改实现。
- Codex first。
- 不删除、不移动旧 Markdown。
- `index.json` 升级 schema v3。
- 只有通过 `Digest Gate` 的 session 建 folder。
- 短会话默认不生成 digest，只保留 locator-only source locator。
- 批量整理默认跳过中断且无结论/无代码/无验证的会话；单个 session 默认包含中断会话。
- 工具调用按 `success / failed / unknown` 统计，失败单独写 `derived/tool-failures.md`。
- AI 默认读 `derived/ai-context.md` 和 `derived/summary.md`。

## Artifact Updates

本轮设计应同步更新：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/roadmap.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/progress.md`

后续执行型 SDD 创建后，应把本设计复制 / 引用到该 SDD 的 `design/`，并把用户裁决写入该 SDD `progress.md`。
