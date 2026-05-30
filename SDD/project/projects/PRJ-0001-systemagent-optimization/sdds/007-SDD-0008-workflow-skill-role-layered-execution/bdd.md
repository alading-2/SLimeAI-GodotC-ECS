# Feature: Workflow / Skill / Role Layered Execution

> SystemAgent 入口更短，任务事实交给 SDD，角色和能力边界清晰。

## Scenario: Route output is stable

- **Given** 用户触发一个 SystemAgent workflow
- **When** AI 完成路由判断
- **Then** 输出包含 workflow、task_size、sdd、must_read、mode 和 subagent
- **And** 该输出可写入 SDD progress 或最终报告

## Scenario: Small task avoids full ritual

- **Given** 用户请求低风险单文件修正
- **When** task_size 被判断为 small
- **Then** AI 不创建 SDD
- **And** AI 仍执行读文件、git status、验证和总结的基本规则

## Scenario: Large task uses SDD artifact

- **Given** 用户请求跨目录 SystemAgent 改造
- **When** task_size 被判断为 large
- **Then** workflow 使用 SDD README、tasks、progress、bdd 作为恢复事实源
- **And** 不依赖 AI 临场回忆完成 gate
