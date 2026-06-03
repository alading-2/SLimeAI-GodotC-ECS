# Entity 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/Entity/**`、Capability 具体 Entity Node、`Data/DataKey/Generated/`、`Src/ECS/Capabilities/*/Events/`、`Src/ECS/Runtime/Event/Global/`。
> 完成态事实源：`../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`。
> 设计包：`../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`。

## 1. 阅读顺序

1. `Entity使用说明.md`：当前可执行 API、owner 引用、生命周期和归因规则。
2. `EntityManager.md`：`EntityManager` 当前 facade 边界和 `Core` 子目录结构。
3. `../Component/README.md`：`IComponent`、`ComponentRegistrar` 和 Entity-Component owner 索引。
4. `Concepts/Entity架构设计理念.md`：为什么 Scene 即 Entity、模块边界、与纯 ECS 对比。
5. `Concepts/EntityManager设计理念.md`：为什么拆分为 Pipeline、为什么不让 EntityManager 兼任业务。
6. `Concepts/Entity受控迁移设计.md`：迁移语义、边界和适用场景。
7. `../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`：SDD-0024 完成态、任务和验证记录。
8. `../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/README.md`：Entity / Relationship hard cutover 的设计裁决和历史上下文。
9. `Entity规范.md`：旧 `Src/ECS` 文档迁移稿，只用于审计历史写法，不作为新代码模板。

## 2. 当前源码结构

`Src/ECS/Runtime/Entity` 已按职责拆分子目录：

| 子目录 | 职责 |
| --- | --- |
| `Identity/` | `EntityId`、`EntityIdList` typed runtime identity。 |
| `Registry/` | `EntityRegistry` 的 id/node 双向注册表。 |
| `Spawn/` | `EntitySpawnPipeline` 和 spawn request/result。 |
| `Lifecycle/` | `LifecycleTree`、`LifecycleLink`、`ParentDestroyPolicy`、`EntityDestroyPipeline`。 |
| `Components/` | `ComponentRegistrar` 和 EntityManager component partial。 |
| `References/` | `OwnedReferenceDescriptor`、`OwnedReferenceRegistry`。 |
| `Attribution/` | `EntityAttributionResolver`，供 Damage / Movement 等系统解析 owner/source。 |
| `Migration/` | Entity migration 配置和 facade。 |
| `LegacyRelationship/` | 旧 `EntityRelationship*` 隔离区；只允许兼容/审计，不作为新入口。 |
| `Manager/` | `EntityManager` 薄 facade 和少量旧生命周期 glue。 |

## 3. 当前裁决

- Entity 是纯身份和运行时状态容器，只暴露 `Data`、`Events`；业务逻辑放 Component / System / Service / Tool。
- 创建统一走 `EntityManager.Spawn/Register`；`EntityManager.Spawn<T>` 是 `EntitySpawnPipeline` 的薄 facade。
- `EntitySpawnConfig` 只保留通用创建事实和 `LifecycleParentId / ParentDestroyPolicy`；不恢复 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- lifecycle parent 只表达销毁树，进入 `LifecycleTree`；它不表达 owner、source、target、UI binding 或 damage credit。
- Entity 引用的 public runtime API 使用 `EntityId / EntityIdList`；`GeneratedDataKey.Id` 只作为 DataOS / snapshot / observation 字符串投影。
- 业务 owner list 使用 capability service + generated Data projection：Ability 用 `AbilityInventoryService.Runtime`，Projectile 用 `ProjectileOwnershipService.Runtime`，Effect 用 `EffectOwnershipService.Runtime`。
- `OwnedReferenceRegistry` 只负责 owner-list projection 和 child destroy cleanup，不决定 child 是否随 owner 销毁。
- Component owner 反查走 `ComponentRegistrar` 内部索引，不再用 `ENTITY_TO_COMPONENT`。
- `IEntity`、`TemplateEntity` 位于 `Src/ECS/Runtime/Entity/`；具体 `EnemyEntity`、`PlayerEntity`、`TargetingIndicatorEntity` 等属于 `Src/ECS/Capabilities/<owner>/Entity/`。
- `IComponent`、`TemplateComponent` 位于 `Src/ECS/Runtime/Component/`；具体业务组件属于 `Src/ECS/Capabilities/<owner>/Component/`。
- Damage / Movement 归因统一走 `EntityAttributionResolver`，读取 Projectile / Effect / Source / Origin projection，不沿旧 parent-chain 猜 owner。
- Event 当前以 payload 类型作为事件 key；新增 Entity lifecycle 事件必须用 `readonly record struct`，不新增字符串事件名或 `XxxEventData`。

## 4. 禁止恢复的旧写法

- 在 Entity 模板里手写 `public string EntityId` 并用 `GetInstanceId().ToString()` 作为业务身份。
- 用 `DataKey.Id` 或 raw string 参数表达 runtime entity 引用。
- 在 `EntitySpawnConfig` 恢复 `ParentEntity / AutoAddParentRelation / ParentRelationTypes`。
- 用 `EntityRelationshipManager / EntityRelationshipType / EntityRelationshipTraversal` 表达 projectile、effect、ability、UI、component owner 或 damage attribution。
- 新增 `EntityManager_Ability` 这类业务 partial，把领域能力塞回 EntityManager。
- 让 current 文档或新代码重新指向 `DocsNew`、`Src/ECS/**.md` 作为框架文档入口。
- 把具体 Entity、Component 或 Preset 放回旧 `Src/ECS/Base` 或 DocsAI 顶层技术分类。

## 5. 验证入口

优先使用当前框架仓门禁：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate --all
```

如果涉及承载游戏 Godot 场景，再进入有 runner 的游戏仓补充 scene smoke。

```bash
cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```
