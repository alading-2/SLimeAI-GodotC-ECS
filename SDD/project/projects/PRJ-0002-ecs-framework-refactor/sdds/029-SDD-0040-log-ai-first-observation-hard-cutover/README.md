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

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — SDD-0040 的范围、取舍、DeepThink 确认包和 DesignCritic 结论
3. `design/source-request.md` — 用户原始问题与去重后的不可丢失约束
4. `../../design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` — 当前整理契约、完成清单和样本验证数据
5. `../../design/Tool/10.Log/第二部分-语义提炼整理/04-当前实现审查报告.md` — 当前实现逐条审查，区分 analyzer DONE 与 Validation/Godot 未完成
6. `design/07-当前样本日志问题与整理方案.md` — 历史 T2 复盘和修复路线快照
7. `execution-prompt.md` — 新会话执行提示词
8. `tasks.md` — 当前任务拆分
9. `progress.md` — 最近结论和恢复点
10. `bdd.md` — 行为场景或不适用说明
11. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T2.6 Validation artifact adoption
- **Last Conclusion**: T2 analyzer 语义整理已补齐并复验：默认输出 flow conclusion、success template、failure-first summary 和 semantic missing-fields；复跑同一 output 会清理 stale `by-owner` / `by-phase` / `flows/flows.json`；`query --analysis-dir` 只查语义索引，语义索引为空时返回空结果，不再回退 raw。当前样本 `analysis-semantic` 为 `rawLines=4915`、`defaultReadableLines=303`、`defaultReadableRatio=0.062`；TargetSelector 高频查询返回 1 条 success-template(`count=3041`)；gate 仍是 `no-failure-observed`，因为没有 Validation artifact。
- **Next Action**: 继续 T2.6：让可运行的承载场景或后续验证 run 输出 `ValidationSession` / artifact；在有效 Godot runner 可用前，不声称 scene smoke 或行为通过。
- **Open Blockers**: 最终 Godot scene smoke 仍 blocked：当前没有可验证本框架工作树的承载游戏 runner。该 blocker 不阻止 logctl / DocsAI / skill 非 Godot 门禁，但阻止 SDD 完成态。
