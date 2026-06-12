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
| `TDD-测试系统优化设计.md` | design | proposed | 2026-06-11 | 分析 TDD/Test 规则与执行基础设施断裂，提出 NUnit 项目、共享测试基类、ValidationSession 采用、Flow 打印与 BDD 追溯方案 |
| `Hook系统重启设计.md` | design | proposed | 2026-06-11 | 将 Hook 从高频检查器重设计为低噪音提醒器，并与 review mode 集成 |
| `Code-Review优化设计.md` | design | proposed | 2026-06-11 | 将 code review 与 TDD artifact、ValidationSession、Retrospective 根因统计联动 |
| `Worktree激活设计.md` | design | proposed | 2026-06-11 | 在不默认开启的前提下，通过 skill + SDD 关联激活 git worktree 使用 |
| `SDD精简设计.md` | design | proposed | 2026-06-11 | 压缩 progress.md 膨胀、增加行数上限和去重规则，回到 AI-first 恢复上下文最小集 |

## Recommended Order

1. `SDD精简设计.md`
2. `TDD-测试系统优化设计.md`
3. `Hook系统重启设计.md`
4. `Code-Review优化设计.md`
5. `Worktree激活设计.md`

## Reference Handoff

会话管理工具选型不再放在 `优化/` 主入口。需要查看 `codbash`、`codlogs`、`tracebase`、`claude-replay` 等工具取舍时，读：

- `../会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`
- `../会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
