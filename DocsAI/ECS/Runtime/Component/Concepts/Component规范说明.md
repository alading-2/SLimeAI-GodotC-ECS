# Component 规范说明

> 状态：current
> 更新：2026-06-04
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
| 代码化组合 | Component 默认由代码 composer/profile 创建和组装，不再使用 `.tscn` Preset 作为新组合事实源。 |
| 参数注入 | 固定结构参数由代码注入，不使用 Inspector 导出参数作为默认配置入口。 |

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

Component 是 SlimeAI 自定义生命周期节点。不要用 `_EnterTree()` 或 `_Ready()` 做注册初始化；涉及 `Entity.Data`、`Entity.Events`、owner 缓存和外部订阅的逻辑只放到 `OnComponentRegistered`。

## 代码化组合与参数注入

2026-06-04 裁决：Component Preset 目标形态为纯代码化组合。现有 `Src/ECS/Capabilities/*/Presets/*.tscn` 只作为迁移期 legacy 输入；新组件组合不得新增 Preset `.tscn`，也不得用 Godot Inspector 承载默认参数。

目标调用顺序：

```text
EntitySpawnPipeline / ComponentComposer
  -> new Component
  -> Configure(options) 或构造期注入固定结构参数
  -> AddChild 到 Entity/Component 容器
  -> ComponentRegistrar.RegisterComponent(s)
  -> IComponent.OnComponentRegistered
```

当前 Runtime 入口：

- `Src/ECS/Runtime/Component/ComponentComposition.cs`：`IComponentCompositionProvider`、`ComponentCompositionProfile`、`ComponentCompositionEntry`、`ComponentComposer`。
- `EntitySpawnPipeline`：spawn 路径在 `ComponentRegistrar.RegisterComponents` 前执行 composition。
- `EntityManager.RegisterComponents(entity)`：直接注册路径同样先 composition；composition provider 会绕过旧预热缓存并实时扫描。
- `Src/ECS/Capabilities/Unit/Entity/UnitComponentCompositionProfiles.cs`：UnitCore / Player / Enemy profile。
- `Src/ECS/Capabilities/Ability/Entity/AbilityComponentCompositionProfiles.cs`：Ability profile。

建议写法：

```csharp
public readonly record struct MyComponentOptions(bool EnableDebugBridge);

public partial class MyComponent : Node, IComponent
{
    private bool _enableDebugBridge;

    public void Configure(MyComponentOptions options)
    {
        _enableDebugBridge = options.EnableDebugBridge;
    }

    public void OnComponentRegistered(Node entity)
    {
        // 在这里缓存 Entity/Data 并订阅事件。
    }

    public void OnComponentUnregistered()
    {
        // 在这里清理引用和外部订阅。
    }
}
```

`Configure(...)` 只表达组件结构参数，不替代 `Data`。如果一个值会被其他 Component/System/UI/Test 读取、会参与 runtime snapshot、会被对象池复用清理、或是业务状态真相，它必须进入 `Entity.Data`。

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
- 组件结构参数，例如输出 sink、桥接模式、是否启用某个本组件内部 adapter 分支。

### `EntityOrientationComponent.Sink` 裁决

`EntityOrientationComponent.Sink` 表达“朝向结果写到 root rotation，还是写到 VisualRoot flip”的 Godot bridge 输出策略。它不是移动状态、不是朝向运行态、不是需要 System/UI 读取的业务真相。

因此它不应新增 Data 字段；应作为 `EntityOrientationComponentOptions` 由代码化 Unit composition 注入。这样可以避免 DataOS 出现只有单个 Component 内部使用的噪音字段，也能让 AI 在 composition profile 中直接看到 Player/Enemy/Projectile 的组件结构差异。

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

## Preset 迁移边界

旧 Preset 是组合多个 Component 的场景资源，不是 Runtime Component 类型。SDD-0030 后，Player / Enemy / TargetingIndicator / Ability 的默认组件组合事实源已经迁到代码化 `ComponentComposer` / `ComponentCompositionProfile`。

当前规则：

- 现有 Preset `.tscn` 可作为对照输入，直到执行型 SDD 删除或停止引用。
- 新增 Component 组合必须写在代码 profile/composer 中。
- 不把 Preset 放回 `Runtime/Component` 或顶层 `Component/`。
- 不再通过 Inspector 导出参数维护 Component 默认参数。

## 快速检查清单

- [ ] 组件实现 `IComponent`，或明确只是旧命名兼容组件。
- [ ] 注册时缓存 `_entity` / `_data`，注销时清理引用。
- [ ] 不使用 `_EnterTree()` / `_Ready()` 做 Entity/Data/Event 初始化。
- [ ] 不使用 Inspector 导出参数作为 Component 默认配置来源。
- [ ] 固定结构参数由代码化 composer/profile 注入。
- [ ] 共享状态使用 generated `DataKey<T>`。
- [ ] 跨组件通信使用 `Entity.Events` 或正式 service。
- [ ] 没有新增字符串 DataKey、旧 `DataMeta/DataRegistry` 或 Relationship owner 查询。
- [ ] 具体组件位于 Capability owner 目录。
