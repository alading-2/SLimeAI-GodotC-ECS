# Data 残余问题代码修复分解

> 日期：2026-05-30
> 范围：当前仍然成立的 Data 中层契约问题，不包含已经被 SDD-0021 收口的旧兼容入口。
> 关系：本文件是 `03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md` 与 `04-BUG:Data无兼容重构后移动与施法失败根因说明.md` 的代码落地版。
> 目的：把“问题是什么、为什么错、具体改哪些代码、怎么改”写到可执行级别。

## 1. 总体裁决

Data no-compat 已经把大头兼容入口删掉了，但现在还剩下一类更难处理的问题：**中层契约没有硬化**。

这些问题不会再以“旧 DataMeta 还能不能跑”这种形式出现，而会以下面几种方式出现：

- 某类 record 的关键字段没有前移到 snapshot。
- 组件注册期字段晚于 `RegisterComponents()` 才写入。
- generator 仍然手写字段类型或字段名。
- runtime 写入失败只返回 `false`，没有结构化错误。
- `object_ref`、`string_array`、`modifier_list` 的运行时语义还没有彻底统一。
- `DataDefinitionCatalog` 逻辑上被冻结，代码上却仍可继续注册。
- 名称查询仍然能把 display name 当成稳定 identity。

下面按问题拆开写修复动作。

## 2. 组件注册期字段前移

### 2.1 问题

`DefaultMoveMode` 是 Movement 的注册期输入，但当前只在部分 runtime 分支被补写，`unit.player` / `unit.enemy` 的 record 也没有统一携带它。

这会导致：

- 玩家进入场景后，`EntityMovementComponent` 读到默认 `None`。
- 敌人即使在 `OnPoolAcquire()` 写入 `AIControlled`，也晚于 `RegisterComponents()`。

### 2.2 影响代码

- `SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql`
- `SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh`
- `SlimeAI/Data/DataOS/Tools/validate-dataos.sh`
- `SlimeAI/Src/ECS/Base/Component/Movement/EntityMovementComponent.cs`
- `SlimeAI/Src/ECS/Base/Entity/Unit/Player/PlayerEntity.cs`
- `SlimeAI/Src/ECS/Base/Entity/Unit/Enemy/EnemyEntity.cs`

### 2.3 怎么改

1. 在 DataOS authoring 里把 `DefaultMoveMode` 明确列入 `unit.player` 和 `unit.enemy` 的必需字段。
2. generator 为 `unit.player` 输出 `DefaultMoveMode = PlayerInput`，为 `unit.enemy` 输出 `DefaultMoveMode = AIControlled`。
3. validator 增加 required field 检查，不允许缺字段的 record 进入最终 snapshot。
4. `EnemyEntity.OnPoolAcquire()` 不再承担默认移动模式写入职责。
5. `PlayerEntity` 不恢复旧兜底写法。
6. `EntityMovementComponent` 只做断言和报错，不做补写 fallback。

### 2.4 推荐代码形态

```csharp
// EntityMovementComponent 注册期只检查，不补写。
var defaultMode = _data.Get<MoveMode>(GeneratedDataKey.DefaultMoveMode);
if (defaultMode == MoveMode.None)
{
    _log.Error("unit record 缺少 DefaultMoveMode");
    return;
}
SwitchStrategy(new MovementParams { Mode = defaultMode });
```

## 3. Spawn 入口去反射化

### 3.1 问题

`EntitySpawnConfig.Config` 仍是 `object`，`EntityManager.InjectVisualScene()` 还会通过反射从 `Config` 里找 `VisualScenePath`。

这条路径会把 Data stable key 再表达一遍，属于残余兼容边界。

### 3.2 影响代码

- `SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs`

### 3.3 怎么改

1. `InjectVisualScene()` 只允许读取 `RuntimeDataRecordDto` 或显式 `VisualSceneOverride`。
2. 删除 `config.GetType().GetProperty(GeneratedDataKey.VisualScenePath.StableKey)` 这条反射回退。
3. `Config` 继续保留为局部运行参数时，只能承载和 Data 无关的临时选项。
4. 如果确实要保留 `VisualScenePath` 局部覆盖，改成明确命名的 `VisualScenePathOverride`，不要复用 Data stable key。

### 3.4 推荐代码形态

```csharp
if (scene == null && config is RuntimeDataRecordDto record
    && TryReadRecordString(record, GeneratedDataKey.VisualScenePath.StableKey, out var recordPath))
{
    scene = CommonTool.LoadPackedScene(recordPath, $"{entity.Name} 视觉");
}
```

后续不再通过 `object Config` 反射读取 Data 字段。

## 4. 写入失败诊断硬化

### 4.1 问题

`DataRuntimeStorage` 当前在写入失败时只返回 `false`，但丢失了 `TryApplyWritePolicies(..., out string error)` 里生成的错误信息。

AI 修改 Data 时，最难调的不是“失败”，而是“失败为什么发生”。现在这个信息还不够强。

### 4.2 影响代码

- `SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs`
- `SlimeAI/Src/ECS/Base/Data/Data.cs`

### 4.3 怎么改

1. 新增 `DataWriteReport` / `DataWriteError`。
2. `TryApplyWritePolicies()` 的错误信息不能只在内部丢弃。
3. `SetUntyped()` / `Set<T>()` 至少提供一个可选 report 入口。
4. 记录 stable key、expected type、actual type、source、policy、raw value。
5. `Set()` 继续保留 bool 版本给旧调用点，但新调用点必须能拿到诊断。

### 4.4 推荐代码形态

```csharp
public sealed record DataWriteError(
    string StableKey,
    string Code,
    string Message,
    DataWriteSource Source,
    string ExpectedType,
    string? ActualType);
```

## 5. 类型契约单一化

### 5.1 问题

`string_array`、`object_ref`、`modifier_list` 现在已经不是纯“类型映射问题”，而是“语言类型、snapshot 表达、validator、generated handle 是否一致”的问题。

### 5.2 影响代码

- `SlimeAI/Src/ECS/Base/Data/DataRuntimeStorage.cs`
- `SlimeAI/Data/DataOS/Tools/generate-data-key-handles.py`
- `SlimeAI/Data/DataOS/Tools/validate-dataos.sh`
- `SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs`

### 5.3 怎么改

1. `string_array` 的唯一标准运行时类型保持 `string[]`。
2. `DataValueConverter.ConvertStringArray()` 继续支持从字符串拆分，但存储和读取都应以 `string[]` 为标准。
3. `modifier_list` 继续以 `FeatureModifierEntryData[]` 为唯一标准运行时类型。
4. `object_ref` 需要区分资源引用和运行时对象引用，不允许再“看起来都是 object_ref 就算了”。
5. `generate-data-key-handles.py` 只按 descriptor 和 `runtime_type_id` 生成唯一 typed handle，不再靠人工约定补语义。

### 5.4 推荐边界

- 资源：`ResourceRef`
- 运行时节点：`Godot.Node2D` 或明确的 runtime ref 句柄
- 数组：`string[]`
- 修改器：`FeatureModifierEntryData[]`

## 6. Catalog 冻结

### 6.1 问题

`DataDefinitionCatalog.ValidateAndBuildIndexes()` 逻辑上是在冻结索引，但 `Register()` 仍然公开可用。

这意味着 catalog 的一致性依赖调用约定，不是代码硬约束。

### 6.2 影响代码

- `SlimeAI/Src/ECS/Base/Data/DataDefinitionCatalog.cs`

### 6.3 怎么改

1. 增加 `_isFrozen` 字段。
2. `ValidateAndBuildIndexes()` 完成后置为 frozen。
3. `Register()` 在 frozen 状态直接 throw。
4. 默认 bootstrap 构建完成后不允许二次注册。

### 6.4 推荐代码形态

```csharp
if (_isFrozen)
{
    throw new InvalidOperationException("DataDefinitionCatalog 已冻结，禁止继续 Register。");
}
```

## 7. 名称查询收口

### 7.1 问题

`RuntimeDataRecordQuery.GetRequiredByName()` 仍然允许把 display name 当稳定查询入口。

这对 UI 有用，对运行时 identity 不安全。

### 7.2 影响代码

- `SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataRecordQuery.cs`
- `SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/DataRuntimeBootstrap.cs`
- `SlimeAI/Src/ECS/Base/System/TargetingSystem/TargetingManager.cs`
- `SlimeAI/Src/ECS/Base/System/TestSystem/Spawn/SpawnTestModule.cs`

### 7.3 怎么改

1. 生产链路只保留 `table/id` 查询。
2. `GetRequiredByName()` 改成 debug/editor helper，或者至少改名为 `GetRequiredByDisplayNameForDebug()`。
3. 生产调用点改为显式 record id。
4. 只在测试或编辑器工具里允许 name lookup。

## 8. 本文件对应的文档更新

这篇文档对应的后续更新目标是：

- `DocsNew/ECS/Data/Data系统说明.md`
- `Src/ECS/Base/System/Movement/README.md`
- `Src/ECS/Base/System/Movement/EntityMovementComponent说明.md`
- `Src/ECS/Base/System/Core/README.md`

`SlimeAI/DocsAI` 已裁决为旧入口，并且目录已经删除；不要恢复或继续局部修补。当前修补清单只面向 `Src/ECS/**` 旁文档、DocsNew、SDD design 和相关 skill/rule。

其中最需要优先纠正的是会直接引导 AI 回到旧写法的页面。
