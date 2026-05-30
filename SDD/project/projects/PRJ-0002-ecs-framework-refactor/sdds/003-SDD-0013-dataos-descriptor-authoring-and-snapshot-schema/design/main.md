# DataOS Descriptor Authoring and Snapshot Schema

## Goal

- **1**. 把 data_key_descriptor 从旧 DataMeta mirror 升级为 descriptor-first 字段定义源。
- **2**. 让 generator 输出包含 core、runtime policy、compute policy、migration policy 和 presentation 的 descriptors。
- **3**. 让 validator 用结构化报告定位 AI authoring 错误。

## Context

- **1**. SQLite 是 authoring 源，运行时不直接查询 SQLite。
- **2**. 业务表仍优先 Unit/Ability/Feature 等清晰表，不退化为万能 EAV 主入口。
- **3**. 本 SDD 依赖 SDD-0012 的 descriptor DTO/catolog 契约，不改 Data.Get/Set 行为。

## Design

- **1**. 补齐 data_key_descriptor 字段：owner_domain、runtime_type_id、storage_policy、write_policy、range_policy、modifier_policy、migration_policy、compute_id、dependencies_json、compute_params_json、allowed_values_json、ui_group、reset_group、unit、format。
- **2**. validator 检查 stable_key 唯一、value_type 合法、default 可转换、min/max、policy 枚举、dependencies 存在、resolver manifest、Feature modifier target 和 record/descriptor 一致性。
- **3**. generator 输出 runtime_snapshot.json.descriptors，字段名与 RuntimeDataDescriptorDto 对齐；disabled capability 在生成阶段裁剪，loader 仅做防御性校验。
- **4**. 新增最小 fixture snapshot，作为 SDD-0012/0017 的测试输入。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. DataOS schema/generator/validator 测试通过。
- **2**. 生成的 fixture snapshot 可被 SDD-0012 BuildCatalog 读取。
- **3**. 运行 `python3 Workspace/SDD/sdd.py validate SDD-0013`。
