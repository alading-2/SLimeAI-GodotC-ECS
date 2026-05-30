# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 确认新 Data TDD 入口和 RED/GREEN/REFACTOR 记录格式
  - **Scope**: 记录测试目录、命名规则、当前构建基线和本切片不处理的旧错误。
  - **Validation**: progress.md 有 R/G/R 记录模板；validate SDD-0012 通过
- [x] T1.2 写 catalog RED tests
  - **Scope**: 覆盖重复 key、空 key、未知 value_type、默认值转换失败、非法 policy、未知依赖、compute cycle 和 missing resolver。
  - **Validation**: 目标测试在实现前失败，失败原因指向新行为缺失
- [x] T1.3 实现 DataDefinition 分层模型和 policy enum
  - **Scope**: 只实现 catalog 需要的字段与转换，不把 presentation 字段放入 Data 热路径。
  - **Validation**: Catalog tests 中类型、默认值和 policy 用例转绿
- [x] T1.4 实现 DataDefinitionCatalog 索引与依赖验证
  - **Scope**: 支持 stable key 查询、反向依赖索引、重复 key fail fast、依赖存在性和循环依赖检测。
  - **Validation**: 依赖索引、未知依赖、循环依赖 tests 通过
- [x] T1.5 实现 descriptor DTO 与 BuildCatalog 最小 loader
  - **Scope**: 从 runtime_snapshot descriptors fixture 构建 catalog，不应用 records，不接 Entity spawn。
  - **Validation**: BuildCatalog fixture tests 通过
- [x] T1.6 实现 DataComputeRegistry 骨架和 resolver 绑定校验
  - **Scope**: computed 字段必须有 compute_id；compute_id 必须能在 resolver manifest/registry 中找到。
  - **Validation**: computed_without_compute_id 和 missing_resolver tests 通过
- [x] T1.7 实现 LegacyDataAuditReport 一次性审计工具
  - **Scope**: 读取旧 DataMeta/DataRegistry/DataKey 清单生成缺口报告；禁止 Data 运行时调用该工具。
  - **Validation**: 审计 tests 输出 missing_in_snapshot/type/default/range/computed/old_path_reference
- [x] T1.8 收尾验证并回填项目状态
  - **Scope**: 记录测试命令、构建命令、旧错误边界和下一切片入口。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0012` 0 error；项目 progress 更新
