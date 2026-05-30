# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Feature applies modifier to owner Data

Given FeatureDefinition.Modifiers 指向 Attribute.BaseAttack 且目标允许 numeric modifier  
When FeatureSystem granted 该 Feature  
Then owner Data 的 Attribute.BaseAttack effective value 增加

### Scenario: Feature removal rolls back source modifiers

Given Feature 已授予并添加 source=feature_id 的 modifiers  
When FeatureSystem removed 该 Feature  
Then 只移除同 source modifiers，其他来源保留

### Scenario: Feature does not compute derived values

Given Feature modifier 改变 Attribute.BaseAttack  
When 读取 Attribute.FinalAttack  
Then computed 由 DataComputeResolver 计算，FeatureSystem 不执行 computed 公式
