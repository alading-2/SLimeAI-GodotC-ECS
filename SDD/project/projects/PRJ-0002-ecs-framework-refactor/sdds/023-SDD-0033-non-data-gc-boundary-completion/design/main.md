# Non-Data GC Boundary Completion

## Goal

在 Data runtime 已完成 generic slot / typed handle 主链路之后，收口非 Data 区域中已经有明确代码证据的 GC / 装箱边界问题：

- EventBus 的动态 object API 不再作为主协议入口。
- Feature / Ability execute 交界面从 raw object payload / result 改为 typed payload / result helper。
- ObjectPoolManager 使用极小非泛型 runtime interface 管理池，不再用反射调用泛型池方法。
- TargetSelector 先引入查询结果 ownership 和 diagnostics，再承接后续 buffer/lease 复用。

非目标：

- 不追求“全仓零 GC”。
- 不在本轮修改 Logger 字符串插值；Logger 需要 profiler 或具体热路径证据。
- 不重写 ObjectPool 生命周期、parking strategy 或碰撞隔离主设计。
- 不直接池化 `List<T>` 并把 ownership 变得更隐蔽。

## Context

已完成输入：

- SDD-0031 / SDD-0032 后 Data runtime 已完成 typed generic slot 与 Data typed contract，不再把 Data 当成本轮重点。
- 用户指出 Data 曾因避开 object 加了过多变量，ObjectPool 也有类似观感；本 SDD 裁决：ObjectPool runtime state 的多字段表达不等于 Data value union 问题，不能为了减少字段而合并成 opaque object。
- 用户明确指出 Logger 不需要为了“GC 肯定有”而盲改；本 SDD 只处理有明显协议问题或热路径证据的区域。
- PRJ-0002 roadmap 已把非 Data GC/装箱优化拆为 Event + Feature/Ability P0，ObjectPool / TargetSelector P1，Logger P2。

当前代码证据：

- `Src/ECS/Runtime/Event/EventBus.cs` 存在 `OnDynamic(Type, Action<object>)`、`OffDynamic(Type, Action<object>)`、`EmitDynamic(object)` 和 `Action<object>` 分支。
- `Src/ECS/Capabilities/Feature/System/FeatureContext.cs` 存在 `ActivationData object?`、`ExecuteResult object?`、`Source object?`、`Trigger object?`、`ExtraData Dictionary<string, object>`。
- `Src/ECS/Capabilities/Ability/System/AbilitySystem.cs` 使用 `FeatureContext { ActivationData = abilityContext, Source = abilityContext.SourceEventData }`，并从 `featureCtx.ExecuteResult is AbilityExecutedResult` 读取结果。
- `Src/ECS/Capabilities/Feature/System/FeatureSystem.cs` 当前写 `ctx.ExecuteResult = handler.OnExecute(ctx);`。
- `Src/ECS/Tools/ObjectPool/Management/ObjectPoolManager.cs` 用 `Dictionary<string, object>` 保存池，并通过 `GetMethod(...)` 反射调用 Release / GetStats / Cleanup / Clear。
- `Src/ECS/Tools/TargetSelector/*TargetSelector.cs` 当前直接返回 `List<T>`，查询诊断和所有权语义不足。

## Design

### 1. Event dynamic object boundary

EventBus 主 API 保留 typed `On<T>` / `Off<T>` / `Emit<T>`。动态入口不作为业务主链路；如果仍需兼容调试或迁移工具，必须标记为 obsolete 并限制使用范围。完成后 grep gate 允许 obsolete 兼容声明存在，但不允许 Capability 主链路继续依赖 `Action<object>` handler 分支。

### 2. Feature / Ability typed execution boundary

`FeatureContext` 增加 typed payload/result helper：

- 写入：`SetActivationPayload<T>(T payload)`、`SetExecutionResult<T>(T result)`。
- 读取：`TryGetActivation<T>(out T value)`、`GetActivation<T>()`、`TryGetExecutionResult<T>(out T value)`、`GetExecutionResult<T>()`。

旧 `ActivationData` / `ExecuteResult` / `Source` / `Trigger` / `ExtraData` 保留为 obsolete 兼容边界，避免一次性破坏外部旧 handler，但 Ability 主链路必须切到 typed helper。`IFeatureHandler.OnExecute` 不再用 `object?` return 传结果，而是直接写入 `FeatureContext`，由 handler 内部决定是否设置 typed result。

### 3. ObjectPool manager runtime interface

新增 `IObjectPoolRuntime`，暴露 manager 需要的极小非泛型能力：pool name、active/inactive count、`ReleaseUntyped(object)`、`Cleanup(float)`、`Clear()`、`GetStats()`。`ObjectPool<T>` 实现该接口；`ObjectPoolManager` 改为保存 `Dictionary<string, IObjectPoolRuntime>`，删除 manager 层反射。

`ReleaseUntyped(object)` 是跨泛型池管理边界，且只处理引用对象归还，不等同于 Data value hot path 的 object 装箱；错误类型返回 false 或抛出明确错误由 manager 策略决定，不能静默反射失败。

### 4. TargetQueryResult / TargetQueryEngine ownership

新增查询结果类型承载：

- `Items`：当前阶段以只读视图表达 ownership，不暴露可变 `List<T>`。
- `Diagnostics`：至少包含 candidate count、returned count、max target、truncated。
- `Dispose()`：为后续 lease / pooled buffer 预留所有权收口点，当前实现不强制池化。

`TargetQueryEngine` 作为新 facade 先接管 entity/position 查询，旧 `EntityTargetSelector.Query` / `PositionTargetSelector.Query` 可保留为兼容 wrapper，但业务新调用点优先使用 engine。

### 5. Logger 裁决

本轮不改 Logger。字符串插值和日志 message 分配只有在高频热路径、日志级别关闭仍构造消息，或 profiler / grep 能指出具体调用点时才进入 P2 SDD。不得为了“GC 一定有”无原则重写日志。

## Verification

必须执行：

- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`
- `python3 Workspace/SDD/sdd.py validate SDD-0033`
- `python3 Workspace/SDD/sdd.py validate --all`
- `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

grep gates：

- `rg -n "EmitDynamic|OnDynamic|OffDynamic|Action<object>|ActivationData =|ExecuteResult is|ctx.ExecuteResult|object\\? OnExecute" Src/ECS`
- `rg -n "GetMethod\\(\"Release\"|GetMethod\\(\"GetStats\"|GetMethod\\(\"Cleanup\"|GetMethod\\(\"Clear\"|Dictionary<string, object> _pools" Src/ECS/Tools/ObjectPool`
- `rg -n "EntityTargetSelector\\.Query|PositionTargetSelector\\.Query" Src/ECS/Capabilities Src/ECS/Tools`

grep 命中不自动等于失败：obsolete 兼容声明或旧 facade 可以存在，但 Feature / Ability / ObjectPool 业务主链路不能继续使用这些旧宽口。
