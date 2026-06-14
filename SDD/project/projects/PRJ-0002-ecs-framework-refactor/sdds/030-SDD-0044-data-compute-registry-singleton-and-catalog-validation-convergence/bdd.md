# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改 Data runtime 默认 computed resolver 入口、catalog build 校验 owner、fatal observation 和 Data 测试断言方式，必须用行为场景约束实现。
- **Source**: `design/main.md`；项目级设计 `../../design/Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md`
- **Executed features**: pending

## Scenarios

### Scenario: Default compute registry is a frozen singleton

Given 默认 Data runtime bootstrap
When 初始化 runtime snapshot catalog
Then bootstrap 使用 `DataComputeRegistry.Default`
And 默认 registry 已包含框架内置 computed resolver
And 默认 registry 已 frozen，不能被运行时追加注册污染。

### Scenario: Custom compute registry remains explicit

Given 测试或工具需要自定义 computed resolver
When 调用方 `new DataComputeRegistry()` 并注册 custom resolver
And 将 custom registry 显式传给 `DataRuntimeBootstrap`
Then catalog build 使用该 custom registry
And 不会修改 `DataComputeRegistry.Default`。

### Scenario: Registry does not validate DataDefinition

Given 一个 `DataComputeRegistry`
When 注册、查询或按泛型获取 resolver
Then registry 只依赖 `IDataComputeResolver`
And 不引用 `DataDefinition`
And 不维护 descriptor value type 到 CLR type 的映射。

### Scenario: Catalog build reports computed binding errors

Given snapshot descriptor 中存在 computed 字段
When catalog build 发现缺 `computeId`、缺 resolver、resolver 输出类型不匹配、依赖不存在或 computed cycle
Then build result 包含稳定 reason code
And code 至少能区分 `catalog.computed_missing_compute_id`、`catalog.compute_resolver_missing`、`catalog.compute_output_mismatch`、`catalog.dependency_missing`、`catalog.computed_cycle`
And 测试不依赖异常消息片段判断失败类型。

### Scenario: Loader does not duplicate catalog validation

Given `RuntimeDataSnapshotLoader.BuildCatalog`
When descriptor DTO 被转换为 `DataDefinition`
Then loader 只负责解析和转换
And computed 跨对象校验只由 catalog build 执行。

### Scenario: Fatal catalog build is observable before throw

Given 默认 bootstrap 初始化时 catalog build 失败
When bootstrap 决定无法继续运行
Then 写出 `owner=Data operation=CatalogBuild phase=Bootstrap outcome=Failed` 的 structured log 或 validation observation
And 随后 `throw` 阻断运行
And 不会只记录 log 后继续使用坏 catalog。

### Scenario: Record apply still uses DataApplyReport

Given snapshot record 包含 unknown key、type mismatch、conversion failure 或 computed/runtime_only 写入
When loader apply record
Then 返回 `DataApplyReport`
And `CreateData()` 这类必须成功的边界可以在 report 后 log + throw
And 普通 report 消费方不需要捕获异常才能知道错误 code。
