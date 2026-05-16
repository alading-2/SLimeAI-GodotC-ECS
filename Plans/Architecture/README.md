# Architecture Plans

本目录放架构级、框架级、长期治理类计划。

## 当前计划

- `Godot_AI_Game_OS_Migration/`
  - Godot AI Game OS 迁移执行计划：承接 `框架整体迁移/迁移.md`，记录阶段状态、资产盘点、新工作区骨架和从当前仓库恢复执行的入口
- `SlimeAI_多仓库彻底迁移/`
  - SlimeAI 多仓库彻底迁移计划：当前 `brotato-my` 降级为迁移输入仓库，新建 `SlimeAI` 顶层工作区，拆分 AI 框架主仓库、Godot 引擎仓库和独立游戏仓库；通过项目引用 / 本地 NuGet / DLL 分阶段管理 GameOS 依赖
- `框架整体迁移/`
  - Godot AI Game OS 框架彻底迁移方案：不保留旧兼容，以 AI 长任务开发、Capability、DataOS、Validation、Observation、外部源码研究、经验库、Skill 和 MCP 接口为目标重构项目结构
- `AI_First_Program_Migration/`
  - AI-First Godot C# ECS 程序开发体系迁移（全部 7 阶段已完成 ✅）
  - 目录内包含总说明、阶段任务、进度、待办、完成记录
- `AI_First_Docs_Code_Alignment/`
  - AI-First 文档与 Skill 按当前代码对齐（第 1-11 批已完成）
  - 短 Skill 入口、DocsAI 契约、源码旁 README 压缩
- `AI_First_Test_Infra_Deep_Docs/`
  - GodotSkill 测试基础设施完善 + Movement/AI/Collision 文档深层审计与迁移（已完成）
- `AI_First_Src_Docs_Deep_Audit/`
  - Src .md 完全删除（40 个文件）、DocsAI / Docs / .claude 引用清理（已完成）
- `AI_First_Feature_Dev_Pilot/`
  - Phase 06: LifecycleComponent 复验（已完成）
- `Core_Regression_Gate/`
  - Phase 07: ECS 核心回归门禁与 Skill 接入（已完成）

## 约定

- 一个实际计划只占一个文件夹。
- 计划说明、阶段拆分、进度、待办、完成记录都放在该计划文件夹内。
- 不再为同一个计划额外创建平级 `_meta` 目录。
