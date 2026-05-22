# AI-First 计划目录

本目录只放项目级执行计划。先按分类分文件夹，再在分类目录下放“一个计划一个文件夹”。

## 目录约定

- `Architecture/`：架构、框架、治理类计划
- 后续可按需要增加 `Gameplay/`、`Refactor/`、`Tools/` 等分类
- 每个实际计划独占一个文件夹
- 该计划的 `README.md`、`Progress.md`、`Backlog.md`、`Done.md` 以及拆分任务文件都放在同一目录

## 使用规则

- 新建计划时先决定分类，再新建“计划文件夹”。
- 如果一个 plan 需要补充材料，把补充文件放在该计划目录下。
- `DocsAI/` 不再承载项目级计划，只保留 AI 执行协议、模块契约、测试和 Skill 映射。

## 当前纠偏分类

`Plans/` 当前不是新的任务事实源入口。回归旧 ECS 主线后，当前执行入口改为：

1. `/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
2. `/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`
3. `DocsAI/INDEX.md`

历史计划保留但先分类：

| 路径 | 分类 | 当前动作 |
| --- | --- | --- |
| `Architecture/AI_First_Program_Migration/` | history/current reference | AI-first 工作流基础可参考 |
| `Architecture/AI_First_Docs_Code_Alignment/` | history | 已完成文档/Skill 对齐记录 |
| `Architecture/AI_First_Test_Infra_Deep_Docs/` | history/current reference | GodotSkill 和测试文档经验可参考 |
| `Architecture/Core_Regression_Gate/` | history/current reference | ECS 核心门禁经验可参考 |
| `Architecture/Godot_AI_Game_OS_Migration/` | history / migration-pointer | 旧迁移方向，不作为当前默认入口 |
| `Architecture/SkilmeAI_多仓库彻底迁移/` | history / migration-pointer | 旧多仓库方向，不作为当前默认入口 |
| `Architecture/框架整体迁移/` | history / migration-pointer | 旧彻底迁移方案，不作为当前默认入口 |

## 推荐入口

1. `Architecture/README.md`
2. `DocsAI/INDEX.md`
3. `/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
