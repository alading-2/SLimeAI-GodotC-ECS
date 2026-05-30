# Tasks

## Progress

- **Status**: done
- **Completed**: 12/12
- **Current**: done

## Task List

- [x] T1.1 建立无兼容 readiness baseline 和红灯门禁
  - **Scope**: 固定当前 snapshot mismatch、generated handle type drift、DataKey implicit string、DataKey compatibility alias、public string-key Data API、Resource/tres authoring、RuntimeTables/legacy snapshot 字段和过期文档命中。
  - **Validation**: baseline 写入 progress；grep/jq/sqlite 证据能复现 `AbilityIcon` mismatch、`dataos_runtime_field_stream=0` 和非标量字段映射错误。
- [x] T1.2 修正 DataOS generator 与 final snapshot validator
  - **Scope**: `generate-runtime-snapshot.sh` record type 改为来自 descriptor；`validate-dataos.sh` 校验最终 `runtime_snapshot.json` descriptor/record 一致性；`legacyTable` / `legacyData` 从 runtime snapshot 退出或迁到 audit artifact。
  - **Validation**: `validate-dataos.sh` 能在当前 mismatch 下失败，修复后通过；最终 jq mismatch 无输出。
- [x] T1.3 统一非标量 descriptor 到 CLR 类型映射
  - **Scope**: 定义 `string_array`、`object_ref`、`modifier_list` 的唯一运行时类型；优先完成 `string_array -> string[]`，并裁决 `AbilityIcon` / `TargetNode` / `Feature.Modifiers` 边界。
  - **Validation**: descriptor type、snapshot record type、DataValueConverter、generated handle 和调用点类型一致。
- [x] T1.4 重写 generated DataKey handle 生成器
  - **Scope**: `generate-data-key-handles.py` 不再把非标量映射成 `string`；删除 `DataKey.Xxx` compatibility alias 输出；重新生成 `DataKey_Generated.cs`。
  - **Validation**: grep 无 `Compatibility aliases`；非标量字段不再生成 `DataKey<string>`。
- [x] T1.5 切断 `DataKey<T>` 到 string 的隐式绕路
  - **Scope**: 删除 `DataKey<T>.implicit operator string`；评估并收紧 `.Key` alias；业务层编译错误按真实 typed handle 修复。
  - **Validation**: grep 无 `implicit operator string(DataKey`；错误调用不再能通过 string overload 编译。
- [x] T1.6 收紧 Data public API 到 typed handle
  - **Scope**: `Data.Get/Set/Has/Remove/Add/Modifier` 面向业务只保留 `DataKey<T>` 入口；stable key string 入口改为 internal/loader/test-only，并显式命名。
  - **Validation**: 业务代码 grep 无 `Data.Get<...>("...")`、`.Set("...")` 和由 DataKey 隐式转 string 的旧形态。
- [x] T1.7 删除未绑定 `new Data()` 运行时窗口
  - **Scope**: Entity / test fixture 改为显式 catalog 绑定；无参 `Data()` 删除或降为不可用于业务的初始化占位。
  - **Validation**: grep 旧 `new Data()` 命中只剩明确 test fixture 或无命中；未绑定访问有测试覆盖。
- [x] T1.8 迁移当前已知错误调用点
  - **Scope**: 修复 `AvailableAnimations`、`AbilityTriggerEvent`、`Feature.Modifiers`、`TargetNode`、`AbilityIcon` 等调用点，不再使用 `List<string>` / `object` / `Node2D` 绕过错误 handle。
  - **Validation**: grep 无 `Get<object>(GeneratedDataKey`、`Get<.*List<string>`、`Get<Node2D>(GeneratedDataKey.TargetNode)`、`Set(GeneratedDataKey.TargetNode, node2D)`。
- [x] T1.9 删除 DataMeta / DataRegistry / Legacy audit runtime 依赖
  - **Scope**: `DataMeta`、`DataRegistry`、`LegacyDataAuditReport` 从 runtime 编译面退出；迁移审计保留到 SDD artifact 或测试 fixture。
  - **Validation**: runtime 源码 grep 无 `class DataMeta` / `class DataRegistry` / `DataRegistry.Register` / `DataRegistry.GetMeta`。
- [x] T1.10 清理 RuntimeTables、Resource/tres authoring 和文档事实源
  - **Scope**: RuntimeTables 目录改名或迁出 DTO；FeatureDefinition/SystemConfig/SystemPreset Resource authoring 路线裁决删除或改名为 runtime-only；DocsAI/DocsNew/Data README/ProjectState/roadmap 不再说兼容入口可用。
  - **Validation**: 非历史文档 grep 无 `DataKey.Xxx 兼容`、`new Data() 迁移期兼容`、`RuntimeTables 兼容 API`、`SDD-0020 已完全退出旧路径`。
- [x] T1.11 运行完整 build、DataOS、Godot 和 grep gate
  - **Scope**: 按 design/main.md 的四层验证执行；失败项必须修复或写 blocker。
  - **Validation**: build/DataOS/Godot/grep/SDD validate 结果写入 progress。
- [x] T1.12 回填 SDD、项目、DocsAI 和执行提示词状态
  - **Scope**: 更新 tasks/progress/latest resume、PRJ-0002 roadmap/progress/README/project.json、DocsAI ProjectState、DocsNew/Data 文档和 `execution-prompt.md`。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0021` 和 `validate --all` 0 error；相关文档指向 SDD-0021。
