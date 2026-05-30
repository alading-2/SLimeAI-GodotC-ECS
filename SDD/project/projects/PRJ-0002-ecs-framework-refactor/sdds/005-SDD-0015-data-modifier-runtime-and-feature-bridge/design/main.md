# Data Modifier Runtime and Feature Bridge

## Goal

- **1**. 实现 modifier_policy 驱动的 Additive/Multiplicative/FinalAdditive/Override/Cap/Source/Priority。
- **2**. 让 FeatureGranted/FeatureRemoved 正确应用和移除 owner Data modifiers。
- **3**. 验证 Feature.Modifiers target key 必须存在、数值化且允许 modifier。

## Context

- **1**. 依赖 SDD-0014 的 DataSlot 与 write/range policy。
- **2**. computed dirty 标记可先记录接口，完整 resolver/cache 由 SDD-0016 落地。
- **3**. 不废弃 FeatureSystem，也不把 Feature 当 computed resolver。

## Design

- **1**. DataModifierPolicy 从 bool 升级为 none/numeric/debug_only。
- **2**. Data modifier pipeline 在 DataSlot 层计算 effective value。
- **3**. Feature.Modifiers descriptor 使用 stable_key=Feature.Modifiers、value_type=modifier_list、storage_policy=authoring_blob、write_policy=loader_only。
- **4**. FeatureModifierEntry.DataKeyName 在 DataOS validator 和运行时防御校验中检查目标 key。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. Modifier 小测试通过。
- **2**. Feature bridge 纯 C# service 测试通过；必要时补 Godot smoke。
- **3**. `python3 Workspace/SDD/sdd.py validate SDD-0015` 通过。
