# SDD-0040 Log AI-first Observation Hard Cutover

## Index Card

- **Status**: pending
- **Created**: 2026-06-09
- **Updated**: 2026-06-09
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/logger
- **Tags**: tools, log, observation, ai-first, hard-cutover

## What This SDD Is About

本 SDD 执行 Log AI-first Observation hard cutover：把当前 `Src/ECS/Tools/Logger/Log.cs` 的 legacy 文本输出升级为结构化观测入口，并同批收口 Validation artifact、Godot scene runner 分析、`logctl analyze/query`、owner `Log.md` 和测试 PASS/FAIL 事实源。

本轮只生成 SDD 和新会话执行提示词，不改源码实现；后续实现会话从 `execution-prompt.md` 和 `tasks.md` 恢复。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — SDD-0040 的范围、取舍、DeepThink 确认包和 DesignCritic 结论
3. `execution-prompt.md` — 新会话执行提示词
4. `tasks.md` — 当前任务拆分
5. `progress.md` — 最近结论和恢复点
6. `bdd.md` — 行为场景或不适用说明
7. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: Log hard cutover SDD 已创建并导入 `design/Tool/10.Log/` 设计包；推荐同批处理 Logger core、Validation helper、Log CLI/analyzer、godot-scene-test wrapper、第一批 owner flow 文档和降噪回写。
- **Next Action**: 按 `execution-prompt.md` 进入 T1.1 readiness baseline，先只读和记录证据，再开始 Logger core TDD。
- **Open Blockers**: none
