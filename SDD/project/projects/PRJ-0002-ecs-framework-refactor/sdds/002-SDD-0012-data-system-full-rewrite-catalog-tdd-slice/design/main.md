# Data System Full Rewrite - Catalog TDD Slice

## Goal

- **1**. 冻结 Data 完整重构执行基线，明确 descriptors 是字段定义唯一事实源。
- **2**. 用 RED/GREEN/REFACTOR 建立纯 C# catalog 小测试和最小实现。
- **3**. 新增一次性 LegacyDataAuditReport，只做迁移缺口审计，不进入 Data 运行时。

## Context

- **1**. 共享设计裁决：DataOS SQLite authoring -> runtime_snapshot.json.descriptors/records -> DataDefinitionCatalog -> Data。
- **2**. 旧 DataMeta/DataRegistry/DataKey 静态定义只能作为一次性审计输入，不作为长期 API 或 fallback。
- **3**. 本切片先证明 catalog 与 descriptor-first 模型成立，暂不改 Entity spawn、Feature bridge、旧路径删除和全量字段迁移。

## Design

- **1**. 新增 DataValueType、DataStoragePolicy、DataWritePolicy、DataRangePolicy、DataModifierPolicy、DataMigrationPolicy。
- **2**. 新增 DataDefinition 与 DataAllowedValue，字段按 core/runtime policy/compute/migration/presentation 分层组织。
- **3**. 新增 DataDefinitionCatalog，启动时冻结索引，重复 stable key、空 key、未知依赖和 computed resolver 缺失必须 fail fast。
- **4**. 新增 RuntimeDataDescriptorDto 与 RuntimeDataSnapshotLoader.BuildCatalog 的最小闭环，只从 descriptors 构建 catalog，不应用 records。
- **5**. 新增 DataComputeRegistry 骨架和 resolver manifest 校验；computed_without_compute_id、missing_resolver、compute_cycle 都进入测试。
- **6**. 新增 LegacyDataAuditTool / LegacyDataAuditReport，输出 missing_in_snapshot、type/default/range/computed mismatch 和 old_path_reference。

## Dependencies

- **Previous SDDs**: none
- **Shared design**: `../../design/Runtime/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. 新增 catalog RED tests，并在 progress 记录 RED/GREEN/REFACTOR 证据。
- **2**. 运行目标 Data 小测试；若全仓仍有 Event 预存错误，记录与本 SDD 无关的阻塞范围。
- **3**. 运行 `python3 Workspace/SDD/sdd.py validate SDD-0012`。
- **4**. 运行 `git diff --check -- SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/002-SDD-0012-data-system-full-rewrite-catalog-tdd-slice`。
