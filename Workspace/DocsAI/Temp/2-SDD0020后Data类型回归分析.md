# SDD-0020 后 Data 类型回归分析

## 结论摘要

`Workspace/DocsAI/Temp/1.md` 中的新问题不是单个组件的局部异常，而是 SDD-0020 把运行时从旧 Data fallback 切到 descriptor-first / snapshot-first 后，暴露了 Data 类型契约没有端到端统一的问题。

当前至少有两条已确认回归链路：

1. `AvailableAnimations` descriptor 是 `string_array`，但生成的 `DataKey` 是 `DataKey<string>`，组件写入 `List<string>`，读取 `List<string>`，而 `DataValueConverter` 只认可 `string` / `string[]`。结果 `UnitAnimationComponent` 在注册阶段报 `Data.Get 类型不匹配`。
2. `AbilityIcon` descriptor 是 `object_ref`，但 runtime snapshot record 生成器把 record field type 写成 `string`。`RuntimeDataSnapshotLoader.ApplyRecord` 现在严格要求 record field type 等于 descriptor value type，因此 `ability.dash` apply 被拒绝，技能实体创建失败。

这两条都和 SDD-0020 的方向一致：旧宽松 fallback 退出后，类型漂移不再被吞掉。问题不应通过恢复 fallback 解决，而应收口 descriptor、snapshot、typed handle、converter 和调用点的类型契约。

## 复现证据

原始日志：`Workspace/DocsAI/Temp/1.md`

关键错误：

```text
[20:17:37][ERROR][EntityManager_Component] Component 回调失败: UnitAnimationComponent, 错误: Data.Get 类型不匹配：AvailableAnimations expected=StringArray, actual=List`1
```

该错误在 Player 和 Enemy 注册 `UnitAnimationComponent` 时各出现一次。随后技能创建失败：

```text
[20:17:37][ERROR][EntityManager] DataApplyReport failed: ability/ability.dash, applied=19, errors=1
- snapshot.type_mismatch AbilityIcon: record field type 与 descriptor 不一致：string != ObjectRef
[20:17:37][ERROR][EntityManager_Ability] 创建技能实体失败: 冲刺
```

这说明主场景可以启动系统和进入 Gameplay，但在实体组件注册和 Ability record apply 时被严格 Data 类型校验挡住。

## 调用链 1：AvailableAnimations

### 事实

Descriptor seed：

```text
Data/DataOS/Authoring/DataKeyDescriptors.seed.sql
('AvailableAnimations', ..., 'string_array', ...)
```

生成后的 typed handle：

```csharp
Data/DataKey/Generated/DataKey_Generated.cs
public static readonly DataKey<string> AvailableAnimations = new("AvailableAnimations");
```

生成器映射：

```python
Data/DataOS/Tools/generate-data-key-handles.py
"string_array": "string",
```

写入点：

```csharp
Src/ECS/Base/Component/Unit/Common/UnitAnimationComponent/UnitAnimationComponent.cs
var animNames = new List<string>();
_data.Set(GeneratedDataKey.AvailableAnimations, animNames);
```

读取点：

```csharp
UnitAnimationComponent.cs
var availableAnims = _data.Get<List<string>>(GeneratedDataKey.AvailableAnimations);

AttackComponent.cs
var availableAnims = _data.Get<List<string>>(GeneratedDataKey.AvailableAnimations);
```

Converter 当前契约：

```csharp
DataRuntimeStorage.cs
DataValueType.StringArray => targetType == typeof(string) || targetType == typeof(string[])
```

### 根因

`string_array` 的运行时 CLR 类型没有统一：

- descriptor 说它是 `string_array`。
- generated handle 把它降成 `string`。
- component 代码按 `List<string>` 使用。
- converter 只允许 `string` 或 `string[]`。
- default conversion 对 `StringArray` 仍返回文本，不是数组。

因此 `Data.Set(...)` 写入 `List<string>` 时大概率返回 `false`，但调用点没有检查返回值；随后 `Data.Get<List<string>>` 被 strict compatibility 拒绝，产生日志中的 `expected=StringArray, actual=List'1`。

### 为什么 SDD-0020 后才明显

旧 Data 路线允许无 catalog 字典和宽松对象值；`List<string>` 可以直接放进去再取出来。SDD-0020 后 Data 已 catalog-bound，所有读写都走 descriptor policy 和 `DataValueConverter`，旧对象直通路径被移除，所以类型漂移变成显式错误。

## 调用链 2：AbilityIcon

### 事实

Descriptor seed：

```text
Data/DataOS/Authoring/DataKeyDescriptors.seed.sql
('AbilityIcon', ..., 'object_ref', 'Texture2D', ...)
```

runtime snapshot descriptor：

```json
{
  "stableKey": "AbilityIcon",
  "valueType": "object_ref",
  "runtimeTypeId": "Texture2D"
}
```

runtime snapshot record `ability.dash`：

```json
"AbilityIcon": {
  "type": "string",
  "value": "res://icon.svg"
}
```

record 生成器：

```bash
Data/DataOS/Tools/generate-runtime-snapshot.sh
UNION ALL SELECT ..., 'AbilityIcon', 'string', icon_path, ...
```

apply 校验：

```csharp
RuntimeDataSnapshotLoader.ApplyRecord(...)
if (!TryParseSnapshotValueType(field.Type, out var fieldValueType) || fieldValueType != definition.ValueType)
{
    report.AddError("snapshot.type_mismatch", stableKey, $"record field type 与 descriptor 不一致：{field.Type} != {definition.ValueType}");
    continue;
}
```

### 根因

`object_ref` 字段目前实际承载的是 `res://` 路径字符串，但 record field type 被硬编码为 `string`，而 descriptor value type 是 `object_ref`。SDD-0020 后 apply 层变为 strict type equality，因此它不再接受这个语义漂移。

这里不是 `AbilityIcon` 单字段问题，而是 snapshot generator 的 record field type 仍由手写 UNION 硬编码维护，和 `data_key_descriptor.value_type` 没有绑定。

## 风险面

### 高风险

- 所有 descriptor 为 `string_array` 的字段。当前已知 `AvailableAnimations` 直接触发错误，`System.Dependencies` 等字段也需要确认读写类型是否一致。
- 所有 descriptor 为 `object_ref` 但 record 生成器写 `string` 的字段。已知 `AbilityIcon` 触发错误，`TargetNode` 是 runtime-only 目前不走 record apply，但后续也需要保持语义一致。
- `modifier_list` / `authoring_blob` 字段。当前代码存在 `feature.Data.Get<object>(GeneratedDataKey.FeatureModifiers)`，但 converter 对 `ModifierList` 的兼容性目前偏向 string；如果真实 Feature.Modifiers 进入 runtime apply，也可能出现类似问题。

### 中风险

- `GeneratedDataKey` 类型映射过粗。现在 `string_array`、`modifier_list`、`object_ref` 都映射成 `string`，可以编译，但会掩盖 descriptor 语义，导致调用点靠隐式 string overload 绕过 typed handle。
- 调用点忽略 `Data.Set(...)` 返回值。`UnitAnimationComponent` 写入失败后继续运行，错误延迟到下一次读取才暴露，定位成本变高。

## 修复方向建议

### 方案 A：端到端收紧类型契约（推荐）

把 descriptor value type 作为唯一事实源，统一生成器、converter 和调用点。

具体动作：

1. `generate-data-key-handles.py`
   - `string_array` 映射为 `string[]`。
   - `object_ref` 可继续映射为 `string`，但 record field type 必须是 `object_ref`。
   - `modifier_list` / `authoring_blob` 需要单独设计，不应一律降为 `string`。

2. `DataValueConverter`
   - `StringArray` 写入接受 `string[]`，可短期接受 `IEnumerable<string>` / `List<string>` 并归一化为 `string[]`。
   - `StringArray` 读取 `string[]` 时返回数组；如需要 `IReadOnlyList<string>`，应明确支持，而不是让调用点随意 `List<string>`。
   - `ConvertDefaultValue` 对 `string_array` 应把 `""` / `"[]"` 转为空数组，而不是原样字符串。

3. 调用点
   - `UnitAnimationComponent` 写入 `animNames.ToArray()`。
   - `UnitAnimationComponent` / `AttackComponent` 读取 `string[]` 或 `IReadOnlyList<string>`，不要读 `List<string>`。
   - 写入返回 `false` 时至少记录 error/warn，避免 silent failure。

4. `generate-runtime-snapshot.sh`
   - `AbilityIcon` record field type 改为 `object_ref`。
   - 更好的做法是 active_fields 最终输出 type 时 join `data_key_descriptor`，使用 descriptor 的 `value_type`，只保留少量例外，而不是在每条 UNION 里手写类型。

5. 测试
   - DataOS scene 增加 string_array 写入/读取数组、List 输入归一化、default 空数组。
   - Snapshot apply 增加 object_ref record apply。
   - MainTest 或 Ability scene 覆盖 `ability.dash` 创建成功。

### 方案 B：只放宽 converter（不推荐作为最终方案）

让 `DataValueConverter.IsCompatible` 接受 `List<string>`，让 `ConvertStringArray` 接受 `IEnumerable<string>`，同时让 object_ref 接受 string field type。

优点是改动小，能快速消除日志错误。缺点是它会把 descriptor-first 重新变成“多种类型都行”的弱契约，后续 generated handle 和 snapshot generator 仍会继续漂移。若采用，只适合作为短期热修，并必须补一个后续任务把 generated handle / snapshot type 收紧。

## 推荐最小修复顺序

1. 先修 `AbilityIcon` record type：把 snapshot generator 中 `AbilityIcon` 的 field type 改为 `object_ref`，重新生成/校验 snapshot，确认 `ability.dash` apply 不再失败。
2. 再修 `string_array` 端到端契约：generator 改 `string_array -> string[]`，converter 支持数组默认值和 List 输入归一化，调用点改数组读写。
3. 补 Godot MainTest 回归：`res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn`，关注是否还出现 `Component 回调失败`、`DataApplyReport failed`、`创建技能实体失败`。
4. 扩展 grep gate：
   - `rg -n "Get<.*List<string>.*AvailableAnimations|Set\\(GeneratedDataKey.AvailableAnimations, .*List" SlimeAI/Src`
   - `rg -n "'AbilityIcon', 'string'|\"AbilityIcon\"\\s*:\\s*\\{\\s*\"type\"\\s*:\\s*\"string\"" SlimeAI/Data`
   - `rg -n "\"string_array\": \"string\"" SlimeAI/Data/DataOS/Tools`

## 建议验证命令

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
node /home/slime/Code/SlimeAI/.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/GlobalTest/MainTest/MainTest.tscn --build --errors-only --log-dir .ai-temp/scene-tests/runs
node /home/slime/Code/SlimeAI/.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn --build --errors-only --log-dir .ai-temp/scene-tests/runs
node /home/slime/Code/SlimeAI/.codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn --build --errors-only --log-dir .ai-temp/scene-tests/runs
```

## 当前不建议做的事

- 不要恢复无 catalog `Data` fallback。
- 不要在 `UnitAnimationComponent` 里 catch 后继续吞掉类型错误。
- 不要把 `AbilityIcon` descriptor 改回 `string` 来规避 apply 错误；如果它语义上是资源引用，就应保持 `object_ref`，让 snapshot record 使用同一 value type。
- 不要只改 `GeneratedDataKey.AvailableAnimations` 手写生成物；应改 generator，再重新生成。

## 后续决策点

需要确认 `string_array` 的最终 CLR 表达：

- 若选择 `string[]`：最简单，和当前 converter 初衷一致；调用点用数组或 `IReadOnlyList<string>`。
- 若选择 `IReadOnlyList<string>`：API 语义更好，但 `DataKey<T>` 生成、converter、序列化和 Godot 互操作都需要明确支持。
- 不建议选择 `List<string>` 作为 Data 存储规范，因为它是可变集合，容易绕过 Data changed / dirty 事件。

我的建议是：Data 存储规范采用不可变语义的 `string[]`，调用层需要集合操作时局部转换成 `List<string>`。
