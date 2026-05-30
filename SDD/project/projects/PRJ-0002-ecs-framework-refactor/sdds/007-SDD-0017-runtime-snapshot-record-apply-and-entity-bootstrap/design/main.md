# Runtime Snapshot Record Apply and Entity Bootstrap

## Goal

- **1**. 实现 RuntimeDataSnapshotLoader.LoadFromJson、ApplyRecord 和 DataApplyReport。
- **2**. 启动时构建 DataDefinitionCatalog，再按 records 初始化 Entity.Data。
- **3**. unknown key、type mismatch、computed/runtime_only 写入等错误结构化报告。

## Context

- **1**. 依赖 SDD-0012 catalog、SDD-0013 snapshot schema、SDD-0014 Data.SetUntyped 和 SDD-0016 compute registry。
- **2**. 不在本 SDD 做全量业务字段迁移；字段覆盖由 SDD-0018 承担。

## Design

- **1**. RuntimeDataSnapshot DTO 只表达 JSON shape，转换逻辑放 loader。
- **2**. ApplyRecord 对每个 record field 查 catalog、检查 field.type、转换 value、按 write/storage policy 调用 Data.SetUntyped。
- **3**. DataApplyReport 聚合 table、record id、field errors 和 applied count，不因单个错误直接崩溃除非调用方要求 fail fast。
- **4**. DataRuntimeBootstrap 注册 framework computes、读取 snapshot、构建 catalog、提供 record lookup。
- **5**. Entity spawn 或 profile bootstrap 从 DataRuntimeBootstrap 获得 catalog 和 record。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. Snapshot apply 小测试通过。
- **2**. 至少一个 Entity/Data bootstrap smoke 通过或记录 Godot 场景阻塞。
- **3**. `python3 Workspace/SDD/sdd.py validate SDD-0017` 通过。
