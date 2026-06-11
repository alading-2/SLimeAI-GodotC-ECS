# SystemAgent 说明文档

> 生成日期：2026-06-11
> 状态：初版，基于 SystemAgent v0.3.0 分析

## 什么是 SystemAgent

SystemAgent 是 SlimeAI 工作区的 **AI 执行框架**——它不是代码框架，而是一套定义 AI 代理（Claude、Codex、Devin、Trae）在 SlimeAI 项目中如何工作的完整规范体系。

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

```
SlimeAI = AI-first ECS 游戏框架
        = ECS 主线（Entity/Component/Data/Event/System）
        + AI-first 工程层（SystemAgent + DocsAI + SDD + Skill + Log + Test）
        + 具体系统优化（PRJ-0002 中执行）
```

SystemAgent 是 AI-first 工程层的核心。它不包含 ECS 运行时代码，但定义了 AI 如何理解、修改、验证和复盘 ECS 代码。

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
- **测试**：仅 Godot 场景测试，无 NUnit/xunit
- **Worktree**：策略已写，从未实际使用
