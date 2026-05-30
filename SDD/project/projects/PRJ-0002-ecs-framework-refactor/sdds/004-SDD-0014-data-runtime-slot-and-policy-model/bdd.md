# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Unknown key is rejected

Given catalog 中没有 Attribute.BaseHP  
When 业务调用 Data.Set("Attribute.BaseHP", 10)  
Then Data 拒绝写入并报告 unknown key，不能创建新字段

### Scenario: Range policy does not always clamp

Given descriptor 声明 range_policy=reject_runtime 且 max=100  
When 运行时 Set 传入 120  
Then Set 返回失败，不静默改成 100

### Scenario: Descriptor default is returned

Given descriptor 声明 Attribute.BaseHp 默认值 10 且 Data 未写入该 key  
When 调用 Data.Get(DataKey.AttributeBaseHp)  
Then 返回 10，来源为 descriptor default
