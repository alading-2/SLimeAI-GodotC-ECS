# SDD-0035 Runtime Mount And Node Lifecycle Hard Cutover

## Index Card

- **Status**: pending
- **Created**: 2026-06-07
- **Updated**: 2026-06-07
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/runtime/node-lifecycle
- **Tags**: tools, runtime-mount, node-lifecycle, hard-cutover

## What This SDD Is About

本 SDD 执行 `design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md` 的第一优先级切片：把旧 `ParentManager` 自由字符串挂载点 hard cutover 为 manifest 化 `RuntimeMountRegistry` / `SceneMountRegistry`，并把 `NodeLifecycleManager` 从 `Tools/` 普通工具迁到 Runtime 底层 Node registry。

目标不是保留旧 API，而是保留功能：统一管理运行时 Entity / Pool / UI / Tool 节点在 SceneTree 中的路径，默认挂到 `/root/SlimeAIRuntime`，并让 Node 注册、注销、owner/source metadata、invalid cleanup 和 diagnostics 成为 AI 可读事实源。

本 SDD 是后续 `SDD-0036 Target Query Engine Hard Cutover` 的前置建议：TargetSelector 不应继续直接扫 `NodeLifecycleManager.GetNodesByInterface<Node2D>()`。

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
- **Last Conclusion**: SDD-0035 已生成执行胶囊。默认顺序是先完成 Runtime mount 和 NodeLifecycle 底座，再执行 TargetQueryEngine。
- **Next Action**: 进入实现前先读 `execution-prompt.md`，从 T1.1 readiness baseline 开始；不要直接修改源码。
- **Open Blockers**: none
