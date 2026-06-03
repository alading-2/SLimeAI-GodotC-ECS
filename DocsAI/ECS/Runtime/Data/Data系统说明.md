# Data 系统说明

> 状态：当前实现说明；SDD-0022 已把 DataOS snapshot -> generated typed handle -> catalog-bound Data 主链路继续硬化到 projection、diagnostics、catalog freeze 和 runtime record completeness。
> 范围：`Src/ECS/Runtime/Data/`、`Data/DataOS/`、`Data/DataKey/Generated/`、`Src/ECS/Capabilities/*/Events/`、`Src/ECS/Runtime/Event/Global/`、`Src/ECS/Runtime/Data/Events/`、`Data/Config/`、`Data/ResourceManagement/`、`Src/ECS/Runtime/Data/Tests/DataOS/`。
> 设计事实源：`../../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/`。
> 更新：2026-06-01

## 1. 一句话定位

Data 是旧 Godot C# ECS 主线的运行时数据容器。字段定义事实源来自 DataOS descriptor，运行时按下面的链路消费生成结果：

```text
DataOS SQLite authoring
  -> Data/DataOS/Snapshots/runtime_snapshot.json
     -> descriptors  字段定义事实源
     -> records      实体/配置初始值事实源
  -> DataDefinitionCatalog
  -> DataRuntimeBootstrap / RuntimeDataSnapshotLoader
  -> Data.Get / Data.Set / Modifier / Computed / Entity.Events
```

核心原则是：字段定义给 DataOS 和 snapshot 管，运行时只消费生成结果；C# 业务代码通过 typed handle 读写稳定 key。

如果你正在排查当前仍成立的 residual 问题，先读：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/design/main.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/tasks.md`

## 2. 核心概念

### 2.1 Stable Key

Stable key 是字段的长期名字，例如 `BaseHp`、`FinalAttack`、`Feature.Modifiers`。它来自 DataOS descriptor，不应在业务代码里重新手写裸字符串。

新代码优先使用：

```csharp
var hp = entity.Data.Get<float>(GeneratedDataKey.FinalHp);
entity.Data.Set(GeneratedDataKey.BaseHp, 100f);
```

旧 `DataKey` 别名和 `DataKey<T> -> string` 隐式转换已经删除；新代码只写唯一 generated handle。

### 2.2 Descriptor

Descriptor 是字段定义，运行时 DTO 是 `RuntimeDataDescriptorDto`，catalog 实体是 `DataDefinition`。它描述：

| 类别 | 典型字段 |
| ---- | ---- |
| 核心定义 | `stableKey`、`valueType`、`defaultValue` |
| 归属 | `ownerDomain`、`ownerCapability`、`ownerSkill` |
| 写入/存储策略 | `storagePolicy`、`writePolicy`、`migrationPolicy` |
| 运行时约束 | `rangePolicy`、`minValue`、`maxValue`、`allowedValues`、`modifierPolicy` |
| computed | `computeId`、`dependencies`、`computeParams` |
| 展示信息 | `displayName`、`description`、`uiGroup`、`unit`、`format` |

Descriptor 是字段定义事实源。不要再新增 C# metadata 注册作为长期入口。

### 2.3 Catalog

`DataDefinitionCatalog` 是运行时索引。它在启动时注册全部 `DataDefinition`，建立 computed 反向依赖索引，并在 `ValidateAndBuildIndexes()` 成功后冻结注册入口。

它会 fail fast：

- stable key 为空或重复。
- `valueType` / policy 字符串非法。
- computed 没有 `computeId`。
- computed 依赖不存在。
- computed 依赖形成环。
- 绑定了 `DataComputeRegistry` 时找不到 resolver。

### 2.4 DataKey<T>

`DataKey<T>` 是 C# 调用侧的 typed handle：

```csharp
public readonly record struct DataKey<T>(string StableKey)
{
}
```

它只提供类型和 stable key，不承载默认值、范围、modifier、computed 等定义。生成文件是 `Data/DataKey/Generated/DataKey_Generated.cs`。

### 2.5 DataRuntimeStorage 与 DataSlot

`Data` 绑定 catalog 后，实际读写走 `DataRuntimeStorage`：

- 每个字段按需创建 `DataSlot`。
- 未写入时读取 descriptor default。
- 写入时执行 `writePolicy`、类型转换、`allowedValues` 和 `rangePolicy`。
- modifier 存在 slot 内，并按 Additive / Multiplicative / FinalAdditive / Override / Cap 管线计算有效值。
- base value 或 modifier 变化会标脏依赖它的 computed 字段。

默认构造的 `Data` 会绑定 `DataRuntimeBootstrap.Default.Catalog`；需要测试隔离时通过测试基类或显式 catalog-bound helper 创建。运行时 Data 必须始终绑定 `DataDefinitionCatalog`。

### 2.6 Computed Resolver

数据库不保存 C# lambda。Computed 字段只在 descriptor 中声明 `computeId` 和依赖，运行时通过 `DataComputeRegistry` 绑定 resolver。

示例：

```text
FinalAttack
  computeId: AttributeBonus
  dependencies: BaseAttack, AttackBonus
```

`AttributeBonusComputeResolver` 负责读取依赖并返回计算结果。Feature 不直接负责 computed；Feature 只通过 modifier 改变输入字段。

### 2.7 Runtime Snapshot Record

`runtime_snapshot.json.records` 保存实体、系统、技能、Feature 等初始值。`RuntimeDataSnapshotLoader.ApplyRecord` 应用记录时会：

1. 查 catalog，unknown key 进入结构化错误。
2. 校验 record field type 和 descriptor value type。
3. 执行严格转换。
4. 拒绝写入 `computed` / `runtime_only` 字段。
5. 通过 `Data.SetUntyped(..., DataWriteSource.Loader)` 写入。

失败会保留结构化错误，不做旧路径 fallback。

### 2.8 数据目录和协议边界

`Src/ECS/Runtime/Data/` 负责运行时容器行为，`Data/` 负责数据协议和配置输入。不要把两者混成一个入口：

| 目录 | 角色 |
| ---- | ---- |
| `Data/DataOS/` | SQLite authoring、schema、seed、generator 和 `runtime_snapshot.json`，是字段定义和数据输入事实源。 |
| `Data/DataOS/RuntimeModels/` | snapshot projection DTO；不提供查询 facade，不作为 authoring 或 runtime 数据事实源。 |
| `Data/DataKey/Generated/` | descriptor 生成的 typed handle，只保存 stable key 和 C# 类型。 |
| `Src/ECS/Capabilities/*/Events/`、`Src/ECS/Runtime/Event/Global/`、`Src/ECS/Runtime/Data/Events/` | `Entity.Events` / `GlobalEventBus` 的事件名和事件载荷协议。 |
| `Data/Config/` | 系统级配置结构，不等同于 Entity 运行时状态。 |
| `Data/ResourceManagement/` | 资源路径、资源目录和加载入口，不存业务运行时状态。 |

共享业务状态应放进 `Entity.Data`，不要藏在 Component 私有字段里；跨系统命令和流程仍走事件或系统 API，不用 Data 替代 EventBus。

## 3. 怎么改字段

先判断要改的是哪类数据：

- Entity 运行时状态：写 DataOS descriptor，生成 `GeneratedDataKey`，通过 `DataRuntimeBootstrap` / snapshot record 注入。
- 系统级规则或全局配置：优先进入 DataOS 业务表并投影为 `system.config` / `system.preset` snapshot records；不是 Entity 状态时不要强行定义 DataKey。
- 事件协议：放对应 Capability 的 `Events/` 目录或 `Src/ECS/Runtime/Event/Global/`，事件载荷优先保持稳定结构。
- 资源路径或资源目录：放 `Data/ResourceManagement/` / `ResourcePaths` / `ResourceCatalog`；DataOS 和 Data 中保存 `res://` 字符串路径、稳定 id 或关系，不保存 `PackedScene`、Node、Resource 等运行时对象。
- 复杂结构：优先建清晰业务表或关系表，不把稳定结构塞进未约束 JSON；只有 `Feature.Modifiers` 这类明确校验的输入才用 `authoring_blob`。

新增或修改普通 Entity.Data 字段时，按这个顺序：

1. 改 DataOS descriptor authoring：`Data/DataOS/Authoring/DataKeyDescriptors.seed.sql` 或相关 schema / seed。
2. 重新生成 `Data/DataOS/Snapshots/runtime_snapshot.json`。
3. 运行 `Data/DataOS/Tools/generate-data-key-handles.py`，更新 `Data/DataKey/Generated/DataKey_Generated.cs`。
4. 改 Component / System 调用点，使用唯一 generated handle 或 snapshot projection。
5. 补或更新 `Src/ECS/Runtime/Data/Tests/DataOS/` 下的场景测试。

数据约定：

- snapshot records 的稳定 id 必须明确；不要通过名称推断或 fallback 作为新入口。
- 数值型“无限制”哨兵值统一用 `-1`，除非该域已有更具体 enum / flags 约定。
- 概率 authoring 统一使用 `0-100`，计算时再换算成 `0-1`。
- DataOS 数据只存标量、enum 文本、资源路径、稳定 id 和关系表。

不要做：

- 只新增 `const string`。
- 手写 C# 元数据作为新字段定义。
- 在 `DataKey<T>` 上放默认值、范围或 computed 规则。
- 让运行时热路径直接查 SQLite。

## 4. 怎么在代码里用

### 4.1 创建 catalog-bound Data

```csharp
var bootstrap = DataRuntimeBootstrap.Default;
var enemyRecord = bootstrap.FindRecord("unit.enemy", "enemy.yuren");
var data = bootstrap.CreateData(enemyRecord);
data.Set(GeneratedDataKey.BaseHp, 100f);
var finalHp = data.Get<float>(GeneratedDataKey.FinalHp);
```

### 4.2 从 snapshot record 初始化

```csharp
var bootstrap = DataRuntimeBootstrap.Default;
var enemyRecord = bootstrap.FindRecord("unit.enemy", "enemy.yuren");
var data = bootstrap.CreateData(enemyRecord);
```

已有 `Data` 容器可绑定 catalog 并应用 record：

```csharp
var report = bootstrap.ApplyToData(entity.Data, enemyRecord);
if (report.HasErrors)
{
    throw new InvalidOperationException(report.ToSummary());
}
```

### 4.3 读写策略

普通运行时写入使用 `DataWriteSource.Runtime`。Snapshot/loader 写入用 `DataWriteSource.Loader`。系统内部强制写入用 `System`，调试工具用 `Debug`。

`Data.Set(...)` 返回 `false` 通常代表策略拒绝、类型错误、范围错误或值未变化。需要查详细原因时优先看场景测试和 `DataValueConverter`。

### 4.4 Modifier

字段只有 `modifierPolicy` 允许时才能加 modifier：

```csharp
data.TryAddModifier(
    GeneratedDataKey.BaseAttack,
    new DataModifier(ModifierType.Additive, 20f, id: "buff.attack", source: featureInstance));
```

按来源回滚：

```csharp
data.RemoveModifiersBySource(featureInstance);
```

这会触发依赖 computed 字段重新计算。

### 4.5 生命周期和对象池

对象池回收或 Entity 销毁时，`Events.Clear()` 和 `Data.Clear()` 由 `EntityManager` 的统一生命周期处理。业务 Component 不应为了回收自己手动清空整个 Data；只清理自己持有的外部订阅、临时缓存，或按 source 回滚自己授予的 modifier。

## 5. 事件

Data 变更事件统一走 Entity 的事件系统：

```csharp
entity.Events.On<GameEventType.Data.PropertyChanged>(
    evt => { /* evt.Key / evt.OldValue / evt.NewValue */ });
```

绑定 catalog 后，`DataRuntimeStorage.Changed` 会被 `Data.OnRuntimeDataChanged` 转成 `GameEventType.Data.PropertyChanged`。触发场景包括：

- `Set` 成功写入新值。
- `Remove` / `Clear` 清除运行时值或 modifier。
- 添加、移除、按来源回滚 modifier。

读取 computed 不发事件；computed 只在依赖变化时标脏，下一次读取时重新计算。

## 6. 怎么测试

DataOS 测试场景统一放在：

```text
Src/ECS/Runtime/Data/Tests/DataOS/
```

当前四个场景职责：

| 场景 | 覆盖 |
| ---- | ---- |
| `DataCatalogTestScene.tscn` | descriptor 解析、catalog 校验、generated handle、旧定义审计 |
| `DataRuntimeTestScene.tscn` | typed Get/Set、write/range/allowed policy、事件、modifier、computed cache |
| `DataSnapshotApplyTestScene.tscn` | snapshot 反序列化、record apply、结构化错误、bootstrap |
| `DataFeatureBridgeTestScene.tscn` | Feature.Modifiers authoring blob、modifier 影响 computed、默认 resolver |

推荐验证：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly

GODOT=/home/slime/Code/Godot/GodotEngine/4.x/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn
$GODOT --headless --path . --scene res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn
```

`Tools/DataCatalogTdd` 不再作为推荐测试入口；Data 系统回归应进入上面的 DataOS 场景包。

## 7. 常见问题

### 7.1 为什么不直接在 DataKey 写默认值？

因为默认值、范围、modifier、computed 和展示信息都属于 descriptor。放回 C# handle 会重新制造双事实源。

### 7.2 为什么 computed 不交给 Feature？

Feature 有生命周期和副作用，computed 是读值时的纯计算。正确关系是：Feature 通过 modifier 改输入，Data resolver 计算输出。

### 7.3 `Data.Set` 返回 false 怎么查？

先看字段 descriptor：`writePolicy`、`rangePolicy`、`allowedValues`、`modifierPolicy`。Snapshot apply 场景会输出结构化错误；普通 runtime 写入目前返回 bool，不抛详细策略错误。

### 7.4 什么时候可以用裸字符串？

测试 helper、迁移审计和底层 loader 可以用 stable key 字符串。业务系统、Component 和新测试断言优先使用 `GeneratedDataKey` 或局部测试定义的 `DataKey<T>`。
