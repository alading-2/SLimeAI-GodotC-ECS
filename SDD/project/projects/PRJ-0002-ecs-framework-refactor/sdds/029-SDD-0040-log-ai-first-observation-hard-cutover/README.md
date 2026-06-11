# SDD-0040 Log AI-first Observation Hard Cutover

## Index Card

- **Status**: blocked
- **Created**: 2026-06-09
- **Updated**: 2026-06-10
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
4. `design/07-当前样本日志问题与整理方案.md` — 当前样本的 T2 复盘、实现顺序和验收门禁
5. `execution-prompt.md` — 新会话执行提示词
6. `tasks.md` — 当前任务拆分
7. `progress.md` — 最近结论和恢复点
8. `bdd.md` — 行为场景或不适用说明
9. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T2.5 owner hot-spot cleanup
- **Last Conclusion**: T2.1~T2.4 已完成第一批闭环：`logctl analyze` 现在能为当前样本生成 `summary.md`、强 `ai-context.md`、`noise/top-contexts.md`、`missing-fields/index.md`、`flows/index.md` 和 `failures/index.md`；gate 不再把无 artifact / Validation 的 structured log 误报为 `passed`，当前样本为 `no-failure-observed`。
- **Next Action**: 继续 T2.5：TargetSelector / ObjectPool / System 的运行时 aggregate summary 仍未完成；HealthBarUI、Damage 和 Logger source/duration 字段已做第一批补强。随后推进 T2.6，让承载样本或后续场景输出 Validation artifact。
- **Open Blockers**: 最终 Godot scene smoke 仍 blocked：当前没有可验证本框架工作树的承载游戏 runner。该 blocker 不阻止 T2 analyzer/owner follow-up 在现有样本上推进。
