# ObjectPool 反射管理接口设计

## 当前结论

ObjectPool 确实存在反射调用和 object 字典问题，但优先级低于 Data/Event。它不应作为本轮 P0 的阻塞项；应合入既有 `SDD-0028 ObjectPool Collision ParkedInTree Cutover` 或后续 ObjectPool cleanup SDD。

## Data 完成后的重新裁决

`SDD-0031` 完成后，ObjectPool 仍然不是下一批 P0。重新分析源码后，ObjectPool 的问题是真实的，但性质和 Data/Event/Feature 不同：

- `ObjectPoolManager._pools Dictionary<string, object>` 和 `GetMethod(...).Invoke(...)` 是管理器缺少统一 runtime contract，不是值类型热路径装箱问题。
- ObjectPool 管的是 class / Node 实例，`ReleaseUntyped(object)` 可以作为跨泛型池的低频引用类型边界；它不应被当成 Data/Event 的 object 宽口类比。
- 应改的是 manager 反射和字符串方法名，不是对象池生命周期策略。ParkedInTree、runtime state、collision guard 等已有设计不能被这个小切片顺手重写。
- 该切片可独立做，不应混入 Event + Feature / Ability typed boundary。

## 当初为什么这么设计

`ObjectPoolManager` 需要管理不同 `T` 的 `ObjectPool<T>`。C# 泛型类型擦不成统一 `ObjectPool<T>` 集合时，最省事的写法就是：

```csharp
Dictionary<string, object> _pools
poolObj.GetType().GetMethod("Release").Invoke(...)
```

这类写法对人工调试方便，但不符合 AI-first：AI 看不到 pool manager 对池对象的最小 contract，只能在反射字符串 `"Release"`、`"GetStats"`、`"Cleanup"` 上猜。

## 源码证据

| 文件 | 证据 |
| --- | --- |
| `ObjectPoolManager.cs` | `_pools Dictionary<string, object>` |
| `ObjectPoolManager.cs` | `ReturnToPool` 用 `GetMethod("Release")` 和 `Invoke(poolObj, new[] { instance })` |
| `ObjectPoolManager.cs` | `GetAllStats`、`CleanupAll`、`DestroyAll` 用反射调用 |
| `ObjectPool.cs` | 注释“接口实现已移除（IObjectPool 接口已废弃）” |
| `DocsAI/ECS/Tools/ObjectPool/README.md` | 当前 ObjectPool 重点是 ParkedInTree、runtime state、collision guard 和 validation |

## 目标设计

恢复一个极小非泛型接口，不恢复旧“大而全对象池抽象”。

```csharp
internal interface IObjectPoolRuntime
{
    string PoolName { get; }
    Type ItemType { get; }
    PoolStats GetStats();
    bool ReleaseUntyped(object instance);
    void Cleanup(int retainCount);
    void Clear();
}
```

`ObjectPool<T>` 实现：

```csharp
public bool ReleaseUntyped(object instance)
{
    if (instance is not T typed)
    {
        return false;
    }

    Release(typed);
    return true;
}
```

`ObjectPoolManager` 改为：

```csharp
private static readonly Dictionary<string, IObjectPoolRuntime> _pools = [];
```

## AI-first 边界

- `ReleaseUntyped(object)` 是 ObjectPool manager 边界 API，不是业务对象协议。
- 它不会导致值类型装箱，因为 ObjectPool 管的是 class / Node 实例。
- 注释仍应说明：这是跨泛型池管理的低频边界，不得复制到 Data/Event hot path。

## 迁移步骤

1. 新增 `IObjectPoolRuntime`。
2. `ObjectPool<T>` 实现接口。
3. `ObjectPoolManager._pools` 改为 `Dictionary<string, IObjectPoolRuntime>`。
4. 删除 `GetMethod/Invoke` 反射路径。
5. 更新 ObjectPool contract tests，覆盖 wrong type release、stats、cleanup、destroy。
6. 同步 `DocsAI/ECS/Tools/ObjectPool/README.md` 和 `tools` / ObjectPool owner skill。

执行边界：

- 不修改 pool parking、collision isolation、Node lifecycle 策略。
- 不把 `ReleaseUntyped(object)` 推广到 Data/Event/Feature；这里只是 ObjectPool manager 的跨泛型引用类型管理接口。
- 不承诺 GC 数字，验收以 grep gate、contract test 和现有 ObjectPool Godot validation 为准。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "GetMethod\\(\"Release\"|GetMethod\\(\"GetStats\"|GetMethod\\(\"Cleanup\"|GetMethod\\(\"Clear\"|Dictionary<string, object> _pools" Src/ECS/Tools/ObjectPool
```

如果合入 SDD-0028，还需继续跑 ObjectPool contract 和 Godot collision validation；反射接口本身不替代碰撞验证。

## 不推荐

- 不推荐为了去反射重写 ObjectPool 生命周期策略；生命周期策略已由 ObjectPool 设计包决定。
- 不推荐把 ObjectPool manager 变成 EntityManager 或 Data 初始化入口。
