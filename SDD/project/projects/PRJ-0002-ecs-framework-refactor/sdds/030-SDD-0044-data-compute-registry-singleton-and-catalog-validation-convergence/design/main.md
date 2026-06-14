# Data Compute Registry Singleton And Catalog Validation Convergence

## Goal

把 Data computed resolver 默认入口和 catalog 校验职责收敛到清晰边界：

- `DataComputeRegistry.Default` 成为框架内置 resolver 的默认单例入口。
- 自定义 resolver 仍可通过 `new DataComputeRegistry()` 构造并显式传给 `DataRuntimeBootstrap`。
- `DataComputeRegistry` 只做 resolver 注册、冻结和查询，不再校验 `DataDefinition`。
- computed binding、resolver 缺失、输出类型、依赖缺失和循环引用统一由 catalog build 产生结构化 report。
- fatal bootstrap 错误先写 `owner=Data operation=CatalogBuild` 的 structured log / validation observation，再 `throw` 阻断运行。

## Context

项目级设计来源：

- `../../../design/Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md`

当前代码事实：

- `DataRuntimeBootstrap` 通过私有 `CreateDefaultComputeRegistry()` 每次构造默认 resolver registry。
- `RuntimeDataSnapshotLoader.ValidateComputeBinding()` 和 `DataDefinitionCatalog.ValidateAndBuildIndexes()` 对 computed binding 有重复校验。
- `DataComputeRegistry.ValidateResolver(DataDefinition)` 让 registry 理解 descriptor value type，职责过宽。
- `DataDefinitionCatalog` 目前以直接 `throw` 表达 catalog build 失败，缺少可供 Log / ValidationSession / 测试稳定断言的 report。

边界约束：

- 不恢复旧 `DataMeta` / `DataRegistry` / string-key 主链路。
- 不把 Data 降级为弱类型字典。typed `DataKey<T>`、descriptor-first snapshot 和 computed resolver 仍是当前 Data 契约。
- 不在业务热路径 `Get/Set` 默认打日志；diagnostic 走 report、artifact 和 structured log。

## Design

### 1. Default Registry

`DataComputeRegistry.Default` 持有框架内置 6 个 resolver，并在创建后 frozen。`DataRuntimeBootstrap()` 使用该默认单例，不再私有 new 一份默认 registry。

自定义 resolver 的入口保留为：

```csharp
var registry = new DataComputeRegistry();
registry.Register(new CustomResolver());
var bootstrap = new DataRuntimeBootstrap(registry);
```

默认单例不接受运行时追加注册，避免全局状态污染测试和不同 profile。

### 2. Registry Boundary

`DataComputeRegistry` 保留：

- `Register(IDataComputeResolver resolver)`
- `Contains(string computeId)`
- `GetRequired(string computeId)`
- `GetRequired<T>(string stableKey, string computeId)`
- `Freeze()` / `IsFrozen`

迁出：

- `ValidateResolver(DataDefinition definition)`
- `ResolveExpectedOutputType(DataDefinition definition)`

输出类型匹配属于 catalog build 的跨对象校验，不属于 resolver table。

### 3. Catalog Build Report

新增或等价落地 `DataCatalogBuildReport` / `DataCatalogBuildResult`。最小错误码包括：

- `catalog.empty_stable_key`
- `catalog.duplicate_stable_key`
- `catalog.computed_missing_compute_id`
- `catalog.compute_resolver_missing`
- `catalog.compute_output_mismatch`
- `catalog.dependency_missing`
- `catalog.computed_cycle`

实现可分两步：先让现有 `DataDefinitionCatalog.ValidateAndBuildIndexes()` 产出 report，再视复杂度引入 `DataDefinitionCatalogBuilder`。最终目标是 catalog 成功后 frozen，失败时调用方能拿到稳定 report。

### 4. Loader Boundary

`RuntimeDataSnapshotLoader` 负责 JSON / DTO 到 `DataDefinition` 的解析、默认值转换和 record apply。它不再做 computed binding 跨 descriptor / resolver 校验，避免与 catalog build 重复。

### 5. Log / Throw Boundary

致命边界仍应 `throw`，但必须先保留 observation：

- `DataRuntimeBootstrap.Initialize()` catalog build 失败：写 `owner=Data operation=CatalogBuild phase=Bootstrap outcome=Failed`，再 `throw`。
- `DataRuntimeBootstrap.CreateData()` record apply 失败：继续用 `DataApplyReport`，必要时写 structured log 后 `throw`。
- DataOS validator / Godot Data scene：通过 `ValidationSession` / artifact 断言 report code，不依赖异常消息片段。

## Not Doing

- 不开始 Event / Entity / Feature 大范围重构。
- 不把所有 catalog 错误改成只 Log 不阻断。
- 不把 custom resolver 注册进 `DataComputeRegistry.Default`。
- 不为旧 API 或旧 Data 事实源增加兼容层。
- 不在本 SDD 中解决所有 DataOS authoring schema 问题，除非实现 report 需要补最小测试 fixture。

## Verification

完成实现后至少执行：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn
python3 Workspace/SDD/sdd.py validate SDD-0044
```

如果修改 `.ai-config/skills/ecs/ecs-data/SKILL.md`，还必须执行：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```
