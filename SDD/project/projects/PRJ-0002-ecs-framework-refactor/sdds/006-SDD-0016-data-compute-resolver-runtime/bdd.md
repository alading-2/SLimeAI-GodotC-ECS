# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Computed uses resolver and dependencies

Given Attribute.FinalHp descriptor 依赖 Attribute.BaseHp 与 Attribute.HpBonus  
When 调用 Data.Get(Attribute.FinalHp)  
Then Data 调用 AttributeBonusComputeResolver 并返回派生值

### Scenario: Computed cache invalidates when dependency changes

Given Attribute.FinalHp 已缓存  
When Data.Set(Attribute.BaseHp, 新值)  
Then Attribute.FinalHp 标脏，下次 Get 重新计算

### Scenario: Computed cannot be set directly

Given Attribute.FinalHp write_policy=computed_readonly  
When 业务调用 Data.Set(Attribute.FinalHp, 999)  
Then Set 失败并报告 computed readonly
