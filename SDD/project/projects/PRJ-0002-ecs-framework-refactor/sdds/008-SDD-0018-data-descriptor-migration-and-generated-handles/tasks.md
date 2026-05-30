# Tasks

## Progress

- **Status**: done
- **Completed**: 9/9
- **Current**: done

## Task List

- [x] T1.1 建立旧 DataKey 到 stable_key 的迁移账本
  - **Scope**: 从 LegacyDataAuditReport 和 DataKey_*.cs 生成字段清单、owner、类型、默认值、policy 和 computed mapping。
  - **Validation**: 迁移账本记录 missing/mismatch 基线
- [x] T1.2 迁移 Base / Unit descriptors
  - **Scope**: 身份、名称、阵营、实体类型和 Unit 模板字段进入 descriptor。
  - **Validation**: 审计 Base/Unit missing_in_snapshot 收敛
- [x] T1.3 迁移 Attribute descriptors
  - **Scope**: Base/Bonus/Final/Percent 字段、modifier_policy 和 compute_id/dependencies 映射。
  - **Validation**: Attribute tests 与审计通过
- [x] T1.4 迁移 Movement descriptors
  - **Scope**: 速度、方向、持续时间、距离等字段遵守 -1 表示不限制的语义。
  - **Validation**: Movement descriptor 审计通过
- [x] T1.5 迁移 Ability descriptors
  - **Scope**: 伤害、冷却、概率、范围、链式/触发字段按 owner_skill 和 0-100 概率语义表达。
  - **Validation**: Ability descriptor 审计通过
- [x] T1.6 迁移 Feature descriptors
  - **Scope**: Feature 状态、计数、分类和 Feature.Modifiers authoring_blob 进入 descriptor。
  - **Validation**: Feature descriptor 与 bridge tests 通过
- [x] T1.7 迁移 AI / Test descriptors
  - **Scope**: AI 运行时引用标记 runtime_only，测试字段进入 test owner。
  - **Validation**: AI/Test descriptor 审计通过
- [x] T1.8 实现 generated DataKey<T> thin handle
  - **Scope**: 从 descriptors/codegen 生成 handle，不包含默认值、范围、computed 或 modifier 策略。
  - **Validation**: 生成文件编译通过且不成为定义事实源
- [x] T1.9 迁移业务调用点并运行 grep gate
  - **Scope**: 业务 Data.Get/Set 使用 generated handle；裸字符串限 loader/fixture/audit。
  - **Validation**: grep gate 和 build/test 结果记录到 progress
