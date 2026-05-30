# SDD-0006 SystemAgent Information Architecture Refresh

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
- **Tags**: systemagent

## What This SDD Is About

本 SDD 先把 `Workspace/SystemAgent` 的正式目录结构、职责边界和入口链升级到最新分层模型，再进入 Hook、Gate、Workflow、DesignDiscovery 和 Subagent 后续改造。它只生成并执行信息架构层面的 plan，不在本阶段实现 hook 逻辑、wrapper skill 同步或并行 subagent dispatcher。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SystemAgent 信息架构刷新完成：新增 `Capabilities/`、`Policies/INDEX.md` 和 `Policies/SubagentPolicy.md`，README/INDEX/manifest/DocumentationManagement 已同步，旧入口审计无 violation。
- **Next Action**: 下一步从 PRJ-0001 roadmap 创建 Hook / Gate P0 Stability 子 SDD。
- **Open Blockers**: none
