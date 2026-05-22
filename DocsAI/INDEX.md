# DocsAI 索引

按任务类型查找文档。本文只做导航，不写长篇解释。

## 任务入口

- 当前旧 ECS 主线纠偏：`/home/slime/Code/SlimeAI/Workspace/DocsAI/SlimeAINewReorientation/00-README.md`
- 当前 OpenSpec change：`/home/slime/Code/SlimeAI/openspec/changes/reorient-slimeainew-ecs-framework/`
- 当前项目状态：`DocsAI/ProjectState.md`
- AI 开发闭环：`DocsAI/Workflows/AI开发闭环.md`
- 文档迁移协议：`DocsAI/Workflows/文档迁移协议.md`
- 长任务恢复：`DocsAI/Workflows/长任务上下文协议.md`
- ECS 核心门禁：`DocsAI/Workflows/ECS核心修改门禁.md`
- 外部资料与源码研究：`DocsAI/Protocols/外部资料与源码研究协议.md`
- AI 原生数据层：`DocsAI/Protocols/AI原生数据层协议.md`
- SlimeAI 多仓库 AI 工作流：`DocsAI/Protocols/SlimeAI多仓库AI工作流协议.md`
- AI 表现复盘：`DocsAI/Protocols/AI表现复盘协议.md`
- 踩坑与经验：`DocsAI/Experience/踩坑与经验索引.md`
- Skill 设计规则：`DocsAI/Skills/Skill设计规则.md`
- Skill 映射：`DocsAI/Skills/Skill到DocsAI映射.md`

## 当前路由顺序

1. 先读工作区纠偏入口和当前 OpenSpec change。
2. 再读本文件、`DocsAI/ProjectState.md` 和对应 `DocsAI/Modules/<owner>.md`。
3. 触碰核心 ECS 前读 `DocsAI/Workflows/ECS核心修改门禁.md` 和 `DocsAI/Tests/测试矩阵.md`。
4. 修改代码后按 `DocsAI/Tests/` 的验证阶梯执行；找不到真实命令时记录 blocker，不猜参数。

## 计划入口

- 计划目录：`Plans/README.md`
- AI-First 迁移计划（history/current reference）：`Plans/Architecture/AI_First_Program_Migration/README.md`
- AI-First 文档与 Skill 对齐（完成）：`Plans/Architecture/AI_First_Docs_Code_Alignment/README.md`
- 测试基础设施与文档深层对齐（完成）：`Plans/Architecture/AI_First_Test_Infra_Deep_Docs/README.md`
- Godot AI Game OS 迁移计划（history / migration-pointer，不作为当前默认入口）：`Plans/Architecture/框架整体迁移/迁移.md`
- Godot AI Game OS 执行计划（history / migration-pointer，不作为当前默认入口）：`Plans/Architecture/Godot_AI_Game_OS_Migration/README.md`
- SkilmeAI 多仓库彻底迁移（history / migration-pointer，不作为当前默认入口）：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/README.md`
- SkilmeAI 多仓库执行状态（history / migration-pointer，不作为当前默认入口）：`Plans/Architecture/SkilmeAI_多仓库彻底迁移/Progress.md`

## 高频 Skill

`.codex/skills/` 目录下是本仓历史本地 skill 入口；当前未发现 `.claude/skills/`。这些不是工作区 `.ai-config` 同步副本，后续是否迁入统一源另开任务决定。

- 项目导航：`project-index`
- Godot 场景测试：`GodotSkill` / `godot-scene-test`
- Entity 生命周期：`ecs-entity`
- Component 开发：`ecs-component`
- Data 运行时读写：`ecs-data`
- EventBus：`ecs-event`
- AbilitySystem：`ability-system`
- FeatureSystem：`feature-system`
- DamageSystem：`damage-system`
- 数据配置：`data-authoring`
- 外部资料与源码研究：`research-reference-framework`
- TestSystem：`test-system`
- UI 绑定：`ui-bind`
- 工具系统：`tools`
- Movement 系统：`movement-system`
- AI 系统：`ai-system`
- Collision 系统：`collision-system`

## 人类文档入口

- 总入口：`Docs/README.md`
- 框架索引：`Docs/框架/项目索引.md`

## 模块契约

`DocsAI/Modules/` 下所有 AI 执行契约：`Entity / Component / Data / DataAuthoring / Event / SystemCore / FeatureSystem / AbilitySystem / DamageSystem / Movement / AI / Collision / TestSystem / UI / Tools`

## 测试文档

- `DocsAI/Tests/Godot场景测试.md`
- `DocsAI/Tests/测试矩阵.md`
- `DocsAI/Tests/日志判定与Debug.md`

## 协议与经验

- `DocsAI/Protocols/外部资料与源码研究协议.md`
- `DocsAI/Protocols/SlimeAI多仓库AI工作流协议.md`
- `DocsAI/Protocols/AI原生数据层协议.md`
- `DocsAI/Protocols/AI表现复盘协议.md`
- `DocsAI/Experience/踩坑与经验索引.md`
