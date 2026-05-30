# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Record applies typed fields

Given snapshot record 包含 Attribute.BaseHp type=float value=20  
When ApplyRecord 应用到 Entity.Data  
Then Data.Get(Attribute.BaseHp) 返回 20

### Scenario: Unknown field is reported without silent creation

Given record.fields 包含 Attribute.Unknown  
When ApplyRecord 执行  
Then DataApplyReport 包含 snapshot.unknown_key，Data 不新增该字段

### Scenario: Bootstrap builds catalog before records

Given runtime_snapshot 包含 descriptors 和 records  
When DataRuntimeBootstrap.Initialize 执行  
Then 先构建 catalog，再应用 records，computed resolver 缺失会在初始化阶段失败
