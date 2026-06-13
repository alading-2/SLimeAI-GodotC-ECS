# Workflow Governance

读取时机：每次框架功能、重构、迁移、bugfix、DataOS、GodotBridge、游戏 adapter、skill / rule / command 变更开始前读取；如任务极小，至少按本文件完成验证和 retrospective 检查。

长期入口：`Workspace/SystemAgent/README.md` 与 `Workspace/SystemAgent/INDEX.md`。

## 完整闭环

```text
用户请求
-> 任务分类 / 边界判断
-> SDD
-> FeatureSpec 功能实现规格
-> TDD 验收标准先行
-> 实现
-> 验证
-> artifact 分析
-> AI retrospective
-> 必要时更新 DocsAI / skill / rule / direct hook/subagent config
-> 完成汇报
```

## SDD

大型功能、架构变更、跨模块重构、长期设计决策、迁移账本和跨目录文档治理必须进入 `SDD/active/<sdd>/`。

计划 artifact 必须包含验证项：

- `README.md`：范围和非目标。
- `design/`：技术方案、风险、回滚。
- `.FeatureSpec.md`：设计冻结后的功能、实现指引和 TDD 交接（如任务涉及功能实现）。
- `bdd.md`：当前任务行为场景摘录或 FeatureSpec Source 引用（如任务涉及行为验收）。
- `tasks.md`：实现任务和验证任务。
- `progress.md`：State / Next / Blocker、少量关键决策和验证入口。

执行中发现新增测试、文档、skill、artifact 或风险时，立即更新 `tasks.md`，不要等最终汇报补。

## TDD / 标准答案

这里的 TDD 是“先定义可复验标准答案”，不局限单元测试。

- Runtime / Capability：优先补可运行的 Godot headless scene、Validation artifact 或 owner 现有验证入口，写清输入、期望状态、期望事件或期望错误。
- DataOS：补 schema / migration / seed / generator / validator / runtime loader 断言，写清 source 与 generated snapshot。
- GodotBridge / 游戏 adapter：补独立 Godot validation scene，README 和 artifact 写清 `expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath`。
- 文档 / skill / rule：补 SDD validate、文件清单、sync 检查和 git status。
- hook / subagent：直接检查 `.claude/.codex` 项目配置文件清单；不要走 `.ai-config` 同步。

`run-main-smoke`、普通主场景或 playable acceptance 只能作为回归补充，不能替代新功能专项验证。

## 角色提示词

角色提示词事实源：`Workspace/SystemAgent/Roles/`。

需要 planner、implementer、test designer、reviewer、verifier、research analyst 或 retrospective 行为时，先读对应 `Workspace/SystemAgent/Roles/<Role>.md`。不要在一次性计划中发明长期 prompt。

## AI Retrospective

完成前必须回答：

- 本轮是否主动补了专项测试或验证场景？
- 标准答案是否写进 FeatureSpec、test、README、artifact 或 SDD bdd/design？
- 日志和 artifact 是否足够 AI 分析失败？
- `tasks.md` 是否反映执行中发现的新任务？
- 是否需要更新 DocsAI、owner skill、rule、direct hook/subagent config 或 role prompt？
- 用户最可能指出的方向问题是什么？是否已提前修正？

发现流程缺口时，优先在当前 SDD 内修正；不适合当前 SDD 的，记录 follow-up SDD 候选。

## Hooks / Subagent / MCP / Git

- hooks：直接维护 `.claude/settings.json` 和 `.codex/hooks.json`；第一阶段只做 advisory，不自动改文件、不自动提交或推送。启用阻断型 hook 必须单独 SDD。
- subagent：直接维护 `.claude/agents/*.md` 和 `.codex/agents/*.toml`；适合并行研究、测试设计和审查，不默认接管关键路径实现。
- `.ai-config`：只管理 skill、rule、command，同步副本；不要把 hook/subagent 放进 `.ai-config`。
- 外部资源：默认不预加载 `Resources/*`；需要时按 `Workspace/SystemAgent/Policies/ExternalResources.md` 记录 resource-id、scope、reason、expires。Context7 是 IDE/CLI 工具能力，不属于 SystemAgent 本地资源策略。
- git：见顶层 Git Safety 与 `Workspace/SystemAgent/Rules/Git.md`；AI 可按规则自动 commit/push，禁止 force push、历史改写、跨 git 边界提交或混入用户改动。
