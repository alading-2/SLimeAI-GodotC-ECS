# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: complete

## Task List

- [x] T1.1 写 compute RED tests
  - **Scope**: 覆盖 resolver 使用、dependencies、compute_params、cache、transitive dirty、missing resolver、Set computed fail。
  - **Validation**: RED tests 失败并指向旧 DataMeta.Compute 依赖
- [x] T1.2 实现 DataComputeRegistry 完整注册行为
  - **Scope**: 重复 ComputeId、空 ComputeId、missing resolver 均有明确错误。
  - **Validation**: registry tests 通过
- [x] T1.3 实现 computed 依赖图与循环检测
  - **Scope**: BuildCatalog 阶段检测 unknown dependency 和 cycle。
  - **Validation**: cycle 与 dependency tests 通过
- [x] T1.4 实现 GetComputed 与 cache
  - **Scope**: 根据 descriptor dependencies 调用 resolver；cache 命中直到依赖变化。
  - **Validation**: cache tests 通过
- [x] T1.5 实现 transitive dirty
  - **Scope**: Set base value 或 modifier 变化时递归标脏所有 computed dependents。
  - **Validation**: transitive dirty tests 通过
- [x] T1.6 实现基础 resolver 示例
  - **Scope**: AttributeBonus、Percent、AttackInterval 等常见计算语义只在 C# resolver 中实现。
  - **Validation**: resolver 示例 tests 通过
- [x] T1.7 记录 computed 无副作用契约
  - **Scope**: 明确 resolver 不得写 Data、发事件、访问场景树或启动 timer。
  - **Validation**: contract 文档与测试 double 覆盖
- [x] T1.8 运行验证并记录 Feature 边界
  - **Scope**: 证明 Feature 改输入，Data compute 算输出。
  - **Validation**: validate SDD-0016 通过
