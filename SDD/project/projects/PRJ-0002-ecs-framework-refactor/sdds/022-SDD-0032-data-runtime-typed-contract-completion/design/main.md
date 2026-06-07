# Data Runtime Typed Contract Completion

## Goal

本 SDD 解决 SDD-0031 后剩余的 Data typed contract 债务：Data 主存储已经是 `DataSlot<T> + IDataSlot`，但业务调用、system write、computed resolver、change event、modifier source 和 diagnostic API 仍有 object/string 宽口。

目标：

- 业务 Capability 和 AI 行为树不再通过 `Data.Get<T>(string)`、`Data.Add(string)`、`TrySetUntyped(stableKey, ...)` 作为普通路径。
- Data system/debug 写入保留 `DataWriteSource` 语义，但入口必须接受 `DataKey<T>`。
- computed resolver 输出 typed 化，resolver 类型不匹配 fail-fast。
- Data change 事件提供 typed payload；`PropertyChanged(object?)` 仅保留 diagnostic compatibility。
- `GetAll()` 改为明确 diagnostic snapshot API，兼容包装标记为边界。
- modifier source 从任意 object identity 收口为稳定 typed source id。
- Energy / Ammo 因 `AbilityCostType` 已存在，按 DataOS descriptor-first 补 typed key，不手写 generated key。

非目标：

- 不追求全仓零 object。
- 不把 snapshot DTO、loader、RuntimeDataRecordDto、RuntimeDataDescriptorDto 泛型化。
- 不把 BrotatoLike 专属资源默认写入框架 seed。
- 不做 array-backed storage、field id 或传统 chunk/archetype ECS 重构。

## Context Read

已读事实源：

- `AGENTS.md`
- `DocsAI/README.md`
- `DocsAI/ECS/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/INDEX.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/ECS框架优化/0.ECS框架的思考/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/ECS框架优化/0.ECS框架的思考/01-Data作为ECS框架核心的概念复盘与方案批判.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/progress.md`
- ecs-data owner skill 源
- `Src/ECS/Runtime/Data/**`、DataOS descriptor seed / generator、Ability Cost、AI FindEnemy、Unit Recovery / Lifecycle、Feature modifier 调用点和 DataOS tests。

Git Boundary：`/home/slime/Code/SlimeAI/SlimeAI`。Worktree：none，当前任务在用户指定框架仓直接执行；创建 SDD 前工作区 clean。

## Problem Shape

SDD-0031 完成的是 Data runtime 值主存储切换，不等于 Data contract 终局。当前风险分为三类：

- 业务误用：`CostComponent` 用 `resourceKey string` 和 `Data.Add(string)`；`FindEnemyAction` 用 `TrySetUntyped(GeneratedDataKey.TargetNode.StableKey, ...)` 绕过 typed handle。
- 协议债务：`IDataComputeResolver.Compute(): object?`、`DataChangeRecord(object?)`、`PropertyChanged(object?)`、`DataModifier.Source object?` 仍会让业务和 AI 调用方失去 typed contract。
- 合理边界：snapshot DTO、loader、DataValueConverter、TestSystem / migration diagnostic dump 需要跨类型或 JSON 输入，允许继续使用 object，但 API 名称必须说明边界和装箱成本。

## Main Risks

- 改动面跨 Runtime Data、Ability、AI、Unit、Feature、DataOS 和测试，容易引入行为回归。
- `GameEventType.Data.Changed<T>` 作为泛型事件依赖现有 EventBus 是否支持 record struct 泛型类型；实现时必须用现有 `Entity.Events.On<T>` 模式验证。
- `DataModifier.Source` 改 typed source 需要兼容旧构造，避免一次性破坏过多调用点；但新业务入口必须改用 `DataModifierSource`。
- Energy / Ammo 只能补通用 descriptor 默认值，不把具体游戏资源上提为框架默认记录。
- diagnostic `GetAll()` 不能硬删，否则 TestSystem / migration 会断；本轮以 `GetDiagnosticSnapshot()` 主入口 + obsolete wrapper 迁移调用点。

## Options

### Option A：一次性全仓零 object

拒绝。DTO、loader、diagnostic、Godot bridge 中 object 是合理边界；强行泛型化会制造更复杂的包装和低收益重构。

### Option B：只清理 CostComponent / FindEnemyAction

不推荐。能消除最明显业务 string 调用，但 computed、change event、modifier source 会继续给 AI 和业务代码保留错误范例，后续仍会复制 object/string 协议。

### Option C：typed contract completion 小闭环

采用。保留边界 object，收口业务和公共协议：typed system write、typed computed resolver、typed change event、diagnostic API 命名、typed modifier source，并补测试和 grep gate。

## Recommendation

采用 Option C。它不追求形式纯洁，但能把 Data 作为 AI-first typed runtime state protocol 的主链路补齐，且验证面可以落在现有 DataOS Godot 场景和 grep gate 上。

## Must Confirm

无需要等待用户确认的问题。用户已明确要求“完整推进”并给出默认假设；本 SDD 按该计划执行。

## Should Confirm

- 后续是否把 `LifecycleComponent` 的 `MaxLifeTime` 监听也纳入 typed changed 事件迁移。本轮默认纳入，因为它是业务组件，不是 TestSystem diagnostic。
- 是否继续保留 `Data.Get<T>(string)` internal 入口。本轮默认保留 internal 兼容测试/迁移，但 grep gate 禁止普通业务新增。

## Defaults I Will Use

- `PropertyChanged(string, object?, object?)` 保留并标注为 diagnostic compatibility；TestSystem 可以继续监听，业务组件迁往 typed/domain event。
- `DataModifierSource` 最小实现为 readonly record struct，内部保存稳定字符串 SourceId；从 feature / action 统一生成 `feature:<entityIdOrName>` 风格源，兼容旧 object 构造但标记 obsolete/boundary。
- `CurrentEnergy` / `CurrentAmmo` descriptor 与 `CurrentMana` 对齐：`float`、default `0`、persisted、read_write、validate、min `0`。
- `TargetNode` 继续是 runtime_only / system_only object_ref，不降低 write policy。
- `GetDiagnosticSnapshot()` 返回 boxed dictionary，注释说明仅 diagnostic/TestSystem/migration 使用。

## Not Recommended

- 不删除 loader / snapshot `object?`。
- 不把 `DataDefinition.DefaultValue` 改成泛型 DTO。
- 不用裸字符串临时支持 Energy / Ammo。
- 不把 Feature modifier 回滚继续依赖任意 object identity。

## Design

### 1. 业务 string/untyped 调用收口

`CostComponent` 新增 typed resource resolver：

```csharp
private static bool TryGetResourceKey(AbilityCostType type, out DataKey<float> key)
```

Mana -> `GeneratedDataKey.CurrentMana`，Health -> `GeneratedDataKey.CurrentHp`，Energy / Ammo -> 新增 DataOS descriptor 后生成 `GeneratedDataKey.CurrentEnergy` / `GeneratedDataKey.CurrentAmmo`。检查与扣除都走 `caster.Data.Get(key)` / `caster.Data.Add(key, -CostAmount)`。

`FindEnemyAction` 改为 `TrySetSystem(GeneratedDataKey.TargetNode, node2D, out report)`。

### 2. typed source-aware write API

`Data` / `DataRuntimeStorage` 暴露最小 public API：

- `TrySetSystem<T>(DataKey<T> key, T value, out DataWriteReport report)`
- `SetSystem<T>(DataKey<T> key, T value)`
- `TrySetDebug<T>(DataKey<T> key, T value, out DataWriteReport report)` 仅在测试/diagnostic 需要时提供。

### 3. typed default cache

`DataSlot<T>` 创建时把 descriptor default 转成 `_defaultValue`，并记录 `_hasDefaultValue`。未写入读取直接使用缓存；对 `string[]` 和 `FeatureModifierEntryData[]` 这类可变数组，返回和存储时 clone，避免调用方污染全局默认。

### 4. typed computed resolver

`IDataComputeResolver` 只保留 metadata：`ComputeId`、`OutputClrType`。新增 `IDataComputeResolver<T>`：

```csharp
T Compute(Data data, DataDefinition definition);
```

`DataComputeRegistry.GetRequired<T>(stableKey, computeId)` 做类型检查；类型不匹配时错误包含 stable key、compute id、expected T、actual resolver output type。内置 resolver 全部返回 `float`。

### 5. typed changed event

Runtime storage 发出 `DataChangeRecord<T>` typed 事件，同时保留 `DataChangeRecord` diagnostic 记录。`Data.OnRuntimeDataChanged<T>` 先发 `GameEventType.Data.Changed<T>`，再发 diagnostic `PropertyChanged`。

`RecoveryComponent` 监听 `Changed<float>` 并按 key 判断 `FinalHpRegen` / `FinalManaRegen`；`LifecycleComponent` 监听 `Changed<float>` 处理 `MaxLifeTime`。

### 6. diagnostic snapshot

新增 `GetDiagnosticSnapshot()` / `GetAllValuesForDiagnostics()`；迁移 `EntityManager_Migration`、Runtime tests 和 Data 内部 `ApplyDataAsModifiers`。保留 `GetAll()` obsolete wrapper，仅作为边界兼容。

### 7. typed modifier source

新增 `DataModifierSource`，`DataModifier.SourceId` 作为长期 source identity。`RemoveModifiersBySource(DataModifierSource source)` 为主入口；object 版本保留 obsolete compatibility。FeatureSystem、ApplyModifierAction、RemoveModifierAction 和 Data feature bridge tests 改用 typed source。

## Verification

必须执行：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate --all
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash skill-test lint static all --no-fail --summary-only
```

可用时执行四个 DataOS Godot 场景：

```bash
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn
```

grep gate：

```bash
rg -n "Data\\.Get<.*\\(.*StableKey|Data\\.Get<.*\\(\\s*\\\"|\\.Data\\.Add\\(.*resourceKey|TrySetUntyped\\(GeneratedDataKey|GetAll\\(" Src/ECS/Capabilities Src/ECS/Runtime -g '*.cs'
rg -n "DataSlot\\.Value|_computedCache Dictionary<string, object\\?>|TrySetUntyped\\(key\\.StableKey, value" Src/ECS/Runtime/Data
rg -n "PropertyChanged\\(string Key, object\\?|DataChangeRecord\\(string StableKey, object\\?|IDataComputeResolver\\s*$|object\\? Compute\\(" Src/ECS/Runtime/Data Src/ECS/Capabilities -g '*.cs'
```
