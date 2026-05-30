# BDD

## Applicability

- **Required**: true
- **Reason**: Data 无兼容 hard cutover 会改变 snapshot 生成、DataKey 生成、Data API、Entity/Data 调用点和文档事实源，必须用行为场景约束最终结果。

## Scenarios

### Scenario: Snapshot records cannot redefine descriptor types

Given DataOS descriptor declares `AbilityIcon` as `object_ref`  
When runtime snapshot records are generated and validated  
Then every `AbilityIcon` record field type is `object_ref`  
And final snapshot validation fails before runtime if record type differs from descriptor type

### Scenario: Generated handles preserve non-scalar types

Given descriptors include `string_array`, `object_ref` and `modifier_list` fields  
When `DataKey_Generated.cs` is regenerated  
Then `string_array` fields are not generated as `DataKey<string>`  
And `object_ref` / `modifier_list` fields use their explicit target contract instead of pretending to be `string`  
And no `DataKey.Xxx` compatibility alias class is generated

### Scenario: Typed DataKey errors fail at compile time

Given `TargetNode` is not a string field  
When business code attempts `Data.Get<Node2D>(GeneratedDataKey.TargetNode)` through a wrong handle  
Then the call cannot silently fall back to `Data.Get<T>(string)`  
And no `DataKey<T>` implicit string conversion exists

### Scenario: String array fields use one runtime type

Given `AvailableAnimations` is `string_array`  
When UnitAnimationComponent writes available animation names  
Then the Data slot stores the standard runtime type, `string[]`  
And callers read `Data.Get<string[]>(GeneratedDataKey.AvailableAnimations)` or convert outside Data

### Scenario: Documentation routes AI to the new single flow

Given an AI reads PRJ-0002 roadmap, DocsAI ProjectState, DocsNew Data docs or Data README  
When it chooses how to modify Data  
Then it is directed to DataOS authoring -> generator -> snapshot -> generated typed handle -> Base/Data  
And it is not directed to DataMeta, DataRegistry, DataKey alias, `new Data()` compatibility, RuntimeTables compatibility API or Resource/tres authoring as current facts
