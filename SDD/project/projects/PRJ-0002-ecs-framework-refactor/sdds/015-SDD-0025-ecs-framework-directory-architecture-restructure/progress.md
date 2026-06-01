# Progress

## Latest Resume

- **Updated**: 2026-06-01
- **Current Task**: T1.1
- **Last Conclusion**: SDD-0025 已建立为目录架构大重构执行胶囊；项目级设计包裁决 `Src/ECS/Runtime + Src/ECS/Capabilities`、`DocsAI/ECS/Runtime + DocsAI/ECS/Capabilities + DocsAI/ECS/Foundations`，保留 ECS 语义，不迁到 GameOS。
- **Next Action**: 执行 T1.1 readiness baseline：记录 Git dirty 范围和旧路径引用；当前尚未移动源码、DocsAI 或 DocsOld。
- **Open Blockers**: none

## Timeline

### P001 — 2026-06-01 15:53 — resume

- **Context**: 创建 SDD。
- **Conclusion**: 已建立任务上下文胶囊。
- **Evidence**: README、sdd.json、design、tasks、progress、bdd、notes 已生成。
- **Impact**: 后续围绕 tasks.md 和 progress.md 记录执行。
- **Resume**: 从 README 的 Current Resume 继续。

### P002 — 2026-06-01 — design-package-created

- **Context**: 用户确认目录方向：Runtime 以外功能统一放入 `Capabilities/`，DocsAI 同步采用相同结构，并要求生成设计文档、SDD 和提示词。
- **Conclusion**: 已生成项目级 `design/6.ECS框架目录架构大重构/` 设计包、SDD-0025 执行设计、任务拆分、BDD 和总执行提示词；本轮只写设计/SDD，不移动源码。
- **Evidence**: `../../design/6.ECS框架目录架构大重构/README.md`、`01-现状证据与AI-first裁决.md`、`02-目标目录架构与归属规则.md`、`03-迁移切片与验证门禁.md`、`../../directory-architecture-restructure-execution-prompt.md`、本 SDD `README.md` / `design/main.md` / `tasks.md` / `bdd.md`。
- **Impact**: 后续执行会话可从 SDD-0025 T1.1 开始，按 DocsAI 先行、Runtime 内核、Capability 分批、Foundations 原文迁入的顺序推进。
- **Resume**: 先运行 `git status --short` 和旧路径 `rg` baseline，确认不混入当前工作区既有 `.uid` / `__pycache__` 改动。
