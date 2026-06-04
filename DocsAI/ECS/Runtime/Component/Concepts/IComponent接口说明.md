# IComponent 接口说明

> 状态：current
> 更新：2026-06-04
> 源码：`Src/ECS/Runtime/Component/IComponent.cs`、`Src/ECS/Runtime/Entity/Components/ComponentRegistrar.cs`

## 定位

`IComponent` 是 Godot 可挂节点接入 Runtime Entity 的最小生命周期契约。它不是传统纯 ECS 的数据组件，也不是业务继承根；它只让 `EntityManager` 在 Entity 注册和注销时识别组件、建立内部 owner 索引，并回调组件初始化/清理逻辑。

Component 是 SlimeAI 自定义生命周期节点。注册初始化只使用 `OnComponentRegistered`，注销清理只使用 `OnComponentUnregistered`；不要用 Godot `_EnterTree()` 或 `_Ready()` 作为 Entity/Data/Event 初始化入口。

```csharp
public interface IComponent
{
    void OnComponentRegistered(Node entity);
    void OnComponentUnregistered();
}
```

## 识别规则

`ComponentRegistrar` 会递归扫描 Entity 下的 Node，并按以下规则识别 Component：

| 规则 | 说明 | 建议 |
| --- | --- | --- |
| 实现 `IComponent` | 有注册/注销回调，能在注册时缓存 `IEntity` 和 `Data` | 新组件优先使用 |
| 类名以 `Component` 结尾 | 兼容旧节点，只有 owner 索引，没有接口回调 | 只作为兼容 |

注册后，Entity 与 Component 的归属关系只进入 `ComponentRegistrar` 内部索引。新代码不要再通过 `EntityRelationshipManager` 或 `ENTITY_TO_COMPONENT` 查询组件归属。

## 标准写法

```csharp
using Godot;

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
        _entity.Events.On<GameEventType.Unit.HealRequest>(OnHealRequest);
    }

    public void OnComponentUnregistered()
    {
        _entity = null;
        _data = null;
    }

    private void OnDataChanged(GameEventType.Data.PropertyChanged evt)
    {
        if (evt.Key != GeneratedDataKey.CurrentHp.StableKey)
            return;

        // 响应 Data 变化。
    }

    private void OnHealRequest(GameEventType.Unit.HealRequest evt)
    {
        if (_data == null)
            return;

        var hp = _data.Get<float>(GeneratedDataKey.CurrentHp);
        _data.Set(GeneratedDataKey.CurrentHp, hp + evt.Amount);
    }
}
```

要点：

- 在 `OnComponentRegistered` 缓存 `IEntity` 和 `Data`。
- 在 `OnComponentRegistered` 订阅 `Entity.Events`。
- 在 `OnComponentUnregistered` 清理本组件缓存的引用。
- 不需要在组件注销时手动清空 `Entity.Events`；Entity 销毁流程会统一清理。
- 固定结构参数应在注册前由代码化 composer/profile 注入，不使用 `[Export]` / Inspector 作为默认配置来源。

## 参数注入

`IComponent` 不扩展参数签名。参数注入属于组件构造和注册之间的同一创建阶段：

```text
new component
  -> Configure(options)
  -> AddChild
  -> ComponentRegistrar.RegisterComponent
  -> IComponent.OnComponentRegistered
```

示例：

```csharp
public readonly record struct MyComponentOptions(bool EnableBridge);

public partial class MyComponent : Node, IComponent
{
    private bool _enableBridge;

    public void Configure(MyComponentOptions options)
    {
        _enableBridge = options.EnableBridge;
    }

    public void OnComponentRegistered(Node entity)
    {
        // 注册期读取 Entity/Data，并使用已经注入的结构参数。
    }

    public void OnComponentUnregistered()
    {
    }
}
```

参数只用于组件结构或桥接策略。共享业务状态、runtime snapshot 配置和跨系统可观察结果仍进入 `Entity.Data`。

## Data 时序

`OnComponentRegistered` 可以读取 runtime snapshot 已注入的配置数据，但不能假设所有 Spawn 后代码设置的运行时数据已经存在。

| 数据类型 | 示例 | 注册时可读 | 正确处理方式 |
| --- | --- | --- | --- |
| snapshot 配置 | `MaxHp`、`MoveSpeed`、`VisualScenePath` | 可以 | 直接用 generated `DataKey<T>` 读取 |
| Spawn 后设置 | `TargetEntityId`、临时目标、技能等级覆盖 | 不保证 | 监听 `GameEventType.Data.PropertyChanged` |
| 组件内部缓存 | 动画节点引用、阶段缓存 | 自行维护 | 私有字段，注销时清理引用 |

## 查询 owner

组件需要反查所属 Entity 时，使用 Runtime facade：

```csharp
var entityNode = EntityManager.GetEntityByComponent(this);
var data = EntityManager.GetEntityData(this);
```

优先在注册回调中缓存 `_entity` / `_data`；只有动态工具、迁移兼容或调试代码才需要临时反查。

## 生命周期

```text
EntityManager.Spawn/Register
  -> EntityRegistry register
  -> RegisterComponents(entity)
  -> ComponentRegistrar 建立 owner 索引
  -> IComponent.OnComponentRegistered(entity)

EntityManager.Destroy
  -> EntityDestroyPipeline
  -> ComponentRegistrar.UnregisterComponents(entity)
  -> IComponent.OnComponentUnregistered()
  -> Data / Events / registry / pool cleanup
```

## 红线

- 不用 `EntityRelationshipManager` 查 Component owner。
- 不在 `_EnterTree()` / `_Ready()` 里假设 Entity 已注册；注册相关初始化放到 `OnComponentRegistered`。
- 不使用 `[Export]` / Inspector 作为 Component 默认配置来源。
- 不用字符串访问 Data，例如 `_data.Get<float>("CurrentHp")`。
- 不给 Component 私有字段存放跨系统共享业务状态。
- 不把具体业务组件放回 `Runtime/Component`；具体组件归 Capability owner。
