# SystemAgent 说明文档

> 生成日期：2026-06-11
> 状态：初版，基于 SystemAgent v0.3.0 分析

## 什么是 SystemAgent

SystemAgent 是 SlimeAI 工作区的 **AI 执行框架**——它不是代码框架，而是一套定义 AI 代理在 SlimeAI 项目中如何工作的完整规范体系。

### 一句话定位

**让 AI 从"工具调用者"变成"有流程意识的工程代理"。**

### 核心问题

AI 写代码最常见的失败模式：
1. **不读文档就动手** → SystemAgent 的 must-read 入口链解决
2. **改了不验证** → Review Gate 强制验证
3. **验证了不复盘** → Retrospective 强制复盘
4. **复盘了不记住** → Session Adapter 让历史可检索
5. **记住但不更新文档** → DOC-SYNC Gate 强制文档同步

### 与 SlimeAI 框架的关系

```text
SlimeAI = AI-first ECS 游戏框架
        = ECS 主线（Entity/Component/Data/Event/System）
        + AI-first 工程层
          ├─ Control Plane：SystemAgent + SDD + Skill + Gate + Retrospective
          └─ Evidence Plane：DocsAI + Log + Validation + Test + artifact + logctl
        + 具体系统优化（PRJ-0002 中执行）
```

SystemAgent 是 AI-first 工程层中的 **control plane**。它不包含 ECS 运行时代码，但定义了 AI 如何理解、修改、验证和复盘 ECS 代码。Log / Validation / Test 不属于 SystemAgent 本体，而是它依赖的 **evidence plane**。

## 文档索引

| 文档 | 内容 | 适用读者 |
|------|------|----------|
| [01-理念与架构](01-理念与架构.md) | 设计哲学、五层洋葱模型、信息架构 | 首次了解 SystemAgent |
| [02-工作流详解](02-工作流详解.md) | 6 个 Route 的完整说明 | 需要执行具体工作流 |
| [03-角色系统详解](03-角色系统详解.md) | 15 个 Actor 的职责、输入输出、禁止行为 | 理解 AI 扮演的角色 |
| [04-审查门与规则](04-审查门与规则.md) | 8 个 Review Gate + 10 个 Rule | 理解质量保障机制 |
| [05-Session-Adapter详解](05-Session-Adapter详解.md) | 会话历史适配器的架构和使用 | 理解跨会话知识恢复 |
| [06-SDD系统详解](06-SDD系统详解.md) | SDD 任务上下文管理系统 | 使用 SDD 管理中大型任务 |
| [07-TDD与测试系统](07-TDD与测试系统.md) | TDD 协议、测试模式、Logger 在测试中的角色 | 编写和理解测试 |
| [08-Git-Worktree策略](08-Git-Worktree策略.md) | Git 边界、worktree 管理、多对话隔离 | 多任务并行开发 |
| [09-完成度与不足分析](09-完成度与不足分析.md) | 各部分完成状态、已知问题、优化方向 | 了解当前状态和未来方向 |
| [10-Debug工作流与证据链](10-Debug工作流与证据链.md) | DebugFix、Log、Validation、Test、Gate、Retrospective 的证据链协作 | 理解 AI 如何基于证据调试 |
| [11-FeatureSpec功能实现规格](11-FeatureSpec功能实现规格.md) | 设计冻结后把功能、代码落点和 TDD 交接写清楚 | 从设计进入实现前 |

## 当前状态快照

- **版本**：0.3.0（schema v3）
- **Route**：6 个，全部完整定义
- **Actor**：15 个，全部有 SKILL.md
- **Review Gate**：8 个，全部有 5 字段结构
- **Rule**：10 个，全部生效
- **Hook**：暂停中（等待重新设计）
- **Subagent**：4 个注册，全部只读
- **Session Adapter**：活跃，支持 Claude/Codex/OpenCode
- **SDD**：39 done，2 blocked，2 projects
- **测试**：当前主要是 Godot 场景测试；Data 试点走行为标准答案 + Validation artifact，不新增脱离 Godot 的测试框架
- **FeatureSpec**：设计冻结后默认用 `.FeatureSpec.md` 描述功能、实现指引和 TDD 交接；旧 BDD 收缩为行为场景层
- **Log / Observation**：记录层、离线整理层第一版已落地；源码调用点语义化层未完成
- **Worktree**：策略已写，从未实际使用

## Hook 使用边界

Hook 当前仍暂停，不作为自动 gate 使用。后续如果重启，只允许作为低噪音提醒器出现在少数节点：

| 节点 | 可提醒内容 | 不做什么 |
| --- | --- | --- |
| SessionStart | 当前 active / blocked SDD、State、明显下一步 | 不自动改变 SDD 状态 |
| FirstEdit | 首次编辑前提醒 must-read 和事实源边界 | 不拦截探索性读取 |
| PostValidation | build/test/validate 后提醒是否需要 Code Review / Verifier | 不自己判断代码是否合格 |
| PreStop | 有实质改动时提醒 progress / retrospective / git status | 不自动提交或自动复盘 |

Hook 不应在每次工具调用后输出，也不应替代 Reviewer、Verifier、Code Review 或 SDD validate。
