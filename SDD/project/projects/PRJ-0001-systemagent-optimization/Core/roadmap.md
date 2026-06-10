# Roadmap

## Purpose

项目级执行路线图，追踪 `design/` 下每份文档的完成情况和对应 SDD。多份文档可以合并为一个 SDD，不要求一对一。

## Design Progress

| Design Document | Done | SDD | Notes |
| --- | --- | --- | --- |
| `main.md` | — | — | 项目主设计，共享上下文 |
| `SystemAgent问题清单.md` | — | — | 问题 backlog，创建 SDD 时引用 |
| `优化/2026-06-08-SystemAgent工作流内化与核心优化裁决.md` | done | SDD-0039 | 已重构为 PRJ-0001 当前主裁决；仍不做外层 agent / Warp 改造，SystemAgent 聚焦项目内 workflow control plane |
| `会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md` | done | SDD-0039 | 已从 `优化/` 移入参考设计并简化；`codbash` 作为跨工具发现入口，`codlogs` 作为 Codex 高保真后续补充路径 |
| `会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | done | SDD-0039 | 已实现 SlimeAI 薄层 adapter，生成 ChatHistory sidecar 和统一 index schema；OpenCode 保留支持路径 |
| `会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md` | pending | — | 建议新建 `ChatHistory AI-first Session Digest` SDD：在 visible transcript 上增加 Digest Gate；短会话 locator-only，中断可选跳过，工具失败单独记录，通过 gate 后才生成 per-session folder / derived digest / events.jsonl |
| `会话记录适配器参考设计/2026-06-10-Session-Adapter二次审查与会话分析流程设计.md` | pending | — | 建议新建 `Session Adapter Digest Accuracy and Retrospective Handoff`：完整重构命令分类/loop 误判、title/goal/outcome 去噪、tool failure 根因分类、ChatHistory stale report、Retrospective current digest 定位和 GitPolicy/actor push 规则残留冲突；允许破坏性升级 index / digest schema，不维护旧格式 fallback |
| `01-独立SDD转向方案.md` | done | SDD-0001, SDD-0002 | SDD-first 策略已落地 |
| `02-Workflow与Skill触发优化方案.md` | pending | SDD-0006, SDD-0008 | 信息架构部分已落地；workflow/skill/role 执行分层已生成待执行 SDD |
| `03-Hook与Gate重写方案.md` | pending | SDD-0007 | Hook / Gate P0 稳定性 SDD 已生成 |
| `04-Git与Worktree策略.md` | done | SDD-0010 | Git / worktree / subagent 安全策略已落地 |
| `05-OpenSpec退场与兼容策略.md` | done | SDD-0002 (external) | 独立历史 SDD |
| `06-实施路线图.md` | done | SDD-0006, SDD-0007, SDD-0008, SDD-0009, SDD-0010 | 执行顺序已转为子 SDD 队列 |
| `07-DesignDiscovery与DesignCritic方案.md` | done | SDD-0009 | DesignDiscovery capability 与 DesignCritic 条件角色已落地 |
| `08-SDD独立化与文档迁移方案.md` | done | SDD-0004 | 已落地为项目容器模型 |
| `09-WorkflowSkillRole分层模型.md` | pending | SDD-0006, SDD-0008, SDD-0009 | 目录边界和 DesignDiscovery 已落地；workflow/skill/role 执行分层仍待 SDD-0008 |
| `10-Subagent使用场景与采纳策略.md` | done | SDD-0006, SDD-0010 | Subagent policy 边界与只读 launcher 契约已落地 |
| `SDD/SlimeAI-SDD-MVP设计.md` | done | SDD-0001, SDD-0003, SDD-0004, SDD-0005 | SDD 系统共享设计 |
| `SDD/SDD重构与CLI详细执行计划.md` | done | SDD-0001, SDD-0003, SDD-0004, SDD-0005 | SDD CLI 计划 |
| `SDD/SDD-CLI信息质量加固设计.md` | done | SDD-0003, SDD-0005 | 信息质量规则 |

## Next SDDs

| Priority | Design Docs | Goal |
| --- | --- | --- |
| P0 | `03-Hook与Gate重写方案.md` | SDD-0007：Hook / Gate P0 稳定性 |
| P1 | `02-Workflow与Skill触发优化方案.md`, `09-WorkflowSkillRole分层模型.md` | SDD-0008：Workflow / Skill / Role 分层执行 |
| P1 | `07-DesignDiscovery与DesignCritic方案.md`, `09-WorkflowSkillRole分层模型.md` | SDD-0009：done；DesignDiscovery capability 与 DesignCritic 条件角色已落地 |
| P2 | `04-Git与Worktree策略.md`, `10-Subagent使用场景与采纳策略.md` | SDD-0010：done；Git / Worktree / Subagent 安全策略已落地 |
| P2 | `优化/2026-06-08-SystemAgent工作流内化与核心优化裁决.md`, `会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`, `会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md` | SDD-0039：done；已完成 session 基础能力。后续只读资料 subagent pilot、Claude/OpenCode 高保真导出和 retrospective 接入需另建 SDD |
| P2 | `会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md` | 建议新建：ChatHistory AI-first Session Digest；Codex first，Digest Gate，短会话 locator-only，批量整理可跳过无结果中断会话，工具失败单独记录，不删除旧 transcript |
| P1 | `会话记录适配器参考设计/2026-06-10-Session-Adapter二次审查与会话分析流程设计.md` | 建议新建：Session Adapter Digest Accuracy and Retrospective Handoff；先完整重构 digest 准确性和 actor/skill 消费协议，允许破坏性 schema 升级，再考虑 hook 自动化 |
