# Skill 到 DocsAI 映射

本文记录当前 Skill 应读取的 DocsAI 文档，避免每个 Skill 自己携带大量背景知识。

| Skill | DocsAI 入口 | 说明 |
| --- | --- | --- |
| `project-index` | `DocsAI/INDEX.md`、`Docs/框架/项目索引.md` | 查文档、源码、模板和项目导航。 |
| `GodotSkill` / `godot-scene-test` | `DocsAI/Tests/Godot场景测试.md`、`DocsAI/Tests/测试矩阵.md`、`DocsAI/Tests/日志判定与Debug.md` | CLI 场景测试和日志判定。 |
| `ecs-entity` | `DocsAI/Modules/Entity.md`、`DocsAI/Workflows/ECS核心修改门禁.md` | Entity 生命周期、关系、对象池和核心修改门禁。 |
| `ecs-component` | `DocsAI/Modules/Component.md` | Component 开发契约和运动组件要点。 |
| `ecs-data` | `DocsAI/Modules/Data.md` | 运行时 Data 容器、DataMeta、DataRegistry、读写和数据变化事件。 |
| `ecs-event` | `DocsAI/Modules/Event.md` | Entity.Events、GlobalEventBus、事件定义和订阅清理。 |
| `ability-system` | `DocsAI/Modules/AbilitySystem.md`、`DocsAI/Modules/FeatureSystem.md`、`.codex/skills/ability-system/references/ability-logic-parameters.md` | 技能流水线、Feature 接入、目标选择、参数语义。 |
| `damage-system` | `DocsAI/Modules/DamageSystem.md` | 伤害入口、管道处理器、DamageTool 和禁止事项。 |
| `movement-system` | `DocsAI/Modules/Movement.md`、`DocsAI/Modules/Collision.md` | 运动策略、运动参数、朝向、停止流程和运动碰撞。 |
| `ai-system` | `DocsAI/Modules/AI.md`、`DocsAI/Modules/Movement.md`、`DocsAI/Modules/Tools.md` | 行为树、AIComponent、AI DataKey、移动意图和索敌。 |
| `collision-system` | `DocsAI/Modules/Collision.md`、`DocsAI/Modules/Movement.md`、`DocsAI/Modules/DamageSystem.md` | 碰撞层、Hurtbox、接触伤害、运动碰撞和对象池碰撞隔离。 |
| `test-system` | `DocsAI/Modules/TestSystem.md` | TestSystem 宿主、模块、服务层和禁止事项。 |
| `ui-bind` | `DocsAI/Modules/UI.md` | UIBase Bind 模式和响应式 UI 规则。 |
| `tools` | `DocsAI/Modules/Tools.md` | TimerManager、ObjectPool、TargetSelector、ResourceManagement。 |
| `data-authoring` | `DocsAI/Modules/DataAuthoring.md`、`Data/README.md`、`Data/DataNew/README.md`、`Data/DataKey/README.md` | 数据目录配置、DataNew、DataKey、EventType 和资源映射。 |
| `feature-system` | `DocsAI/Modules/FeatureSystem.md`、`DocsAI/Modules/AbilitySystem.md` | Feature 生命周期、Modifier、Handler 与 Ability 接入。 |
| `research-reference-framework` | `DocsAI/Protocols/外部资料与源码研究协议.md`、`DocsAI/Protocols/AI表现复盘协议.md`、`DocsAI/Experience/踩坑与经验索引.md` | 搜索并 clone 成熟框架、游戏项目或 Godot 底层源码，沉淀 ResearchBrief 和可落地决策。 |

## 维护规则

- Skill 只写入口、边界、必读文档、验证命令和禁止事项。
- 模块细节优先写到 `DocsAI/Modules/`。
- 长任务计划统一写到根目录 `Plans/`，并放进对应分类目录下的计划文件夹。
