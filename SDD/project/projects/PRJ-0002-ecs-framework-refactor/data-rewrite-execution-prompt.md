# Data Rewrite Execution Prompt

## 使用方式

把本文件整体复制给新的执行会话、主执行 agent 或编排会话。它是 PRJ-0002 Data System Full Rewrite 的总入口；单个切片执行时再进入对应 SDD 的 `execution-prompt.md`。

## 角色定位

你是 PRJ-0002 Data 系统完整重构的主执行者和集成者。

你的职责不是一次性并行改完整个 Data 系统，而是按 `SDD-0012` → `SDD-0019` 顺序推进，保证每个切片有明确的 RED/GREEN/REFACTOR、验证证据、文档回填和下一步恢复点。

## 工作区与项目

- **Workspace**: `/home/slime/Code/SlimeAI`
- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `SDD-0012`
- **SDD Sequence**: `SDD-0012` → `SDD-0013` → `SDD-0014` → `SDD-0015` → `SDD-0016` → `SDD-0017` → `SDD-0018` → `SDD-0019`

## 必读入口

先读项目事实源：

1. `README.md`
2. `roadmap.md`
3. `progress.md`
4. `design/INDEX.md`
5. `design/main.md`
6. `design/04-优化优先级与SDD拆分建议.md`

再读 Data 完整重构事实源：

1. `design/2.Data系统优化/README.md`
2. `design/2.Data系统优化/01-代码实现说明.md`
3. `design/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md`
4. `design/2.Data系统优化/03-完全重构范围与TDD测试计划.md`

最后读当前 SDD：

1. `sdds/<current>/README.md`
2. `sdds/<current>/execution-prompt.md`
3. `sdds/<current>/design/main.md`
4. `sdds/<current>/tasks.md`
5. `sdds/<current>/bdd.md`
6. `sdds/<current>/progress.md`

## 核心裁决

- **完整重构**: Data 子系统是完整重构，不是兼容迁移。
- **字段定义事实源**: `runtime_snapshot.json.descriptors`。
- **字段值事实源**: `runtime_snapshot.json.records`。
- **运行时定义索引**: `DataDefinitionCatalog`。
- **旧定义边界**: 旧 `DataMeta` / `DataRegistry` / 手写 `DataKey` 静态定义只允许作为一次性审计输入。
- **禁止兼容层**: 不新增 `DataMetaAdapter`、运行时 fallback、长期兼容 API。
- **Feature 边界**: `FeatureSystem` 负责生命周期、效果动作、Modifier 授予/回滚；computed 字段由 `DataComputeRegistry` + `IDataComputeResolver` 处理。
- **旧输入路径**: `SlimeAI/Data/Data/` 与 `SlimeAI/Data/DataNew/` 不作为新 Data 系统输入，最终由 SDD-0019 删除或迁出其 Data 职责。
- **旧测试路径**: `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/` 不修补，最终按新 Data TDD 矩阵重建。

## 执行策略

### 主原则

按 SDD 顺序执行，不要 8 个 SDD 并行改源码。

推荐模式：

```text
主会话 = owner / integrator / 最终写入者
subagent = 只读侦察 / gap report / 测试建议 / 局部迁移草案
```

禁止模式：

```text
8 个 SDD 同时开 8 个 subagent 改源码
```

原因：Data 核心契约、descriptor schema、runtime policy、modifier、compute、snapshot apply 和旧路径删除有强依赖，并行实现容易导致契约漂移、重复定义、测试互踩和旧兼容层回流。

### Subagent 使用规则

允许 subagent：

- **只读分析**: 查找入口、调用点、旧路径和测试现状。
- **审计报告**: 生成 gap list、字段迁移清单、风险列表。
- **测试建议**: 提出 RED tests、fixture 和边界用例。
- **迁移草案**: 为 SDD-0018 分模块整理 descriptor mapping。

不允许 subagent：

- **并行修改 Data 核心 runtime**。
- **并行修改 descriptor schema 契约**。
- **删除旧路径或测试场景**。
- **添加兼容层或 fallback**。
- **绕过主会话统一 stable_key 命名和 generated handle 格式**。

## SDD 执行顺序

### Phase 1 — 基础契约

#### SDD-0012: Data System Full Rewrite - Catalog TDD Slice

必须先由主会话单独执行。

目标：

- Catalog RED tests。
- `DataValueType` / policy enum。
- `DataDefinition`。
- `DataDefinitionCatalog`。
- `BuildCatalog`。
- `DataComputeRegistry` 骨架。
- `LegacyDataAuditReport`。

禁止：

- 不做 records apply。
- 不接 Entity bootstrap。
- 不做 Feature bridge。
- 不删除旧路径。

完成后才能进入后续实现。

### Phase 2 — Authoring 与核心 Data runtime

#### SDD-0013: DataOS Descriptor Authoring and Snapshot Schema

可在 SDD-0012 DTO/catalog 契约稳定后执行。

目标：

- 补齐 `data_key_descriptor` schema。
- 更新 generator 输出 `runtime_snapshot.json.descriptors`。
- 实现 validator。
- 建立 fixture snapshot。

Subagent 可辅助只读分析 DataOS schema/generator/validator 现状，但主会话统一落地。

#### SDD-0014: Data Runtime Slot and Policy Model

目标：

- `DataSlot`。
- `DataValueConverter`。
- descriptor default。
- write/range/allowed values policy。
- typed handle 与内部 string API 收口。

不实现完整 modifier 和 compute。

### Phase 3 — Modifier、Compute、Snapshot runtime

#### SDD-0015: Data Modifier Runtime and Feature Bridge

目标：

- modifier pipeline。
- `modifier_policy`。
- `RemoveModifiersBySource`。
- `Feature.Modifiers` as `authoring_blob`。
- Feature granted/removed bridge。

Feature 只改 Data 输入，不计算 derived/computed 输出。

#### SDD-0016: Data Compute Resolver Runtime

目标：

- `IDataComputeResolver`。
- `DataComputeRegistry`。
- dependencies。
- cycle detection。
- cache。
- transitive dirty。
- computed readonly。

resolver 禁止写 Data、发事件、创建实体、访问场景树或启动 timer。

#### SDD-0017: Runtime Snapshot Record Apply and Entity Bootstrap

目标：

- `RuntimeDataSnapshot` DTO。
- `LoadFromJson`。
- `DataApplyReport`。
- `ApplyRecord`。
- `DataRuntimeBootstrap`。
- Entity/Data 初始化。

records 中 unknown key 不能自动创建 `DataDefinition`。

### Phase 4 — 字段迁移

#### SDD-0018: Data Descriptor Migration and Generated Handles

这是最适合 subagent 分模块审计的阶段，但最终主会话统一合并。

迁移顺序：

1. Base / Unit
2. Attribute
3. Movement
4. Ability
5. Feature
6. AI / Test

Subagent 输出统一格式：

```text
模块：
旧字段来源：
建议 stable_key：
value_type：
default：
storage_policy：
write_policy：
modifier_policy：
compute_id：
dependencies：
需要 generated DataKey<T>：
调用点：
风险：
验证建议：
```

主会话负责：

- 统一 stable_key 命名。
- 统一 descriptor 写入。
- 统一 generated `DataKey<T>` thin handle 格式。
- 统一调用点迁移。
- 统一 grep gate。

### Phase 5 — 删除旧路径与最终门禁

#### SDD-0019: Data Legacy Path Removal and Test Scene Rebuild

必须等 SDD-0012~SDD-0018 完成后执行。

目标：

- 删除或迁出旧 Data 输入职责。
- 删除旧 DataNew DTO 路径。
- 移除旧 DataMeta/DataRegistry 定义事实源。
- 重建 Data Godot 单场景测试。
- 同步长期文档和 Data skill。
- 跑最终 grep/build/test/scene/SDD validate 门禁。

不建议 subagent 并行删除；subagent 只做只读 grep/readiness report。

## 每个 SDD 的固定执行循环

对每个 SDD：

1. 读取当前 SDD 的 `execution-prompt.md`。
2. 读取 `tasks.md`，从当前任务开始。
3. 改源码前运行对应 git 边界的 `git status --short`。
4. RED：先写失败测试或记录缺口。
5. GREEN：实现最小代码使测试通过。
6. REFACTOR：清理命名、边界和重复逻辑。
7. 更新 `tasks.md` checkbox。
8. 更新当前 SDD `progress.md`。
9. 必要时更新项目 `progress.md` 和 `roadmap.md`。
10. 运行验证。
11. 运行 `python3 Workspace/SDD/sdd.py validate <当前SDD>`。
12. 只有当前 SDD 完成并验证通过后，才进入下一个 SDD。

## 推荐验证命令

SDD 文档与索引：

```bash
python3 Workspace/SDD/sdd.py index
python3 Workspace/SDD/sdd.py validate <当前SDD>
python3 Workspace/SDD/sdd.py validate --all
git diff --check -- SDD/INDEX.md SDD/catalog.json SDD/project/projects/PRJ-0002-ecs-framework-refactor
```

框架构建/测试：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```

Godot 场景验证：

```bash
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

注意：框架命令在 `/home/slime/Code/SlimeAI/SlimeAI` 或对应游戏仓执行，不能跨 git 边界混用。

## 最终验收标准

Data 完整重构完成时必须满足：

- **Descriptor-first**: 所有运行时字段定义来自 snapshot descriptors。
- **No runtime fallback**: 无 DataMeta/DataRegistry runtime fallback。
- **Generated handles only**: `DataKey<T>` 是 generated thin handle，不是字段定义事实源。
- **Feature boundary clear**: Feature 只授予/回滚 modifiers，不计算 computed。
- **Compute boundary clear**: computed 由 resolver 负责，有 dependencies、cache、dirty 和 readonly 规则。
- **Snapshot apply strict**: records unknown key/type mismatch/policy violation 有结构化错误。
- **Old paths removed**: 旧 Data 输入路径和 DataNew 不再参与新系统。
- **Tests rebuilt**: Data 小测试和 Godot Data smoke 覆盖新系统，不修补旧测试。
- **Docs/Skill synced**: 长期文档和 Data 相关 skill 与代码一致。
- **Validation passed**: 构建、测试、grep gates、SDD validate 均通过或有明确范围外阻塞记录。

## 下一步

从 `SDD-0012` 开始：

```text
读取 sdds/002-SDD-0012-data-system-full-rewrite-catalog-tdd-slice/execution-prompt.md，并从 T1.1 执行。
```
