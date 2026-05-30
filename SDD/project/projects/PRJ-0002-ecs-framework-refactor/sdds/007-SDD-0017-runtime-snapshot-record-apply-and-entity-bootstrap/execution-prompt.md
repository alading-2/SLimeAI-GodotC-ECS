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

- **ID**: `SDD-0017`
- **Title**: Runtime Snapshot Record Apply and Entity Bootstrap
- **Path**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/007-SDD-0017-runtime-snapshot-record-apply-and-entity-bootstrap`

## 本 SDD 执行目标

- **目标**: 实现 runtime snapshot records 到 Data 容器的结构化写入。
- **实现范围**: `RuntimeDataSnapshot` DTO、`LoadFromJson`、`DataApplyReport`、`ApplyRecord`、`DataRuntimeBootstrap`、Entity/Data 初始化。
- **顺序**: 运行时必须先构建 catalog，再应用 records。

## 明确不做

- **不允许 records 中 unknown key 自动创建 DataDefinition**。
- **不允许 `DataRegistry.GetMeta` 参与 snapshot field 合法性判断**。
- **不做全量业务字段迁移**: 属于 SDD-0018。

## 任务提示

1. 写 snapshot apply RED tests：apply persisted fields、unknown key、type mismatch、conversion failure、computed/runtime_only 写入拒绝、错误聚合。
2. 实现 `RuntimeDataSnapshot` DTO。
3. 实现 `LoadFromJson`。
4. 实现 `DataApplyReport`。
5. 实现 `ApplyRecord`。
6. 实现 `DataRuntimeBootstrap`。
7. 接入 Entity 创建/模板初始化低风险路径。
8. 替换旧 SnapshotLoader/DataRegistry 初始化路径。
9. 运行 snapshot apply tests、Entity/Data bootstrap smoke、grep/compile 验证、`python3 Workspace/SDD/sdd.py validate SDD-0017`。
