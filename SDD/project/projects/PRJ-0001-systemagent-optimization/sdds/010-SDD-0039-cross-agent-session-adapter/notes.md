# Notes

## References

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
- `Workspace/Resources/tool/codbash/README.md`
- `Workspace/Resources/tool/codbash/src/data.js`
- `Workspace/Resources/tool/codbash/src/handoff.js`
- `Workspace/Resources/tool/codlogs/README.md`
- `Workspace/Resources/tool/tracebase/README.md`

## Confirmed Decisions

- OpenCode 只要求支持路径，当前不要求真实 OpenCode 样例。
- 采用推荐方案：SlimeAI 实现薄层只读 adapter，不 fork 上游。
- 第一版只做 `list` / `index` / `summarize`。
- ChatHistory 输出到 `Workspace/DocsAI/ChatHistory/`。
- 不改原始 session 文件名，不复制完整 transcript，不接自动 hook。

## Open Questions

- Claude Code 高保真 tool outputs 是否后续需要专项 exporter。
- `codbash` 是否存在稳定 JSON/API 输出，后续可替换文本解析。
- OpenCode 有真实样例后需要补充 `opencode export <sessionID>` 验证。
