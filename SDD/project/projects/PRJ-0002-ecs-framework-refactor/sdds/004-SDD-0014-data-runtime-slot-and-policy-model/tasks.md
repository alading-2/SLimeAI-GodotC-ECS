# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 写 Data core RED tests
  - **Scope**: 覆盖默认值、typed value、unknown key、wrong type、write policy、range policy、allowed values、remove/clear 语义。
  - **Validation**: RED tests 失败原因指向旧 DataRegistry 行为
- [x] T1.2 实现 DataValueConverter
  - **Scope**: 支持 string/int/float/double/bool/vector2/modifier_list/object_ref 的严格转换和兼容检查。
  - **Validation**: 转换与错误报告 tests 通过
- [x] T1.3 实现 DataSlot 和 descriptor default fallback
  - **Scope**: 未知 key 不创建；未设置字段返回 descriptor default。
  - **Validation**: Data_Get_ShouldReturnDescriptorDefault 与 unknown key tests 通过
- [x] T1.4 实现 write policy 与内部 SetUntyped
  - **Scope**: read_write/loader_only/system_only/computed_readonly/debug_only 按调用来源或入口限制写入。
  - **Validation**: write policy tests 通过
- [x] T1.5 实现 range policy 与 allowed values
  - **Scope**: validate/clamp_runtime/reject_runtime 分离；authoring 错误不被静默 clamp。
  - **Validation**: range 与 allowed values tests 通过
- [x] T1.6 收口 typed handle 与 string API
  - **Scope**: Data typed handle Get/Set 为业务入口；string API 标记内部使用范围。
  - **Validation**: 类型安全调用 tests 或编译验证通过
- [x] T1.7 实现数据变更事件最小契约
  - **Scope**: Set 成功后通过 Entity.Events 发布 key/old/new，不让 Get 产生副作用。
  - **Validation**: Data_Set_ShouldPublishPropertyChanged 通过
- [x] T1.8 运行验证并记录旧 Data 行为差异
  - **Scope**: 记录哪些旧行为已删除或推迟到后续 SDD。
  - **Validation**: validate SDD-0014 通过，progress 有验证摘要
