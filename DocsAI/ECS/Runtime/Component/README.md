# Runtime Component 文档入口

> 状态：current
> 更新：2026-06-04
> 范围：`Src/ECS/Runtime/Component/`、`Src/ECS/Runtime/Entity/Components/`、Capability 内部 `Component/` 目录。

## 定位

`Runtime/Component` 只定义 Godot Node Component 接入 Runtime Entity 的最小契约：

- `IComponent`：可挂节点接入 Entity 注册/注销生命周期的接口。
- `TemplateComponent`：当前组件模板，展示 `Entity.Data`、`Entity.Events` 和 generated `DataKey<T>` 的使用边界。
- `ComponentRegistrar`：位于 `Runtime/Entity/Components/`，维护 Entity 与 Component 的内部 owner 索引。

具体组件不放在 Runtime 下；Ability、Unit、Collision、Movement 等组件必须归到对应 `DocsAI/ECS/Capabilities/<owner>/` 和 `Src/ECS/Capabilities/<owner>/Component/`。

## 阅读顺序

1. `ComponentManifest.md`：当前 Component contract、profile 集合、组件清单和验证门禁。
2. `Concepts/IComponent接口说明.md`：组件注册、识别、生命周期和 owner 反查规则。
3. `Concepts/Component数据驱动设计理念.md`：组件状态该进 Data 还是私有字段的判定。
4. `Concepts/Component规范说明.md`：从旧规范收敛后的当前写法清单。
5. `../Entity/Entity使用说明.md`：Spawn / Destroy / ComponentRegistrar 在 Entity 生命周期中的位置。
6. `../Data/Data系统说明.md`：新增 DataKey 必须先写 DataOS descriptor，再生成 typed handle。

## 归属规则

| 类型 | 归属 |
| --- | --- |
| `IComponent`、`TemplateComponent` | `Src/ECS/Runtime/Component/` |
| `IComponentCompositionProvider`、`ComponentComposer` | `Src/ECS/Runtime/Component/ComponentComposition.cs` |
| Component 注册、注销、owner 反查 | `Src/ECS/Runtime/Entity/Components/` |
| Ability、Unit、Collision 等业务组件 | `Src/ECS/Capabilities/<owner>/Component/` |
| Component 代码化组合 | `Src/ECS/Capabilities/<owner>/Entity/*ComponentCompositionProfiles.cs`；旧 `Presets/` 只作为 legacy 对照输入 |

## 红线

- Component owner 反查走 `EntityManager.GetEntityByComponent` / `ComponentRegistrar`，不恢复旧 Entity relationship 常量。
- Component 间通信优先 `Entity.Events`，不要直接互调具体组件方法。
- 运行时业务状态写入 `Entity.Data`，使用 generated `DataKey<T>`；不要用字符串字面量或旧 `DataMeta/DataRegistry`。
- Component 是 SlimeAI 自定义生命周期节点；注册初始化只用 `OnComponentRegistered`，注销清理只用 `OnComponentUnregistered`。
- 不使用 `_EnterTree()` / `_Ready()` 做 Entity/Data/Event 初始化。
- 不使用 Inspector 导出参数作为 Component 默认配置来源；固定结构参数由代码化 composer/profile 注入。
- `EntitySpawnPipeline` 和 `EntityManager.RegisterComponents(entity)` 都会在注册前执行 `ComponentComposer.Compose(entity)`。
- Capability 的具体 Entity、Component 和 composition profile 都放在功能 owner 下，不按技术类型重新拉出顶层目录。
