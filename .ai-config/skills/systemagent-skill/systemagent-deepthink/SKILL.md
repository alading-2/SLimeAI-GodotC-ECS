---
name: systemagent-deepthink
description: SystemAgent 深度思考 / 需求确认能力。用于用户要求深度思考、广泛搜索、方案设计、方向确认、不要急着实现，或需求零散缺信息且需要先识别问题是否存在、思路是否成立、风险、方案、默认假设和必须向用户确认的问题。
---

# systemagent-deepthink

## 定位

这是 SystemAgent 的 standalone capability skill，不是 SDD workflow，也不是 workflow entry wrapper。

它吸收 `superpowers:brainstorming` 的核心价值：先读上下文、识别范围、提出 2-3 个方案、说明推荐、暴露确认点；但不照搬 hook 强制触发、逐问逐答、默认写 `docs/superpowers/specs/` 或设计阶段自动 commit。

默认假设：用户给出的信息通常不完整，甚至可能把症状、根因、目标和方案混在一起。DeepThink 必须先帮助用户判断“问题是否真实存在”“当前思路有没有问题”“缺哪些信息和决策”，再进入方案推荐。

## 触发条件

- 用户要求深度思考、方案设计、设计确认或不要直接实现。
- 用户要求广泛搜索、详细分析、全面排查、帮忙找没说明清楚的问题。
- 用户提供的内容零散、目标/边界/成功标准不清，继续实现容易走错方向。
- 新功能、重构、迁移、workflow/skill/rule/gate/policy 改动进入计划或 SDD 前需要冻结方向。
- 实施中发现设计矛盾、范围过大、风险未处理、验收空洞或上下文不足。

## 必读

- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`（large 或高风险 medium 时）
- 相关 route、owner skill、DocsAI 或 SystemAgent fact source，按当前任务最小读取。
- 本地证据搜索结果：优先 `rg` / `semble` / 项目脚本，覆盖相关源码、文档、历史设计、现有约束和验证入口；记录搜索范围和未覆盖区域。
- 用户要求分析对话记录、效率、历史会话或当前执行轨迹时，先用 session-adapter 定位 ChatHistory 覆盖范围：`list-digests` 查看已生成 digest，`stale-report` 比较 Codex source JSONL 与 index；输出必须说明 `coverage=complete|stale|partial-current|unknown`、缺失 session 和使用的 digest path。
- 外部资料：仅当用户明确要求、任务依赖当前版本/官方 API/法规/价格/新闻等易变信息，或本地事实不足以判断时使用；必须区分 Evidence / Inference / Unknown。
- 只有任务使用正式 SDD、需要跨会话恢复或需要落盘时，才读取 `Workspace/SDD/docs/SDDFormat.md`、`Workspace/SDD/docs/CLI.md` 和当前 SDD 的 `README.md`、`design/INDEX.md`、`tasks.md`、`progress.md`、`bdd.md`。

## 输出指导

遵循 `Workspace/SystemAgent/Actors/DeepThink.md` 的思考点，但不强制固定标题。

必须讲清：

- 用户原始请求和 AI 归纳目标分别是什么。
- 问题是否真实存在，用户思路是否成立。
- 如任务基于对话记录，ChatHistory coverage、stale/missing 状态和 partial/current 风险是什么。
- 真实问题、误区、信息缺口和决策缺口是什么。
- 推荐方向、可选方案、默认假设和必须确认的问题是什么。
- 如果后续要写设计文档，交给 `systemagent-design-document` / `Workspace/SystemAgent/Rules/DesignDocument.md` 控制写作质量。

## 确认问题落点

- 不使用 SDD 时：最终回复必须有醒目的 `需要确认` 小节，列出 Must Confirm；若能用默认值推进，也列出 `默认假设`。
- 使用 SDD 或已有设计文档时：DeepThink 只提供分析结论、确认点和默认假设；不要把 DeepThink 内部检查点原样写成设计文档标题。
- 用户最终裁决、采用的默认假设和恢复点写入 `progress.md`；可执行事项写入 `tasks.md`；行为预期写入 `bdd.md`；参考来源和开放问题写入 `notes.md`。
- 追问必须通俗、具体、可回答；推荐格式是“问题 -> 为什么问 -> 不回答时默认值 / 影响”。

## 实施门禁

- `Must Confirm` 未解决时，不进入实现。
- 用户回复“按你的建议执行”时，可采用 `Recommendation` 和 `Defaults I Will Use`，但必须把默认假设写入最终回复；使用 SDD 时还要写入 `progress.md`。
- small 任务可压缩为 3-5 行自检，不强制完整 DeepThink 分析。
- 用户明确要求“广泛搜索 / 详细分析”时，不得只凭当前聊天内容输出方案；必须说明搜索范围、证据、推断和未知项。

## 禁止

- 不强制所有小任务都创建 SDD。
- 不把设计文档散落到临时 Idea 目录作为长期事实源。
- 不在设计未清楚时盲目实施大改。
- 不新增 brainstorming hook，不逐问逐答阻塞用户。
- 不复制 `Workspace/SystemAgent/Actors/DeepThink.md` 或 `Workspace/SystemAgent/Actors/DesignCritic.md` 正文；skill 只做入口。
- 不默认用户提出的问题一定存在，也不默认用户提出的方案一定正确。
- 不用空泛问题代替缺口分析。
- 不规定长期设计文档格式；需要写设计文档时使用 `systemagent-design-document`。
