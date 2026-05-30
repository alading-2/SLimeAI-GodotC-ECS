---
name: ecs-data
description: 修改 SlimeAI.GameOS Runtime Data、DataKey、DataCatalog、RuntimeDataSnapshot 或数据变更事件时使用。
---

# Runtime Data 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`
- `DocsAI/DataOS/Overview.md`

## 源码位置

- `GameOS/Runtime/Data/`
- `GameOS/Runtime/Event/EventDataChangeSink.cs`
- `GameOS/Runtime/Data/RuntimeDataSnapshot.cs`
- `Tests/SlimeAI.GameOS.Tests/`
- 旧 ECS PRJ-0002 Data rewrite: `/home/slime/Code/SlimeAI/SlimeAI/Src/ECS/Base/Data/`
- 旧 ECS DataOS 场景测试: `/home/slime/Code/SlimeAI/SlimeAI/Src/ECS/Test/SingleTest/ECS/DataOS/`
- 旧 ECS Data 重构事实源: `/home/slime/Code/SlimeAI/SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/`

## 规则

- `Data` 只存运行时状态，不承担 authoring 表职责。
- 新 DataKey 用 `DataKey<T>` 暴露到对应 Runtime / Capability `*DataKeys.cs`，并通过 `FrameworkDataKeys.RegisterAll()` 或 profile `DataCatalog` 进入 active catalog；不新增 `DataMeta` / `DataRegistry` 入口。
- `Data` 绑定 frozen `DataCatalog` 并使用 typed slot；业务代码只用 `Data.Get/Set/TryGet/Has/Remove(DataKey<T>)`。
- Data 变更通知通过 `IDataChangeSink` 和 `Entity.Events`，不要把业务监听写进 `Data` 容器。
- DataOS SQLite 只在生成 / 校验 / snapshot 阶段使用，运行时热路径读取 `RuntimeDataSnapshot`；snapshot loader 对 unknown key、wrong type、descriptor drift 必须报错。
- Runtime Data 变更至少补纯 C# Runtime tests；若影响 Godot Node / 场景加载 / 游戏胶水，追加独立 Godot 验证场景。
- PRJ-0002 旧 ECS Data 完整重构按 `SDD-0012` → `SDD-0019` 顺序执行；`runtime_snapshot.json.descriptors` 是字段定义事实源，旧 `DataMeta` / `DataRegistry` / 手写 `DataKey` 只允许作为一次性审计输入，不新增长期 adapter 或 runtime fallback。
- 旧 ECS `DataDefinitionCatalog` 切片只做 descriptor catalog、compute resolver 绑定校验和 `LegacyDataAuditReport`；records apply、Entity bootstrap、Feature modifier bridge 和旧路径删除分别留给后续 SDD。
- 旧 ECS `DataRuntimeStorage` 切片负责 descriptor default、typed `DataKey<T>`、write/range/allowed values policy、unknown key fail-fast 和 Data changed 事件桥接；未绑定 catalog 的旧 `Data()` 调用只作为迁移期旧路径保留，不作为新字段入口。
- 旧 ECS `DataRuntimeStorage` modifier 切片负责 `modifier_policy` 校验、Additive/Multiplicative/FinalAdditive/Override/Cap pipeline、source removal 和 dependent computed dirty；Feature 只通过 Data modifier API 授予/回滚输入，不计算 computed。
- 旧 ECS `DataRuntimeStorage` compute 切片负责 `IDataComputeResolver`、`DataComputeRegistry`、computed cache、依赖变化递归 dirty 和基础 resolver 示例；Feature 仍只改输入，computed 输出只由 Data resolver 计算。
- 旧 ECS `RuntimeDataSnapshotLoader` record apply 切片负责 `RuntimeDataSnapshot` / `RuntimeDataRecordDto`、`LoadFromJson`、`DataApplyReport`、`ApplyRecord` 和 `DataRuntimeBootstrap`；record 写入必须先查 `DataDefinitionCatalog`，unknown key / type mismatch / conversion failure / computed 或 runtime_only 写入必须结构化报告，不回退到 `DataRegistry.GetMeta`。
- 旧 ECS `EntityManager.Spawn` descriptor-first 接入只能走显式 `EntitySpawnConfig.RuntimeDataBootstrap + RuntimeDataRecord` 或 `RuntimeDataRecordTable/RuntimeDataRecordId` 分支；未显式传入时保留迁移期 `LoadFromConfig`，避免批量破坏旧 Entity 构造路径。record apply 失败时必须阻断生成并清理实体。
- **Entity 引用 DataKey 必须是 `DataKey<EntityId?>` 或 `DataKey<EntityIdList>`（list 类型由 P1 提供），不允许 `DataKey<IEntity?>` 或 `DataKey<string>` 表达 entity-id**。nullable 默认值用 `null`；非 nullable `DataKey<EntityId>` 默认值必须是静态 `EntityId.Empty`，禁止 `new EntityId(string.Empty)` 或 `new EntityId("")` ad-hoc。
- 写入 typed `DataKey<EntityId?>` 时显式提供类型参数（`Data.Set<EntityId?>(key, entity.EntityId)`）或显式 cast，避免泛型推断歧义。

## 验证

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
dotnet build Brotato_my.csproj --no-restore
GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path . --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Test/SingleTest/ECS/DataOS/DataFeatureBridgeTestScene.tscn
```
