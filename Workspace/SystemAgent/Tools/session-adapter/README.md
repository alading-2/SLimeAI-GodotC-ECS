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
```

## Output

```text
Workspace/DocsAI/ChatHistory/
  index.json
  YYYY-MM-DD-HHmm-<agent>-<slug>-<short-session-id>.md
```

## Boundary

- 不修改 Claude Code / Codex / OpenCode 原始 session。
- 不复制完整原始 transcript。
- 不接 hook / watcher。
- 不自动安装 `codbash` / `codlogs` / `tracebase`。
- OpenCode 当前只保留支持路径；没有本地 OpenCode session 时不视为失败。
