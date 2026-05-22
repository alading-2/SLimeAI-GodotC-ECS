# DocsAI 入口

`DocsAI/` 是本项目给 AI 使用的工程上下文目录。它不追求人类阅读体验，优先保证 AI 能快速找到任务流程、模块契约、测试方式和上下文恢复信息。

## AI 开始任务时先读

1. `AGENTS.md`
   - 项目硬规则、语言、安全要求和架构红线。
2. `DocsAI/README.md`
   - AI 文档体系入口。
3. `DocsAI/INDEX.md`
   - 按任务类型查找文档。
4. `DocsAI/ProjectState.md`
   - 当前旧 ECS 主线纠偏状态、历史迁移材料分类、下一步。

如果任务触发了 Skill，还要读对应 `.codex/skills/*/SKILL.md`。Skill 负责流程入口，详细知识以 DocsAI 为准。

## 当前定位

项目当前定位是旧 Godot C# ECS 框架主线。此前 Godot AI Game OS / SkilmeAI 多仓库迁移材料保留为 history / migration-pointer，不再作为默认实现入口。工作区级纠偏入口是 `/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`，当前 OpenSpec change 是 `/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`。

项目目标是建立一个 AI-First Godot C# ECS 游戏程序开发框架，让 AI 能够在明确约束下完成：

- 阅读文档和 Skill
- 分析现有代码结构
- 开发 System / Component / Event / Tool
- 编写或更新测试
- 执行构建和 Godot 场景验证
- 读取日志并修复问题
- 同步更新文档
- 输出风险点和人工审查重点

当前阶段先关注目录、事实源、模块契约、验证和任务闭环；旧 `Src/ECS` 核心代码优化必须在后续明确 OpenSpec change 中小步执行。

## 文档类型

- `Workflows/`：任务流程、文档迁移、长任务恢复、调试闭环。
- `Modules/`：Entity、Component、Data、Event、System 等模块契约。
- `Tests/`：Godot 场景测试、测试矩阵、日志判定。
- `Skills/`：Skill 设计规则和 Skill 到 DocsAI 的映射。
- `Research/`：外部框架和成熟方案研究记录。
- `Archive/`：过期 AI 文档和迁移中暂存内容。

项目级执行计划统一放在仓库根目录 `Plans/`。`DocsAI` 不再新建独立 `Plans/`，避免计划入口分叉。

## 历史材料分类

| 路径 | 分类 | 当前动作 |
| --- | --- | --- |
| `DocsAI/Modules/`、`DocsAI/Tests/`、`DocsAI/Workflows/ECS核心修改门禁.md` | current | 旧 ECS 修改仍需读取 |
| `DocsAI/Protocols/AI原生数据层协议.md`、`DocsAI/Protocols/AI表现复盘协议.md` | reference | 可借鉴机制，不能替代当前 ECS 路由 |
| `DocsAI/Protocols/SkilmeAI多仓库AI工作流协议.md` | history / migration-pointer | 仅解释旧多仓库方向，不作为当前默认入口 |
| `Plans/Architecture/Godot_AI_Game_OS_Migration/`、`Plans/Architecture/SkilmeAI_多仓库彻底迁移/`、`Plans/Architecture/框架整体迁移/` | history / migration-pointer | 保留为历史计划和旧方向复盘 |
| `.history/` | archive | 不删除，不作为当前 AI 入口 |
| `.codex/skills/` | local legacy skill source or copy, unresolved | 当前不改；后续决定是否迁入工作区 `.ai-config` |

## 核心原则

- AI 不靠猜：模块职责、输入输出、生命周期、测试方式必须能从文档或代码找到。
- AI 不只写代码：修改后必须能构建、运行测试、读日志、修复问题。
- 文档按需加载：Skill 保持短，详细上下文进入 DocsAI。
- 长任务可恢复：跨多轮任务必须更新 `ProjectState.md` 和对应计划文件夹内的 `Progress.md / Backlog.md / Done.md`。
- 人类审查聚焦架构：AI 最终输出必须说明风险点和建议人工审查位置。

## 最小完成标准

一个程序任务至少满足：

1. 实现完成。
2. 构建通过，或说明无法构建的原因。
3. 相关测试通过，或说明缺失测试和风险。
4. 运行日志无新增关键错误。
5. 相关 Docs / DocsAI / Skill 已同步检查。
6. 最终回复包含修改文件、验证命令、验证结果、风险点。
