# Godot C# ECS 框架文档入口

本文档目录面向人类阅读，目标是快速理解项目愿景、架构设计、模块能力和历史决策。`DocsAI/` 已删除；AI 执行任务时优先读取相关 SDD、`Src/ECS/**` 旁文档、owner skill 和项目脚本。

## 推荐阅读顺序

1. [框架项目索引](./框架/项目索引.md)
   - 当前最完整的项目导航，覆盖 ECS、Data、System、UI、Tools 和测试入口。
2. [AI-First Godot C# ECS 程序开发总说明](./框架/优化/AI/ai_first_godot_csharp_ecs_program_overview.md)
   - 项目从“人用框架”转向“AI 开发框架”的总说明。
3. [AI-First 计划目录](../Plans/README.md)
   - 计划分类目录和进入各 plan 的入口。
4. [AI-First 迁移计划索引](../Plans/Architecture/AI_First_Program_Migration/README.md)
   - 将 AI-First 迁移拆成多个可独立执行的阶段任务。
5. [AI-First 文档与 Skill 对齐计划](../Plans/Architecture/AI_First_Docs_Code_Alignment/README.md)
   - 旧迁移完成后的二轮文档收敛，按当前代码校准 Skill 和源码旁文档。
6. [AI-First 测试基础设施与文档深层对齐](../Plans/Architecture/AI_First_Test_Infra_Deep_Docs/README.md)
   - 当前执行中：GodotSkill 测试基础设施 + Movement/AI/Collision 文档深层审计。

## 核心模块

- ECS 架构：`Docs/框架/ECS/`
- UI 架构：`Docs/框架/UI/`
- 工具系统：`Docs/框架/工具/`
- 测试与日志：`Docs/测试/`，手动命令见 [手动测试指引](./测试/手动测试指引.md)
- 数据文档：`Docs/框架/文档/Data数据/`
- 优化与迁移计划：`Docs/框架/优化/`
- 历史思考和问题记录：`Docs/思考/`、`Docs/其他/记录/`

## 文档分层

- `Docs/`：人类阅读入口，讲 Why、What、架构设计、使用指南和决策背景。
- `Src/**/*.md`：源码附近说明，当前作为 AI 执行和模块契约的主要落点之一。
- `DocsNew/`：AI-first ECS 方向、边界和关键系统当前说明。
- `SDD/`：中大型任务的设计、任务、进度和验证事实源。
- `.claude/skills/` / `.codex/skills/`：AI 工具可用的项目 Skill，只保留触发条件、流程入口和命令。

## 维护规则

- 新增功能后，优先更新对应 `Docs/` 用户文档、`Src/ECS/**` 旁文档和相关 SDD。
- 修改框架接口、生命周期或流程时，必须同步检查相关 Skill 是否过期。
- 不把长篇 AI 执行细节写进 `Docs/README.md`，这类内容放到 SDD 或源码旁模块文档。
