# Data Legacy Path Removal and Test Scene Rebuild

## Goal

- **1**. 删除或迁出旧 Data 输入目录的 Data 输入职责。
- **2**. 删除旧 DataNew DTO 路径，不再让 DTO 默认值读取 DataKey.DefaultValue。
- **3**. 重建 DataCatalog/DataRuntime/DataSnapshotApply/DataFeatureBridge 场景测试。
- **4**. 同步长期文档区域、Data skill 和最终 grep/build/test 证据。

## Context

- **1**. 必须在 SDD-0012~SDD-0018 完成且验证通过后执行。
- **2**. 删除前必须确认新测试覆盖、调用点迁移和 descriptor 覆盖完整。
- **3**. 如果修改 AI skill 统一源，必须运行 ai-config sync 和 skill-test。

## Design

- **1**. 以 readiness gate 开始：previous SDDs done、审计报告无 critical missing、build/test 基线可解释。
- **2**. 旧 Data 输入目录若包含非 Data 专属资源，迁到对应业务目录并明确不再作为 Data 字段定义或 records 来源。
- **3**. 旧 Data 测试场景不修补，替换为新场景包。
- **4**. 最终搜索 DataNew、LoadFromConfig、DataKey.DefaultValue、DataRegistry.Register(new DataMeta)、DataMeta.Compute、TestDataKeyMapping 等仅允许历史文档命中。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/Runtime/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. dotnet build / Tools/run-build.sh 通过或只剩明确范围外错误。
- **2**. Tools/run-tests.sh 或 Data 小测试通过。
- **3**. Godot Data 场景 smoke 通过。
- **4**. grep gates 对旧路径/旧 API 无新系统命中。
- **5**. `python3 Workspace/SDD/sdd.py validate SDD-0019` 和 `validate --all` 通过。
