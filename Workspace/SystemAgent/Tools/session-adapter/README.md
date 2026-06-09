# Session Adapter

只读的本地 AI 会话整理工具，把 Claude Code / Codex / OpenCode 等会话整理为 SlimeAI ChatHistory sidecar。

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
```

## Output

```text
Workspace/DocsAI/ChatHistory/
  index.json
  YYYY-MM-DD-HHmm-<agent>-<slug>-<short-session-id>.md
  YYYY/MM/DD/YYYY-MM-DD-HHmm-codex-<slug>-<short-session-id>.md
```

`index/summarize` 生成摘要级 sidecar，适合作为恢复入口。
`export-codex-month` 生成 Codex 可见 transcript 导出，按 `YYYY/MM/DD/` 分目录，并保留可见 message、tool call、tool output、event payload 和 turn context。
导出的 transcript 只在 Metadata 保留会话级 Started/Updated；每条记录标题不输出时间戳，避免给 AI 分析增加无意义噪音。

## Evidence Levels

| Level | 含义 |
| --- | --- |
| `summary` | 摘要级恢复入口；会截取内容，不适合作完整复盘证据 |
| `visible-transcript` | Codex JSONL 中可见内容的 Markdown 导出；不摘要截断 message / tool output |

Codex 的隐藏推理如果以 `encrypted_content` 保存，工具不会伪造解密；导出只记录 bytes 和 sha256。需要字节级完整证据时读取 Markdown 中的 `Source Path` 原始 JSONL。

## Boundary

- 不修改 Claude Code / Codex / OpenCode 原始 session。
- 不复制完整原始 transcript。
- `export-codex-month` 不复制原始 JSONL，只导出可见 Markdown transcript。
- 不接 hook / watcher。
- 不自动安装 `codbash` / `codlogs` / `tracebase`。
- OpenCode 当前只保留支持路径；没有本地 OpenCode session 时不视为失败。
