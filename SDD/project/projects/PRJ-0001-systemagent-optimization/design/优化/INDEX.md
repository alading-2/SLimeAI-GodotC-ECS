# SystemAgent 优化补充设计索引

> 状态：current
> 定位：PRJ-0001 完成后的补充优化裁决，记录新发现的关键问题和后续 SDD 候选。

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `2026-06-08-SystemAgent工作流内化与会话记录优化.md` | design | current | 2026-06-08 | 取消外层 agent / Warp 改造方向，聚焦 SystemAgent 内化、跨 Claude Code / Codex / OpenCode 会话整理和只读资料 subagent |
| `2026-06-08-AI会话管理工具选型分析.md` | research | current | 2026-06-08 | 对比 `codbash`、`codlogs`、`tracebase`、`claude-replay` 等工具；推荐 `codbash` 作为跨工具入口，`codlogs` 仅作 Codex 专项补充 |
