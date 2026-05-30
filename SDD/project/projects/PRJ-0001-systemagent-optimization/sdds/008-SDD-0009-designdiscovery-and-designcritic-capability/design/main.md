# DesignDiscovery and DesignCritic Capability Design

## Goal

将“实现前深度思考”制度化为 SystemAgent capability：DesignDiscovery 可独立运行，也可被 workflow 调用；DesignCritic 作为条件角色负责发现缺陷、遗漏、风险和替代方案。

## Context

现有 SDD 工作流已经能保存任务上下文，但设计发现仍容易停留在聊天中。本 SDD 负责把设计发现的输入、输出、落盘位置、触发条件和角色边界写入正式事实源。

## Design

DesignDiscovery 输出一个确认包：Goal、Context Read、Main Risks、Options、Recommendation、Must Confirm、Should Confirm、Defaults I Will Use、Not Recommended、SDD Updates。

DesignCritic 不作为 skill 泛滥，而作为设计阶段 role 或可选独立评审视角。它的输出聚焦 Assumptions、Missing Context、Design Defects、Better Options、Trade-offs、User Decisions、Recommendation、SDD Updates。

## Non-goals

- 不照搬 superpowers 的逐问逐答流程。
- 不通过 hook 强制触发。
- 不要求所有小任务都进入完整 DesignDiscovery。
- 不自动 commit 设计文档。

## Verification

完成时应能通过 workflow、capability 文档、role 文档、catalog 和 wrapper skill 看到一致的 DesignDiscovery 入口与 DesignCritic 角色边界；如改 `.ai-config`，必须 sync/lint。
