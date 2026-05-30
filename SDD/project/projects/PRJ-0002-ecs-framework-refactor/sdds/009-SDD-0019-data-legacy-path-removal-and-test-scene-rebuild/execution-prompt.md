# Execution Prompt

## 使用方式

把本文件整体复制给执行会话/子代理，或在当前会话中按本文件继续执行。

## 全局执行约束

- **工作区**: `/home/slime/Code/SlimeAI`
- **项目**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **执行序列**: `SDD-0012` → `SDD-0019`
- **当前原则**: Data 子系统是完整重构，不是兼容迁移。
- **字段定义事实源**: `runtime_snapshot.json.descriptors`。
- **字段值事实源**: `runtime_snapshot.json.records`。
- **运行时定义索引**: `DataDefinitionCatalog`。
- **旧系统边界**: 旧 `DataMeta` / `DataRegistry` / 手写 `DataKey` 静态定义只允许作为一次性审计输入。
- **禁止**: 不新增 `DataMetaAdapter`、运行时 fallback、长期兼容层。
- **Feature 边界**: `FeatureSystem` 只负责生命周期、效果动作、Modifier 授予/回滚；computed 字段由 `DataComputeRegistry` + `IDataComputeResolver` 处理。
- **旧路径边界**: `SlimeAI/Data/Data/` 与 `SlimeAI/Data/DataNew/` 不作为新 Data 系统输入，最终由 SDD-0019 删除或迁出其 Data 职责。
- **旧测试边界**: `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/` 不修补，最终按新 Data TDD 矩阵重建。

## 必读入口

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/01-代码实现说明.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/03-完全重构范围与TDD测试计划.md`
8. 当前 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`。

## 执行规则

1. 从当前 SDD 的 `tasks.md` 的 `T1.1` 开始，不跳任务。
2. 行为变更必须 TDD：先 RED，再 GREEN，最后 REFACTOR。
3. 每完成一个任务，更新该 SDD 的 `tasks.md` checkbox 与 `progress.md`。
4. 改源码前后在对应 git 边界运行 `git status --short`。
5. 能验证就运行构建/测试；不能验证必须说明原因。
6. 不要 commit/push，除非用户明确要求。
7. 结束前至少运行：`python3 Workspace/SDD/sdd.py validate <当前SDD>`。
8. 如果修改 Docs/Skill，必须同步对应 skill 源和验证。

## 当前 SDD

- **ID**: `SDD-0019`
- **Title**: Data Legacy Path Removal and Test Scene Rebuild
- **Path**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/009-SDD-0019-data-legacy-path-removal-and-test-scene-rebuild`

## 本 SDD 执行目标

- **目标**: 完成 Data 完整重构收口门禁。
- **旧路径**: 删除或迁出旧 Data 输入目录的 Data 输入职责，删除旧 DataNew DTO 路径。
- **旧事实源**: 移除旧 DataMeta/DataRegistry 定义事实源。
- **测试**: 重建 Data Godot 单场景测试包。
- **文档/Skill**: 同步长期文档和 Data skill。

## 前置条件

- **SDD-0012 至 SDD-0018**: 必须完成并通过验证。
- **Audit**: `LegacyDataAuditReport` 无 critical missing。
- **Runtime**: 新 Data runtime、descriptor、snapshot apply、Feature bridge、compute、generated handles 已可用。
- **Coverage**: 新测试覆盖足够替换旧测试。

## 明确禁止

- **不修补旧 Data 测试**。
- **不保留 DataNew 作为兼容输入**。
- **不保留 LoadFromConfig 作为新 Data 输入**。
- **不让 DataKey.DefaultValue 继续参与 DTO 默认值**。
- **不允许运行时 fallback 到 DataMeta/DataRegistry**。

## 任务提示

1. 执行 readiness gate：前置 SDD 状态、audit report、build/test baseline、grep baseline。
2. 删除或迁出旧 Data 输入职责。
3. 删除旧 DataNew DTO 路径。
4. 移除旧 DataMeta/DataRegistry 定义事实源。
5. 重建 Data scene tests：DataCatalogTestScene、DataRuntimeTestScene、DataSnapshotApplyTestScene、DataFeatureBridgeTestScene。
6. 同步长期文档和 Data skill；如修改 skill，运行 sync/skill-test。
7. 执行最终 grep gate：DataNew、LoadFromConfig、DataKey.DefaultValue、DataRegistry.Register(new DataMeta)、DataMeta.Compute、TestDataKeyMapping。
8. 运行最终验证：build/tests/Godot smoke/SDD validate/all validate。
9. 更新本 SDD 和 PRJ-0002 roadmap/progress。
