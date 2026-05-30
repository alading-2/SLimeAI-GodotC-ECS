# Tasks

## Progress

- **Status**: done
- **Completed**: 7/7
- **Current**: done

## Task List

- [x] T1.1 审计现有 DataOS schema、generator、snapshot descriptors
  - **Scope**: 确认已有字段、缺失字段、旧 mirror 语义和 runtime_snapshot 当前 JSON shape。
  - **Validation**: notes.md 记录 schema gap 和 snapshot shape
- [x] T1.2 补齐 data_key_descriptor 分层字段
  - **Scope**: 新增 core/runtime/compute/migration/presentation 字段，不复制旧 DataMeta 巨型对象语义。
  - **Validation**: schema migration 或等价 authoring 定义通过测试
- [x] T1.3 实现 descriptor validator 规则
  - **Scope**: 覆盖类型、默认值、范围、policy、依赖、resolver manifest、Feature modifier target。
  - **Validation**: validator 对错误 fixture 输出结构化 row/field/code
- [x] T1.4 更新 generator 输出 runtime_snapshot.descriptors
  - **Scope**: 输出字段与 RuntimeDataDescriptorDto 对齐；presentation 保留但不进入热路径。
  - **Validation**: 生成 snapshot 可被 BuildCatalog fixture 消费
- [x] T1.5 实现 capability trimming 与 record/descriptor 一致性校验
  - **Scope**: disabled capability 不进入 active descriptors；records 中未知 key/type mismatch 报错。
  - **Validation**: validator 覆盖 disabled capability 与 record mismatch
- [x] T1.6 建立最小 descriptor fixture 与快照样例
  - **Scope**: 包含 persisted/runtime_state/computed/authoring_blob/allowed_values/modifier_policy 示例。
  - **Validation**: fixture 被纳入 DataOS 与 runtime loader 测试
- [x] T1.7 运行 DataOS 验证并回填下一切片契约
  - **Scope**: 记录生成命令、验证结果和 SDD-0014/0017 依赖接口。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0013` 0 error
