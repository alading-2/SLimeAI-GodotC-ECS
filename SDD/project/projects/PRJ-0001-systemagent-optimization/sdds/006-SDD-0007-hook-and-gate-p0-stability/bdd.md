# Feature: Hook and Gate P0 Stability

> Hook 只做低频安全栏，Gate 读取稳定 SDD 证据。

## Scenario: Stop hook never breaks the conversation

- **Given** Stop hook 收到空 stdin、非法 stdin 或内部异常
- **When** hook 处理 Stop event
- **Then** stdout 仍然是目标工具接受的合法 JSON
- **And** hook 不运行耗时验证命令

## Scenario: PostToolUse advisory is deduplicated

- **Given** 同一 session 内连续触发同类 PostToolUse advisory
- **When** hook 判断该 advisory 已在 cooldown 内输出过
- **Then** hook 不重复输出完整提示
- **And** 新的敏感路径或验证命令仍能触发独立提示

## Scenario: Gate uses SDD evidence

- **Given** 当前任务使用 SDD
- **When** Reviewer 执行 completion 或 retrospective gate
- **Then** gate 优先检查 SDD tasks、progress、bdd 和 validation evidence
- **And** 不要求 AI 仅凭聊天记忆证明流程已完成
