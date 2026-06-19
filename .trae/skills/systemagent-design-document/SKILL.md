---
name: systemagent-design-document
description: SystemAgent 设计文档 / 执行规格写作质量能力。用于新建或修改设计文档，或用户要求从设计生成执行文档、执行规格、FeatureSpec、实现草案、代码落点说明时；确保保留用户原始问题，优先写清问题分析、解决思路、功能行为、代码落点和验证交接，避免把 DeepThink 确认包、证据、风险、验证等流程字段机械格式化为正文。
---

# systemagent-design-document

## 定位

这是设计文档和 FeatureSpec 写作质量 skill，不是 DeepThink、Planner、Documentarian 或 SDD workflow。

它只管长期文档应该把什么写好：设计文档写用户原始问题、问题分析、解决思路；FeatureSpec / 执行文档写功能、行为、代码落点、TDD 交接和 review checklist。它不重新分析问题，不审方案对错，不拆 SDD 任务，不管理 SDD 状态，也不负责 README / INDEX 同步。

## 必读

- `Workspace/SystemAgent/Rules/DesignDocument.md`
- `Workspace/SystemAgent/Docs/11-FeatureSpec功能实现规格.md`（当用户要求执行文档、实现草案、代码怎么改、FeatureSpec 或设计到实现交接时）
- 当前要创建或修改的设计文档。
- 如设计来自 DeepThink / DesignCritic / Planner，读取其结论或当前对话中等价的分析结果。

## 执行原则

- 保留用户原始提问；短请求可直接引用，长请求可放附录或相邻 `source-request.md` 并在正文引用。
- 先写真实问题，再写解决思路；不要先堆流程、搜索范围、工具调用或固定模板标题。
- 用户说“看不懂怎么改”“先生成执行文档”“不需要完整但要知道代码大概要怎么改”时，优先新建同目录 `*.FeatureSpec.md` 草案；文件名可以按执行主题命名，不要求和设计文档同名。
- 设计冻结后如需要指导 AI 实现，优先新建同目录 `<主题>.FeatureSpec.md`，不要把完整功能实现规格塞进设计文档正文。
- FeatureSpec 草案至少写清目标/非目标、当前代码问题点、建议新增或调整的关键类型/方法、第一刀切法、最小验证和仍需确认的问题。
- 证据、风险、验证和开放问题按需要写，能融入问题分析或解决思路就融入，不强制单独成章。
- 删除或压缩 DeepThink / DesignCritic 的内部过程字段；长期设计文档不应出现完整确认包标题序列。
- 允许自然标题，标题服务内容，不要求完全统一格式。

## 输出要求

- 说明设计文档是否已保留用户原始问题。
- 说明问题分析和解决思路是否清楚。
- 若生成 FeatureSpec / 执行文档，说明它是否写清功能行为、代码落点、非目标和验证交接。
- 如没有修改文件，说明建议调整点。

## 禁止

- 不把设计文档改成固定模板。
- 不用 `Evidence / Risks / Validation` 等章节凑完整性。
- 不替代 DeepThink 做方向分析。
- 不替代 Documentarian 更新索引。
- 不替代 sdd-management 创建、启动、完成或校验 SDD。
