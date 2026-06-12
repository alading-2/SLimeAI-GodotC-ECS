# SDD-0040 Log AI-first Observation Hard Cutover

## Index Card

- **Status**: blocked
- **Created**: 2026-06-09
- **Updated**: 2026-06-11
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/logger
- **Tags**: tools, log, observation, ai-first, hard-cutover

## What This SDD Is About

本 SDD 执行 Log AI-first Observation hard cutover：把当前 `Src/ECS/Tools/Logger/Log.cs` 的 legacy 文本输出升级为结构化观测入口，并同批收口 Validation artifact、Godot scene runner 分析、`logctl analyze/query`、owner `Log.md` 和测试 PASS/FAIL 事实源。

当前 T1.1~T1.12 已完成。用户指出原 10/10 完成状态与 `design/Tool/10.Log` 不一致后，已补齐 `Config/Log` profile/rules/overrides、runtime profile metadata、budget suppressed summary、`logctl profile show` 和 `suggest` 的 `profilePatch` dry-run 输出。

2026-06-10 14:51 再复盘：T1 完成的是结构化记录管道，不是完整“打印信息整理”。基于 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl`，新增 T2.1~T2.7 follow-up：analyzer markdown digest、gate 状态语义、flow 边界与聚合、semantic missing-fields、owner hot-spot cleanup、Validation artifact adoption 和真实样本验证同步。SDD 仍为 blocked，原因是最终 Godot scene smoke 缺有效承载游戏 runner；但 T2 analyzer/owner 整理本身不是 runner blocker，后续应先推进 T2。

2026-06-10 补充：项目级 current 设计入口已更新到 `design/Tool/10.Log/README.md`、`source-request.md` 和 `07-当前样本日志问题与整理方案.md`。本 SDD 下 `design/` 子目录保留 2026-06-09 执行快照，若与项目级 current Log 设计冲突，以项目级 `design/Tool/10.Log/` 为准。

2026-06-11 19:26 再校正：用户运行游戏后仍看到分离打印，判断成立。T2 analyzer 默认入口完成只代表“已有 raw 能被离线提炼”，不代表 `Src/ECS` 源码调用点已经完成语义化迁移。当前新增 T3 源码调用点语义化 follow-up：先冻结 live stdout policy、owner flow contract 和 Debug UI/TestSystem 可见性，再按 owner 迁移 `_log.Info`、测试打印和高频成功路径。SDD 不能再描述为“只剩 T2.6 / runner blocker”。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — SDD-0040 的范围、取舍、DeepThink 确认包和 DesignCritic 结论
3. `design/source-request.md` — 用户原始问题与去重后的不可丢失约束
4. `../../design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` — 当前整理契约、完成清单和样本验证数据
5. `../../design/Tool/10.Log/第二部分-语义提炼整理/04-当前实现审查报告.md` — 当前实现逐条审查，区分 analyzer DONE 与 Validation/Godot 未完成
6. `../../design/Tool/10.Log/第三部分-源码调用点语义化/README.md` — 当前 live 打印仍分离的根因、T3 方向、必须确认项和 DoD 草案
7. `design/07-当前样本日志问题与整理方案.md` — 历史 T2 复盘和修复路线快照
8. `execution-prompt.md` — 新会话执行提示词
9. `tasks.md` — 当前任务拆分
10. `progress.md` — 最近结论和恢复点
11. `bdd.md` — 行为场景或不适用说明
12. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T3.0 源码调用点语义化方向冻结
- **Last Conclusion**: 用户对 live 打印仍分离的质疑成立。T2 analyzer 语义整理已完成默认入口，但 `Src/ECS` 仍有大量普通 `_log.*` 调用和少量直接打印；新运行的 stdout 仍可能按旧调用点分离输出。当前新增第三部分设计，明确 T3 不是 analyzer follow-up，而是跨 owner 源码调用点迁移。
- **Next Action**: 先确认第三部分 Must Confirm：默认 stdout 是否严格收口、T3 是否继续归入 SDD-0040、第一验收链路是否以 MainTest / 主场景启动 / 释放技能 / 生成怪物为准、Debug UI/TestSystem 是否默认进入 debug profile。未确认前不做大规模源码迁移。
- **Open Blockers**: 最终 Godot scene smoke 仍 blocked：当前没有可验证本框架工作树的承载游戏 runner。该 blocker 不阻止 T3 设计和静态盘点，但阻止 SDD 完成态和场景行为通过声明。
