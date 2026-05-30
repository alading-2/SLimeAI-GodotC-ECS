---
name: sdd-design-discovery
description: SDD 设计发现能力。用于实现前深度梳理上下文、目标、约束、方案、风险、确认点，并把设计写入当前 SDD。
---

# sdd-design-discovery

## 触发条件

- 用户要求深度思考、方案设计、设计确认或不要直接实现。
- 当前 SDD 缺少明确设计、验收标准或任务拆分。
- 实施中发现设计矛盾、范围过大、风险未处理或上下文不足。

## 必读

- `Workspace/SystemAgent/Actors/DesignDiscovery.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`（large 或高风险 medium 时）
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`
- 当前 SDD 的 `README.md`
- 当前 SDD 的 `design/INDEX.md`
- 当前 SDD 的 `tasks.md`
- 当前 SDD 的 `progress.md`

## 输出结构

遵循 `Workspace/SystemAgent/Actors/DesignDiscovery.md` 的确认包字段：

1. Goal：本任务要解决什么问题和非目标。
2. Context Read：已读取的事实源、未读上下文和 git boundary。
3. Main Risks：实施、维护、验证和边界风险。
4. Options：2-3 个可选方案及取舍。
5. Recommendation：推荐方案和原因。
6. Must Confirm：不确认就不能安全推进的问题。
7. Should Confirm：建议确认但可用默认值推进的问题。
8. Defaults I Will Use：用户不补充时采用的默认假设。
9. Not Recommended：不建议方向和原因。
10. SDD Updates：需要写入的 design、tasks、bdd、progress、notes 更新。

## 写入规则

- 设计正文写入当前 SDD 的 `design/`。
- 短索引写入 `design/INDEX.md`。
- 可执行事项写入 `tasks.md`。
- 关键结论和恢复点写入 `progress.md`。
- 行为约束写入 `bdd.md`。

## 禁止

- 不强制所有小任务都创建 SDD。
- 不把设计文档散落到临时 Idea 目录作为长期事实源。
- 不把完整 SDD 制度复制到 SystemAgent workflow 或 wrapper skill 中。
- 不在设计未清楚时盲目实施大改。
- 不复制 `Workspace/SystemAgent/Actors/DesignDiscovery.md` 或 `Roles/DesignCritic.md` 正文；wrapper 只做入口。
