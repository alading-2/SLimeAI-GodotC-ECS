# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 执行 Data 重构 readiness gate
  - **Scope**: 确认 SDD-0012~SDD-0018 状态、审计报告、新测试和调用点迁移已满足删除条件。
  - **Validation**: readiness checklist 进入 progress，无 critical blocker
- [x] T1.2 删除或迁出 SlimeAI/Data/Data 的 Data 输入职责
  - **Scope**: 不再作为 Data 字段定义或 records 输入；非 Data 资源迁到 owner 目录。
  - **Validation**: grep 确认新系统不引用旧 Data/Data 输入路径
- [x] T1.3 删除 SlimeAI/Data/DataNew
  - **Scope**: 移除 DTO 默认值和 DataKey.DefaultValue 依赖。
  - **Validation**: grep DataNew 和 DataKey.DefaultValue 无新系统命中
- [x] T1.4 移除旧 DataMeta/DataRegistry 定义事实源
  - **Scope**: 删除或降级 DataRegistry.Register(new DataMeta)、DataMeta.Compute、LoadFromConfig 等运行时入口。
  - **Validation**: grep gate 通过，build 通过
- [x] T1.5 重建 Data 单场景测试包
  - **Scope**: 替换旧 SingleTest/ECS/Data 为 DataCatalogTestScene、DataRuntimeTestScene、DataSnapshotApplyTestScene、DataFeatureBridgeTestScene。
  - **Validation**: Godot scene smoke 或 headless 场景验证通过
- [x] T1.6 同步长期文档
  - **Scope**: 更新 SlimeAI/Docs/框架/ECS/Data/DataSystem_Design.md、SlimeAI/Data/README.md、DocsAI Data contract/debug guide。
  - **Validation**: 文档描述 descriptor-first 与删除旧路径
- [x] T1.7 同步 Data 相关 skill
  - **Scope**: 如接口/流程变更影响 skill，修改 .ai-config/skills 源并运行 sync 与 skill-test。
  - **Validation**: sync 和 skill-test 摘要进入 progress
- [x] T1.8 运行最终验证和归档准备
  - **Scope**: 运行 build/tests/Godot smoke/grep gates/SDD validate，并回填 PRJ-0002 roadmap/progress。
  - **Validation**: 所有验收标准满足或明确 blocker
