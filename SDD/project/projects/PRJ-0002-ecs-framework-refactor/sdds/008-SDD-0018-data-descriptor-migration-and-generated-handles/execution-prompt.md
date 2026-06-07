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
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/01-代码实现说明.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md`
8. 当前 SDD 的 `README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`Core/progress.md`。

## 执行规则

1. 从当前 SDD 的 `tasks.md` 的 `T1.1` 开始，不跳任务。
2. 行为变更必须 TDD：先 RED，再 GREEN，最后 REFACTOR。
3. 每完成一个任务，更新该 SDD 的 `tasks.md` checkbox 与 `Core/progress.md`。
4. 改源码前后在对应 git 边界运行 `git status --short`。
5. 能验证就运行构建/测试；不能验证必须说明原因。
6. 不要 commit/push，除非用户明确要求。
7. 结束前至少运行：`python3 Workspace/SDD/sdd.py validate <当前SDD>`。
8. 如果修改 Docs/Skill，必须同步对应 skill 源和验证。

## 当前 SDD

- **ID**: `SDD-0018`
- **Title**: Data Descriptor Migration and Generated Handles
- **Path**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/008-SDD-0018-data-descriptor-migration-and-generated-handles`

## 本 SDD 执行目标

- **目标**: 按模块迁移旧字段能力到 descriptors，并生成 thin `DataKey<T>` handle。
- **迁移顺序**: Base/Unit → Attribute → Movement → Ability → Feature → AI/Test。
- **Handle 规则**: generated `DataKey<T>` 只包含 stable key 和泛型类型，不包含 default/range/computed/modifier policy。

## 明确不做

- **不允许手写 DataKey 继续定义字段**。
- **不允许 `DataKey.DefaultValue`**。
- **不允许 `DataRegistry.Register(new DataMeta)` 继续作为新系统字段注册方式**。
- **不允许业务裸字符串 Data.Get/Set 扩散**。

## 任务提示

1. 建立旧 DataKey 到 stable_key 迁移账本。
2. 跑 `LegacyDataAuditReport`，记录 missing/mismatch baseline。
3. 依次迁移 Base/Unit、Attribute、Movement、Ability、Feature、AI/Test descriptors。
4. 每迁移一个模块就运行审计，确认 missing/mismatch 收敛。
5. 实现 generated thin `DataKey<T>` handle。
6. 迁移业务调用点。
7. 执行 grep gate：`DataRegistry.Register(new DataMeta)`、`DataKey.DefaultValue`、业务 `Data.Get<T>("...")`、业务 `Data.Set("...")`。
8. 运行相关模块 tests、build、grep gate、`python3 Workspace/SDD/sdd.py validate SDD-0018`。
