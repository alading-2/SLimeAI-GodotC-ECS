# 会话记录适配器参考设计

> 状态：current
> 日期：2026-06-09
> 定位：PRJ-0001 补充设计；从 `Workspace/Resources/tool` 中的 `codbash`、`codlogs`、`tracebase` 提炼 Cross-agent Session Adapter 的实现边界。

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | design | current | 2026-06-09 | 参考 `codbash` / `codlogs` / `tracebase`，设计 SlimeAI 薄层 session adapter；不 fork 上游，不改原始 session，不接自动 hook。 |

## Conclusion

第一阶段不是魔改 `codbash`，而是参考三个项目拆出三层能力：

- `codbash`：参考跨 Claude Code / Codex / OpenCode 的 session 发现、搜索、预览和 handoff 思路。
- `codlogs`：参考 Codex 大 session、tool result、Markdown / HTML 导出的保真策略。
- `tracebase`：参考后续失败分析、context waste、scorecard、redacted export 的复盘模型，但不作为第一阶段运行依赖。

SlimeAI 自己只实现薄层 `session-adapter`：消费现成工具或官方 CLI 输出，生成统一 schema、可读文件名、`Workspace/DocsAI/ChatHistory/index.json` 和 Markdown sidecar。
