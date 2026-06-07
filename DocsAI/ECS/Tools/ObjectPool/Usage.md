<!-- migrated-from: Src/ECS/Tools/ObjectPool/ObjectPool.md -->

> 迁移来源：`Src/ECS/Tools/ObjectPool/ObjectPool.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# ObjectPool C# 版本

> 状态：current
> 更新：2026-06-07
> 当前入口：`DocsAI/ECS/Tools/ObjectPool/README.md`

基于 TypeScript 和 GDScript 版本设计的 Godot 4.x C# 高性能对象池实现。专门针对 Godot Node 的生命周期进行了优化，支持自动处理 `ProcessMode` 和 `Visible`。

## 特性

- **泛型支持** - `ObjectPool<T>` 支持任意引用类型。
- **混合命名空间** - 核心工具类位于全局命名空间，无需 `using` 即可随时随地调用。
- **自动管理 Node** - 出池自动激活（`Inherit` + `Visible`），入池自动挂起（`Disabled` + `Invisible`）。
- **场外常驻策略** - `CollisionObject2D` 节点回池后默认仍留在 `SceneTree`，不脱树、不关碰撞、不改 layer/mask/shape，只隐藏、停处理、移动到分散 parking grid。
- **碰撞逻辑验证** - 所有业务碰撞入口必须查 pool runtime state；回池对象和未到 ready frame 的对象不处理碰撞逻辑。
- **激活首帧保护** - `Activate()` 后设置 `CollisionReadyPhysicsFrame = currentPhysicsFrame + 1`，激活第一帧默认不处理业务碰撞。
- **两阶段激活时序** - `Get(false)` 不提前触发 `OnPoolAcquire`，而是等 `Activate()` 时才真正完成挂树保障、恢复节点并触发生命周期。
- **生命周期回调** - 通过 `IPoolable` 接口实现精准的对象重置逻辑。
- **全局管理器** - `ObjectPoolManager` 自动管理所有创建的池，支持**静态归还**与**按名查找**；Node 走 Meta，纯 C# 对象走内部映射。
- **详细统计** - 实时追踪复用率、活跃数、闲置数及创建/销毁总量。
- **管理器线程锁** - `ObjectPoolManager` 内部用 `lock` 保护全局池字典；Godot Node 生命周期操作仍必须在主线程执行。

## 文件组织

- `Core/ObjectPool.cs`: 核心对象池逻辑。
- `Core/IPoolable.cs`: 池化对象生命周期接口。
- `Core/IObjectPoolRuntime.cs`: manager 使用的非泛型运行时接口，避免反射管理泛型池。
- `Management/ObjectPoolManager.cs`: 全局池管理逻辑，负责池的注册、查找与静态归还。
- `Management/ObjectPoolInit.cs`: 推荐的全局初始化入口（AutoLoad）。
- `Lifecycle/`: Node 显隐、ProcessMode、parking grid 和 fallback detach 策略。
- `RuntimeState/`: pool runtime state、节点状态快照和 `CollisionLogicGuard`。
- `Observability/ObjectPoolObservability.cs`: 对象池容量元数据和 TestSystem 观测入口。
- `DocsAI/ECS/Tools/ParentManager/`: 父级挂载工具 owner，负责池化节点的父级路径解析。

## 测试与验证状态

`Src/ECS/Tools/ObjectPool/Tests` 当前只保留 AI-first 自动验证入口：

- `Tests/Contracts/ObjectPoolContractRuntimeTest.cs/.tscn` 是自动 contract checks。
- `Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.cs/.tscn` 是 Godot collision validation scene，旁置 README 五字段和 PASS artifact。
- 历史 UI demo 和 demo fixture 已删除；不再保留依赖 UI、鼠标、随机数或人工观察的 ObjectPool 测试场景。

后续测试重构必须按 [Tests.md](Tests.md) 执行：

1. Contract 与 validation 分目录，不保留 demo 测试场景。
2. Runtime contract checks 覆盖统计、容量、重复归还、manager mapping 和全局污染隔离。
3. Godot collision validation scene 覆盖 `Area2D` / `CharacterBody2D` 根节点场外常驻、`Activate()` 后首帧 guard、raw callback 到 business event oracle、同帧复用和 parking grid 压力。
4. `Tests/Validation/CollisionIsolation/README.md` 包含 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。
5. Godot validation 必须写 PASS artifact；不能只看 stdout、exit code 或 UI 面板。

## 重要限制

### 管理器只走 IObjectPoolRuntime

`ObjectPoolManager` 保存的是 `IObjectPoolRuntime`，不是 `object`。因此全局统计、清理、销毁和静态归还都直接调用接口，不通过 `GetMethod(...).Invoke(...)` 反射泛型池方法。

业务侧仍应优先使用 typed API：

```csharp
var pool = ObjectPoolManager.GetPool<EnemyEntity>(ObjectPoolNames.EnemyPool);
var enemy = pool?.Get();
```

`GetRuntimePool(poolName)` 只给管理器、诊断和 contract test 使用；它不提供无类型 `Get`，避免绕过泛型池的类型安全。

### 静态归还支持 Node 和已映射的纯 C# 对象

`ObjectPoolManager.ReturnToPool()` 当前支持两类对象：

- Godot `Node`：通过 `ObjectPoolName` Meta 查找所属池。
- 纯 C# 对象：通过 `ObjectPoolManager.MapObject()` 维护的内部映射查找所属池。

限制：纯 C# 对象必须通过 `ObjectPool<T>.Get()` 取出，才能建立映射；手动 `new` 出来的对象不能静态归还。

**支持的用法**：

```csharp
// ✅ Node 对象 - 支持静态归还
var enemy = enemyPool.Get();
ObjectPoolManager.ReturnToPool(enemy);  // 自动查找池并归还
```

```csharp
// ✅ 纯 C# 对象 - 必须先从池中 Get，建立归属映射
var pool = new ObjectPool<MyData>(() => new MyData(), config);
var data = pool.Get();
ObjectPoolManager.ReturnToPool(data);
```

**不支持的用法**：

```csharp
// ❌ 手动 new 出来的纯 C# 对象 - 没有池归属映射
public class MyData { }
var data = new MyData();
ObjectPoolManager.ReturnToPool(data);  // 会报错！
```

**纯 C# 类也可以直接调用池的 Release 方法**：

```csharp
// ✅ 直接调用池的 Release 方法
var pool = new ObjectPool<MyData>(() => new MyData(), config);
var data = pool.Get();
pool.Release(data);  // 必须持有池引用
```

**设计原因**：

- Node 使用 Godot 原生 Meta，避免额外字典和循环引用。
- 纯 C# 对象使用内部映射，只覆盖确实从池里取出的对象。
- 直接持有池引用仍是纯 C# 热路径中最清晰的写法。

## 快速开始

### 1. 全局初始化 (推荐)

项目采用集中式初始化模式，通过 `ObjectPoolInit.cs` (AutoLoad) 统一管理核心对象池。

```csharp
// 在 Src/ECS/Tools/ObjectPool/Management/ObjectPoolInit.cs 中
public override void _Ready()
{
    // 使用 ObjectPoolNames 常量避免字符串硬编码
    new ObjectPool<EnemyEntity>(
        () => (EnemyEntity)ResourceManagement.LoadScene<EnemyEntity>().Instantiate(),
        new ObjectPoolConfig
        {
            Name = ObjectPoolNames.EnemyPool,
            InitialSize = 100,
            MaxSize = 500,
            ParentPath = "ECS/Entity/Enemy"
        }
    );
}
```

### 2. 局部创建 (可选)

```csharp
// 在特定脚本中手动创建，创建后会自动注册到管理器
var pool = new ObjectPool<Bullet>(
    factory: () => BulletScene.Instantiate<Bullet>(),
    config: new ObjectPoolConfig { Name = "BulletPool", InitialSize = 50 }
);
```

### 3. 获取和归还对象

```csharp
// 方式 A: 通过管理器查找 (解耦)
var pool = ObjectPoolManager.GetPool<Node>(ObjectPoolNames.EnemyPool);
var enemy = pool?.Get();

// 方式 B: 静态全局归还 (最整洁，推荐 - 仅限 Node 对象)
// 对象不需要持有池的引用，管理器会自动从 Node Meta 中查找池名称
ObjectPoolManager.ReturnToPool(enemy);

// 方式 C: 直接调用池的 Release (适用于所有类型)
pool.Release(enemy);
```

## IPoolable 接口

实现此接口可获得精准的生命周期控制：

```csharp
public partial class Enemy : CharacterBody2D, IPoolable
{
    public void OnPoolAcquire()
    {
        // 从池中取出时：重置状态、播放出生动画等
    }

    public void OnPoolReset()
    {
        // 归还到池中时：重置 HP、清理特效引用等
        Health = MaxHealth;
    }
}
```

## 常见问题与技术细节

### 0. 为什么默认不再脱树 / 关碰撞？

对象池复用 `Area2D` / `CharacterBody2D` 时，旧策略通过“泊车位 + 脱树 + 关碰撞 + 两阶段激活”规避 Godot 物理时序风险。2026-06-03 重新校准后，默认策略改成 `ParkedInTree`：

- 回池后仍在 `SceneTree`。
- 仍保留碰撞体。
- 不 `RemoveChild`。
- 不调用 `SetCollisionTreeActive(false)`。
- 不改 `monitoring/monitorable`、shape disabled、layer/mask。
- 只隐藏、停处理、移动到分散 parking grid，并写 pool runtime state。

原因：如果默认仍脱树或仍关碰撞，新方案就没有意义；而且反复切 layer/mask/shape/monitoring 会重新引入 deferred 和 pair 重建窗口。业务正确性改由统一 guard 保证。

推荐最小 guard：

```text
release:
  CollisionLogicActive = false
  CollisionReadyPhysicsFrame = int.MaxValue
  move to parking grid

activate:
  CollisionLogicActive = true
  CollisionReadyPhysicsFrame = currentPhysicsFrame + 1

collision callback:
  reject if !CollisionLogicActive
  reject if currentPhysicsFrame < CollisionReadyPhysicsFrame
```

补充边界：

- Godot 文档说明 `Area2D` overlap 列表每个 physics step 更新一次，不会在对象移动后立即同步。
- `SetDeferred` 用于把属性修改提交到安全点，不证明对象已经立即退出物理世界。
- `Detach` / `CollisionObject2D.disable_mode = Remove` 只作为 fallback / 对照验证，不再是默认策略。
- parking grid 不是唯一正确性来源；它负责减少停车区 pair 压力，业务正确性靠 guard。
- 碰撞事件到达后仍由 Collision / Damage / Movement owner 过滤实体有效性、team、owner 和生命周期。

### 1. 为什么 ObjectPoolInit 使用 \_EnterTree 而非 \_Ready？

**关键时序问题**：

```
AutoLoad 加载顺序（按 Priority）：
1. ObjectPoolInit._EnterTree()  ← 必须在这里初始化对象池
2. TimerManager._EnterTree()    ← 在这里获取对象池
3. ObjectPoolInit._Ready()      ← 如果在这里初始化就太晚了！
4. TimerManager._Ready()
```

**原因**：

- Godot 的生命周期是：所有节点的 `_EnterTree()` 执行完后，才开始执行 `_Ready()`
- 如果 ObjectPoolInit 在 `_Ready()` 中初始化，其他系统在 `_EnterTree()` 中就无法获取对象池
- 使用 `_EnterTree()` 确保对象池在所有系统需要时已经就绪

**Priority 的作用**：

- `Priority.System - 10` 确保 ObjectPoolInit 比其他系统先进入场景树
- 但只有在 `_EnterTree()` 中初始化，才能保证时序正确

### 2. 自动初始化时机 (AutoLoad Timing)

**现象**：在 `ObjectPoolInit` (AutoLoad) 的 `_Ready` 中创建 `ParentPath` 时，场景树中不显示节点。
**原因**：Godot 的 AutoLoad 节点在主场景加载前初始化。此时 `SceneTree.Root` 虽然存在，但立即同步调用 `AddChild` 可能会因为引擎内部的状态机尚未切换到“就绪”而导致节点挂载失败或在远程调试器中不可见。
**解决方案**：

- 推荐在 `_Ready` 中使用 `CallDeferred(nameof(MyInitMethod))`。
- 确保初始化逻辑在引擎完成首帧设置后执行。

### 3. 节点层级管理 (ParentManager)

为了保持场景树整洁并防止场景切换时对象池被销毁，`ParentManager` 默认将对象池挂载在 `/root` 下：

- **全局持久化**：挂载在 `root` 下的节点不会随 `CurrentScene` 的切换而销毁。
- **唯一性**：通过 `ObjectPoolConfig.Name` 和 `Guid` 确保每个对象在场景中都有唯一标识，避免 Godot 默认命名导致的挂载冲突。

## API 参考

### 核心 API (`ObjectPool<T>`)

| 方法                                 | 描述                                                 |
| :----------------------------------- | :--------------------------------------------------- |
| `Get()`                              | 获取对象；若为 Node，会处理挂树保障，按参数决定是否立即激活。 |
| `Spawn()`                            | 获取对象，自动处理显隐、处理模式并挂载到预设父节点。 |
| `Activate(T obj)`                    | 完成延迟激活：防御性挂树、恢复显隐/处理/碰撞，并触发生命周期。 |
| `SpawnBatch(int count)`              | 批量执行 `Spawn`。                                   |
| `Release(T item)`                    | 归还对象到池中。                                     |
| `ReleaseBatch(IEnumerable<T> items)` | 批量归还对象。                                       |
| `ReleaseAll()`                       | 回收当前池中所有正在外部使用的对象。                 |
| `GetActiveSnapshot()`                | **新增**: 获取所有活跃对象的只读快照列表。           |
| `ForEachActive(Action<T>)`           | **新增**: 对所有活跃对象执行操作（内部使用快照）。   |
| `Warmup(int count)`                  | 手动预热，提前实例化指定数量的对象。                 |
| `Cleanup(int retainCount)`           | 清理闲置对象，仅保留指定数量。                       |
| `Clear()`                            | 销毁池内所有闲置对象。                               |
| `GetStats()`                         | 获取复用率、活跃数等统计信息。                       |

`PoolStats` 关键口径：

- `TotalCreated`：总创建次数（包含预热）。
- `TotalCreatedOnAcquire`：获取流程中因池空触发的扩容新建次数（不含预热）。
- `TotalReused`：获取流程中直接从闲置池复用的次数。
- `ReuseRate`：`TotalReused / TotalAcquired`，表示获取时来自池内复用的比例。

### ObjectPoolManager (全局管理器)

| 方法                            | 描述                                                               |
| :------------------------------ | :----------------------------------------------------------------- |
| `GetPool<T>(string name)`       | 根据名称查找已注册的对象池（泛型版本，类型安全）。                 |
| `ReturnToPool(object instance)` | **核心方法**：自动查找对象所属的池并归还（**仅支持 Node 对象**）。 |
| `GetAllStats()`                 | 获取所有已注册池的统计快照。                                       |
| `CleanupAll(int retainCount)`   | 批量清理所有池的闲置对象。                                         |
| `DestroyAll()`                  | 一键清理所有池（建议在场景切换时调用）。                           |

## 项目规范建议

1.  **名称管理**：所有全局池名称必须定义在 `ObjectPoolNames` 结构体中。
2.  **初始化**：核心系统的对象池（敌人、玩家、掉落物）应在 `ObjectPoolInit` 中完成预热。
3.  **回收**：
    - **Node 对象**：优先在对象自身的死亡逻辑中调用 `ObjectPoolManager.ReturnToPool(this)`。
    - **纯 C# 类**：必须持有池引用并调用 `pool.Release(obj)`。
4.  **性能**：对象池会自动处理节点的挂载逻辑。对于极高频率（如每秒百发）的子弹，建议在配置中指定合理的 `ParentPath`。
5.  **类型选择**：本项目主要池化 Node 对象（Enemy、Bullet、Item），极少需要池化纯 C# 类。
6.  **碰撞型根节点**：凡是根节点为 `CollisionObject2D` 的池化实体，默认走 `ParkedInTree`：不脱树、不关碰撞、不改 layer/mask/shape；业务碰撞入口必须查 `ObjectPoolRuntimeStateStore`，并遵守激活首帧不处理碰撞。
7.  **重构边界**：后续 ObjectPool 重构优先拆内部策略和补观测，不先修改 `Get/Release/Activate` public API。
