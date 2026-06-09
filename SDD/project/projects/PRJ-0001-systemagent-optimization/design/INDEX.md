# PRJ-0001 Design Index

> 状态：current
> 定位：PRJ-0001 项目级设计入口；详细历史设计在 `1/`，补充优化裁决在 `优化/`。

## Main Design

- `1/main.md`

## Design Groups

| Path | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `1/INDEX.md` | baseline | current | 2026-05-25 | PRJ-0001 SystemAgent 优化主设计包 |
| `优化/INDEX.md` | supplement | current | 2026-06-08 | PRJ-0001 完成后的补充优化裁决 |
| `会话记录适配器参考设计/INDEX.md` | supplement | current | 2026-06-09 | 参考 `codbash` / `codlogs` / `tracebase` 的 Cross-agent Session Adapter 详细设计 |

## Current Supplements

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `优化/2026-06-08-SystemAgent工作流内化与会话记录优化.md` | design | current | 2026-06-08 | 不做外层 agent / Warp 改造；优先复用现成 session 工具，聚焦跨 Claude Code / Codex / OpenCode session adapter 和只读资料 subagent pilot |
| `优化/2026-06-08-AI会话管理工具选型分析.md` | research | current | 2026-06-08 | Linux 会话管理工具选型；推荐 `codbash` 作为跨工具入口，`codlogs` 仅作 Codex 专项补充 |
| `会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | design | current | 2026-06-09 | 从已 clone 的 `codbash`、`codlogs`、`tracebase` 提炼 SlimeAI 薄层 session adapter 架构、schema、阶段和风险 |
