# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改资源加载失败语义、路径迁移闭环和 Common Utilities owner 边界。

## Scenarios

### Scenario: Resource loading fails fast without exact key

Given an exact resource key is missing
When the resource is loaded by key and category
Then the structured result reports KeyNotFound
And no contains fallback is used

### Scenario: LoadPath requires source policy

Given a DataOS snapshot stores a resource path
When runtime loads that path
Then the load request includes source, owner and usage
And diagnostics can identify the record or field that requested it

### Scenario: Resource path migration verifies old path residue

Given a resource directory moved from old path to new path
When project-filesystem runs with apply mode
Then current runtime code, DataOS refs, generated catalog and current DocsAI use the new path
And every old path residue from rg is classified or removed

### Scenario: Common Utilities rejects owned functionality

Given a helper loads a PackedScene or queries all entities
When the helper is reviewed for Common Utilities
Then the helper is rejected and routed to ResourceLoading or Entity/TargetSelector owner
