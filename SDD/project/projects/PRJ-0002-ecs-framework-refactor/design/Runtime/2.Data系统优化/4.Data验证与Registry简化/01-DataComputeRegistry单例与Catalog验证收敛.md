# DataComputeRegistry 单例与 Catalog 验证收敛

> 日期：2026-06-14  
> 范围：`Src/ECS/Runtime/Data/DataComputeRegistry.cs`、`Src/ECS/Runtime/Data/DataDefinitionCatalog.cs`、`Src/ECS/Runtime/Data/RuntimeSnapshot/RuntimeDataSnapshotLoader.cs`、`Src/ECS/Runtime/Data/RuntimeSnapshot/DataRuntimeBootstrap.cs`、`Src/ECS/Tools/Logger/`。  
> 状态：方向设计，尚未实施。  

## 用户原始问题

> - 我说的静态其实是DataComputeRegistry单例即可，而不是每个地方都新建一个DataComputeRegistry  
>   你说的manifest什么意思，好像是默认值，还不如我这里单例，你想自定义也可以新建一个DataComputeRegistry
> - 验证冗余在DataComputeRegistry、DataDefinitionCatalog都存在
> - throw可以，但是是在程序无法执行的情况下才throw,log更通用，通过分析log一样可以找到bug
> - 深度思考详细分析

## 结论

用户判断总体成立。当前 Data runtime 的问题不是“完全不该验证”，而是三个边界混在一起：

1. `DataComputeRegistry` 默认 resolver 是框架写死的能力，却通过 `DataRuntimeBootstrap.CreateDefaultComputeRegistry()` 每次构造注册表，默认事实源不够直观。
2. `DataComputeRegistry`、`RuntimeDataSnapshotLoader`、`DataDefinitionCatalog` 都承担了一部分 computed 校验，导致验证 owner 分散。
3. Catalog / bootstrap 阶段大量直接 `throw`，能 fail-fast，但没有先生成统一结构化报告和 `owner=Data` 日志，不符合当前 Log / ValidationSession 的 AI-first observation 方向。

推荐方案不是把 Data 降成无约束字典，也不是把所有问题都靠 `throw` 解决，而是：

```text
DataComputeRegistry.Default 单例
  -> 框架内置 resolver 唯一默认入口

DataDefinitionCatalogBuilder / BuildReport
  -> 唯一 catalog 构建与跨字段校验入口

RuntimeDataSnapshotLoader
  -> 只负责 snapshot DTO 解析和 DataDefinition 转换

DataRuntimeBootstrap
  -> 负责把 fatal report 写 Log，然后 throw 阻断运行
```

## 真实问题

### 1. 默认 computed resolver 不应该每处重新注册

当前默认链路是：

```text
new DataRuntimeBootstrap()
  -> CreateDefaultComputeRegistry()
     -> new DataComputeRegistry()
     -> Register(AttributeBonus / Percent / AttackInterval / Regen / EffectiveHp / Dps)
```

这有两个问题：

- 对框架默认计算函数来说，它们不是运行时动态数据，不需要每个默认 bootstrap 都新建一份注册表。
- 对 AI 来说，默认 resolver 的事实源藏在 bootstrap 私有方法里，不如 `DataComputeRegistry.Default` 直观。

用户提出“单例即可，自定义可以新建一个 `DataComputeRegistry`”更符合当前使用证据。生产侧目前只有默认 bootstrap 注册 6 个 resolver；测试侧才需要构造临时 registry 注入 fake resolver。

### 2. `DataComputeRegistry` 不应该验证 `DataDefinition`

`DataComputeRegistry` 的职责应很窄：

- 注册 resolver。
- 按 `computeId` 查询 resolver。
- 防止空 `ComputeId` 和重复 `ComputeId`。
- 在读取 computed 值时按泛型类型获取 typed resolver。

当前 `ValidateResolver(DataDefinition definition)` 把 descriptor / catalog 的知识放进 registry：它需要知道 `DataDefinition.ValueType` 到 CLR 类型的映射。这个职责更适合 catalog build，因为只有 catalog build 阶段同时知道所有 definitions、dependencies、resolver registry 和 computed graph。

因此应把下面这类逻辑从 `DataComputeRegistry` 移出：

```text
DataDefinition.ValueType -> expected CLR Type
resolver.OutputClrType 是否匹配 descriptor
```

移出后，`DataComputeRegistry` 更像一个轻量 resolver table，不再兼任 DataDefinition validator。

### 3. Catalog 校验要保留，但要收敛到一个入口

用户质疑“验证过多”是成立的，因为同一类 computed binding 检查现在分散在多个地方：

- `RuntimeDataSnapshotLoader.ValidateComputeBinding()` 会检查 computed 是否缺 `computeId`、resolver 是否存在。
- `DataDefinitionCatalog.ValidateAndBuildIndexes()` 又检查 computed 是否缺 `computeId`、resolver 是否存在、resolver 输出类型、依赖存在、依赖环。
- `DataComputeRegistry.ValidateResolver()` 再承担输出类型验证。

这些验证不应全删。descriptor-first 的 Data 依赖 snapshot descriptor 与 C# resolver 绑定，二者之间没有编译期连接；如果不在 catalog build 阶段检查，错误会延迟到玩法运行时。

但验证必须收敛：

```text
RuntimeDataSnapshotLoader
  - 只解析 value_type / policy / defaultValue
  - 不做 computed binding 跨对象校验

DataDefinitionCatalogBuilder
  - stable key 空 / 重复
  - computed 缺 computeId
  - resolver 缺失
  - resolver 输出类型不匹配
  - dependency 不存在
  - computed dependency cycle
  - build 成功后返回 frozen catalog
```

循环引用检查可以保留。它不是热路径验证，只在 catalog build 发生；成本很低，一旦漏掉，最坏会在 computed 读取时递归到难定位的问题。它应该作为 `catalog.computed_cycle` 结构化错误存在，而不是散落的直接 `throw`。

### 4. Log 和 throw 不是二选一

用户提出“throw 可以，但程序无法执行时才 throw；Log 更通用，通过分析 Log 可以找到 bug”，这个判断适合当前框架。

更精确的边界是：

| 场景 | 推荐行为 |
| --- | --- |
| Catalog 构建失败 | 生成 `DataCatalogBuildReport`，写 `owner=Data operation=CatalogBuild` 错误日志，然后 throw 阻断 bootstrap。 |
| Snapshot record 应用失败 | 返回 `DataApplyReport`；`CreateData()` 这类必须创建成功的边界再写 Log 并 throw。 |
| 普通 runtime 写入失败 | 优先 `TrySet/TrySetUntyped` 返回 `DataWriteReport`，调用方按场景决定是否 Log/阻断。 |
| 业务热路径 `Get/Set` 误用 | 可继续 throw，因为调用契约已破坏；但外层测试或 bootstrap 应把异常转成 Validation artifact / structured log。 |
| DataOS validator / scene test | 用 `ValidationSession` 写 artifact 和 Validation channel，不靠裸 stdout。 |

也就是说，`throw` 负责控制流，Log / Report 负责 observation。致命错误仍然要阻断，不应只 Log 后继续跑坏 catalog。

## 设计方案

### 1. `DataComputeRegistry.Default` 单例

新增默认单例，表达框架内置 resolver 的唯一默认入口：

```csharp
public sealed class DataComputeRegistry
{
    public static DataComputeRegistry Default { get; } = CreateDefault();

    private static DataComputeRegistry CreateDefault()
    {
        var registry = new DataComputeRegistry();
        registry.Register(new AttributeBonusComputeResolver());
        registry.Register(new PercentComputeResolver());
        registry.Register(new AttackIntervalComputeResolver());
        registry.Register(new RegenComputeResolver());
        registry.Register(new EffectiveHpComputeResolver());
        registry.Register(new DpsComputeResolver());
        registry.Freeze();
        return registry;
    }
}
```

说明：

- `Default` 是框架默认单例，不再由 `DataRuntimeBootstrap` 私有方法重复 new。
- 自定义计算语义仍然允许 `new DataComputeRegistry()`，然后传给 `new DataRuntimeBootstrap(customRegistry)`。
- 默认单例应 frozen，避免运行时任意地方往全局默认表注册 resolver，造成测试污染或跨 profile 隐式状态。
- 自定义 registry 可在构造期注册，传入 bootstrap 后也建议 frozen。

这里不使用“manifest”作为长期术语。之前提到的 manifest 本质是默认 resolver 列表；在当前代码和用户认知中，`DataComputeRegistry.Default` 更直接。

### 2. `DataComputeRegistry` 收窄职责

保留：

- `Register(IDataComputeResolver resolver)`
- `Contains(string computeId)`
- `GetRequired(string computeId)`
- `GetRequired<T>(string stableKey, string computeId)`
- `Freeze()` / `IsFrozen`

移除或迁移：

- `ValidateResolver(DataDefinition definition)`
- `ResolveExpectedOutputType(DataDefinition definition)`

这些逻辑迁到 catalog build validator。Registry 不再依赖 DataDefinition。

### 3. `DataDefinitionCatalogBuilder` 或 build report

当前 `DataDefinitionCatalog` 同时负责可变注册、校验、索引构建、冻结。更清晰的形态是：

```text
DataDefinitionCatalogBuilder
  Register(DataDefinition)
  Build(DataComputeRegistry registry) -> DataCatalogBuildResult

DataCatalogBuildResult
  bool Success
  DataDefinitionCatalog? Catalog
  DataCatalogBuildReport Report
```

`DataDefinitionCatalog` 本身成为构建完成后的查询对象：

- 不再公开 `Register()`。
- 不再公开 `ValidateAndBuildIndexes()`。
- 只提供 `TryGet()`、`GetRequired()`、`Definitions`、`GetDependentComputedKeys()`、`ComputeRegistry`。

如果短期不想引入 builder，也可以先做小步改造：

- 保留 `Register()`，但标记为 builder-only 使用。
- `ValidateAndBuildIndexes()` 改成返回 `DataCatalogBuildReport` 或 `DataCatalogBuildResult`。
- 所有错误先进入 report，最后由调用方决定是否 throw。

### 4. `DataCatalogBuildReport`

新增结构化报告，错误至少包含：

```text
Code
StableKey
ComputeId
Dependency
ExpectedType
ActualType
Message
Severity
```

建议错误码：

| Code | 含义 |
| --- | --- |
| `catalog.empty_stable_key` | stable key 为空。 |
| `catalog.duplicate_stable_key` | stable key 重复。 |
| `catalog.computed_missing_compute_id` | computed 字段缺 computeId。 |
| `catalog.compute_resolver_missing` | computeId 找不到 resolver。 |
| `catalog.compute_output_mismatch` | resolver 输出类型与 descriptor valueType 不一致。 |
| `catalog.dependency_missing` | dependency 没有对应 descriptor。 |
| `catalog.computed_cycle` | computed 依赖成环。 |

这样 Log / ValidationSession / 测试都能断言稳定 reasonCode，而不是只断言异常消息片段。

### 5. Bootstrap 写 Log 后 throw

`DataRuntimeBootstrap.Initialize()` 是运行时默认 catalog 入口。这里构建失败属于程序无法正确执行，应 throw；但 throw 之前必须写结构化 Log：

```text
owner=Data
operation=CatalogBuild
phase=Bootstrap
severity=Error
outcome=Failed
fields:
  errorCount
  firstCode
  firstStableKey
  computeId
  dependency
  snapshotPath
```

如果未来 DataOS validator 或编辑器工具只想检查 catalog，可以直接消费 `DataCatalogBuildReport`，不必依赖 throw。

## 不做什么

- 不把 Data 退回弱类型字典。字典读写只是底层存储形态，不是 Data 系统的完整契约。
- 不让 registry 继续理解 `DataDefinition`。Registry 只管 resolver 表。
- 不在热路径 `Get/Set` 每次打 Log。热路径仍走 typed API；诊断入口使用 report / artifact。
- 不把所有错误都吞掉只 Log。坏 catalog、坏 bootstrap 和无法创建的 Data record 必须阻断。
- 不把自定义 resolver 注册到全局默认单例。自定义应新建 registry 并显式传入 bootstrap。

## 实施切片建议

1. 新增 `DataComputeRegistry.Default`，把 `DataRuntimeBootstrap.CreateDefaultComputeRegistry()` 替换为默认单例。
2. 给 registry 增加 frozen 状态，默认单例构建后冻结；自定义 registry 可显式冻结。
3. 将 `ValidateResolver()` 和 value type -> CLR type 映射迁出 registry。
4. 删除 `RuntimeDataSnapshotLoader.ValidateComputeBinding()`，避免 loader 与 catalog 重复验证。
5. 新增 `DataCatalogBuildReport`，让 catalog build 收集错误。
6. `DataRuntimeBootstrap.Initialize()` 在 report 失败时写 `owner=Data operation=CatalogBuild` 日志，再 throw。
7. 更新 DataOS 场景测试：断言默认 registry 单例、custom registry 注入、重复 resolver、missing resolver、output mismatch、dependency missing、cycle 和 Log/Validation artifact。
8. 更新 `DocsAI/ECS/Runtime/Data/` 与 `ecs-data` skill，明确 registry 单例、catalog build report、Log/throw 边界。

## 待确认

1. 默认单例是否必须 frozen。建议 frozen；否则全局默认 registry 会变成隐式可变状态。
2. 自定义 resolver 是否是当前版本正式能力。建议保留构造注入能力，但不把自定义注册混入 `Default`。
3. `DataDefinitionCatalogBuilder` 是否本轮引入。如果只做小改，可先用 `DataCatalogBuildReport` 收敛错误，再后续拆 builder。
