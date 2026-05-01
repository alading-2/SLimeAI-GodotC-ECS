# AI-First 程序开发体系迁移计划索引

本文把 `ai_first_godot_csharp_ecs_program_overview.md` 拆成多个可独立执行的任务计划。每个计划都可以单独交给 AI 执行，但必须按依赖顺序推进。

## 执行顺序

当前执行状态见：

- `Progress.md`
- `Backlog.md`
- `Done.md`
- `PlanTemplate_Example.md`

这个计划目录内同时放计划说明、阶段拆分、进度和记录。`DocsAI/` 只放 AI 执行协议、模块契约、测试说明和 Skill 映射。

1. `01_DocsAI_Document_System_Migration.md`
   - 建立人类文档和 AI 文档的分层。
   - 后续所有 Skill、测试、长任务计划都依赖这个入口。
2. `02_Godot_CLI_Test_Debug_Loop.md`
   - 打通 Godot CLI 场景测试、日志摘要和失败判定。
   - 目标是减少人工打开 Godot、复制日志的重复劳动。
3. `03_Skill_System_Refactor.md`
   - 把 Skill 改成短入口 + DocsAI 详细文档 + scripts 的结构。
   - 让 AI 在开发功能时能自动加载正确上下文。
4. `04_Framework_Module_Contract_Docs.md`
   - 为 Entity / Component / Data / Event / System 等核心模块建立 AI 可读契约。
   - 目标是减少代码审查时发现“方向不对”的概率。
5. `05_Long_Task_Context_And_Project_Plans.md`
   - 建立长任务上下文恢复、任务拆分、阶段交接和项目级计划文件。
   - 解决上下文清空后 AI 无法继续的问题。
6. `06_AI_First_Feature_Development_Pilot.md`
   - 选择一个小功能跑完整 AI 开发闭环。
   - 用真实功能验证文档、Skill、测试、日志是否能闭环。
7. `07_Core_Regression_And_Review_Gate.md`
   - 建立 ECS 核心修改审查门禁和回归测试矩阵。
   - 目标是保护底层框架质量。

## 总原则

- 每个计划只做一个阶段，不混做。
- 每个阶段完成后必须更新对应索引和下一阶段输入。
- 不要求一次性重构全部历史文档，优先建立新入口和迁移规则。
- 不直接改 ECS 核心代码，除非对应计划明确要求。
- 每个阶段必须输出：完成内容、修改文件、验证命令、验证结果、风险点、建议人工审查位置。

## 当前推荐起点

从 `01_DocsAI_Document_System_Migration.md` 开始。

原因：

- 现在 `Docs/`、`Src/**/*.md`、`.codex/skills` 的职责混在一起。
- 如果先改 Skill 或测试流程，后续还会因为文档入口变化反复改链。
- 先固定 Docs / DocsAI / Skill 边界，后续任务才能减少返工。
