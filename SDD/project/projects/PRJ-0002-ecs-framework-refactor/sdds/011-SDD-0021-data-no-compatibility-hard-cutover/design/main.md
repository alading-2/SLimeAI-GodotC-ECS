# Data No-Compatibility Hard Cutover

## Goal

本 SDD 解决 SDD-0020 后新暴露的 Data 类型回归和旧兼容残留。目标不是修两个局部报错，而是让 Data 系统真正只剩一条链路：

```text
DataOS SQLite authoring
  -> generator
  -> runtime_snapshot.json descriptors/records
  -> GeneratedDataKey typed handles
  -> DataDefinitionCatalog / DataRuntimeStorage / DataRuntimeBootstrap
  -> Entity.Data / System projection / ResourceCatalog projection
```

完成后：

- `runtime_snapshot.json.records` 不再作为字段类型第二事实源。
- `validate-dataos.sh` 必须检查最终 snapshot，而不是只检查空的中间表。
- `GeneratedDataKey` 必须按 descriptor 输出真实 CLR 类型。
- `DataKey<T>` 不再隐式转 `string`。
- 业务层不再依赖 `DataKey.Xxx` 兼容 alias 或 public string-key Data API。
- 非标量类型一次性定标准：`string_array -> string[]`，`object_ref` / `modifier_list` 不再伪装成 `string`。
- `DataMeta` / `DataRegistry` / RuntimeTables 兼容语义 / Resource authoring 旧入口从当前事实源退出。

## Context

上游事实源：

- 项目级总审计：`design/README.md`，来源为 `PRJ-0002/design/2.Data系统优化/06-无兼容完全重构总审计/README.md`。
- 运行报错复盘：`PRJ-0002/design/2.Data系统优化/05-Data重构运行报错根因分析.md`。
- 当前临时分析：`Workspace/DocsAI/Temp/2-SDD0020后Data类型回归分析.md`。
- 当前 runtime 源码：`SlimeAI/Src/ECS/Base/Data/`。
- 当前 DataOS 源码：`SlimeAI/Data/DataOS/`。
- 当前 generated handle：`SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs`。

直接故障：

1. `AbilityIcon` descriptor 是 `object_ref`，但 snapshot record 被 generator 写成 `string`，loader strict apply 报 `string != ObjectRef`。
2. `AvailableAnimations` descriptor 是 `string_array`，generated handle 是 `DataKey<string>`，业务写/读 `List<string>`，converter 只接受 `string` / `string[]`，报 `StringArray != List<string>`。
3. `validate-dataos.sh` passed，但 `dataos_runtime_field_stream` 当前为空，未检查最终 `runtime_snapshot.json`。

根本问题：

```text
descriptor 已经成为事实源，但 generator、validator、generated handle、Data API、业务调用点和文档没有全部听 descriptor。
```

## Design

### 1. 先建立红灯门禁

第一步不是直接修组件，而是让错误在生成/验证阶段变红：

- snapshot descriptor/record type mismatch 必须由 validator 拦截。
- generated handle type drift 必须由生成器或校验脚本拦截。
- `DataKey<T> -> string`、generated compatibility alias、`Get<object>(GeneratedDataKey...)`、`Get<List<string>>` 等旧入口必须进入 grep gate。

### 2. generator 不再维护第二套字段类型

`generate-runtime-snapshot.sh` 的 record field type 必须来自 `data_key_descriptor.value_type`。`field_rows` 只能声明业务表、record id、field key 和 value，不允许再重新决定 type。

`legacyTable` / `legacyData` 不应出现在运行时 snapshot。若还需要迁移追踪，输出到单独 audit artifact。

### 3. generated handle 按 descriptor 映射唯一 CLR 类型

初始硬裁决：

| descriptor type | generated type |
| --- | --- |
| `string_array` | `DataKey<string[]>` |
| `object_ref` | 不再是 `DataKey<string>`；定义 `ObjectRef` / `ResourceRef` / `NodeRef` 或按字段迁出 Data slot |
| `modifier_list` | 不再是 `DataKey<string>`；定义 loader-only typed blob 或 `FeatureModifierEntry[]` |

`AbilityIcon` 和 `TargetNode` 都是 `object_ref`，但语义不同：前者是可序列化资源引用，后者是运行时 Node 引用。本 SDD 必须明确边界，不能继续用同一个 `string` 映射盖住问题。

### 4. Data API 只暴露 typed handle 给业务层

业务层目标 API：

```text
Data.Get<T>(DataKey<T>)
Data.Set<T>(DataKey<T>, T)
Data.Has<T>(DataKey<T>)
Data.Remove<T>(DataKey<T>)
Data.Add<T>(DataKey<T>, T)
Data.AddModifier<T>(DataKey<T>, DataModifier)
```

string stable key 入口只允许 loader/test/internal，并且方法名必须表达用途，不再叫普通 `Get<T>(string)` / `Set<T>(string)`。

### 5. 删除兼容命名和旧事实源

必须退出：

- generated `DataKey.Xxx = GeneratedDataKey.Xxx` compatibility aliases。
- `DataKey<T>` implicit string operator。
- public `new Data()` 未绑定 catalog 入口。
- runtime 源码中的 `DataMeta` / `DataRegistry` 长期依赖。
- `Data/DataOS/RuntimeTables` 的兼容语义命名和 README。
- `FeatureDefinition` / `SystemConfig` / `SystemPreset` Resource authoring 旧事实源。
- 非历史文档中“SDD-0020 已完全退出旧路径”的过期结论。

## Verification

最低验证分四层：

### 生成与数据门禁

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
jq -r '[.descriptors[] | {key: .stableKey, type: .valueType}] as $defs | .records[] as $r | ($r.fields // {}) | to_entries[] | .key as $k | .value.type as $rt | ($defs[] | select(.key == $k) | .type) as $dt | select($rt != $dt)' Data/DataOS/Snapshots/runtime_snapshot.json
```

### build / runtime tests

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
```

如果仓库恢复了纯 C# Data test project，则运行对应 tests。否则以 Godot DataOS 场景为准。

### Godot DataOS 场景

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataCatalogTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataSnapshotApplyTestScene.tscn --build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Runtime/Data/Tests/DataOS/DataFeatureBridgeTestScene.tscn --build
```

### 无兼容 grep gate

```bash
cd /home/slime/Code/SlimeAI
rg -n "implicit operator string\\(DataKey|Compatibility aliases|public static partial class DataKey" SlimeAI/Src/ECS/Base/Data SlimeAI/Data
rg -n "DataKey<string> (AbilityIcon|TargetNode|AbilityTriggerEvent|AvailableAnimations|Dependencies|EnabledSystemIds|DisabledSystemIds|FeatureModifiers)" SlimeAI/Data/DataKey/Generated
rg -n "Get<object>\\(GeneratedDataKey|Get<.*List<string>|Set\\(GeneratedDataKey\\.TargetNode|Get<Node2D>\\(GeneratedDataKey\\.TargetNode" SlimeAI/Src/ECS SlimeAI/Data
rg -n "legacyTable|legacyData" SlimeAI/Data/DataOS/Snapshots/runtime_snapshot.json SlimeAI/Data/DataOS/Tools
rg -n "class DataMeta|class DataRegistry|LegacyDataAuditReport" SlimeAI/Src/ECS/Base/Data
```

允许命中只能是历史 SDD / 历史设计文档 / 本 SDD 的问题描述。
