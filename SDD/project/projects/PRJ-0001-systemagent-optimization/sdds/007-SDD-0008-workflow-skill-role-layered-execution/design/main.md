# Workflow Skill Role Layered Execution Design

## Goal

把 SystemAgent 从“长入口链 + 临场理解”调整为“短入口路由 + workflow phase + capability skill + role 视角 + SDD artifact”的执行模型。

## Context

SDD-0006 已经为 Workflow、Capability、Role、Artifact、Gate、Policy 和 Subagent 提供正式落点。本 SDD 不再讨论目录放哪里，而是定义这些层如何在实际任务中协作。

## Design

本 SDD 的核心是四个契约：

1. Route contract：每次进入 SystemAgent workflow 输出 selected workflow、task_size、sdd、must_read、mode、subagent。
2. Workflow phase contract：workflow 第一屏说明触发条件、任务规模、SDD 策略、执行步骤和退出条件。
3. Skill boundary contract：workflow entry skill 保持短入口；capability skill 提供可复用能力；owner skill 只在实现阶段按影响面调用。
4. Role boundary contract：role 是执行视角和审查职责，不作为独立 skill 泛滥。

## Non-goals

- 不在本 SDD 实现 DesignDiscovery 细节。
- 不在本 SDD 重写 hook runtime。
- 不引入并行 subagent dispatcher。

## Verification

完成时应能从 README / INDEX / workflow-catalog / wrapper skill 看出同一套分层；如果改 `.ai-config`，必须运行 sync 和 skill-test。
