# SystemAgent 优化补充设计索引

> 状态：current
> 日期：2026-06-11
> 定位：PRJ-0001 完成后的 SystemAgent 核心优化裁决入口；本目录现在同时承载 2026-06-11 的 SystemAgent 深度分析后续优化设计。

## Core Decision

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `2026-06-08-SystemAgent工作流内化与核心优化裁决.md` | design | current | 2026-06-09 | 当前主裁决：不做外层 AI CLI manager，不魔改 Warp；SystemAgent 聚焦项目内 workflow、SDD、ChatHistory、只读资料 subagent、hook 边界和任务拆分协议 |
| `Log与Debug边界裁决.md` | design | current | 2026-06-11 | 补充裁决：SystemAgent 属于 control plane；Log / Validation / Test 属于 evidence plane，第三部分源码调用点语义化仍是未完成大阶段 |

## Optimization Designs

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `TDD-测试系统优化设计.md` | design | proposed | 2026-06-12 | 纠正脱离 Godot 的测试框架方向；Data 试点改为行为标准答案先行、Godot scene 与 Validation artifact 证据 |
| `Hook系统重启设计.md` | design | proposed | 2026-06-11 | 将 Hook 从高频检查器重设计为低噪音提醒器；当前只记录可用节点，不启用 |
| `Code-Review优化设计.md` | design | proposed | 2026-06-12 | 重新定位 Code Review：功能实现度优先，补充必要质量维度，测试 artifact 只作为 evidenceRef |
| `FeatureSpec-功能实现规格设计.md` | design | proposed | 2026-06-13 | 将旧 BDD 升级为设计冻结后的功能实现规格，默认使用 `.FeatureSpec.md` 旁路文档承载功能、代码指引和 TDD 交接 |
| `Worktree激活设计.md` | design | proposed | 2026-06-11 | 在不默认开启的前提下，通过 skill + SDD 关联激活 git worktree 使用 |
| `SDD精简设计.md` | design | proposed | 2026-06-12 | 参考 OpenSpec 的轻量思想，改成薄任务状态面板，停止逐任务流水账、设计快照复制和冗余 BDD |

## Recommended Order

1. `SDD精简设计.md`
2. `FeatureSpec-功能实现规格设计.md`
3. `TDD-测试系统优化设计.md`（Data Godot/Validation 试点）
4. `Code-Review优化设计.md`
5. `Hook系统重启设计.md`（仅记录节点，暂不启用）
6. `Worktree激活设计.md`

## Reference Handoff

会话管理工具选型不再放在 `优化/` 主入口。需要查看 `codbash`、`codlogs`、`tracebase`、`claude-replay` 等工具取舍时，读：

- `../会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`
- `../会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
