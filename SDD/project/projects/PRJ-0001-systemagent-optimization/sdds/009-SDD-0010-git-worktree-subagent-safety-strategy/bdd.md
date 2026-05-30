# Feature: Git Worktree and Subagent Safety Strategy

> worktree 是隔离工具，subagent 是可选执行基座；默认不并行写文件。

## Scenario: Dirty main workspace does not get overwritten

- **Given** 目标 Git 边界存在未提交或未跟踪改动
- **When** SystemAgent 判断任务需要隔离
- **Then** AI 报告 dirty 状态并建议 worktree
- **And** AI 不自动删除、覆盖或清理现有改动

## Scenario: Read-only subagent returns structured report

- **Given** workflow 调用只读 subagent 搜索资料或评审设计
- **When** subagent 返回结果
- **Then** 结果包含 Scope、Evidence、Inference、Unknown、Risks、Recommended Main-Thread Action
- **And** Files Touched 为 none

## Scenario: Dispatcher is deferred

- **Given** 任务看起来可以拆成多个工作包
- **When** worktree、work package、validation artifact 或 cleanup 策略未稳定
- **Then** SystemAgent 不启动并行写入 dispatcher
- **And** 主对话保留最终写入和合并责任
