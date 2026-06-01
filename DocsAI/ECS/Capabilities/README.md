# ECS Capabilities 文档

> 状态：current
> 定位：SlimeAI ECS 功能 owner 文档入口。
> 更新：2026-06-01

## 定位

`Capabilities/` 是 AI 修改功能时的默认入口。每个 Capability 可以包含 Component、System、Events、Tests、DataKeys 等 ECS 子结构，但顶层路由先按功能 owner 聚合。

## Owner

| Owner | 当前文档来源 | 目标源码 |
| ---- | ---- | ---- |
| Ability | [Ability/](Ability/) | `Src/ECS/Capabilities/Ability/` |
| Damage | [Damage/](Damage/) | `Src/ECS/Capabilities/Damage/` |
| Movement | [Movement/](Movement/) | `Src/ECS/Capabilities/Movement/` |
| Collision | [Collision/](Collision/) | `Src/ECS/Capabilities/Collision/` |
| Feature | [Feature/](Feature/) | `Src/ECS/Capabilities/Feature/` |
| Effect | [Effect/](Effect/) | `Src/ECS/Capabilities/Effect/` |
| Projectile | [Projectile/](Projectile/) | `Src/ECS/Capabilities/Projectile/` |
| AI | [AI/](AI/) | `Src/ECS/Capabilities/AI/` |
| Spawn | [Spawn/](Spawn/) | `Src/ECS/Capabilities/Spawn/` |
| StatusSystem | [StatusSystem/](StatusSystem/) | `Src/ECS/Capabilities/StatusSystem/` |
| TestSystem | [TestSystem/](TestSystem/) | `Src/ECS/Capabilities/TestSystem/` |
| Unit | [Unit/](Unit/) | `Src/ECS/Capabilities/Unit/` |

## 当前状态

SDD-0025 后，功能源码入口统一在 `Src/ECS/Capabilities/<owner>/`。`Src/ECS/Base/` 已无当前源码文件；旧 DocsAI 顶层 `System/`、`Component/` 文档也已迁入对应 owner。

## 红线

- 不把 Entity、Data、EventBus 或 System lifecycle 重新实现在 Capability 内。
- 不把 Tools/UI 为了整齐强行迁入 Capability。
- Capability 内部保留 ECS 语义，禁止退化成无结构功能文件夹。
- Entity、Component、Preset 是否放入 Capability 取决于 owner：具体业务 Entity / Component / Preset 放 Capability，`IEntity` / `IComponent` / Runtime registrar 放 Runtime。
