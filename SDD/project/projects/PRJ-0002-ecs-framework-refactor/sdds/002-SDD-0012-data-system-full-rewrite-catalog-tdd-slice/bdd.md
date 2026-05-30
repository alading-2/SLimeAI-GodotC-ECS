# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Catalog rejects duplicate descriptor keys

Given runtime_snapshot descriptors 中存在两个相同 stable_key  
When BuildCatalog 被调用  
Then loader 返回结构化错误或抛出明确异常，不能静默覆盖旧定义

### Scenario: Computed field requires resolver binding

Given descriptor 声明 storage_policy=computed 且 compute_id=AttributeBonus  
When DataComputeRegistry 未注册 AttributeBonus  
Then BuildCatalog fail fast 并报告 missing resolver

### Scenario: Legacy audit is not runtime fallback

Given 旧 DataMeta 中存在 BaseHp 但 snapshot descriptor 缺失  
When LegacyDataAuditTool 执行审计  
Then 报告 missing_in_snapshot；Data.Get/Set 不允许从 DataMeta fallback
