# Data Descriptor Migration and Generated Handles

## Goal

- **1**. 让 descriptor 覆盖旧 DataMeta 字段能力。
- **2**. 生成 C# DataKey<T> thin handle，但不保存默认值、范围、分类、computed、modifier 策略。
- **3**. 迁移调用点，减少裸字符串 Data.Get/Set 和 DataRegistry.Register(new DataMeta)。

## Context

- **1**. 依赖 SDD-0012 audit、SDD-0013 descriptor schema、SDD-0017 runtime apply。
- **2**. 旧 DataKey_*.cs 可作为迁移输入清单，但最终不作为字段定义事实源。
- **3**. 概率字段继续遵守 0-100 语义，计算时 /100。

## Design

- **1**. 建立 stable_key 命名映射，如 Attribute.BaseHp、Ability.Damage、Feature.Enabled。
- **2**. 每个模块迁移后运行 LegacyDataAuditReport，确认 missing/mismatch 收敛。
- **3**. Codegen 输出 DataKey<T> handle，来源为 descriptors；handle 只包含 stable key 与泛型类型。
- **4**. 业务调用点用 generated handle；loader/测试/审计工具以外不新增 string API。

## Dependencies

- **Previous SDDs**: SDD-0012
- **Shared design**: `../../design/2.Data系统优化/README.md` and related Data rewrite documents.
- **Boundary**: This SDD must not reintroduce DataMeta/DataRegistry/DataKey static definitions as long-term field-definition sources.

## Verification

- **1**. 每个模块迁移后审计报告收敛。
- **2**. grep gate 检查 DataRegistry.Register(new DataMeta)、DataKey.DefaultValue、Data.Get<T>("业务 key")。
- **3**. `python3 Workspace/SDD/sdd.py validate SDD-0018` 通过。
