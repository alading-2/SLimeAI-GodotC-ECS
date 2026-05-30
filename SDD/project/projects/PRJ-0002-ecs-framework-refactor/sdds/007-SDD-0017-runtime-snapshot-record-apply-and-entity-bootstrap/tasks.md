# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 写 snapshot apply RED tests
  - **Scope**: 覆盖 apply persisted fields、unknown key、type mismatch、conversion failure、computed/runtime_only 写入和错误聚合。
  - **Validation**: RED tests 指向旧 SnapshotLoader/DataRegistry 行为缺口
- [x] T1.2 实现 RuntimeDataSnapshot DTO 与 LoadFromJson
  - **Scope**: DTO 不包含业务逻辑，JSON options 稳定解析 descriptors/records。
  - **Validation**: LoadFromJson tests 通过
- [x] T1.3 实现 DataApplyReport
  - **Scope**: 结构化记录 table、record id、error code、stable key、message 和 applied count。
  - **Validation**: report tests 通过
- [x] T1.4 实现 ApplyRecord
  - **Scope**: field key 必须存在 descriptor；type/value 必须转换；policy 决定是否写入。
  - **Validation**: apply tests 通过
- [x] T1.5 实现 DataRuntimeBootstrap
  - **Scope**: 注册 resolver、加载 snapshot、构建 catalog、提供 record lookup。
  - **Validation**: bootstrap tests 通过
- [x] T1.6 接入 Entity 创建/模板初始化低风险路径
  - **Scope**: Entity.Data 使用 new Data(owner, catalog)；record apply 失败时阻断错误模板。
  - **Validation**: Entity/Data smoke 通过或明确阻塞
- [x] T1.7 替换旧 SnapshotLoader/DataRegistry 初始化路径
  - **Scope**: 不再依赖 DataRegistry.GetMeta 判断 snapshot field 合法性。
  - **Validation**: grep/compile 验证旧路径不在新初始化链路
- [x] T1.8 运行验证并记录调用点迁移范围
  - **Scope**: 记录尚未迁移的业务字段留给 SDD-0018。
  - **Validation**: validate SDD-0017 通过
