# Session Adapter

只读的本地 AI 会话整理工具，把 Claude Code / Codex / OpenCode 等会话整理为 SlimeAI ChatHistory sidecar。

当前实现有两层：

- `index/summarize`：沿用 `codbash` 的摘要级 sidecar，兼容 Claude / Codex / OpenCode 发现入口。
- `digest-codex*`：Codex JSONL 专用 AI-first digest，直接解析 JSONL 生成 `raw/` 与 `derived/`，不从 Markdown 反解析。

第一版依赖本地 `codbash` clone 作为发现入口，默认路径：

```text
Workspace/Resources/tool/codbash
```

## Commands

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 5
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize --session <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export-codex-month --source-root /home/slime/.codex/sessions/2026/06
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex --session <id-or-path>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex-month --source-root /home/slime/.codex/sessions/2026/06 --limit 3 --skip-interrupted
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --status digest --failed-tools
```

## Output

```text
Workspace/DocsAI/ChatHistory/
  index.json
  YYYY-MM-DD-HHmm-<agent>-<slug>-<short-session-id>.md
  YYYY/MM/DD/YYYY-MM-DD-HHmm-codex-<slug>-<short-session-id>.md
  YYYY/MM/DD/YYYY-MM-DD-HH-MM-codex-<topic-slug>-<short-session-id>/
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
      files.md
      validation.md
      interruptions.md
      noise.md
      tool-failures.md
```

`index/summarize` 生成摘要级 sidecar，适合作为恢复入口。
`export-codex-month` 生成 Codex 可见 transcript 导出，按 `YYYY/MM/DD/` 分目录，并保留可见 message、tool call、tool output、event payload 和 turn context。
导出的 transcript 只在 Metadata 保留会话级 Started/Updated；每条记录标题不输出时间戳，避免给 AI 分析增加无意义噪音。

`digest-codex` / `digest-codex-month` 生成 per-session folder。文件夹名使用 `YYYY-MM-DD-HH-MM-<agent>-<topic-slug>-<short-session-id>`，小时和分钟中间有 `-`。`short-session-id` 使用去掉分隔符后的前 13 位，避免同一分钟连续 Codex session 的前 8 位相同而覆盖 folder。

短会话默认只写 `index.json` locator，不生成 folder：

```text
meaningful_user_turns < 2
AND tool_calls < 5
AND code_edit_signals == 0
AND validation_signals == 0
AND final_conclusion == false
```

启用 `--skip-interrupted` 时，中断且无最终结论、无代码修改、无验证的会话也写为 `locator-only`。单个 `digest-codex` 默认包含中断会话；批量历史整理建议显式传 `--skip-interrupted`。

## Evidence Levels

| Level | 含义 |
| --- | --- |
| `summary` | 摘要级恢复入口；会截取内容，不适合作完整复盘证据 |
| `visible-transcript` | Codex JSONL 中可见内容的 Markdown 导出；不摘要截断 message / tool output |
| `digest` | AI-first digest；默认读 `derived/ai-context.md`，再按需读 summary / tools / validation / raw |

Codex 的隐藏推理如果以 `encrypted_content` 保存，工具不会伪造解密；导出只记录 bytes 和 sha256。需要字节级完整证据时读取 Markdown 中的 `Source Path` 原始 JSONL。

## Index v3

`index.json` 会升级到 `schema_version=3`。digest entry 包含：

- source locator：`source_path`、`source_sha256`、`source_bytes`、`source_lines`
- digest locator：`folder_path`、`digest_path`、`raw_transcript_path`
- gate 字段：`digest_status`、`priority`、`skip_reasons`、`digest_reasons`
- 工具状态：`tool_success_count`、`tool_failed_count`、`tool_unknown_count`、`tool_failure_path`
- 恢复信号：`meaningful_user_turns`、`files_mentioned`、`validation_signals`、`interrupted`、`final_conclusion`

`locator-only` entry 不要求存在 `folder_path` / `digest_path` / `raw_transcript_path`。

## Tool Status

Codex digest 通过 `call_id` 配对 `function_call` 与 `function_call_output`：

- `success`：`Process exited with code 0`、明确成功/验证通过等稳定标记。
- `failed`：非零 exit code、error / exception / failure / timeout / protocol mismatch / validation failure。
- `unknown`：没有 output、output 截断或没有稳定状态字段。
- `not_applicable`：非工具事件。

只要 `tool_failed_count > 0`，就生成 `derived/tool-failures.md`。该文件只写失败摘要、命令/工具名、失败原因、raw ref 和恢复状态，不复制完整 output。

## Boundary

- 不修改 Claude Code / Codex / OpenCode 原始 session。
- 不复制完整原始 JSONL。
- `export-codex-month` 不复制原始 JSONL，只导出可见 Markdown transcript。
- `digest-codex*` 不调用外部 LLM 摘要 API，不接 hook / watcher。
- 不接 hook / watcher。
- 不自动安装 `codbash` / `codlogs` / `tracebase`。
- OpenCode 当前只保留支持路径；没有本地 OpenCode session 时不视为失败。
