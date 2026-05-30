# BDD

## Applicability

- **Required**: true
- **Reason**: 修改编译期类型系统和运行时数据加载路径，需要行为场景验证正确性。

## Scenarios

### Scenario: `DataKey<T>` 提供编译期类型安全

Given `DataKey<float> BaseHp` 已在 `DataRegistry` 中注册  
When gameplay 代码调用 `data.Set(DataKey.BaseHp, "hello")`  
Then 编译器报类型错误，不能通过编译

Given `DataKey<float> BaseHp` 已在 `DataRegistry` 中注册  
When gameplay 代码调用 `data.Set(DataKey.BaseHp, 100f)` 和 `data.Get(DataKey.BaseHp)`  
Then 编译通过，`Get` 直接返回 `float`，不需要显式指定 `<float>`

### Scenario: SnapshotLoader 从 JSON 写入 Data 容器

Given `runtime_snapshot.json` 包含某个 record（如 `table=Unit, id=warrior`）  
And `DataRegistry` 已注册该 record 包含的所有 key  
When 调用 `SnapshotLoader.Apply(snapshotPath, data, table, id)`  
Then `data.Get(DataKey.BaseHp)` 返回 snapshot 中对应的值

Given `runtime_snapshot.json` 的某个 field key 未在 `DataRegistry` 注册  
When `SnapshotLoader` 遍历到该 field  
Then 记录警告并跳过，不抛出异常，其他 field 正常写入

### Scenario: 构建通过

Given T1.1~T3.1 全部完成  
When 运行 `dotnet build SlimeAI/SlimeAI.csproj`  
Then 编译 error 为 0（当前约 196 个错误全部修复）
