# BDD

## Applicability

- **Required**: true
- **Reason**: Data 子系统完整重构会改变运行时行为、输入事实源或验证门禁，必须保留行为场景。

## Scenarios

### Scenario: Old DataNew path is gone

Given Data 完整重构前存在 SlimeAI/Data/DataNew DTO  
When 执行旧路径删除  
Then DataNew 不再参与编译或新 Data 输入，grep 只允许历史文档命中

### Scenario: New Data scenes replace old tests

Given 旧 SingleTest/ECS/Data 验证 DataMeta/DataRegistry/DataNew 行为  
When 运行新 Data 场景 smoke  
Then 场景验证 catalog、runtime policy、snapshot apply 和 Feature bridge，而不是旧兼容行为

### Scenario: Final grep gate blocks legacy fallback

Given 源码中可能残留 LoadFromConfig 或 DataRegistry.Register(new DataMeta)  
When 执行最终 grep gate  
Then 除历史文档/删除说明外无新系统命中
