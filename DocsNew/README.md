# DocsNew

> 状态：方向决策目录入口。
> 范围：只记录旧 Godot C# ECS 主线与 AI-first 方向的定位、边界、弯路和参考来源；不承接具体系统实现方案。
> 说明：`../DocsAI/` 已删除，不再作为当前入口；当前模块事实源临时回到 `../Src/ECS/` 旁文档和 SDD design。

## 当前事实源

| 文档 | 作用 |
| ---- | ---- |
| [`01-ECS框架与AIFirst方向决策.md`](./01-ECS框架与AIFirst方向决策.md) | 当前方向事实源：确认 SlimeAI 继续走 AI-first ECS 游戏框架，而不是纯 AI/GameOS 替代旧 ECS |
| [`02-Data系统说明.md`](./02-Data系统说明.md) | 当前旧 ECS Data 系统实现说明：概念、使用方式、测试场景和事件 |

## 阅读顺序

1. **方向定位**：读 [`01-ECS框架与AIFirst方向决策.md`](./01-ECS框架与AIFirst方向决策.md)。
2. **Data 当前实现**：读 [`02-Data系统说明.md`](./02-Data系统说明.md)。
3. **具体系统设计**：进入 [`../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/`](../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/)。
4. **执行前契约**：先读对应 `../Src/ECS/**` 旁文档，再读 owner skill 和项目脚本。

## 边界

DocsNew 优先回答“为什么是 AI-first ECS，以及哪些方向不要再走”。当前 Data 系统补充一份实现说明，用于降低跨 SDD 阅读成本；完整设计、任务和验证证据仍以 SDD / `Src/ECS` 旁文档为准：

| 内容 | 事实源 |
| ---- | ---- |
| Data / Event / Entity / Relationship / 字符串键名等优化分析 | `../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/` |
| 当前模块入口、规则、验证方式 | `../Src/ECS/**` 旁文档、owner skill、项目测试脚本 |
| 代码实现计划、验证矩阵、阶段进度 | 后续执行型 SDD |

## 本方案参考来源

DocsNew 当前方向不是凭空制定，主要参考了以下来源：

- **旧 ECS 主线现状**：`../Src/ECS/`、`../Docs/框架/项目索引.md`，用于确认当前可工作的 ECS 基础概念、模块入口和验证现状。
- **PRJ-0002 优化分析**：`../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/`，用于承接 Data / Event / Entity / Relationship / 字符串键名等具体系统问题。
- **AI-first 参考实现**：`../../SlimeAI-AiFirst/`，参考其 DataOS、typed DataKey、typed Event、Observation、Capability owner、验证 artifact 等思想，但不把当前旧 ECS 直接替换成新 GameOS。
- **外部框架分析资料**：`../../Resources/Engine/Docs/`，参考 Bevy、Unity DOTS、Unreal GAS、DefaultEcs 等框架的 ECS 分层、数据驱动、验证和工具链经验，同时避免复制不适合 Godot C# 主线的 public API。
- **工作区 AI 流程资产**：`../../Workspace/SystemAgent/`、`../../Workspace/SDD/`，参考 SDD、Skill、Workflow、Gate、Validation 的 AI 可执行流程。
- **网上 ECS / 数据驱动资料**：用于辅助理解 authoring data、runtime data、baking / snapshot、schema validation 等通用设计，但最终以本仓旧 ECS 主线和 PRJ-0002 为准。

## 非目标

- 不把 `Src/ECS` 默认迁出到新 GameOS。
- 不引入第三方 ECS 运行时依赖。
- 不复制 Bevy / Unity DOTS / Unreal GAS / DefaultEcs 的 public API。
- 不在 DocsNew 直接规定 Data / Event / Entity / Relationship / System 的具体改造方案。
- 不让 AI 通过全局 query、裸字符串、隐式工作流随意修改框架。
