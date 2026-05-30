# Workflow Skill Role Execution Plan

## Target Contracts

| Contract | Expected Result |
| --- | --- |
| Route output | selected workflow、task_size、sdd、must_read、mode、subagent 可被 gate 或 progress 引用 |
| Workflow first screen | 前 30 行能说明怎么执行，而不是只列制度说明 |
| Entry skill | 只负责触发、必读、禁止事项和 workflow 指针 |
| Capability skill | 可独立运行、可被 workflow 调用、有输入输出和 artifact 边界 |
| Role | 作为视角和责任边界，不膨胀成 skill 路由 |

## Execution Order

1. 先更新 route 输出和 workflow-catalog 的共享约定。
2. 再重写 workflow 文件的第一屏和 phase 结构。
3. 再检查 wrapper skill 是否仍是短入口。
4. 最后同步 catalog、docs、rules 或 skill lint 证据。
