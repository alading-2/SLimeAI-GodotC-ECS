# Notes

## Shared Design Inputs

- `../../design/Runtime/2.Data系统优化/README.md`
- `../../design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md`
- `../../design/Runtime/2.Data系统优化/04-Data系统现状复查与兼任问题.md`
- `../../data-rewrite-execution-prompt.md`

## Current Known Legacy Use Buckets

- RuntimeTables data source: `SlimeAI/Data/DataOS/RuntimeTables/`
- Runtime table scanner: `SlimeAI/Data/DataOS/RuntimeTables/DataTable.cs`
- Entity config inference: `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs`
- System config readers: `SystemConfigService`, `SystemPresetService`
- Spawn reader: `SpawnSystem`
- Ability/TestSystem readers: `AbilityTestService`, `FeatureDebugService`, `EntityManager_Ability`
- Resource directory reader: `ResourceCatalog`
- Data fallback: `Data.cs`, `DataRegistry.cs`, `DataMeta.cs`
- Old editor route: `SlimeAI/addons/DataConfigEditor`
- Rule/docs route: `SlimeAI/AGENTS.md`, `SlimeAI/CLAUDE.md`, `SlimeAI/DocsAI`, `SlimeAI/DocsNew`

## Open Questions

- DataConfigEditor should likely be deleted from this runtime cutover and rebuilt later as a DataOS DB editor if still needed.
- Ability API can start with record id / record handle; a typed `AbilityRecordId` can be deferred unless it naturally falls out of the implementation.
