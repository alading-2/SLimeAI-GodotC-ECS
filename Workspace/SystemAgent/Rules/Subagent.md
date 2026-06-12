# Subagent Policy

## Source-of-truth boundary

Subagent 是可选执行基座，不是 workflow、skill 或 role 的同义词。SystemAgent 的 subagent 策略写在本文件；具体运行配置仍在工具侧配置中直接维护。

| 内容 | 事实源或维护源 |
| --- | --- |
| Subagent 使用策略 | `Workspace/SystemAgent/Rules/SubagentPolicy.md` |
| Claude subagent 配置 | `.claude/agents/` |
| Codex subagent 配置 | `.codex/agents/`、`.codex/config.toml` |
| 其他 AI 工具的子代理 | IDE/CLI 工具内置能力，不作为项目内可维护配置 |

## Allowed usage

- 只读资料搜索。
- 多源研究汇总。
- 独立评审或测试设计检查。
- 大型只读审计。
- 设计冻结前的独立批判视角。
- 验证证据或文档一致性审计。

只读 subagent 输出必须包含：

- Scope
- Evidence
- Inference
- Unknown
- Risks
- Recommended Main-Thread Action
- Files Touched: none

字段含义：

| Field | Meaning |
| --- | --- |
| Scope | subagent 实际查看的目录、文档、命令或问题边界 |
| Evidence | 可复查的路径、行号、命令摘要或 artifact |
| Inference | 基于 evidence 推导出的结论，必须与 evidence 区分 |
| Unknown | 未读取、无法验证或仍需主对话确认的信息 |
| Risks | 如果主对话采纳建议可能产生的边界、验证或冲突风险 |
| Recommended Main-Thread Action | 建议主对话执行的下一步；subagent 不直接执行写操作 |
| Files Touched | 只读 subagent 必须为 `none` |

## Write boundary

- 默认不允许 subagent 写文件。
- 写操作必须由主对话统一执行。
- 多个 subagent 不得同时写同一个事实源。
- 跨 Git 边界写操作不得由 subagent 并行执行。
- subagent 不能 commit、push、创建 worktree、删除 worktree、清理文件或修改 `.ai-config` 同步源。
- 如果未来确需写入型 subagent，必须先有独立 SDD 设计 work package、文件 owner、worktree、验证 artifact、cleanup 和冲突处理策略。

## Dispatcher boundary

当前不实现 tackle 式 agent dispatcher，也不把并行实现作为默认 workflow。只有在以下基础稳定后，才可单独设计 dispatcher：

1. SDD work package 格式稳定。
2. worktree 策略落地。
3. 文件 owner 协议明确。
4. validation artifact 标准稳定。
5. cleanup、timeout、retry 和冲突处理策略明确。

## Relationship to workflow / capability / role

- Workflow 可以决定是否调用 subagent。
- Capability 可以把只读研究或独立评审委托给 subagent。
- Role 可以由主对话执行，也可以在需要独立视角时由 subagent 执行。
- Subagent 输出由主对话裁决，不能替代最终设计取舍。
