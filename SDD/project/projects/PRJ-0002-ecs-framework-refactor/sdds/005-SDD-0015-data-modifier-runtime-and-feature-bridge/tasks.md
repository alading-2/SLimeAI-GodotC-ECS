# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 写 modifier 与 Feature bridge RED tests
  - **Scope**: 覆盖非数值字段、无 modifier_policy、Additive/Multiplicative/Override/Cap、source removal、Feature target 校验。
  - **Validation**: RED tests 失败并指向旧 modifier/Feature 裸字符串问题
- [x] T1.2 实现 modifier_policy 校验
  - **Scope**: 只有 numeric/debug_only 等允许策略可添加 modifier；Id/EntityType/CurrentHp 等默认拒绝。
  - **Validation**: reject tests 通过
- [x] T1.3 实现完整 modifier pipeline
  - **Scope**: 支持 Additive、Multiplicative、FinalAdditive、Override、Cap、Priority、Source。
  - **Validation**: modifier 计算 tests 通过
- [x] T1.4 实现 RemoveModifiersBySource 与 dirty 标记接口
  - **Scope**: 按 source 精确移除 modifier，并通知依赖 computed 后续失效。
  - **Validation**: source removal 与 dirty marker tests 通过
- [x] T1.5 把 Feature.Modifiers 表达为 authoring_blob descriptor
  - **Scope**: 删除长期裸 const string 入口，descriptor/validator 知道该字段语义。
  - **Validation**: FeatureModifiers_ShouldBeRepresentedAsAuthoringBlobDefinition 通过
- [x] T1.6 接入 FeatureSystem 授予与移除流程
  - **Scope**: Granted 自动应用 modifier；Removed 按 source 回滚；不调用 computed resolver。
  - **Validation**: FeatureGranted/FeatureRemoved tests 通过
- [x] T1.7 验证 Feature modifier target key
  - **Scope**: 目标 key 必须存在、数值字段且 modifier_policy 允许。
  - **Validation**: target key validator tests 通过
- [x] T1.8 运行验证并记录 Feature/Compute 边界
  - **Scope**: progress 记录 Feature 改变 Data 输入、computed 读取输入的边界。
  - **Validation**: validate SDD-0015 通过
