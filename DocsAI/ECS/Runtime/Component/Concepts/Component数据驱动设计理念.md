# Component 数据驱动设计理念

> 状态：current
> 更新：2026-06-04
> 相关入口：`DocsAI/ECS/Runtime/Data/Data系统说明.md`、`DocsAI/ECS/Runtime/Component/README.md`

## 一句话结论

Component 可以有私有缓存，但业务真相必须在 `Entity.Data`、typed DataKey、`Entity.Events` 和对应 Capability service 之间闭环。私有字段只保存当前组件内部算法或节点引用，不作为跨组件共享状态。

## 为什么状态放 Data

Data 是 Runtime 的共享状态面。把业务状态放进 `Entity.Data` 有四个直接收益：

| 收益 | 说明 |
| --- | --- |
| 对象池复用稳定 | Entity 销毁/回池时统一清理 Data，避免遗漏私有字段 reset。 |
| 组件解耦 | Movement、Damage、Ability、UI/Test 可以通过 Data/Event 协作，不直接互调组件。 |
| 可观察 | TestSystem、ResourceCatalog、日志和后续 observation 可以统一读取 Data。 |
| 可迁移 | Entity migration、snapshot apply 和调试工具只需要理解 Data 契约。 |

## DataKey 当前事实源

新增 DataKey 不再手写 `const string`、`DataMeta` 或 `DataRegistry.Register(...)`。当前流程是：

```text
DataOS descriptor
  -> runtime snapshot
  -> generated DataKey<T>
  -> 业务代码用 generated handle 访问 Entity.Data
```

正确示例：

```csharp
var hp = _data.Get<float>(GeneratedDataKey.CurrentHp);
_data.Set(GeneratedDataKey.CurrentHp, hp - damage);
_data.Add(GeneratedDataKey.Score, 10);
```

错误示例：

```csharp
_data.Get<float>("CurrentHp");
public const string CurrentHp = "CurrentHp";
DataRegistry.Register(...);
```

## 存 Data 还是私有字段

| 情况 | 放哪里 | 示例 |
| --- | --- | --- |
| 其他组件/System/调试工具需要读取 | `Entity.Data` | `CurrentHp`、`DefaultMoveMode`、`MovementFacingDirection` |
| 对象池复用后必须恢复默认 | `Entity.Data` 或正式生命周期服务 | HP、冷却、状态标签 |
| runtime snapshot 配置 | DataOS descriptor + generated key | `MaxHp`、`MoveSpeed`、`VisualScenePath` |
| 组件内部算法推进 | 私有字段 | 当前角速度、阶段索引、累计角度 |
| 节点或控件引用缓存 | 私有字段 | `AnimatedSprite2D`、`CollisionShape2D`、UI 控件 |
| 临时局部变量 | 私有字段或方法局部变量 | 本帧目标缓存、一次性计算结果 |
| 组件结构参数 | code composer/profile options | `EntityOrientationComponent.Sink`、内部桥接开关 |

判断标准：删掉这个 DataKey 后，除了当前组件内部实现外没有任何调用方受影响，它大概率不该是 DataKey。

## Component 参数不是 Data

纯代码化 Component 组合后，固定结构参数由 composition profile 注入，不通过 `[Export]` / Inspector，也不默认进入 DataOS。

进入 Data 的条件是“业务状态或配置需要被跨系统读取、观察、迁移或由 runtime snapshot 管理”。只影响单个 Component 如何桥接 Godot 节点的参数，应留在 Component options 中。

示例：`EntityOrientationComponent.Sink` 只决定朝向输出写到 root rotation 还是 VisualRoot flip。它不会改变移动系统的朝向状态真相，其他系统也不应依赖它做业务判断。因此它属于 composition profile 参数，不应新增 DataKey。

## 事件协作

Component 之间不要直接调用具体组件方法。优先通过 Entity 局部事件协作：

```csharp
_entity.Events.Emit(new GameEventType.Unit.HealRequest(amount, source));
_entity.Events.On<GameEventType.Unit.HealRequest>(OnHealRequest);
```

Data 变化响应也通过 `Entity.Events` 中的 typed payload。不要恢复旧 `Data.On(...)` 监听模型。

## 对象池与注销

Component 注销时只清理本组件持有的引用、取消外部订阅或释放非 Data 资源。业务状态不靠 Component 自己 `Reset()`：

```csharp
public void OnComponentUnregistered()
{
    _sprite = null;
    _entity = null;
    _data = null;
}
```

如果组件订阅了 `GlobalEventBus.Global` 或外部 C# event，必须在注销或失活时显式解绑；`Entity.Events.Clear()` 只清理 Entity 自己的局部总线。

## 常见错误

- 用 `_currentHp`、`_moveSpeed` 这类私有字段保存业务真相。
- 用字符串 key 访问 Data。
- 在 Component 内新建 DataKey 事实源。
- 在 `_EnterTree()` / `_Ready()` 里读取 Spawn 后才会写入的数据。
- 用 `[Export]` / Inspector 承载 Component 默认参数。
- 用 `GetComponent<T>()` 直接调用其他组件方法来完成常规通信。
- 为了目录整齐把 Capability 组件移回 `Runtime/Component`。
