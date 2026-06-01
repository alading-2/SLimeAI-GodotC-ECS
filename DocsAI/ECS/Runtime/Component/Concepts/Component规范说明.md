# Component 规范说明

> 状态：current
> 更新：2026-06-01
> 迁移来源：`Src/ECS/Base/Component/Component规范.md`
> 当前归属：`DocsAI/ECS/Runtime/Component/Concepts/`

## 核心原则

Component 是挂在 Godot Entity 场景下的可组合行为节点。它承担引擎桥接、输入消费、表现控制或局部行为执行，但不拥有 Entity 身份，也不替代 Capability service / Runtime System。

| 原则 | 当前规则 |
| --- | --- |
| 单一职责 | 一个 Component 只做一个清晰职责，例如移动桥、动画桥、碰撞桥。 |
| 数据驱动 | 共享业务状态写入 `Entity.Data`，通过 generated `DataKey<T>` 访问。 |
| 事件驱动 | 组件间默认通过 `Entity.Events` 通信。 |
| owner 内聚 | 具体业务组件放在 `Src/ECS/Capabilities/<owner>/Component/`。 |
| Runtime 最小化 | `Runtime/Component` 只保留 `IComponent`、模板和共性规则。 |

## 标准初始化模式

```csharp
public partial class MyComponent : Node, IComponent
{
    private IEntity? _entity;
    private Data? _data;

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity)
            return;

        _entity = iEntity;
        _data = iEntity.Data;

        _entity.Events.On<GameEventType.Data.PropertyChanged>(OnDataChanged);
    }

    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }
}
```

不要在 `_Ready()` 中假设 Entity 已完成 Runtime 注册。涉及 `Entity.Data`、`Entity.Events` 或 owner 的初始化放到 `OnComponentRegistered`。

## 通信优先级

| 优先级 | 方式 | 用途 |
| --- | --- | --- |
| 1 | `Entity.Events` | 解耦请求、结果、状态变化通知。 |
| 2 | `Entity.Data` | 共享状态读写和无状态查询。 |
| 3 | Capability service / Runtime facade | 跨 Entity、owner 引用、系统级操作。 |
| 4 | `EntityManager.GetComponent<T>()` | 调试、兼容或极少数必须直连的场景。 |

常规业务不要直接调用其他 Component 方法：

```csharp
// 推荐：发送请求，由对应组件或系统监听。
_entity.Events.Emit(new GameEventType.Unit.HealRequest(amount, source));

// 谨慎：只在明确需要直连、且文档说明原因时使用。
var animation = EntityManager.GetComponent<UnitAnimationComponent>(entity);
```

## Data 存储规则

必须写入 Data：

- HP、状态、速度、阵营、运行时标签等共享业务状态。
- 其他 Component、System、UI、TestSystem 需要读取的结果。
- 对象池复用后必须统一清理或恢复的状态。
- runtime snapshot record 注入的配置字段。

可以放私有字段：

- 当前组件内部算法状态。
- 节点引用缓存。
- UI 控件引用。
- 本帧临时计算结果。
- 不需要被其他模块观察、保存、迁移或测试的内部细节。

## DataKey 规则

```csharp
var hp = _data.Get<float>(GeneratedDataKey.CurrentHp);
_data.Set(GeneratedDataKey.CurrentHp, hp - amount);
```

新增字段必须先写 DataOS descriptor，再生成 typed handle。禁止：

- `_data.Get<float>("CurrentHp")`
- `public const string CurrentHp = "CurrentHp"`
- 新增旧式 `DataMeta` / `DataRegistry` 作为字段事实源
- 恢复旧 `DataKey.*` 兼容入口作为新代码模板

## 生命周期

```text
EntityManager.Spawn/Register
  -> runtime snapshot data apply
  -> EntityRegistry register
  -> ComponentRegistrar.RegisterComponents
  -> IComponent.OnComponentRegistered

EntityManager.Destroy
  -> EntityDestroyPipeline
  -> ComponentRegistrar.UnregisterComponents
  -> IComponent.OnComponentUnregistered
  -> Entity Data / Events / registry / pool cleanup
```

Component 不需要手动维护 Entity-Component 关系。owner 索引由 `ComponentRegistrar` 维护。

## Preset 归属

Preset 是组合多个 Component 或默认节点结构的场景资源，不是 Runtime Component 类型。

| 类型 | 目录 | 资源分类 |
| --- | --- | --- |
| Ability preset | `Src/ECS/Capabilities/Ability/Presets/` | `Preset` |
| Unit preset | `Src/ECS/Capabilities/Unit/Presets/` | `Preset` |
| 其他 owner preset | `Src/ECS/Capabilities/<owner>/Presets/` | `Preset` |

不要因为 preset 组合了 Component 就放回 `Runtime/Component` 或顶层 `Component/`。

## 快速检查清单

- [ ] 组件实现 `IComponent`，或明确只是旧命名兼容组件。
- [ ] 注册时缓存 `_entity` / `_data`，注销时清理引用。
- [ ] 共享状态使用 generated `DataKey<T>`。
- [ ] 跨组件通信使用 `Entity.Events` 或正式 service。
- [ ] 没有新增字符串 DataKey、旧 `DataMeta/DataRegistry` 或 Relationship owner 查询。
- [ ] 具体组件位于 Capability owner 目录。

