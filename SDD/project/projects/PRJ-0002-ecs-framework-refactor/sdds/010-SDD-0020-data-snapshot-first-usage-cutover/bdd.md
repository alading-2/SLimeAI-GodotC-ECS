# BDD

## Applicability

- **Required**: true
- **Reason**: Data 取用 hard cutover 会改变运行时配置读取、Entity 初始化、系统配置、测试面板和资源目录行为，必须保留行为验收场景。

## Scenarios

### Scenario: Runtime configuration is snapshot-first

Given DataOS authoring 已生成 runtime snapshot records  
When SpawnSystem、SystemConfigService、AbilityTestService 或 ResourceCatalog 需要运行时配置  
Then 它们通过 snapshot query/projection 获取 typed view  
And 不读取 RuntimeTables 静态实例或 `DataTable.GetAll<T>()`

### Scenario: Entity spawn requires explicit runtime record

Given 调用方要生成 Player、Enemy、TargetingIndicator 或 Ability entity  
When 调用 `EntityManager.Spawn`  
Then 调用方必须传 `RuntimeDataRecord` 或 `RuntimeDataRecordTable + RuntimeDataRecordId`  
And `EntityManager` 不再通过 config object 类型名和 `Name` 推断 record

### Scenario: Catalog-bound Data rejects legacy fallback

Given 运行时 Data 容器需要读写字段  
When 调用 `Data.Get/Set`  
Then 字段必须存在于 `DataDefinitionCatalog`  
And unknown key fail fast  
And 不回退到 `DataRegistry`、`DataMeta` 或未绑定 catalog 的字典存储

### Scenario: Legacy runtime tables are removed

Given 旧 RuntimeTables 曾包含 `AbilityData.Slam`、`EnemyData.Yuren` 等手写静态数据  
When 完成 SDD-0020  
Then `Data/DataOS/RuntimeTables` 不再保存手写业务数据事实源  
And grep gate 对 `DataTable.GetAll`、`AbilityData.All`、`EnemyData.All`、`SystemData.All` 无运行时命中

### Scenario: Documentation no longer routes AI to old data paths

Given AI 会读取 AGENTS、CLAUDE、DocsAI 和 module README 决定 Data 修改路线  
When 完成 SDD-0020 文档同步  
Then 非历史文档只推荐 DataOS authoring、runtime snapshot、generated typed handle 和 catalog-bound Data  
And 不推荐 `DataMeta`、`DataRegistry.Register`、`DataNew` 或手写 RuntimeTables
