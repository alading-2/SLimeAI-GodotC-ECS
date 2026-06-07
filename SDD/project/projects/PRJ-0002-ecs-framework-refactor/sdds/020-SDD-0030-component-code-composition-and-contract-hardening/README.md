# SDD-0030 Component Code Composition And Contract Hardening

## Index Card

- **Status**: done
- **Created**: 2026-06-04
- **Updated**: 2026-06-04
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/runtime/component
- **Tags**: component, runtime, ai-first

## What This SDD Is About

把 Runtime Component 默认组合从 `.tscn` Preset 切到 C# profile / composer，并补齐 AI-first Component manifest、DocsAI 入口和 owner skill 规则。核心行为边界是：`IComponent` 生命周期不扩签名，Component 仍是 Godot Node adapter；代码化组合只负责注册前创建组件和注入 typed options。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 的实现边界、取舍和验证要求
3. `design/04-Component代码化组合与参数注入裁决.md` — 用户裁决：完全代码化、typed options、禁用 Inspector 默认参数
4. `tasks.md` — 当前任务拆分
5. `bdd.md` — 行为场景和标准答案
6. `Core/progress.md` — 最近结论和恢复点
7. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0030 已完成：Component 默认组合事实源迁到 C# profile / composer，spawn 与直接 RegisterComponents 都在注册前 compose，DocsAI ComponentManifest 和 ecs-component skill 已同步。
- **Next Action**: 后续 Component 深化另起 SDD：subscription cleanup audit、dynamic component policy、preflight 或 legacy Preset 文件清理。
- **Open Blockers**: none
