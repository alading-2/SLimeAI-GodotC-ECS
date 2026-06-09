# SystemAgent 优化补充设计索引

> 状态：current
> 日期：2026-06-09
> 定位：PRJ-0001 完成后的 SystemAgent 核心优化裁决入口；工具选型细节已移入 `../会话记录适配器参考设计/`。

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `2026-06-08-SystemAgent工作流内化与核心优化裁决.md` | design | current | 2026-06-09 | 当前主裁决：不做外层 AI CLI manager，不魔改 Warp；SystemAgent 聚焦项目内 workflow、SDD、ChatHistory、只读资料 subagent、hook 边界和任务拆分协议 |

## Reference Handoff

会话管理工具选型不再放在 `优化/` 主入口。需要查看 `codbash`、`codlogs`、`tracebase`、`claude-replay` 等工具取舍时，读：

- `../会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`
- `../会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
