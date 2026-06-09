# Workspace DocsAI 索引

本目录存放 AI 专用的工作区级别文档，覆盖跨仓库工作流、git submodule 管理、工作区打开方式和历史评审记录。当前 SystemAgent 正文事实源在 `Workspace/SystemAgent/`。

## 文档清单

| 文档 | 内容 |
|------|------|
| [../SystemAgent/README.md](../SystemAgent/README.md) | SystemAgent 唯一事实源根入口 |
| [../SystemAgent/INDEX.md](../SystemAgent/INDEX.md) | SystemAgent workflow 路由索引 |
| [GitSubmoduleWorkflow.md](GitSubmoduleWorkflow.md) | git submodule 操作唯一指南，含故障排查 |
| [OpenWorkspace.md](OpenWorkspace.md) | 工作区打开方式与目录结构说明 |
| [MultiGameLayout.md](MultiGameLayout.md) | 多游戏架构与工作流（从框架 DocsAI 迁移） |
| [思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md](思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md) | 游戏开发 SystemAgent 流程 Agent 历史长分析；当前精简裁决已迁入 `PRJ-0001/design/优化/` |
| [SlimeAINewReorientation/00-README.md](SlimeAINewReorientation/00-README.md) | SlimeAINew 旧 ECS 框架回归主线与新 GameOS 冻结参考的规划文档包 |
| [Reviews/2026-05-18-systemagent-mvp-consolidation-analysis.md](Reviews/2026-05-18-systemagent-mvp-consolidation-analysis.md) | SystemAgent MVP 统一收敛分析记录 |
| [Reviews/2026-05-14-openspec-first-round-refactor-review.md](Reviews/2026-05-14-openspec-first-round-refactor-review.md) | OpenSpec 第一轮 AI-first 重构计划质量评审 |

## 原型与历史目录

- `Workspace/SlimeAISystemAgent/`：runtime prototype / 历史实验目录，不是当前 SystemAgent 正文入口。
- `Workspace/DocsAI/Idea/`：方向草案与历史想法，除非迁入 `Workspace/SystemAgent/` 或 OpenSpec baseline，否则不作为当前规则。

## 相关文档

- SystemAgent 正文：`Workspace/SystemAgent/README.md`
- 框架文档：`DocsAI/README.md`（统一入口，按 ECS 分类 + owner 聚合）
- 框架设计思考：`DocsAI/思考/`
- 游戏文档：`Games/BrotatoLike/DocsAI/INDEX.md`
