# Phase 01 资产盘点

> 日期：2026-05-04
> 范围：当前 `brotato-my` 仓库作为迁移输入。

## 分流原则

- 通用 Runtime 机制迁入 `SkilmeAI/GameOS/Runtime`。
- 高频玩法能力迁入 `SkilmeAI/GameOS/Capabilities`。
- 数据 authoring、DataKey、EventType、ResourceCatalog 迁入 `SkilmeAI/GameOS/Authoring` 或后续 `DataOS`。
- 游戏资产、场景、游戏特定脚本迁入 `Games/BrotatoLike`。
- DocsAI 跨模块协议迁入新仓库 DocsAI；模块长知识下沉到 Capability Contract。
- 当前仓库不再新增长期架构能力。

## Runtime 输入

| 旧路径 | 目标 | 备注 |
| --- | --- | --- |
| `Src/ECS/Base/Entity/Core` | `SkilmeAI/GameOS/Runtime/Entity` | 生命周期、注册、销毁、关系绑定和迁移入口。 |
| `Src/ECS/Base/Component/IComponent.cs` | `SkilmeAI/GameOS/Runtime/Component` | 只迁基础组件契约。 |
| `Src/ECS/Base/Data` | `SkilmeAI/GameOS/Runtime/Data` | 运行时状态容器，不包含 authoring 表。 |
| `Src/ECS/Base/Event` | `SkilmeAI/GameOS/Runtime/Event` | EventBus 与基础事件协议。 |
| `Src/ECS/Tools/ObjectPool` | `SkilmeAI/GameOS/Runtime/Pool` | 对象池作为 Runtime 机制。 |
| `Src/ECS/Tools/TimerManager` | `SkilmeAI/GameOS/Runtime/Timer` | 统一计时器入口。 |
| `Data/ResourceManagement` | `SkilmeAI/GameOS/Runtime/Resource` + `SkilmeAI/GameOS/Authoring/ResourceCatalog` | 运行时加载和资源目录生成拆开。 |

## Capability 输入

| 旧路径 | 目标 Capability | 备注 |
| --- | --- | --- |
| `Src/ECS/Base/System/Movement`、`Src/ECS/Base/Component/Movement` | `Movement` | 第一批样板。 |
| `Src/ECS/Base/Component/Collision` | `Collision` | Hurtbox、ContactDamage、对象池隔离规则。 |
| `Src/ECS/Base/System/DamageSystem` | `Damage` | 伤害管线与处理器。 |
| `Src/ECS/Base/System/FeatureSystem` | `Feature` | 通用能力生命周期。 |
| `Src/ECS/Base/System/AbilitySystem`、`Src/ECS/Base/Component/Ability` | `Ability` | 施法、冷却、消耗、触发和执行上下文。 |
| `Src/ECS/Base/System/AISystem` 或相关 AI 文件 | `AIBehavior` | 行为树、AIContext、行为块。 |
| `Src/ECS/Base/Entity/Projectile`、`Src/ECS/Base/System/ProjectileSystem` | `Projectile` | 通用投射物实体和生成工具。 |
| `Src/ECS/Base/System/Spawn` | `Spawn` | 波次和生成能力。 |
| `Src/ECS/UI`、UI 相关组件 | `UIHud` | UI 绑定不进入 Runtime。 |
| `Src/ECS/Base/System/TestSystem` | `TestSystem` 或 `Validation` | 测试 UI 与运行时调试拆分。 |

## Authoring / Data 输入

| 旧路径 | 目标 | 备注 |
| --- | --- | --- |
| `Data/DataNew` | `SkilmeAI/DataOS` + generated snapshot | 作为迁移输入，不作为最终主数据源。 |
| `Data/DataKey` | `SkilmeAI/GameOS/Authoring/DataKeys` | 按 Capability 分域。 |
| `Data/EventType` | `SkilmeAI/GameOS/Authoring/EventTypes` | 按 Capability 分域。 |
| `Data/Config`、`Data/Data` | `Archive` 或 DataOS seed 输入 | 旧 Resource/.tres 运行时入口不迁为新主入口。 |
| `addons/DataConfigEditor` | `SkilmeAI/DataOS` 或 `BrotatoLike` 工具输入 | 后续由 DataForge / Editor UI 接管。 |

## 游戏输入

| 旧路径 | 目标 | 备注 |
| --- | --- | --- |
| `assets/` | `Games/BrotatoLike/Assets` | 游戏资产先迁入游戏仓库，后续清理命名。 |
| `Src/Main` | `Games/BrotatoLike/Src/Game` | 游戏入口逻辑。 |
| `project.godot`、`Brotato_my.csproj` | `Games/BrotatoLike` | 新游戏项目重新生成名称和引用。 |
| `Src/ECS/Test/SingleTest` | `SkilmeAI/GameOS/Validation` 或 `Games/BrotatoLike/Scenes/Tests` | 框架测试与游戏测试拆分。 |

## 文档和 Skill 输入

| 旧路径 | 目标 | 备注 |
| --- | --- | --- |
| `DocsAI/Protocols` | 新框架仓库 `DocsAI/Protocols` | 保留跨模块协议。 |
| `DocsAI/Modules` | Capability `Contract.md` | 模块知识下沉。 |
| `DocsAI/Tests` | `GameOS/Validation` + 新 DocsAI | 测试矩阵和日志规则保留。 |
| `.codex/skills` | 框架 active skills 或游戏入口 skills | 按仓库角色重写短入口。 |
| `Docs/框架/项目索引.md` | 新仓库索引 + 当前仓库归档索引 | 当前仓库只保留迁移导航。 |

## 高风险清单

- `SystemManager` 与 `Data/DataNew/System` 绑定较深，迁移 Schedule 前不能直接删除旧入口。
- `EntityManager` 目前同时承担 Spawn、Pool、Relationship、VisualInjection，迁移时需要先切稳定 API。
- `DataKey` 和 `EventType` 被大量业务代码引用，迁移时必须先建立生成或映射规则。
- Godot 场景 `.tscn` 保存脚本路径，目录迁移会影响资源引用，需要专门脚本检查。
- 对象池碰撞问题依赖 Godot 物理时序，迁移后必须保留 Observation 计划。

