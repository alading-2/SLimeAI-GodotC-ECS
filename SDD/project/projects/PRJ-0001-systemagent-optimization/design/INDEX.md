# PRJ-0001 Design Index

> 状态：current
> 定位：PRJ-0001 项目级设计入口；详细历史设计在 `1/`，补充优化裁决在 `优化/`。

## Main Design

- `1/main.md`

## Design Groups

| Path | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `1/INDEX.md` | baseline | current | 2026-05-25 | PRJ-0001 SystemAgent 优化主设计包 |
| `优化/INDEX.md` | supplement | current | 2026-06-09 | PRJ-0001 完成后的 SystemAgent 核心优化裁决 |
| `会话记录适配器参考设计/INDEX.md` | supplement | current | 2026-06-09 | 会话记录工具选型、`codbash` / `codlogs` / `tracebase` 参考和 Cross-agent Session Adapter 详细设计 |

## Current Supplements

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `优化/2026-06-08-SystemAgent工作流内化与核心优化裁决.md` | design | current | 2026-06-09 | 当前主裁决：不做外层 agent / Warp 改造，SystemAgent 作为项目内 workflow control plane；会话记录、只读资料 subagent、hook 和任务拆分都有明确边界 |
| `会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md` | research | reference-current | 2026-06-09 | Linux 会话管理工具选型已降级为参考资料；`codbash` 作为跨工具入口，`codlogs` 作 Codex 高保真补充，`tracebase` 作复盘维度参考 |
| `会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | design | current | 2026-06-09 | 从已 clone 的 `codbash`、`codlogs`、`tracebase` 提炼 SlimeAI 薄层 session adapter 架构、schema、阶段和风险 |
| `会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md` | design | current | 2026-06-09 | 在 `visible-transcript` 之上新增 AI-first digest 层；定义每 session 文件夹、结构化事件、Digest Gate、短会话 locator-only、中断可选跳过、工具失败单独记录和 index v3 |
| `会话记录适配器参考设计/2026-06-10-Session-Adapter二次审查与会话分析流程设计.md` | design | current | 2026-06-10 | 基于 8-10 日会话二次审查 session-adapter：确认 transcript 可见证据层成立，提出 digest 分类误判、ChatHistory stale、Retrospective 定位协议和 SystemAgent push 规则残留冲突的后续修正方向 |
