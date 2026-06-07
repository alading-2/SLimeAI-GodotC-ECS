# Component 优化设计包

> 状态：current
> 更新：2026-06-04
> 范围：`DocsAI/ECS/Runtime/Component/`、`Src/ECS/Runtime/Component/`、`Src/ECS/Runtime/Entity/Components/`、`Src/ECS/Capabilities/*/Component/`
> 结论：保留现有 Component 最小契约，不把它扩成纯 ECS storage；Component 组合目标改为纯代码化，围绕 AI-first 补 composition profile、manifest、注册/销毁一致性、外部订阅清理、动态组件边界和验证门禁。

## 一句话结论

旧 Component 系统内容确实不多，给人用基本够清楚；但给 AI 用时还不够“可判定”：

- AI 容易把 Godot Node Component 误解成传统 ECS 数据组件。
- AI 不容易一次看清哪些 Capability Component 存在、谁拥有、是否有外部订阅、是否使用 `_Process`、是否可动态 Add/Remove。
- Entity 生成已通过 `EntitySpawnPipeline` 接入 `ComponentRegistrar`，但销毁路径仍存在旧 `EntityManager.Destroy` 与新 `EntityDestroyPipeline` 并行的事实，后续要收口。
- `EntityManager.GetComponent<T>()` 仍有少量真实业务调用，需要显式标注为过渡/例外，不应继续扩散。

因此 Component 不应重写成 Bevy / Unity DOTS / Flecs / EnTT 式纯数据组件，也不应恢复 `ENTITY_TO_COMPONENT` 关系图。推荐路线是：保留 `IComponent + ComponentRegistrar`，将 Component Preset 迁移为代码化 composition profile / composer，并补一个 AI-first Contract Layer。

## 文档入口

| 文档 | 职责 |
| --- | --- |
| [01-现状证据与AI-first裁决.md](01-现状证据与AI-first裁决.md) | 本地源码事实、外部资料采纳、DeepThink 确认包、主要裁决 |
| [02-目标架构与优化路线.md](02-目标架构与优化路线.md) | ComponentManifest、LifecycleContract、SubscriptionContract、DynamicComponentPolicy 和分阶段路线 |
| [03-调用点迁移与验证计划.md](03-调用点迁移与验证计划.md) | 调用点审计、未来执行任务、BDD、grep gate 和验证命令 |
| [04-Component代码化组合与参数注入裁决.md](04-Component代码化组合与参数注入裁决.md) | 用户确认后的纯代码化组合裁决、参数注入、Data 边界和 Preset 迁移策略 |

## 当前源码入口

```text
Src/ECS/Runtime/Component/
  IComponent.cs
  TemplateComponent.cs

Src/ECS/Runtime/Entity/Components/
  ComponentRegistrar.cs
  EntityManager_Component.cs
  EntityManager_Component_Init.cs

Src/ECS/Capabilities/<owner>/Component/
```

## 当前正式模型

| 领域 | 当前事实 |
| --- | --- |
| 接口 | `IComponent.OnComponentRegistered(Node)` / `OnComponentUnregistered()` |
| 生命周期 | SlimeAI 自定义生命周期只认 `OnComponentRegistered` / `OnComponentUnregistered`；不使用 Godot `_EnterTree()` / `_Ready()` 做注册初始化 |
| 识别 | `IComponent` 优先；类名以 `Component` 结尾作为 legacy 兼容 |
| owner 索引 | `ComponentRegistrar` 内部维护 Entity -> Component / Component -> Entity，不进入 Relationship |
| 注册时机 | `EntitySpawnPipeline` 在 Data apply、视觉注入、registry register 后调用 `RegisterComponents` |
| 注销时机 | `EntityManager.UnregisterEntity` 会通过 `_componentRegistrar.UnregisterComponents` 注销；`EntityDestroyPipeline` 测试覆盖了注销应先于 Data/Events 清理 |
| 状态归属 | 共享业务状态进入 `Entity.Data` + generated DataKey；组件私有字段只放内部缓存、节点引用和算法临时状态 |
| 参数归属 | 固定结构参数由代码化 composer/profile 注入；不使用 `[Export]` / Inspector，不默认进入 DataOS |
| 组合入口 | 新方向为 `ComponentCompositionProfile` / `ComponentComposer`；旧 `Presets/` 只作为迁移期对照 |
| 通信 | 默认通过 `Entity.Events`；外部事件和 Timer 必须在注销时显式解绑/取消 |
| 具体组件归属 | 业务组件放 `Src/ECS/Capabilities/<owner>/Component/`，Runtime 只放接口、模板和通用规则 |

## 非目标

- 不把 Runtime Component 改成 archetype / chunk / sparse-set ECS storage。
- 不引入第三方 ECS runtime。
- 不恢复 `EntityRelationshipType.ENTITY_TO_COMPONENT` 或通用 Component relationship 图。
- 不把所有业务组件上提到 `Runtime/Component`。
- 不在本设计包直接修改代码；代码实施必须另建或扩展执行型 SDD。
- 不要求所有 `GetComponent<T>()` 调用本轮一次性删除；先标注边界和迁移方向。

## 后续执行入口

若进入实现，建议创建新的执行型 SDD：

```text
Title: Component Contract Manifest And Lifecycle Hardening
Scope: Src/ECS/Runtime/Component, Src/ECS/Runtime/Entity/Components, DocsAI/ECS/Runtime/Component, Src/ECS/Capabilities/*/Component
```

默认首切片应是 Component 代码化组合硬化：先设计并落地 ComponentCompositionProfile / ComponentComposer，再补 manifest、lifecycle/preflight 检查、subscription cleanup 审计、DocsAI/skill 同步和 ComponentRegistrar runtime artifact。是否合并 `EntityManager.Destroy` 到 `EntityDestroyPipeline` 属于行为影响更大的后续切片，需要单独执行和场景验证。
