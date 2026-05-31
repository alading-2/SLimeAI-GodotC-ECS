# DocsAI 框架文档索引

> 状态：current
> 定位：SlimeAI 框架仓统一文档入口，AI-first 设计。
> 更新：2026-05-31

## 入口

| 文档 | 作用 |
| ---- | ---- |
| [管理/README.md](./管理/README.md) | DocsAI 管理规则入口 |
| [管理/DocsAI统一管理与索引规则.md](./管理/DocsAI统一管理与索引规则.md) | 统一事实源、文档分层、索引维护、迁移规则 |
| [ECS框架与AIFirst方向决策.md](./ECS框架与AIFirst方向决策.md) | 方向决策事实源 |
| [ECS/README.md](./ECS/README.md) | 框架核心文档总览 + 阅读顺序 |
| [思考/README.md](./思考/README.md) | 设计思考与深度分析 |
| [Archive/README.md](./Archive/README.md) | 历史归档（不作为执行依据） |

## 工作区文档

工作区级文档（Git submodule、多游戏架构、AI 流程）见 `../Workspace/DocsAI/INDEX.md`。

## AI 路由规则

| 场景 | 读取目标 |
| ---- | ---- |
| 要维护 DocsAI 结构、索引或迁移规则 | `管理/DocsAI统一管理与索引规则.md` |
| 不懂概念或边界 | `ECS/README.md` 中对应 owner 的完整文档入口 |
| 要改代码或接入功能 | 对应 owner 的完整迁移文档和 `../Src/ECS/**` 源码 |
| 要验证或 Debug | 对应 owner 的测试章节、`Tests.md` 或验证脚本 |
| 需要理解设计思考 | `思考/<主题>/` |
| 需要理解历史决策 | `Archive/` 或 Concept.md 的"历史备注"节 |
| 中大型任务设计 | `../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/` |
| 方向决策 | `ECS框架与AIFirst方向决策.md` |
| 工作区操作 | `../Workspace/DocsAI/` |

## 非目标

- `Archive/` 和 `history` 文档不得作为实现依据
- `思考/` 文档不作为代码修改的直接依据，仅作为设计参考
- 已确定的 DocsAI 规则不得放入 `思考/`，必须放入 `管理/`
- `Src/ECS/` 不作为框架 Markdown 文档入口
- 不恢复 `Plans/` 目录
