# Component 现状证据与 AI-first 裁决

> 状态：current
> 更新：2026-06-04
> 目标：用 AI-first 视角复查旧 Component 系统是否需要完善，并冻结后续优化方向。

## DeepThink

### Goal

本轮要解决的问题：

- 判断旧 ECS Component 是否需要为了 AI 使用而完善。
- 明确 `IComponent`、`TemplateComponent`、`ComponentRegistrar` 和 Capability Component 的可靠部分、真实缺口和不建议方向。
- 在 `design/Runtime/7.Component/` 下生成可恢复的共享设计包。

非目标：

- 本轮不修改 `Src/ECS/Runtime/Component` 或 Capability Component 代码。
- 本轮不创建执行型 SDD，不切换 PRJ-0002 当前 SDD。
- 本轮不把 SlimeAI Component 改成 Bevy / Unity DOTS / Flecs / EnTT 式纯数据 ECS component。

### Context Read

本地事实源：

- `DocsAI/ECS框架与AIFirst方向决策.md`
- `DocsAI/ECS/Runtime/Component/README.md`
- `DocsAI/ECS/Runtime/Component/Concepts/IComponent接口说明.md`
- `DocsAI/ECS/Runtime/Component/Concepts/Component规范说明.md`
- `DocsAI/ECS/Runtime/Component/Concepts/Component数据驱动设计理念.md`
- `DocsAI/ECS/Runtime/Entity/README.md`
- `Src/ECS/Runtime/Component/IComponent.cs`
- `Src/ECS/Runtime/Component/TemplateComponent.cs`
- `Src/ECS/Runtime/Entity/Components/ComponentRegistrar.cs`
- `Src/ECS/Runtime/Entity/Components/EntityManager_Component.cs`
- `Src/ECS/Runtime/Entity/Spawn/EntitySpawnPipeline.cs`
- `Src/ECS/Runtime/Entity/Lifecycle/EntityDestroyPipeline.cs`
- `Src/ECS/Runtime/Entity/Manager/EntityManager.cs`
- `Src/ECS/Runtime/Entity/Tests/ComponentRegistrarRuntimeTest.cs`
- `Src/ECS/Runtime/Entity/Tests/EntitySpawnPipelineRuntimeTest.cs`
- `Src/ECS/Runtime/Entity/Tests/EntityDestroyPipelineRuntimeTest.cs`
- 代表性 Capability Component：Movement、Collision、Health、Lifecycle、AI。
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Actors/ResearchAnalyst.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/CLI.md`

本地参考资料：

- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/bevy/01-Bevy-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/flecs/02-Flecs-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/entt/03-EnTT-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/Arch/04-Arch-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/EntityComponentSystemSamples/07-Unity-Entities-Samples-源码分析报告.md`
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/godot-4.6.2-stable/12-Godot-源码分析报告.md`

外部资料：

- Context7：`/websites/rs_bevy_bevy`，查询 Bevy ECS component、bundle、query、hierarchy。
- Bevy ECS docs：`https://docs.rs/bevy/latest/bevy/ecs/index.html`
- Bevy `Query` docs：`https://docs.rs/bevy/latest/bevy/ecs/system/struct.Query.html`
- Bevy `ChildOf` docs：`https://docs.rs/bevy/latest/bevy/ecs/prelude/struct.ChildOf.html`
- Godot Node docs：`https://docs.godotengine.org/en/stable/classes/class_node.html`
- Web/curl 尝试访问 Unity Entities、Flecs、EnTT、Godot 官方细页；部分页面在当前网络下超时，因此不作为强证据。相关方向仅作为与本地 Resources 报告一致的辅助参考。

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 本轮开始前 `git status --short` 已有大量既有修改、`.uid` 删除和未跟踪 `.ai-temp` / `__pycache__`；本设计只新增/修改 Component 优化相关 SDD 设计文档和项目索引，不覆盖既有改动。

未读上下文：

- 未逐个打开所有 24 个 Capability Component，只读代表性组件并通过 `rg` 做调用点扫描。
- 未运行 Godot 场景验证，因为本轮不改代码；验证计划写入 `03-调用点迁移与验证计划.md`。

### Problem Shape

现有 Component 对人类开发已经够轻量，但 AI 使用时有五类缺口：

1. **概念歧义**：外部 ECS 资料里的 Component 通常是数据片段，而 SlimeAI 的 Component 是 Godot Node adapter。AI 容易把两者混用，写出私有状态真相、世界 query 或纯 ECS storage 方向。
2. **清单缺口**：没有一张 current manifest 列出所有 Capability Component、owner、节点类型、订阅、Timer、`_Process`、动态 Add/Remove 支持和验证入口。
3. **生命周期缺口**：spawn 已走 `EntitySpawnPipeline + ComponentRegistrar`，但 destroy 仍存在 `EntityManager.Destroy` 旧递归路径和 `EntityDestroyPipeline` 新测试路径并行；AI 不能只看一个文件就判断唯一销毁顺序。
4. **直连调用缺口**：`EntityManager.GetComponent<T>()` 仍被 Targeting / Damage 等真实业务调用使用。它不是完全 dead API，但应被标成例外，而不是新代码默认通信方式。
5. **外部资源清理缺口**：`GlobalEventBus`、Godot signal、Timer、对象池状态和 `_Process` 热路径清理规则分散在组件实现和 owner 文档中，没有统一 preflight / audit。

隐藏假设：

- 组件数量当前不大，约 24 个 Capability Component；人工 grep 还能核对，但 AI 在跨 owner 修改时仍容易漏掉清理与验证。
- 组件注册时可读 snapshot 配置，但不保证 Spawn 后设置的数据已经存在；这个时序目前写在文档里，但还没有机器可检查 gate。
- 组件 owner 索引只在内存中维护，不进入 DataOS 或 Relationship；这是正确方向，但需要 manifest 帮 AI 看懂边界。

### Main Risks

| 风险 | 影响 | 判断 |
| --- | --- | --- |
| 把 Component 改成纯数据 ECS storage | 推翻 Godot Node / SceneTree / adapter 现状，破坏既有组件 | 高风险，不建议 |
| 恢复 `ENTITY_TO_COMPONENT` 关系图 | 把已收口的 Relationship hard cutover 重新打开 | 高风险，不建议 |
| 继续放任 `GetComponent<T>()` 扩散 | 组件间直连增加，Data/Event/Service 边界变弱 | 中风险，需要 gate |
| 销毁路径并行不收口 | 组件注销顺序、Data/Events 清理、对象池回收语义可能不一致 | 中高风险，后续执行需要验证 |
| 外部订阅漏解绑 | 对象池复用或场景切换后触发旧组件回调 | 中高风险，需要 manifest/audit |
| 只写文档不建验证 | AI 读得懂但后续仍可能写错组件 | 中风险，后续应补 preflight 和 runtime tests |

### Options

#### 方案 A：保留现状，只补 README

做法：把现有 Component 规则写得更清楚。

优点：成本最低。

缺点：不能解决组件清单、销毁路径一致性、外部订阅和动态组件验证问题。

结论：不足以满足 AI-first。

#### 方案 B：把 Component 重写为纯数据 ECS component

做法：参考 Bevy / Unity DOTS / Flecs / EnTT，把 component 变成数据片段，由系统 query 处理。

优点：概念上接近外部 ECS，性能模型清晰。

缺点：与 SlimeAI 当前 Godot C# 框架冲突。Godot 场景、Area2D/CharacterBody2D、动画、碰撞、输入和对象池都需要 Node adapter；重写会替换架构，而不是解决 AI 入口问题。

结论：不推荐。

#### 方案 C：保留最小 Component 契约，补 AI-first Contract Layer

做法：不改 `IComponent` 主体，新增或完善：

- `ComponentManifest`：AI 可读组件清单。
- `ComponentLifecycleContract`：spawn/register/unregister/destroy/pool 时序。
- `SubscriptionContract`：Entity.Events / GlobalEventBus / Godot signal / Timer 的清理规则。
- `DynamicComponentPolicy`：Add/RemoveComponent 的允许场景、测试要求和禁止事项。
- `ComponentPreflight`：扫描组件 owner、订阅、热路径、旧关系、字符串 DataKey 和直连调用。

优点：小步、可验证、不破坏现有模型；最贴合 PRJ-0002 “保留旧 ECS 主线、按真实问题优化”的方向。

缺点：需要维护 manifest，且销毁路径收口会触及 EntityManager 行为，需要后续执行型 SDD 验证。

结论：推荐。

### Recommendation

推荐采用方案 C。

Component Core 当前不需要整体重写。它已经具备 AI-first 的基础结构：

- 少入口：`IComponent` 只有注册/注销两个回调。
- 少事实源：组件 owner 归属只在 `ComponentRegistrar` 内部索引，不再进入 Relationship。
- 数据闭环清楚：共享状态通过 `Entity.Data`、generated DataKey 和 `Entity.Events` 协作。
- Godot 适配明确：具体组件天然是 Node / Area2D / CharacterBody2D 桥接，不应假装成纯数据结构。

真正要补的是 AI 可独立判断的证据层，而不是替换 Component 模型。

### Must Confirm

进入代码实施前必须确认：

1. 是否接受首个执行型 SDD 限定为“无行为变化的 manifest / preflight / docs / tests”，暂不合并 `EntityManager.Destroy` 到 `EntityDestroyPipeline`。
2. 后续是否把 `EntityManager.GetComponent<T>()` 定义为“仅允许 owner documented exception”，新业务默认不得新增。

### Should Confirm

建议确认但可用默认值推进：

1. Component manifest 是先放 `DocsAI/ECS/Runtime/Component/ComponentManifest.md`，还是同时输出 `.ai-temp/component/component-manifest.json`。
2. `class name ends with Component` legacy 识别是否继续允许，还是后续要求全部实现 `IComponent`。
3. 是否把 `EntityDestroyPipeline` 作为后续销毁唯一入口，还是只修正当前旧路径与测试覆盖的差异。

### Defaults I Will Use

若用户后续只说“按建议执行”，默认采用：

- 不重写 `IComponent` 接口。
- 不引入第三方 ECS。
- Component Preset 目标改为完全代码化，首切片先做 `ComponentCompositionProfile` / `ComponentComposer`。
- 组件结构参数在注册前 typed 注入，不扩展 `OnComponentRegistered(Node)` 签名。
- 不使用 `[Export]` / Inspector 作为 Component 默认配置来源。
- 不使用 `_EnterTree()` / `_Ready()` 作为 Entity/Data/Event 初始化入口。
- `GetComponent<T>()` 保留但标为例外；新组件通信默认走 Data/Event/Service。
- `EntityRelationshipType.ENTITY_TO_COMPONENT` 继续禁止恢复。
- docs artifact 写入 DocsAI；机器 artifact 后续放 `.ai-temp/component/`，不作为事实源。

### Not Recommended

- 不建议复制 Bevy / Unity DOTS 的 component storage 或 query API。
- 不建议复制 Flecs pair / relationship 作为组件 owner 模型。
- 不建议让 Component 私有字段保存共享业务真相。
- 不建议在 `_EnterTree()` / `_Ready()` 中读取 Entity.Data 或订阅 Entity.Events。
- 不建议用 `[Export]` / Inspector 承载 Component 默认参数。
- 不建议让 `AddComponent<T>()` 成为普通玩法组合入口；ComponentComposer / profile / EntitySpawnPipeline 应承担默认组合。

### Artifact Updates

本轮写入：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/01-现状证据与AI-first裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/02-目标架构与优化路线.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/03-调用点迁移与验证计划.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/7.Component/04-Component代码化组合与参数注入裁决.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`

## 当前 Component 证据

### Runtime Core

| 文件 | 当前事实 | 判断 |
| --- | --- | --- |
| `IComponent.cs` | 只有 `OnComponentRegistered` / `OnComponentUnregistered` | 接口足够小，不需要扩展生命周期方法 |
| `TemplateComponent.cs` | 展示 Data / Event / generated DataKey / typed options 用法 | 已校准为代码化参数注入模板 |
| `ComponentRegistrar.cs` | 维护 owner 索引、类型索引和注册/注销回调 | 正确替代旧 `ENTITY_TO_COMPONENT` |
| `EntityManager_Component.cs` | 提供预热缓存、RegisterComponents、GetComponent、Add/RemoveComponent | API 面偏宽，需要 AI 使用边界 |
| `EntitySpawnPipeline.cs` | data apply 后注册组件，再 attach lifecycle | 生成顺序合理 |
| `EntityManager.Destroy` | 仍有旧递归路径、旧 Relationship cleanup 和 `_componentRegistrar.UnregisterComponents` | 后续应与 `EntityDestroyPipeline` 收口 |
| `EntityDestroyPipeline.cs` | 测试覆盖 owner cleanup -> component unregister -> Data/Events clear 顺序 | 目标行为合理，但尚不是唯一销毁入口 |

### Capability Component 扫描

当前 `Src/ECS/Capabilities/*/Component/` 下约 24 个组件，主要分布：

| Owner | 组件类型 | AI 风险 |
| --- | --- | --- |
| Ability | ActiveSkillInput、Charge、Cooldown、Cost、Trigger | 动态 AddComponent 测试较多，需明确运行时组合边界 |
| AI | AIComponent | `_Process` 每帧 tick，需保持 no new / Data gate |
| Collision | Collision、ContactDamage、Hurtbox、Pickup | Godot signal、对象池、collision guard 清理风险高 |
| Effect | EffectComponent | 生命周期和视觉桥接风险 |
| Movement | EntityMovement、Orientation | `_Process` / `_PhysicsProcess`、策略切换、Data completeness 风险 |
| Unit | Attack、DataInit、Health、Lifecycle、Recovery、Status、Animation、State、TargetingIndicatorControl | GlobalEventBus、Timer、动画事件、状态 Data 交织 |

### 调用点扫描摘要

- `EntityManager.AddComponent(...)` 主要出现在 Ability / Damage / ActiveSkillInput / ECSTest 测试场景。
- `EntityManager.GetComponent<T>()` 有真实业务调用：
  - `TargetingManager` 获取 `TargetingIndicatorControlComponent`。
  - `HealthExecutionProcessor` 获取 `HealthComponent`。
- `EntityRelationshipType.ENTITY_TO_COMPONENT` 只在 legacy relationship 文件和文档中出现，Runtime Component current docs 已禁止恢复。
- `Data.On(...)` 没有作为 current 代码入口出现，只在文档中作为禁用示例。

## Research Adoption

```yaml
externalResources:
  enabled:
    - engine-framework
    - official-docs
  scope:
    - Resources/Engine/Docs/FrameworkAnalysis/Reports/* 中 ECS component / relationship / GodotBridge 片段
    - Context7 /websites/rs_bevy_bevy 的 ECS component、bundle、query、ChildOf 文档片段
    - Godot Node lifecycle 官方文档入口
  reason: 校准 SlimeAI Component 应保留 Godot Node adapter 语义，还是改成纯 ECS 数据组件
  expires: current-task
  copiedCodeOrAssets: none
```

### Evidence

- Bevy 官方 ECS 文档把 ECS 拆成 Entity、Component、System，并强调组件数据由系统查询处理；Bundle 用于组合多个组件。
- Bevy `Query` 文档显示系统通过 query 读取组件数据，而不是组件节点自己执行 Godot 生命周期。
- Bevy `ChildOf` 文档和本地报告共同支持 hierarchy source-of-truth 与反向缓存分离；这支持 SlimeAI 不恢复 `ENTITY_TO_COMPONENT` 关系图。
- 本地 Godot 报告显示 Node ENTER/EXIT/READY、SceneTree 移除、PhysicsServer2D RID 和 C# GCHandle 生命周期是 GodotBridge 的核心约束；SlimeAI Component 作为 Node adapter 有实际价值。
- 本地综合报告明确拒绝引入外部 ECS runtime、archetype/chunk/sparse-set/world query public API，推荐 small runtime + capability owner + validation/observation。

### Inference

- SlimeAI 的 Component 不应和外部 ECS 的“数据组件”同名同义；文档应明确称其为 Godot Node Component / GodotBridge Adapter。
- 外部 ECS 的可采纳点不是存储模型，而是组合清单、生命周期约束、结构变更 guard 和验证 artifact。
- Component manifest 可以承担 Bevy Bundle / Unity baking manifest 的“组合可读性”价值，但不应成为运行时配置事实源。

### Unknown

- Unity / Flecs / EnTT 官方细页在当前网络下部分超时；本轮不把它们作为强证据，只引用本地 Resources 已完成报告中的采纳/拒绝结论。
- `EntityDestroyPipeline` 是否应成为 `EntityManager.Destroy` 的唯一实现，需要后续执行型 SDD 通过场景验证裁决。

### Adopt Now / Later / Reject

| 研究项 | 决策 | SlimeAI 落点 |
| --- | --- | --- |
| Component manifest / bundle-like 清单 | Adopt Now | `DocsAI/ECS/Runtime/Component/ComponentManifest.md` 或后续 JSON artifact |
| 保留 Godot Node Component / Adapter | Adopt Now | `IComponent`、Capability Component、DocsAI Runtime/Component |
| owner 索引内聚到 ComponentRegistrar | Adopt Now | `ComponentRegistrar`，禁止 Relationship owner 图 |
| 外部订阅 / Timer cleanup 审计 | Adopt Now | `ComponentPreflight`、owner docs、runtime tests |
| 销毁路径统一到 EntityDestroyPipeline | Adopt Later | 需要执行型 SDD 和 Godot 场景验证 |
| Dynamic Add/RemoveComponent policy | Adopt Later | 先文档和测试 gate，后续再收窄 API |
| 纯数据 ECS component storage | Reject | 不适配 Godot C# Node lifecycle |
| 通用 world query / pair graph | Reject | 破坏 Capability owner 与 AI 路由边界 |
