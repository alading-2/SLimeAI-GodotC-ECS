# Data Runtime Generic Slot Hard Cutover

## Goal

把 Runtime Data 主链路从 `object?` 值槽切到泛型槽位，降低 typed `DataKey<T>` 读写、modifier、computed cache 的装箱拆箱和动态转换风险。

本轮目标：

- `Data.Set<T>(DataKey<T>, T)` / `Data.Get<T>(DataKey<T>)` 对非 computed 字段直接命中 `DataSlot<T>`。
- 数值 modifier 的基础值、有效值和 slot cache 以 `T` 保存，不再通过 `object? -> double -> object?` 作为存储主链路。
- computed 仍兼容现有 resolver 注册表，但 runtime cache 进入 typed `DataSlot<T>`；resolver 返回值只在边界转换一次。
- `SetUntyped`、snapshot loader、debug/TestSystem dump 暂留 Data 边界，并明确标注装箱/GC 风险。
- 只修改 Data owner 范围；Event、Feature、Ability、ObjectPool、TargetSelector、Logger 不在本 SDD 改。

## Context

### Context Read

- Git Boundary: `/home/slime/Code/SlimeAI/SlimeAI`
- Worktree: none。用户要求在当前仓按 Data 方案继续执行；创建临时 worktree 会增加 SDD/AI config 同步噪音，本轮在干净工作区直接改。
- Baseline Status: SDD 创建前 `git status --short` clean；创建并启动后只有 SDD index/catalog 和新 SDD 目录改动。
- Submodule Boundary: 不涉及游戏仓和 submodule。
- Workflow: `NewFeature` / large SDD refactor。
- Owner skills: `ecs-data`、`data-authoring`；实现只触及 Data runtime，不改 DataOS schema / seed 时不运行生成器。
- 已读事实源：
  - `Workspace/SystemAgent/Routes/NewFeature.md`
  - `Workspace/SystemAgent/Actors/DeepThink.md`
  - `Workspace/SystemAgent/Actors/DesignCritic.md`
  - `Workspace/SystemAgent/Actors/Planner.md`
  - `Workspace/SystemAgent/Actors/TestDesigner.md`
  - `Workspace/SystemAgent/Rules/ReviewGates.md`
  - `Workspace/SystemAgent/Rules/Documentation.md`
  - `Workspace/SDD/docs/SDDFormat.md`
  - `Workspace/SDD/docs/CLI.md`
  - `Workspace/SDD/docs/ValidationRules.md`
  - `DocsAI/ECS/Runtime/Data/Data系统说明.md`
  - `design/00-总览与AI-first裁决.md`
  - `design/01-Data运行时object去除设计.md`
  - `design/01-DataSlot结构化装箱.md`
  - `Src/ECS/Runtime/Data/*.cs`
  - `Src/ECS/Runtime/Data/Tests/DataOS/*.cs`

### Problem Shape

当前 Data 已完成 descriptor-first / snapshot-first / generated `DataKey<T>`，但 storage 仍在热路径丢失泛型信息：

- `DataRuntimeStorage.TrySet<T>` 调 `TrySetUntyped(key.StableKey, value, ...)`，值类型写入会进入 `object?` 参数。
- `DataSlot.Value object?` 保存所有运行时基础值。
- `DataSlot.GetEffectiveValue(): object?` 和 `DataValueConverter.ConvertForRead(object?)` 让读取继续走动态转换。
- `DataSlot.ApplyModifiers(object?)` 把数值拆成 `double` 后再按 descriptor 装回 `object`。
- `_computedCache Dictionary<string, object?>` 保存 computed 输出。
- `DataChangeRecord` 和 `GameEventType.Data.PropertyChanged` 仍承载 `object?` old/new，但这是 Event/diagnostic 边界，本轮不强行改 Event 协议。

### Main Risks

- **范围膨胀**：如果本轮同时删除 `PropertyChanged(object?)` 或改 Feature/Ability context，会跨到 Event/Capability SDD。处理方式：只在 Data runtime 内部 typed 化，公共事件协议暂留。
- **loader 兼容边界**：snapshot loader 当前只能传 untyped 值。处理方式：保留 `SetUntyped`，但让它只作为边界 API，内部转换后写 typed slot。
- **computed resolver 迁移成本**：一次性把所有 resolver 改成 `IDataComputeResolver<T>` 会牵动 Feature test 和 registry。处理方式：本轮先让 computed slot/cache typed 化，resolver 返回 object 只作为 registry 边界；完整 `IDataComputeResolver<T>` 可以后续独立收口。
- **测试证据不足**：DataOS Godot scene 只能证明行为，不直接证明无装箱。处理方式：新增 runtime contract 测试检查 slot 形态和 typed API 不走 untyped counter，再用 grep gate 检查主链路。

## Design

### Options

1. **推荐：渐进 hard cutover 到 `DataSlot<T> + IDataSlot`**
   - 优点：符合项目级裁决；保留 dictionary 管理边界；行为风险可由现有 DataRuntimeTestScene 覆盖。
   - 代价：需要在 `DataRuntimeStorage.cs` 内拆出 typed slot、typed converter/policy 和 computed typed helper。

2. **一次性完全改 `IDataComputeResolver<T>`、typed `DataChanged<T>` 和 public untyped 删除**
   - 优点：更接近项目级最终形态。
   - 代价：会跨 Event/Feature/Ability/TestSystem 调用点，违背“先改 data，其他不动”。

3. **只加注释和 grep gate，不重构 storage**
   - 优点：低风险。
   - 代价：不能解决 `DataSlot.Value object?` 和 `Data.Get/Set<T>` 热路径问题。

### Recommendation

采用方案 1。本 SDD 做 Data 内部硬切：`DataSlot<T>` 存储业务值，`IDataSlot` 只暴露 metadata、modifier 操作、diagnostic dump 和边界写入；`Dictionary<string, IDataSlot>` 作为跨类型管理边界可保留。公共 Event payload、Feature/Ability context 和 TestSystem UI 展示留给后续 SDD。

### Target Shape

- `IDataSlot`
  - 暴露 `DataDefinition Definition`、`Type ValueClrType`、`bool HasValue`。
  - 提供 `GetEffectiveValueAsObjectForDiagnostics()` 和 `GetStoredValueAsObjectForDiagnostics()`，只供事件/diagnostic/dump 边界使用。
  - 不提供 `object? Value` 作为业务值入口。
- `DataSlot<T>`
  - 保存 `T?` 或 `T` + `HasValue`。
  - `GetEffectiveValue()` 返回 `T`。
  - `SetValue(T value)` 用 `EqualityComparer<T>.Default` 判等。
  - `TrySetFromBoundary(object? raw, ...)` 先走 converter/policy，再写 typed value。
- `NumericDataSlot<T>`
  - 只用于 `int/float/double` descriptor。
  - modifier 管线内部用 double 计算，但写回 typed `T`，slot/cache 不保存 `object?`。
- `ComputedDataSlot<T>`
  - 缓存 `T _cachedValue` + dirty 状态。
  - resolver 边界返回 object 后立即转换成 `T`。
- `DataRuntimeStorage`
  - `_slots: Dictionary<string, IDataSlot>`。
  - `TrySet<T>(DataKey<T>, T, ...)` 直接解析 definition -> `DataSlot<T>` -> typed policy -> `SetValue(T)`。
  - `Get<T>(DataKey<T>)` 直接取 `DataSlot<T>.GetEffectiveValue()`。
  - `SetUntyped` 保留为 loader/debug/TestSystem 边界，注释标明值类型会装箱，不推荐业务调用。

### Must Confirm

- none。本轮用户已明确“按照 Data 的方案生成 SDD 并执行，先改 data，其他不动”。为避免阻塞执行，本 SDD 采用 Data-only 默认假设推进。

### Should Confirm

- 后续是否创建独立 Event SDD 删除 `PropertyChanged(object?)` 和 Event dynamic object。
- 后续是否创建独立 Feature/Ability SDD 删除 `ActivationData/ExecuteResult object?`。

### Defaults I Will Use

- `SetUntyped`、`TrySetUntyped` 和 `GetAll` 暂留 Data boundary，不作为业务热路径。
- `GameEventType.Data.PropertyChanged(string, object?, object?)` 本轮不改，只从 typed slot 在通知边界生成 old/new object。
- `IDataComputeResolver` 公共接口本轮不改，computed slot 内部 typed cache 先落地。
- 不改 DataOS schema、snapshot、generated DataKey，除非编译或测试暴露必须同步。
- 不改游戏仓和 BrotatoLike submodule。

### Not Recommended

- 不引入 `DataRuntimeValue` 多字段 union。项目级设计已裁决该方案冗余，会复制 descriptor 类型信息并制造新动态分发层。
- 不用多字典 `Dictionary<string, float/int/bool>` 作为本轮默认方案；它无法自然覆盖 ResourceRef、string array、modifier list、computed 和 diagnostics。
- 不在本 SDD 删除 Event/Feature object API；那会跨出用户指定的 Data-only 范围。

## Verification

### Standard Answer

- expectedInputs: Data descriptor definitions for float/int/double/bool/string/string_array/object_ref/modifier_list/computed; typed `DataKey<T>` set/get; loader `SetUntyped`; modifier changes; computed dependency dirty.
- expectedObservations: typed set/get creates `DataSlot<T>` or numeric typed slot; typed path does not increment untyped boundary counter; modifier and computed effective values match previous behavior; loader/debug untyped still converts and reports structured errors.
- passCriteria: DataRuntimeTestScene covers typed slot contract, modifier pipeline, computed cache, diagnostics and existing DataOS behavior; grep gate shows `DataSlot.Value object?` and `_computedCache Dictionary<string, object?>` removed from Data runtime.
- failCriteria: typed `DataKey<T>` write still calls `TrySetUntyped`; slot exposes business `object? Value`; modifier/computed behavior regresses; DataOS validator or build fails.
- artifactPath: `Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.cs` plus command outputs recorded in `progress.md`.

### Commands

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
python3 Workspace/SDD/sdd.py validate SDD-0031
python3 Workspace/SDD/sdd.py validate --all
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
rg -n "class DataSlot\\b|object\\? Value|Dictionary<string, object\\?> _computedCache|TrySetUntyped\\(key\\.StableKey, value" Src/ECS/Runtime/Data
```

如果 Godot CLI 可用，追加：

```bash
GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
```
