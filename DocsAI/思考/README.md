# 设计思考与深度分析

> 状态：current
> 定位：设计分析、深度研究、范式探讨等非执行文档。
> 更新：2026-06-16

## 说明

本目录存放设计思考类文档，用于帮助 AI 理解决策背后的"为什么"。

**注意**：思考文档不作为代码修改的直接依据，仅作为设计参考。2026-06-16 后框架方向裁决已改为弃用 ECS；执行依据优先看 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/`，再看对应 owner 文档。旧 `ECS/` 目录仍是历史路径名和当前文件位置，不代表继续实现 ECS runtime。

已经确定的 DocsAI 管理、索引、迁移和维护规则不放在本目录，统一放入 [`../管理/`](../管理/)。

## 目录

| 目录 | 主题 | 文件 |
| ---- | ---- | ---- |
| [碰撞问题/](碰撞问题/) | 碰撞系统深度分析 | 幽灵碰撞问题深度分析 |
| [框架/ECS框架/](框架/ECS框架/) | ECS 概念研究 | 真正 ECS 框架、Godot 适配性、SlimeAI 应保留/放弃的概念 |
| [框架/](框架/) | 框架设计思考 | 单位分类与阵营设计 |
| [AI开发范式/](AI开发范式/) | AI-first 开发范式 | ai_first_godot_csharp_ecs_program_overview |
| [ArchitectureDecisionRecords/](ArchitectureDecisionRecords/) | 架构决策记录与深度分析 | Data 系统、GameOS 概念边界、语言选型 |
| [数学工具/](数学工具/) | 数学工具选型与重构 | CSharp与Godot数学工具选型分析、运动与目标选择重构方案 |
