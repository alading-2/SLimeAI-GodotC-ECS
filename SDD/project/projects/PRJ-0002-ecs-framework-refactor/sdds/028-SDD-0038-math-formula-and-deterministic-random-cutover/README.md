# SDD-0038 Math Formula And Deterministic Random Cutover

## Index Card

- **Status**: pending
- **Created**: 2026-06-07
- **Updated**: 2026-06-07
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/math
- **Tags**: tools, math, deterministic-random, hard-cutover

## What This SDD Is About

本 SDD 执行 `design/Tool/其他Tool/03-Math目标架构与验证.md`：把 `MyMath` 杂项公式和不可控随机收敛为 AI-first 数学工具边界。

目标是保留 Math 功能，但不保旧聚合类形状：纯几何留 `Geometry2D`，曲线和 Bezier 继续作为可测试纯算法，业务公式拆到明确 owner，概率/采样支持 deterministic RNG。TargetSelector 查询语义不下沉到 Math。

本 SDD 建议在 SDD-0036 之后执行，避免 `GeometryCalculator` / `TargetSelectorQuery` 与 Math ownership 迁移互相打架。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 目标架构和边界
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为场景
5. `progress.md` — 最近结论和恢复点
6. `execution-prompt.md` — 新会话执行提示词
7. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: SDD-0038 已生成执行胶囊。默认不引入第三方数学库，不迁到 `System.Numerics`，继续使用 Godot `Vector2`。
- **Next Action**: 进入实现前先读 `execution-prompt.md`，从 T1.1 readiness baseline 开始。
- **Open Blockers**: none
