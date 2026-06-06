# TargetSelector 集合分配与 LINQ 设计

## 当前结论

集合分配和 LINQ 是真实问题，但不是所有命中都应立即修。需要按热路径分类：

- TargetSelector 查询：P1，高频战斗路径，应该随 `TargetQueryEngine Hard Cutover` 一起处理。
- AbilityInventoryService：P1/P2，取决于 UI/输入调用频率。
- ComponentRegistrar：P2，注册/注销阶段分配多于帧热路径，但 AI-first contract 仍可优化。
- TestSystem/UI/diagnostics：低优先级，不为了零分配牺牲可读性。

## 当初为什么这么设计

早期实现用 `List<T>`、LINQ、`ToArray()` 是为了可读性和快速完成迁移。PRJ-0002 前几个阶段重点是 Data、Entity、目录架构、Component 组合和 Tool owner 边界，不是性能热路径。

现在 TargetSelector 已经被 `design/Tool/其他Tool/05-TargetSelector查询契约.md` 裁决为要升级成 `TargetQueryEngine + TargetQueryResult`，所以集合分配优化应作为这个切片的一部分，而不是在旧 `EntityTargetSelector.Query` 上做零散小修。

## 源码证据

| 文件 | 证据 | 分类 |
| --- | --- | --- |
| `EntityTargetSelector.cs` | `new List<IEntity>()` 后 Single 分支再次 `new List<IEntity>()` | 可直接修 |
| `EntityTargetSelector.cs` | `GetRange(0, MaxTargets)` 分配新 List | 热路径应修 |
| `EntityTargetSelector.cs` | `new Random()` | 热路径应修，且影响 determinism |
| `AbilityInventoryService.cs` | `GetManualAbilities` 用 `Where().ToList()` | 可能热路径，建议手写循环 |
| `AbilityInventoryService.cs` | `GetAbilityByName/GetAbilityById` 用 `FirstOrDefault(lambda)` | 可读性尚可，按频率决定 |
| `ComponentRegistrar.cs` | `Distinct().ToArray()`、`GetComponents().ToArray()`、`OfType<T>().FirstOrDefault()` | 注册/查询 contract 可优化 |
| `DataRuntimeStorage.cs` | `GetModifiers()` 返回新 `List<DataModifier>` | Data P0 一并处理 |

## 目标设计：TargetQueryResult

不要让 `Query` 直接返回可变 `List<IEntity>` 并让调用方随意持有。

目标：

```csharp
public readonly struct TargetQueryResult
{
    public ReadOnlySpan<IEntity> Targets { get; }
    public TargetQueryDiagnostics Diagnostics { get; }
}
```

如果 Godot/C# 版本或生命周期不适合 `ReadOnlySpan` 暴露，可以用 pooled buffer + disposable result：

```csharp
public readonly struct TargetQueryLease : IDisposable
{
    public IReadOnlyList<IEntity> Targets { get; }
    public void Dispose();
}
```

关键不是某个类型，而是：候选列表、过滤列表、排序和截断由 `TargetQueryEngine` 管，调用方不新建中间集合。

## TargetSelector 具体规则

- 候选 buffer 复用，避免每次 `Query` 多个 `new List`。
- `MaxTargets` 截断用原地 `RemoveRange` 或 result count，不用 `GetRange`。
- 随机排序注入 deterministic RNG，不用 `new Random()`。
- `SortTargets` 中重复 `Data.Get<float>` 可通过局部缓存或排序 key 预计算控制。
- diagnostics 记录候选数、过滤原因、排序方式、截断数，但 diagnostics 默认低频或 debug 开启。

## AbilityInventoryService 规则

当前 `GetAbilities(owner)` 本身返回新 List；`GetManualAbilities` 再 `Where().ToList()` 会叠加分配。

推荐：

- `GetAbilities(owner, List<AbilityEntity> buffer)` 或 `EnumerateAbilities(owner)`。
- `GetManualAbilities` 手写循环，必要时返回 pooled/readonly result。
- UI 需要长期持有列表时，由 UI 明确复制；系统热路径不默认复制。

## ComponentRegistrar 规则

ComponentRegistrar 已有 `_componentsByType`，但 `GetComponent<T>` 没用它。建议：

- `GetComponent<T>` 直接查 `_componentsByType[typeof(T)]` 再确认 owner。
- `GetComponents(entity)` 返回只读 view 或内部集合，不默认 `ToArray()`。
- `UnregisterComponents` 如需 snapshot 防止修改集合，可复用临时 buffer，而不是每次公开查询都复制。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "new Random\\(|GetRange\\(|\\.Where\\(.*\\)\\s*\\.ToList\\(|\\.Distinct\\(\\)\\.ToArray\\(|GetComponents\\(.*\\)\\.OfType" Src/ECS/Tools/TargetSelector Src/ECS/Capabilities/Ability Src/ECS/Runtime/Entity/Components
```

执行 `TargetQueryEngine` 时应补：

- determinism test：同 seed 下 Random 排序稳定。
- allocation smoke：多次 query 不随调用次数线性增加 managed allocations。
- behavior test：Circle/Ring/Box/Line/Cone/Global、team/type/lifecycle filter、sorting、MaxTargets 不回归。

## 不推荐

- 不推荐把所有 LINQ 全仓禁止。TestSystem、初始化和 diagnostics 保留 LINQ 可读性没问题。
- 不推荐在旧 `EntityTargetSelector.Query` 上只删两个 `new List` 就宣称完成；真正目标是 `TargetQueryEngine` typed result 和 diagnostics。
