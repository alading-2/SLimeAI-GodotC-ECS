# Entity 架构设计

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Entity/`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/3.Entity系统优化/`

## 1. Scene 即 Entity

本项目基于 Godot 哲学：`.tscn 文件即 Entity`。

- Entity = 可挂载到场景树的 Godot Node
- Component = Entity 场景下的子节点
- 业务逻辑 = Component / System / Service / Tool，不在 Entity 类中

## 2. 为什么不用基类

C# 单继承。Player 需要 `CharacterBody2D`，Bullet 需要 `Area2D`，Buff 只需要 `Node`。强制继承同一基类会冲突。

解决方案：`IEntity` 接口 + 组合。

```csharp
public interface IEntity
{
    Data Data { get; }
    EventBus Events { get; }
}

public partial class Player : CharacterBody2D, IEntity { }
public partial class Bullet : Area2D, IEntity { }
```

## 3. AI-first 原则

| 旧问题 | AI-first 规则 |
|--------|--------------|
| `string relationType` 看不出语义 | 每种引用必须有 typed 字段名和 owner 文档 |
| `ParentEntity` 既像销毁 parent 又像伤害 owner | Lifecycle parent 只负责生命周期；伤害归因走 `EntityAttributionResolver` |
| `EntityManager` partial 太多 | Entity core 不包含 Ability / Projectile / Effect / UI 业务方法 |
| Data id / InstanceId 混用 | Runtime API 只接受 `EntityId`；`GeneratedDataKey.Id` 只作为 DataOS 字符串投影 |
| 旧事件字符串 / `XxxEventData` 双写 | Entity lifecycle 事件必须用 `readonly record struct` payload |

## 4. 模块边界

| 模块 | 职责 | 禁止 |
|------|------|------|
| `EntityId` | Typed runtime identity | 不隐式转 string，不表达 Godot InstanceId |
| `EntityRegistry` | id -> node、node -> id 注册表 | 不知道 Ability、Projectile、UI |
| `EntitySpawnPipeline` | 编排 spawn 阶段，集中失败回滚 | 不直接处理业务 relationType |
| `ComponentRegistrar` | Component 扫描、注册、注销、owner 索引 | 不通过 public Relationship 表 |
| `LifecycleTree` | 单 parent 生命周期树 | 不表达 owner/source/target/ability/ui |
| `OwnedReferenceRegistry` | Owner list 自动清理 | 不决定 child 生命周期 |
| `EntityAttributionResolver` | Damage / Movement 归因解析 | 不作为 gameplay 查询源 |

## 5. 关系语义拆分

旧 `EntityRelationshipManager` 把多种语义混在一个字符串关系图里：
- lifecycle parent
- Component owner
- Projectile / Effect / Ability owner
- UI binding
- Damage attribution

当前拆分：
- **lifecycle parent** -> `LifecycleTree`（单 parent、销毁策略）
- **Component owner** -> `ComponentRegistrar` 内部索引
- **Projectile owner** -> `ProjectileOwnershipService` + typed Data projection
- **Effect host/owner** -> `EffectOwnershipService` + typed Data projection
- **Ability owner** -> `AbilityInventoryService`
- **Damage attribution** -> `EntityAttributionResolver`

## 6. 与纯 ECS 对比

| 特性 | 传统纯 ECS | 本项目 |
|------|-----------|--------|
| Entity 定义 | C# 类 + ID | `.tscn` + `IEntity` |
| 组件管理 | 组件数组/archetype | Godot 场景树 + `ComponentRegistrar` |
| 继承 | 需基类 | 接口 + 组合 |
| 编辑器支持 | 弱 | 强（可视化编辑） |
| Godot 兼容性 | 需大量适配 | 原生契合 |

## 7. 历史判断

Entity 重构采用 hard cutover：
- 删除通用字符串关系图
- 删除 parent-chain 归因
- 删除 `EntityManager` 业务 partial

替换为：typed `EntityId` + `EntityRegistry` + `EntitySpawnPipeline` + `LifecycleTree` + `ComponentRegistrar` + `OwnedReferenceRegistry` + explicit `EntityAttributionResolver`。

核心判断：AI 无需猜语义。每种引用必须有 typed 字段名和明确文档。
