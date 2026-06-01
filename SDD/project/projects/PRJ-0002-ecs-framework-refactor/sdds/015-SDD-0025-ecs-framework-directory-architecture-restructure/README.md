# SDD-0025 ECS Framework Directory Architecture Restructure

## Index Card

- **Status**: done
- **Created**: 2026-06-01
- **Updated**: 2026-06-01
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Src/ECS
  - DocsAI/ECS
  - Data
- **Tags**: ecs, directory-architecture, capability, docsai

## What This SDD Is About

本 SDD 执行 PRJ-0002 的 ECS 框架目录架构大重构。

目标不是把当前仓库迁到 AiFirst `GameOS/`，也不是去掉 ECS 概念，而是把物理目录调整为 AI 更容易路由的结构：

```text
Src/ECS/Runtime + Src/ECS/Capabilities
DocsAI/ECS/Runtime + DocsAI/ECS/Capabilities + DocsAI/ECS/Tools + DocsAI/ECS/UI
```

其中：

- `Runtime` 承载 Entity / Data / Event / System Core 等跨域 ECS 基础设施。
- `Capabilities` 承载 Ability / Damage / Movement / Collision / Feature / Effect / Projectile / AI / Spawn / Unit 等功能 owner。
- Capability 内部继续保留 Component / System / Events / Tests / DataKeys 等 ECS 语义。
- `Foundation/Foundations` 已从当前路由层移除；历史概念材料按 owner 分散到 `Concepts/`，或进入 `DocsAI/Archive/` / `DocsAI/思考/`。

## Reading Order

1. `../../design/6.ECS框架目录架构大重构/README.md` — 项目级目录架构设计包入口
2. `../../design/6.ECS框架目录架构大重构/01-现状证据与AI-first裁决.md` — 当前问题、AiFirst 参考和裁决
3. `../../design/6.ECS框架目录架构大重构/02-目标目录架构与归属规则.md` — Runtime / Capabilities / Tools / UI 归属规则
4. `../../design/6.ECS框架目录架构大重构/03-迁移切片与验证门禁.md` — 分阶段迁移计划和验证门禁
5. `../../directory-architecture-restructure-execution-prompt.md` — 可交给新执行会话的总提示词
6. `execution-prompt.md` — 本 SDD 局部执行提示词
7. `design/INDEX.md` — 本 SDD 设计入口
8. `design/main.md` — 本 SDD 执行设计
9. `tasks.md` — 当前任务拆分
10. `progress.md` — 最近结论和恢复点
11. `bdd.md` — 行为验收场景
12. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0025 已完成并追加测试目录收口：`Src/ECS/Test/SingleTest` 已清空；DataOS 测试迁到 `Src/ECS/Runtime/Data/Tests/DataOS/`，Runtime/Entity/System/ECS smoke 和各 Capability/Tools 测试迁到对应 owner `Tests/`；ResourceGenerator 已重新生成资源 manifest。
- **Next Action**: 后续任务按 `DocsAI/ECS/README.md` 进入对应 owner；如果继续 PRJ-0002，优先处理新的 active SDD 或归档项目状态。
- **Open Blockers**: none
