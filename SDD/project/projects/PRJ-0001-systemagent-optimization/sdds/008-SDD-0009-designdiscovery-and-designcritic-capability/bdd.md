# Feature: DesignDiscovery and DesignCritic Capability

> 中大型任务实现前先形成确认包，设计缺陷在计划冻结前暴露。

## Scenario: Medium or large feature receives a confirmation package

- **Given** 用户请求中大型新功能、重构或行为改动
- **When** SystemAgent 调用 DesignDiscovery
- **Then** AI 输出 Goal、Risks、Options、Recommendation、Must Confirm 和 Defaults
- **And** 关键默认假设写入当前 SDD progress 或 design

## Scenario: User approves recommendation without answering every question

- **Given** DesignDiscovery 输出 Must Confirm 和 Defaults I Will Use
- **When** 用户回复按推荐执行
- **Then** AI 使用推荐方案和默认假设继续
- **And** 不进行逐问逐答式阻塞

## Scenario: Small task uses compressed self-check

- **Given** 用户请求低风险小任务
- **When** task_size=small
- **Then** DesignDiscovery 不输出完整确认包
- **And** AI 只记录必要假设和验证方式
