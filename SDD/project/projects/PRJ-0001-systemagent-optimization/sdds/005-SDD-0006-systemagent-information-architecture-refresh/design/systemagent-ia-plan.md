# SystemAgent Information Architecture Plan

## Purpose

本文件保存 SDD-0006 的任务级信息架构计划，避免 `design/main.md` 只作为单页摘要引用项目外路径。项目级长设计仍以 `../../design/` 为共享事实源；本文件只记录本 SDD 将要落地的 SystemAgent 结构决策。

## Target Layers

| Layer | Role | Expected Fact Source |
| --- | --- | --- |
| Entry | 最短人工入口和路由入口 | `README.md`, `INDEX.md` |
| Workflow | 端到端任务编排 | `Workflows/` |
| Capability | 可复用能力正文 | `Capabilities/` 或明确替代目录 |
| Role | 执行视角和审查职责 | `Roles/` |
| Artifact | SDD、validation、report 等恢复证据 | SDD 实例与 artifacts |
| Gate | 阶段检查与 verdict | `Gates/` |
| Policy | Git、文档、配置、subagent 边界 | `Policies/` |
| Catalog | 机器可读索引和 schema 说明 | `Catalog/` |
| Runtime Config | hook、subagent、review mode 等运行配置指针 | `Config/` 与工具实际配置 |
| Tools | lint、sync、smoke 等维护工具 | `Tools/` |

## T1.1 Decisions To Resolve

- 是否新增 `Capabilities/` 目录承载 DesignDiscovery、SDDManagement、ValidationRelease 等能力正文。
- 是否新增 `Policies/SubagentPolicy.md`，将 subagent 默认只读、主对话统一写入和 dispatcher 暂缓规则落到长期事实源。
- 是否保留 `Skills/` 仅作为 wrapper skill policy 目录，避免与 `.ai-config/skills/` 混淆。
- 哪些 catalog 字段需要同步更新，哪些只需 README/INDEX 文档更新。

## Out Of Scope

- 不实现 hook JSON fallback。
- 不改 `.ai-config` wrapper skill 源。
- 不创建 DesignDiscovery capability 的完整执行协议。
- 不引入并行 subagent dispatcher。
