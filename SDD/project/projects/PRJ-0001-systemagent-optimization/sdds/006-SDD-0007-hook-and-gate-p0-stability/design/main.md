# Hook and Gate P0 Stability Design

## Goal

稳定 SystemAgent hook 与 review gate 的基础行为，先修复会中断对话或制造提示疲劳的 P0 问题，再为后续 SDD-aware gate 奠定输入格式。

## Context

当前问题集中在三类：Stop hook 可能输出非法 JSON 并打断对话；PostToolUse 提示频率过高导致 AI 忽略；Gate 需要 selected workflow、must-read、tasks、progress、bdd 和 validation evidence，但这些输入缺少稳定来源。

SDD-0006 已完成信息架构刷新，Hook/Gate 后续改动应落在新的 SystemAgent 目录和 policy 边界内，不再回到旧入口。

## Design

本 SDD 分三层推进：

1. Hook P0 stability：统一 JSON 输出路径、异常 fallback、stdout/stderr 边界和 Stop 事件耗时命令禁用。
2. Hook noise control：为 PostToolUse 增加同类提示去重、cooldown 和关键路径触发条件。
3. Gate input contract：将 gate 输入从临场回忆迁移到 SDD artifact，包括 route summary、must-read 状态、tasks、progress、bdd 和 validation evidence。

## Non-goals

- 不实现完整 workflow 重写。
- 不实现 DesignDiscovery capability。
- 不新增并行 subagent dispatcher。
- 不让 hook 自动创建或修改 SDD。

## Verification

完成时至少需要 hook smoke 覆盖 Claude / Codex 的关键事件，SDD 校验通过，git diff 检查通过；如修改 `.ai-config` 或 hook/subagent 配置，追加 sync / lint / smoke 证据。
