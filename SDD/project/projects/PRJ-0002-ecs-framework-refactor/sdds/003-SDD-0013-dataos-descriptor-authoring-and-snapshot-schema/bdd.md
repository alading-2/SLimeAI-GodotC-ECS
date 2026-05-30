# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Invalid default value reports authoring location

Given data_key_descriptor 中 value_type=float 且 default_value_text=abc  
When validator 运行  
Then 报告 default conversion 错误，包含 stable_key 和字段名

### Scenario: Disabled capability is trimmed before runtime

Given 某 capability 在 manifest 中 disabled  
When generator 生成 runtime_snapshot  
Then 该 capability 的 descriptors 和 records 不进入 active snapshot

### Scenario: Record field must match descriptor

Given record.fields 中出现 descriptor 不存在的 key  
When validator 或 loader 校验 snapshot  
Then 报告 unknown_key，不能自动创建 DataDefinition
