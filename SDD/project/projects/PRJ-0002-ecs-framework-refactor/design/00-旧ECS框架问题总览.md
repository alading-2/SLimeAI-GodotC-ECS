# 旧 ECS 框架问题总览

> 更新：2026-05-26
> 范围：`SlimeAI/Src/ECS`、`SlimeAI/Data` 中旧 Godot C# ECS 框架的 Data、Event、Entity、Relationship 基础设施。
> 结论：旧 ECS 框架不需要整体重构，需要围绕真实问题做渐进优化。

## 1. 方向纠偏

当前项目不再采用以下方向：

- 不再把旧 ECS 当作迁移输入。
- 不再改变 `SlimeAI/Src` 的主目录方向。
- 不再追求一次性删除旧 ECS API。
- 不再用外部参考框架作为目标结构。

当前目标是：

```text
保留旧 ECS 的整体架构与 Godot 集成方式，
只解决 Data / Event / Entity / Relationship 中真实存在的维护问题。
```

## 2. 旧 ECS 做得好的部分

### 2.1 Godot Node 与 ECS 概念贴合当前项目

当前框架不是纯 ECS，也不需要伪装成纯 ECS。它的有效假设是：

- Entity 是 Godot 节点。
- Component 也是 Godot 节点。
- 场景树提供可视化、生命周期和编辑器组织。
- `EntityManager` 提供统一生成、注册、注销和销毁入口。
- `EntityRelationshipManager` 补足场景树无法表达的业务关系。

这个方向对 Godot 项目是合理的，不需要为了抽象纯净而拆掉。

### 2.2 核心服务已经能承载玩法

当前已有：

- `Data`：运行时数据容器、默认值、修改器、计算值、变更事件。
- `EventBus`：局部事件、全局事件、优先级、一次性订阅、重入保护。
- `EntityManager`：Spawn、对象池、ResourceManagement、Component 注册、Destroy。
- `EntityRelationshipManager`：父子关系、Component 关系、Ability/Projectile/UI 关系索引。

这些都是真实可用的框架能力，不应被整体推翻。

## 3. 真实问题地图

| 问题域 | 核心问题 | 影响 |
| --- | --- | --- |
| Data | DataKey 已经部分类型化，但运行时仍以 string key 为主，裸字符串和 `DataMeta` 混用 | 改名风险、默认值/类型不一致、AI 容易写错键 |
| Event | `GameEventType` 字符串常量树与 payload 分离，且当前 `.cs` 定义缺失 | 编译事实源断裂、事件名/数据类型不同步 |
| Entity | Entity/Component 都是 Node 的设计合理，但生命周期入口和直接 Godot 操作边界需要更明确 | 容易绕过 `EntityManager`，对象池和注册状态出错 |
| Relationship | 关系类型和关系数据仍是字符串/字典，缺少统一 typed key 规则 | 关系名拼写、关系数据解析、生命周期策略一致性风险 |
| 文档/SDD | 旧 design 文档大量保留整体替换和外部参考语言 | 后续 AI 会被引回错误方向 |

## 4. 最高优先级

当前最高优先级不是目录重组，而是**字符串变量名统一**。

它同时影响：

- `Data.Get/Set(string key)`
- `DataMeta.Key`
- `DataKeyAttribute`
- `GameEventType.*`
- `EventBus.On/Emit(string eventName, ...)`
- `EntityRelationshipType.*`
- `RelationshipRecord.Data`
- Resource / scene path 的字符串映射

如果这个问题不收口，后续任何 AI 或人类修改都会继续引入裸字符串、重复常量和错拼键。

## 5. 推荐拆分

后续不要创建一个“ECS 整体替换”任务，而应拆成：

1. DataKey 与字符串键名统一。
2. Event 定义事实源恢复与事件主键优化。
3. Entity 生命周期边界和直接 Godot 操作审计。
4. Relationship 类型和关系数据键统一。
5. 文档与 Skill 同步更新。

每个任务都应有独立构建/测试证据，避免一次性大改。
