# ECS 框架文档

> 状态：historical path / current owner docs
> 定位：历史 `ECS` 路径下的 Runtime / Capabilities / Tools / UI owner 文档集合。
> 更新：2026-06-16

## 方向状态

2026-06-16 后，SlimeAI 已裁决弃用 ECS 作为框架身份，正式框架名为 `SlimeAIFramework`。当前目录仍保留为历史路径名和 owner 文档位置；后续新设计应使用 `Object / Component / System / Feature / Event / Data` 语义，不再把本目录理解为 ECS runtime 目标。

方向入口：

- [`../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/README.md`](../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/README.md)
- [`../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/README.md`](../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/README.md)
- [`../思考/框架/ECS框架/README.md`](../思考/框架/ECS框架/README.md)

## 阅读顺序

1. **方向定位**：读 PRJ-0002 [`SlimeAIFramework Godot OOP 框架方向`](../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/README.md)。
2. **共享内核**：改 Entity / Data / Event / System lifecycle 时读 [Runtime/](Runtime/)。
3. **功能 owner**：改 Ability / Damage / Movement 等功能时读 [Capabilities/](Capabilities/) 下对应 owner。
4. **通用工具和 UI**：改 Timer、ObjectPool、Input、UI binding 时读 [Tools/](Tools/) 或 [UI/](UI/)。
5. **迁移追溯**：需要旧路径来源时读 [`../管理/目录架构迁移清单.md`](../管理/目录架构迁移清单.md) 和具体文档的 `migrated-from` 标记；不要把旧 `System/`、`Component/`、`Base/` 当入口。

## 组织规则

`DocsAI/ECS` 是历史路径下的功能 owner 文档集合。原 `Src/ECS/**.md` 长文档已迁入这里；`Src/ECS` 不再保留框架 Markdown 文档。该目录名暂不改，避免在概念方向冻结前制造大范围路径 churn。

当前框架第一目标已改为 Godot/OOP 功能驱动：快速开发游戏 MVP，功能能通过 Object / Component / System / Feature / Event / Data 组合、裁剪和受控启停。`Runtime + Capabilities + Tools + UI` 仍是历史目录分层，后续新设计不再把 `Component`、`System`、`Data` 理解成 ECS runtime 必备结构。

后续大型 Runtime / Capability 改动先检查：

- 功能是否能作为 Feature 被组合或裁剪。
- 需要 System 的原因是否明确，是否只是 Component 内部逻辑被过度上提。
- Component 是否职责单一、可启停，并允许保存自己的内部状态。
- 只有跨功能共享、表格驱动或需要验证追踪的状态才进入 Data；单 owner cache / 临时状态留在 owner 内。
- 运行中增删 Component / Feature 是否有生命周期、事件注销和 diagnostics 规则。

目录重构后的默认 AI 路由是：

```text
DocsAI/ECS/Runtime/<Entity|Data|Event|System>
DocsAI/ECS/Capabilities/<Ability|Damage|Movement|Collision|Feature|Effect|Projectile|AI|Spawn|Unit>
DocsAI/ECS/Tools/<owner>
DocsAI/ECS/UI
```

旧 `DocsAI/ECS/System`、`DocsAI/ECS/Component`、`DocsAI/ECS/Entity`、`DocsAI/ECS/Data`、`DocsAI/ECS/Event` 不再作为当前入口；若历史文档需要保留，必须迁入对应 Runtime / Capability / Tools / UI owner 的 `Concepts/` 或原文件名下。

Owner 文档不强制拆成固定文件名。优先保证入口清晰、事实源少、内容不重复：

- `README.md` 通常作为 owner 主入口，说明当前事实、源码入口、常用操作、扩展规则和验证入口。
- `Concept.md`、`Usage.md`、`Tests.md`、`Debug.md`、`InputMap.md` 这类文件只是可选分层；内容少时可以合并在 `README.md`。
- 原文结构完整、拆分会降低可读性或增加维护成本时，可以保留原文件名。
- 如果拆分多个文档，必须在 `README.md` 说明每个文件的职责，避免多份文档重复表达同一事实。

完整治理规则见 [`../管理/DocsAI统一管理与索引规则.md`](../管理/DocsAI统一管理与索引规则.md)。

## 目录

### Runtime/ — 共享 ECS 内核

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| Runtime 总览 | [Runtime/](Runtime/) | Entity / Data / Event / System Core 的共享内核入口 |
| Entity | [Runtime/Entity/](Runtime/Entity/) | 实体身份、生命周期、Spawn/Destroy、LifecycleTree、引用注册 |
| Component | [Runtime/Component/](Runtime/Component/) | `IComponent`、`TemplateComponent`、ComponentRegistrar 接入规则 |
| Data | [Runtime/Data/](Runtime/Data/) | DataOS 管道、typed DataKey、runtime snapshot、运行时存储 |
| Event | [Runtime/Event/](Runtime/Event/) | EventBus、EventContext、GlobalEventBus 和事件协议边界 |
| System | [Runtime/System/](Runtime/System/) | System lifecycle、registration、state 和 Godot system wrapper |
| Mount | [Runtime/Mount/](Runtime/Mount/) | `/root/SlimeAIRuntime` 运行时挂载 registry 与 diagnostics |
| NodeLifecycle | [Runtime/NodeLifecycle/](Runtime/NodeLifecycle/) | 底层 Godot Node 注册、owner/source metadata 和 diagnostics |

### Capabilities/ — 功能 owner

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| Capabilities 总览 | [Capabilities/](Capabilities/) | 功能 owner 路由和 Capability 内部 ECS 子结构规则 |
| Ability | [Capabilities/Ability/](Capabilities/Ability/) | 技能触发、冷却、充能、消耗、Feature handler 接入 |
| Damage | [Capabilities/Damage/](Capabilities/Damage/) | 伤害处理管线、DamageTool、统计和归属 |
| Movement | [Capabilities/Movement/](Capabilities/Movement/) | 移动策略、速度投影、碰撞移动、方向组件 |
| Collision | [Capabilities/Collision/](Capabilities/Collision/) | Godot 碰撞桥、Hurtbox、ContactDamage、Pickup |
| Feature | [Capabilities/Feature/](Capabilities/Feature/) | Feature 授予、启用、禁用、modifier/action |
| Effect | [Capabilities/Effect/](Capabilities/Effect/) | 视觉/逻辑效果实体与 EffectTool |
| Projectile | [Capabilities/Projectile/](Capabilities/Projectile/) | 投射物生命周期、source attribution、命中控制 |
| AI | [Capabilities/AI/](Capabilities/AI/) | 行为树、AIComponent、AI 数据与动作节点 |
| Spawn | [Capabilities/Spawn/](Capabilities/Spawn/) | 程序化生成和 spawn config |
| StatusSystem | [Capabilities/StatusSystem/](Capabilities/StatusSystem/) | 状态效果定义、实例、集合与快照 |
| TestSystem | [Capabilities/TestSystem/](Capabilities/TestSystem/) | 运行时调试面板、模块和资源目录测试 |
| Unit | [Capabilities/Unit/](Capabilities/Unit/) | 单位实体、Health/Lifecycle/State/Animation、阵营/归属基础能力 |

### Tools/ — 工具层

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| ObjectPool | [Tools/ObjectPool/](Tools/ObjectPool/) | 通用 C# 对象池 |
| Timer | [Tools/Timer/](Tools/Timer/) | 高性能定时器系统 |
| Math | [Tools/Math/](Tools/Math/) | 数学工具层 |
| TargetSelector | [Tools/TargetSelector/](Tools/TargetSelector/) | 目标查询三层架构 |
| ResourceLoading | [Tools/ResourceManagement/](Tools/ResourceManagement/) | strict 资源加载 facade、generated catalog、路径迁移 workflow 和资源目录 diagnostics |
| CommonUtilities | [Tools/CommonUtilities/](Tools/CommonUtilities/) | 受约束的通用工具 owner，不是杂物箱 |
| Input | [Tools/Input/](Tools/Input/) | 输入管理 |
| Logger | [Tools/Logger/](Tools/Logger/) | 日志系统 |
| Singleton | [Tools/Singleton/](Tools/Singleton/) | 非继承式 Node 单例守卫 |

### UI/ — UI 层

| Owner | 文档 | 一句话说明 |
| ----- | ---- | ---- |
| UI | [UI/](UI/) | Binding Pattern，UI 不是 Entity Component |
