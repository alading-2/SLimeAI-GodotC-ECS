# 会话记录适配器参考设计

> 状态：current
> 日期：2026-06-09
> 定位：PRJ-0001 补充设计；沉淀会话记录工具选型，并从 `Workspace/Resources/tool` 中的 `codbash`、`codlogs`、`tracebase` 提炼 Cross-agent Session Adapter 的实现边界。

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `2026-06-08-AI会话管理工具选型分析.md` | research | reference-current | 2026-06-09 | 从 `优化/` 移入本目录并简化；保留工具排序和采纳边界：`codbash` 发现入口、`codlogs` Codex 高保真补充、`tracebase` 复盘维度参考。 |
| `2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | design | current | 2026-06-09 | 参考 `codbash` / `codlogs` / `tracebase`，设计 SlimeAI 薄层 session adapter；不 fork 上游，不改原始 session，不接自动 hook。 |
| `2026-06-09-ChatHistory-AI-first整理与价值评分设计.md` | design | current | 2026-06-09 | 在 `visible-transcript` 之上新增 AI-first digest 层；定义每 session 文件夹、结构化事件、Digest Gate、短会话 locator-only、中断可选跳过、工具失败单独记录和 index v3。 |
| `效率指标设计.md` | design | current (implemented) | 2026-06-09 | 分析 6/8-6/9 会话后发现验证循环、文件读放大等效率问题；新增 `derived/efficiency.md`、index.json 效率字段、VALIDATION_RE 扩展和 Retrospective 集成。 |
| `2026-06-10-会话效率与自动化流程问题分析.md` | design | superseded | 2026-06-10 | 全局规则已改但副本未同步；OpenSpec 残留 20+ 处；效率指标无人消费；SDD 自动化流程不完整。给出四批修改清单。 |
| `2026-06-10-Session-Adapter效率优化与自动化流程改进.md` | design | current (implemented-with-follow-up) | 2026-06-10 | 完整记录：用户原始问题、问题分析（验证循环 vs git diff）、五项解决方案、修改文件清单、效率数据；二次审查发现 efficiency 分类仍需修正。 |
| `2026-06-10-Session-Adapter二次审查与会话分析流程设计.md` | design | current | 2026-06-10 | 检查 8-10 日会话和最新 digest 模式：确认 `transcript.visible.md` 方向成立，同时发现 SDD CLI 被误判为 edit、ChatHistory stale、Retrospective 缺 current digest 定位、tool failure 缺根因分类；后续 session-adapter 重构允许破坏性 schema/digest 重建，不维护旧格式 fallback。 |

## Conclusion

第一阶段不是魔改 `codbash`，而是参考三个项目拆出三层能力：

- `codbash`：参考跨 Claude Code / Codex / OpenCode 的 session 发现、搜索、预览和 handoff 思路。
- `codlogs`：参考 Codex 大 session、tool result、Markdown / HTML 导出的保真策略。
- `tracebase`：参考后续失败分析、context waste、scorecard、redacted export 的复盘模型，但不作为第一阶段运行依赖。

SlimeAI 自己只实现薄层 `session-adapter`：消费现成工具或官方 CLI 输出，生成统一 schema、可读文件名、`Workspace/DocsAI/ChatHistory/index.json` 和 Markdown sidecar。

`SDD-0039` 已完成第一层 `visible-transcript`。下一步推荐进入 `ChatHistory AI-first Session Digest`：先用简单 `Digest Gate` 判断是否生成整理文档；短会话默认只写 `locator-only` index entry，不生成 folder / derived 文件；批量整理时中断且无结论/无代码/无验证的会话可用 `--skip-interrupted` 跳过。通过 gate 的 session 再建 folder，保留 raw transcript，新增 `derived/ai-context.md`、`summary.md`、`events.jsonl`；工具调用必须统计成功 / 失败 / 未知，失败单独写 `derived/tool-failures.md`。

二次审查后的优先级调整：不要先接自动 hook；先修正 digest 质量和 Retrospective 消费协议。重点是命令分类、`verification_loops` 误判、resume boilerplate 去噪、失败恢复判断、ChatHistory stale report 和 current digest 定位。session-adapter 属于内部 AI 工具，后续要改就按新契约完整重构，允许升级或重建 index / digest schema，不维护旧格式 fallback。
