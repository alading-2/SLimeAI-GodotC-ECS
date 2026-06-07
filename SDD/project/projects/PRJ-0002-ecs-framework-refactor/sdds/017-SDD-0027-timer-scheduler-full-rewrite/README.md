# SDD-0027 Timer Scheduler Full Rewrite

## Index Card

- **Status**: blocked
- **Created**: 2026-06-02
- **Updated**: 2026-06-03
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/timer
- **Tags**: timer, tools, ai-first

## What This SDD Is About

本 SDD 将 Timer 从当前 `TimerManager._Process + ObjectPool<GameTimer>.ForEachActive()` 的便利实现，重构为通用框架级 Timer 系统：

- `TimerScheduler` 是纯 C# 调度核心，不依赖 Godot API。
- `TimerManager` 保留为统一入口、Godot driver 和兼容 facade。
- gameplay callback 只在主线程稳定派发点执行。
- 默认调度结构为 min-heap by due time，不再每帧扫描全部 active timer。
- 新增 `TimerHandle(id, generation)`、`TimerOwner`、`TimerPurpose`、`TimerClock`、cancel reason 和 diagnostics。
- 高风险 gameplay 调用点一次性迁移到 owner/purpose/cancel point。
- 新增 debug summary/dump/JSON export 和 `TimerStressValidation` 压力验证场景。

本任务不是局部性能补丁；不能只删除 `ToList()`，也不能把 gameplay timer 换成 Godot `SceneTreeTimer` 或 .NET ThreadPool timer。

## Reading Order

1. `design/README.md` — Timer 共享设计包总裁决副本
2. `design/01-现状证据与AI-first裁决.md` — 当前热路径、成熟框架证据和拒绝项
3. `design/02-目标架构与优化路线.md` — 纯 C# scheduler、heap、handle、owner/purpose、clock、diagnostics
4. `design/03-调用点迁移与验证计划.md` — 调用点、grep gate、benchmark、压力场景和验证门禁
5. `design/INDEX.md` — 本 SDD 的设计入口
6. `design/main.md` — 本 SDD 执行级设计摘要
7. `tasks.md` — 可执行任务拆分
8. `bdd.md` — 行为验收场景
9. `execution-prompt.md` — 可直接交给新执行会话的一次性提示词
10. `Core/progress.md` — 最近结论和恢复点
11. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T1.8
- **Last Conclusion**: Timer scheduler core、TimerManager adapter、owner/purpose callsite migration、diagnostics、benchmark、TimerStressValidation 文件、DocsAI Timer 文档和 tools skill 同步已完成；可执行 gates 通过。
- **Next Action**: 初始化当前 BrotatoLike 的 Godot project/runner 或提供可用 Godot CLI 后，运行 TimerStressValidation、log analyzer、scene-gate 和 BrotatoLike smoke，再完成 T1.8/T1.9/T1.12。
- **Open Blockers**: Current BrotatoLike target has no project.godot, C# project, Tools/run-godot-scene.sh or analyzer, and this environment has no godot/godot4 CLI; cannot produce TimerStressValidation index.json/result.json/PASS artifact, scene-gate report or BrotatoLike smoke evidence.
