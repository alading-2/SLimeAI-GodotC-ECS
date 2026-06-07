# SDD-0026 Input Contract Manifest And Facade Hardening

## Index Card

- **Status**: done
- **Created**: 2026-06-01
- **Updated**: 2026-06-02
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/tools/input
- **Tags**: input, tools, ai-first, controller

## What This SDD Is About

本 SDD 把 Input 从“Godot action + 静态 wrapper + 组件里猜按钮语义”收口为 AI-first Input Contract。

第一阶段已经完成文档/manifest 和 `project.godot` 最小键盘备用绑定；后续执行聚焦两件事：

- 将 `InputManager` 中 `IsX/IsY/IsLeftBumper/IsRightBumper` 这类按钮名 API 补上业务语义 facade。
- 分阶段迁移 `ActiveSkillInputComponent`、`TargetingIndicatorControlComponent`、`PauseMenuSystem` 等调用点，让 AI 按业务 action/context 改键，而不是从按钮名猜功能。

## Reading Order

1. `../../design/Tool/Input/README.md` — 项目级 Input 设计包入口
2. `../../design/Tool/Input/01-现状证据与AI-first裁决.md`
3. `../../design/Tool/Input/02-目标架构与优化路线.md`
4. `../../design/Tool/Input/03-调用点迁移与验证计划.md`
5. `design/INDEX.md` — 本 SDD 设计文档列表
6. `design/main.md` — 本 SDD 执行设计
7. `tasks.md` — 任务拆分和验证要求
8. `bdd.md` — 行为验收场景
9. `execution-prompt.md` — 下一轮执行提示词
10. `Core/progress.md` — 最近结论和恢复点

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0026 已完成；2026-06-02 追加文档治理裁决：Input DocsAI 主入口是 `DocsAI/ECS/Tools/Input/README.md`，`Concept.md / Usage.md / InputMap.md` 只是可选辅助页，不是固定模板。
- **Next Action**: 本 SDD 无需继续；后续 Input 深化另建 SDD 覆盖 ControllerGlyphProfile、运行时 InputContext 或 manifest 自动校验。
- **Open Blockers**: none
