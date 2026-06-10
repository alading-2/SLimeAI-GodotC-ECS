# Notes

## References

- PRJ-0001 共享设计：`../../design/会话记录适配器参考设计/2026-06-10-Session-Adapter二次审查与会话分析流程设计.md`
- 初始实现设计：`../../design/会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
- AI-first digest 设计：`../../design/会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md`
- 工具入口：`Workspace/SystemAgent/Tools/session-adapter/session_adapter.py`
- 测试入口：`Workspace/SystemAgent/Tools/session-adapter/test_session_adapter.py`
- Retrospective actor：`Workspace/SystemAgent/Actors/Retrospective.md`
- Retrospective skill 源：`.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md`
- DeepThink skill 源：`.ai-config/skills/systemagent-skill/systemagent-deepthink/SKILL.md`
- Git policy：`Workspace/SystemAgent/Rules/Git.md`

## Open Questions

- 10 日 digest 是否正式写入仓库：本 SDD 只用 `/tmp/sdd-0041-chat-v2` 做临时验证；正式重建仓内 `Workspace/DocsAI/ChatHistory` 可作为后续发布/归档任务处理。
- 新 schema version：已升级为 `index.json` entry `schema_version=4`，默认入口不维护旧 schema fallback。
- Claude/OpenCode 高保真 digest：不纳入 SDD-0041，后续另建 SDD。
- Skill-test R005 advisory：当前 catalog coverage 仍有 9 条登记差异，Critical 为 0；是否清理 catalog 作为独立配置治理任务处理，不阻塞 SDD-0041。
