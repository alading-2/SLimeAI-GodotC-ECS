# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Generated handle is not a field definition

Given descriptor 定义 Attribute.BaseHp 默认值、范围和 modifier_policy  
When codegen 生成 DataKey<float> AttributeBaseHp  
Then 生成 handle 只包含 stable key 和泛型类型，不包含默认值或范围

### Scenario: Ability probability keeps 0-100 semantics

Given Ability.TriggerChance descriptor 默认 25 且 unit=percent  
When 业务 resolver 或系统读取该字段  
Then 计算时显式 /100，不由 IsPercentage 隐式决定

### Scenario: Old DataKey audit closes missing descriptor gaps

Given 旧 DataKey_Attribute 中有 BaseAttack  
When 迁移 Attribute descriptors 后运行审计  
Then BaseAttack 不再出现在 missing_in_snapshot
