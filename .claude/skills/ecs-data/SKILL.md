---
name: ecs-data
description: 修改 SlimeAI 历史 ECS 路径下的 Runtime Data、DataKey、DataCatalog、RuntimeDataSnapshot、数据变更事件或 SlimeAIFramework Data 设计时使用。
---

# Runtime Data 入口

## 方向状态

2026-06-16 后 SlimeAI 已裁决弃用 ECS 作为框架身份，正式框架名为 `SlimeAIFramework`。本 skill ID 暂保留为 `ecs-data` 以覆盖旧查询；新设计保留 `Data` 名字，Data 语义收窄为受控共享状态、表格驱动、DataBinding、descriptor 约束和 DataModifier。当前 `Data` 实现仍是现状事实源，但后续重构不再默认执行旧 Data Runtime Simplification / Type Contract / RuntimeId Storage 路线。

## 必读入口

- `DocsAI/ECS/Runtime/Data/Data系统说明.md` — Data 系统当前实现说明
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/README.md` — 2026-06-16 弃用 ECS 方向裁决
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data/README.md` — Data 后续重写方向入口
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data/05-外部方案证据与采纳边界.md` — Godot / Unity Entities / Unreal GAS 对 Data 方案的证据和采纳边界
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md` — Data 后续问题收敛边界
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/` — Data 重构设计包
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/5.Data类型系统重构/00-README.md` — 历史 Data 问题证据，已 superseded
- `Src/ECS/Runtime/Data/` — 当前 Data runtime 实现源码
- `Src/ECS/Runtime/Data/Tests/DataOS/` — DataOS 场景测试

## 源码位置

- `Src/ECS/Runtime/Data/`
- `Data/DataOS/`
- `Data/DataKey/`
- `Src/ECS/Runtime/Data/Events/`
- `Src/ECS/Runtime/Data/Tests/DataOS/`
- 当前实现事实源：`DocsAI/ECS/Runtime/Data/Data系统说明.md`
- 新 Data 方向事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data/README.md`
- 新 Data 外部证据边界：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/10.GodotOOP框架方向/Data/05-外部方案证据与采纳边界.md`
- 历史 Data 重构事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/2.Data系统优化/`

## 规则

- 2026-06-16 后续重构默认按 `Runtime/10.GodotOOP框架方向/Data/README.md` 执行：状态默认留在 Component / Feature / System 内部，证明需要跨功能共享、表格驱动、验证追踪、authoring 或持久化后，才进入 Data。
- 不强制所有字段进入 Data。Feature 内部缓存、索引、临时状态、展示 metadata 和“谁能写”的组织纪律 policy 不默认进 Data；它们应留在 owner system / component / system config / projection / manifest，并补 invalidation、diagnostics 或验证入口。
- 后续 Data 大改必须先冻结 Data 进入条件、authority、DataBinding、Component mirror 和对象池同步规则，不继续从 `SDD-0044`、Data Runtime Simplification、Data Type Contract 或 RuntimeId Storage 孤立开工。
- `write_policy` 这类权限约束不作为长期 runtime enforcement 目标；保留真正影响数据形态的 contract：类型、默认值、computed、modifier、range、allowed values、runtime_only / authoring_blob。
- `Data` 只存运行时状态，不承担 authoring 表职责。
- 新 DataKey 先写 DataOS descriptor，再由 generated handle 暴露 typed `DataKey<T>`；不恢复旧 `DataMeta` / `DataRegistry` 事实源。
- `Data` 绑定 frozen `DataCatalog` 并使用 `DataSlot<T> + IDataSlot`；业务代码只用 `Data.Get/Set/TrySet/Has/Remove(DataKey<T>)`，不要新增 string-key 或 untyped 写入。
- `SetUntyped` / `TrySetUntyped` / `GetAll` 只允许作为 snapshot loader、debug、TestSystem dump 或 obsolete compatibility 边界；值类型进入这些 API 会装箱，不能作为业务热路径。新 diagnostic dump 使用 `GetDiagnosticSnapshot()`。
- system_only / debug_only 字段用 `TrySetSystem<T>` / `TrySetDebug<T>`，不要为了写入来源绕回 stable key string。
- Data 业务变更通知通过 `GameEventType.Data.Changed<T>`；`PropertyChanged(string, object?, object?)` 只允许 TestSystem/debug diagnostic 兼容层使用。
- DataOS SQLite 只在生成 / 校验 / snapshot 阶段使用，运行时热路径读取 `RuntimeDataSnapshot`；snapshot loader 对 unknown key、wrong type、descriptor drift 必须报错。
- Runtime Data 变更至少补可运行的 Data 验证；当前默认走 Godot headless Data 场景、DataOS validator 和 Validation artifact，不新增脱离 Godot 运行语义的测试框架。
- Data owner 使用 `owner=Data`；Data runtime 热路径不默认为每次 `Get/Set` 写日志，snapshot loader / descriptor 校验 / record apply / DataOS validation 才写 artifact 和 structured log。
- Data 测试断言走 `ValidationSession` / artifact / structured log，不新增 `[PASS]` / `[FAIL]` 或裸 stdout marker。
- PRJ-0002 旧 ECS Data 完整重构按 `SDD-0012` → `SDD-0019` 顺序执行；`runtime_snapshot.json.descriptors` 是字段定义事实源，旧 `DataMeta` / `DataRegistry` / 手写 `DataKey` 只允许作为一次性审计输入，不新增长期 adapter 或 runtime fallback。
- 旧 ECS `DataDefinitionCatalog` 切片只做 descriptor catalog、compute resolver 绑定校验和 `LegacyDataAuditReport`；records apply、Entity bootstrap、Feature modifier bridge 和旧路径删除分别留给后续 SDD。
- 旧 ECS `DataRuntimeStorage` 切片负责 descriptor default、typed `DataKey<T>`、write/range/allowed values policy、unknown key fail-fast 和 Data changed 事件桥接；未绑定 catalog 的旧 `Data()` 调用只作为迁移期旧路径保留，不作为新字段入口。
- 旧 ECS `DataRuntimeStorage` modifier 切片负责 `modifier_policy` 校验、Additive/Multiplicative/FinalAdditive/Override/Cap pipeline、source removal 和 dependent computed dirty；Feature 只通过 Data modifier API 授予/回滚输入，不计算 computed。
- 旧 ECS `DataRuntimeStorage` compute 切片负责 `IDataComputeResolver`、`DataComputeRegistry`、computed cache、依赖变化递归 dirty 和基础 resolver 示例；Feature 仍只改输入，computed 输出只由 Data resolver 计算。
- SDD-0032 后，`DataRuntimeStorage` typed `Get/Set<T>` 必须直接命中 `DataSlot<T>`；default value 缓存在 typed slot 内；computed resolver 必须实现 `IDataComputeResolver<T>`；Feature/Buff modifier 回滚使用 `DataModifierSource`，不要新增任意 object source。
- 旧 ECS `RuntimeDataSnapshotLoader` record apply 切片负责 `RuntimeDataSnapshot` / `RuntimeDataRecordDto`、`LoadFromJson`、`DataApplyReport`、`ApplyRecord` 和 `DataRuntimeBootstrap`；record 写入必须先查 `DataDefinitionCatalog`，unknown key / type mismatch / conversion failure / computed 或 runtime_only 写入必须结构化报告，不回退到 `DataRegistry.GetMeta`。
- 旧 ECS `EntityManager.Spawn` descriptor-first 接入只能走显式 `EntitySpawnConfig.RuntimeDataBootstrap + RuntimeDataRecord` 或 `RuntimeDataRecordTable/RuntimeDataRecordId` 分支；未显式传入时保留迁移期 `LoadFromConfig`，避免批量破坏旧 Entity 构造路径。record apply 失败时必须阻断生成并清理实体。
- **Entity 引用 DataKey 必须是 `DataKey<EntityId?>` 或 `DataKey<EntityIdList>`（list 类型由 P1 提供），不允许 `DataKey<IEntity?>` 或 `DataKey<string>` 表达 entity-id**。nullable 默认值用 `null`；非 nullable `DataKey<EntityId>` 默认值必须是静态 `EntityId.Empty`，禁止 `new EntityId(string.Empty)` 或 `new EntityId("")` ad-hoc。
- 写入 typed `DataKey<EntityId?>` 时显式提供类型参数（`Data.Set<EntityId?>(key, entity.EntityId)`）或显式 cast，避免泛型推断歧义。

## 验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn
# Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Data
```
