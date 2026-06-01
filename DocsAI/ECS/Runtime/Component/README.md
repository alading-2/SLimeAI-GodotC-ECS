# Runtime Component 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/Component/`、`Src/ECS/Runtime/Entity/Components/`、Capability 内部 `Component/` 目录。

## 定位

`Runtime/Component` 只定义 Godot Node Component 接入 Runtime Entity 的最小契约：

- `IComponent`：可挂节点接入 Entity 注册/注销生命周期的接口。
- `TemplateComponent`：当前组件模板，展示 `Entity.Data`、`Entity.Events` 和 generated `DataKey<T>` 的使用边界。
- `ComponentRegistrar`：位于 `Runtime/Entity/Components/`，维护 Entity 与 Component 的内部 owner 索引。

具体组件不放在 Runtime 下；Ability、Unit、Collision、Movement 等组件必须归到对应 `DocsAI/ECS/Capabilities/<owner>/` 和 `Src/ECS/Capabilities/<owner>/Component/`。

## 阅读顺序

1. `Concepts/IComponent接口说明.md`：组件注册、识别、生命周期和 owner 反查规则。
2. `Concepts/Component数据驱动设计理念.md`：组件状态该进 Data 还是私有字段的判定。
3. `Concepts/Component规范说明.md`：从旧规范收敛后的当前写法清单。
4. `../Entity/Entity使用说明.md`：Spawn / Destroy / ComponentRegistrar 在 Entity 生命周期中的位置。
5. `../Data/Data系统说明.md`：新增 DataKey 必须先写 DataOS descriptor，再生成 typed handle。

## 归属规则

| 类型 | 归属 |
| --- | --- |
| `IComponent`、`TemplateComponent` | `Src/ECS/Runtime/Component/` |
| Component 注册、注销、owner 反查 | `Src/ECS/Runtime/Entity/Components/` |
| Ability、Unit、Collision 等业务组件 | `Src/ECS/Capabilities/<owner>/Component/` |
| 组合若干组件的场景预设 | `Src/ECS/Capabilities/<owner>/Presets/`，资源分类为 `Preset` |

## 红线

- Component owner 反查走 `EntityManager.GetEntityByComponent` / `ComponentRegistrar`，不恢复 `EntityRelationshipType.ENTITY_TO_COMPONENT`。
- Component 间通信优先 `Entity.Events`，不要直接互调具体组件方法。
- 运行时业务状态写入 `Entity.Data`，使用 generated `DataKey<T>`；不要用字符串字面量或旧 `DataMeta/DataRegistry`。
- Capability 的具体 Entity、Component、Preset 都放在功能 owner 下，不按技术类型重新拉出顶层目录。

