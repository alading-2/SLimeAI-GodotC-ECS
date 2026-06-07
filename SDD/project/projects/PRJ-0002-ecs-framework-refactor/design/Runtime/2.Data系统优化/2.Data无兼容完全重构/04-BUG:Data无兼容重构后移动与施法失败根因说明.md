# Data 无兼容重构后移动与施法失败根因说明

> 日期：2026-05-30
> 范围：旧 ECS `SlimeAI/Src/ECS` DataOS authoring -> generated snapshot / generated DataKey -> Runtime Data -> Entity / Component 初始化链路。
> 目的：解释 Data 无兼容 hard cutover 后仍出现“无法移动、无法施放技能”等问题的原因、证据、解决方向和后续门禁。
> 结论类型：设计包内 Bug 复盘文档，不改变 PRJ-0002 当前主线；Data 主链路仍按 descriptor-first / snapshot-first / no-compat 继续收口。

## 1. 总结

这次问题不是单个 `Data.Get` 或某个组件写错，而是 **Data 数据传递链路缺少“端到端可用性契约”**：

```text
SQLite authoring / seed
    -> generator SQL 投影
    -> runtime_snapshot.json
    -> GeneratedDataKey<T>
    -> RuntimeDataSnapshotLoader.ApplyRecord
    -> EntityManager.Spawn / RegisterComponents
    -> Component 首帧行为
```

Data 无兼容重构删除了旧 `LoadFromConfig`、旧 `DataMeta` 默认值和 Entity 局部兜底后，系统开始严格依赖 snapshot record。这个方向是正确的，但当前链路只证明了“字段定义能生成、record 能应用一部分”，没有证明“组件注册前必须存在的关键字段已经在 record 中覆盖，且类型、时序、读取方式全部一致”。

直接导致移动失败的根因已经明确：

- `DefaultMoveMode` descriptor 默认值是 `None`。
- `unit.player` record 当前没有写入 `DefaultMoveMode = PlayerInput`。
- `EntityMovementComponent` 在组件注册时只读取一次 `DefaultMoveMode`，读到 `None` 就不会进入任何默认移动策略。
- 玩家旧的 Entity 内兜底写入已被注释，不再生效。
- 敌人的 `OnPoolAcquire()` 会写 `DefaultMoveMode = AIControlled`，但对象池 `Activate()` 发生在 `RegisterComponents()` 之后；对“组件注册时读取一次”的字段来说太晚。

施法失败的风险同源：

- Ability 实体同样通过 `EntityManager.Spawn()` 应用 runtime record。
- 如果 ability record 中任一字段与 descriptor / generated key / runtime converter 不一致，`ApplyRecord` 会失败，`Spawn<AbilityEntity>()` 失败，后续手动技能列表自然为空或技能无法触发。
- 即使当前 `runtime_snapshot.json` 已把 `AbilityIcon` 输出为 `object_ref`，generator 的原始 `field_rows` 仍手写了 `AbilityIcon` 为 `string`，说明投影层仍存在“字段类型由两处表达”的漂移风险。

因此，本 Bug 的本质是：**Data no-compat cutover 完成了旧入口删除，但没有同步补齐业务必需字段、生命周期初始化契约和 final snapshot 级验证矩阵。**

## 2. 证据

### 2.1 Movement 组件只在注册时读取默认模式

`SlimeAI/Src/ECS/Base/Component/Movement/EntityMovementComponent.cs:92-97`：

```csharp
var defaultMode = _data.Get<MoveMode>(GeneratedDataKey.DefaultMoveMode);
if (defaultMode != MoveMode.None)
{
    SwitchStrategy(new MovementParams { Mode = defaultMode });
}
```

这段逻辑意味着：

- `DefaultMoveMode == None` 时不会创建默认策略。
- 后续再写入 `DefaultMoveMode` 不会自动触发 `SwitchStrategy()`。
- 这是一次性初始化字段，不是普通运行时状态字段。

### 2.2 玩家旧兜底已注释

`SlimeAI/Src/ECS/Base/Entity/Unit/Player/PlayerEntity.cs:48-50`：

```csharp
// public void OnPoolAcquire()
// {
//     Data.Set(GeneratedDataKey.DefaultMoveMode, MoveMode.PlayerInput);
```

玩家不走对象池，这段旧兜底也已经注释。无兼容重构后，玩家只能依赖 `unit.player` snapshot record 或更早的 spawn bootstrap 写入 `DefaultMoveMode`。当前 `runtime_snapshot.json` 的 `unit.player/player.deluyi` 字段只包含基础属性和移动速度，没有 `DefaultMoveMode`。

### 2.3 敌人写入时机晚于组件注册

`SlimeAI/Src/ECS/Base/Entity/Unit/Enemy/EnemyEntity.cs:52-55`：

```csharp
public void OnPoolAcquire()
{
    Data.Set(GeneratedDataKey.DefaultMoveMode, MoveMode.AIControlled);
}
```

但 `EntityManager.Spawn()` 的顺序是先注册组件，再激活对象池。

`SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs:248-257`：

```csharp
if (!NodeLifecycleManager.IsRegistered(id))
{
    Register(entity);
    RegisterComponents(entity);
}

if (pool != null)
{
    pool.Activate(entity);
```

`SlimeAI/Src/ECS/Tools/ObjectPool/ObjectPool.cs:414-418`：

```csharp
ReattachToTree(node);
ApplyActiveState(node);

if (obj is IPoolable poolable) poolable.OnPoolAcquire();
```

因此敌人链路是：

```text
ApplySpawnData
    -> RegisterComponents
        -> EntityMovementComponent 读取 DefaultMoveMode
    -> pool.Activate
        -> EnemyEntity.OnPoolAcquire 写 DefaultMoveMode
```

这对 `DefaultMoveMode` 这种注册期字段是错误时序。

### 2.4 `DefaultMoveMode` descriptor 默认是 `None`

`SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql:83`：

```sql
('DefaultMoveMode', ..., 'enum', 'MoveMode', 'None', ...)
```

这个默认值适合投射物、静态对象或无移动单位，但不适合玩家和敌人。no-compat 后不能再假设 Entity 子类会自动补默认移动模式。

### 2.5 generator 没有从 unit 表投影默认移动模式

`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh:35-57` 的 `unit.player` 投影覆盖了 `Name`、`Team`、`EntityType`、`MoveSpeed` 等字段，但没有 `DefaultMoveMode`。

`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh:58-89` 的 `unit.enemy` 投影也没有 `DefaultMoveMode`。

这说明 authoring record 到 runtime record 的业务必需字段不完整。

### 2.6 Ability 数据链路仍有类型漂移风险

`AbilityIcon` descriptor 是 `object_ref`：

`SlimeAI/Data/DataOS/Authoring/DataKeyDescriptors.seed.sql:40`：

```sql
('AbilityIcon', ..., 'object_ref', 'Texture2D', ...)
```

当前 generated key 也已经是 `ResourceRef`：

`SlimeAI/Data/DataKey/Generated/DataKey_Generated.cs:142`：

```csharp
public static readonly DataKey<ResourceRef> AbilityIcon = new("AbilityIcon");
```

但是 generator 原始 `field_rows` 仍手写成 `string`：

`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh:104`：

```sql
... 'AbilityIcon', 'string', icon_path, ...
```

最终 snapshot 当前通过 `active_fields` JOIN descriptor 后使用 descriptor type 输出，`runtime_snapshot.json` 中 `ability.dash` 的 `AbilityIcon` 已是 `object_ref`。这说明运行时当前未必还会因为 `AbilityIcon` 直接失败，但它暴露了一个更深的问题：**field projection 仍重复表达 value_type，未来任何字段只要 final snapshot 没有被 descriptor 强制覆盖，或者 validator 跳过 final snapshot，就会重新出现 ApplyRecord 失败。**

### 2.7 Runtime loader 已经严格拒绝类型错误

`SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot/RuntimeDataSnapshotLoader.cs:64-74`：

```csharp
if (!TryParseSnapshotValueType(field.Type, out var fieldValueType) || fieldValueType != definition.ValueType)
{
    report.AddError("snapshot.type_mismatch", stableKey, ...);
    continue;
}

if (!DataValueConverter.TryConvert(rawValue, definition.ValueType, out var convertedValue, out var convertError))
{
    report.AddError("snapshot.conversion_failed", stableKey, convertError);
    continue;
}
```

这是 no-compat hard cutover 应有的行为。问题不在 loader 太严格，而在上游没有保证所有 record 字段和业务必需字段都满足严格契约。

### 2.8 Spawn 失败会直接导致技能不可用

`SlimeAI/Src/ECS/Base/System/AbilitySystem/EntityManager_Ability.cs:102-120`：

```csharp
ability = Spawn<AbilityEntity>(new EntitySpawnConfig
{
    RuntimeDataRecord = runtimeDataRecord,
    RuntimeDataRecordTable = runtimeDataRecordTable,
    RuntimeDataRecordId = runtimeDataRecordId,
    UsingObjectPool = true,
    ...
});

if (ability == null)
{
    _abilityLog.Error($"创建技能实体失败: {abilityName}");
```

`EntityManager.ApplySpawnData()` 在 record 查找或 `ApplyRecord` 失败时返回 false，Spawn 会失败：

`SlimeAI/Src/ECS/Base/Entity/Core/EntityManager.cs:289-313`。

所以施法失败不一定发生在施法动作本身，也可能发生在“技能实体创建 / 技能 record 应用 / 技能关系挂载”阶段。

## 3. 问题分层

### 3.1 直接问题：缺少移动模式 record

`DefaultMoveMode` 是 `EntityMovementComponent` 的注册期输入。它不能只存在于 descriptor 默认值，也不能由对象池 acquire 后补写。

当前实际效果：

```text
玩家：
    snapshot record 没有 DefaultMoveMode
    -> Data.Get(DefaultMoveMode) 返回 descriptor default None
    -> MovementComponent 不创建 PlayerInputStrategy
    -> 输入存在但没有默认移动策略消费

敌人：
    snapshot record 没有 DefaultMoveMode
    -> MovementComponent 注册时读到 None
    -> pool.Activate 后才写 AIControlled
    -> 默认 AIControlledStrategy 没有在注册期创建
```

### 3.2 链路问题：业务必需字段没有 manifest

DataOS 当前知道字段定义，却不知道某类 record 的“组件启动必需字段”。

例如：

- `unit.player` 必须有 `DefaultMoveMode = PlayerInput`。
- `unit.enemy` 必须有 `DefaultMoveMode = AIControlled`。
- 需要主动施法的 ability 必须有 `AbilityType`、`AbilityTriggerMode`、`FeatureHandlerId`、`AbilityFeatureGroup` 等字段，并且类型能被 generated key 和 runtime converter 读取。

如果没有 record completeness manifest，validator 只能检查“出现的字段是否类型正确”，不能检查“必须出现的字段是否出现”。

### 3.3 时序问题：Entity / Pool 生命周期仍承担 Data 初始化职责

无兼容重构后，Data 初始化应该集中在 spawn bootstrap：

```text
EntityManager.Spawn
    -> ApplySpawnData
    -> InjectVisualScene
    -> AddToSceneTree / bind relationship
    -> RegisterComponents
    -> pool.Activate
```

其中 `RegisterComponents` 之前的数据才适合被组件注册期读取。`OnPoolAcquire()` 可以重置运行时状态、绑定事件、恢复可见性和碰撞，但不应写入“组件注册期决定策略”的基础配置。

### 3.4 类型问题：descriptor、snapshot、generated key、runtime converter 必须四方一致

Data no-compat 后，下面四个位置必须完全一致：

```text
data_key_descriptor.value_type / runtime_type_id
runtime_snapshot.records[*].fields[*].type
GeneratedDataKey<T>
DataValueConverter / Data.Get<T> 调用方类型
```

任一处漂移都会在以下位置暴露：

- `RuntimeDataSnapshotLoader.ApplyRecord()`：record type 与 descriptor 不一致。
- `DataValueConverter.TryConvert()`：record value 无法转换到 descriptor type。
- `Data.Get<T>()`：调用方读取类型与 descriptor type 不兼容。
- generated key validator：`GeneratedDataKey<T>` 与 descriptor 不一致。

### 3.5 验证问题：当前门禁还没有覆盖“能玩”的关键路径

DataOS 校验应覆盖三层：

1. **结构层**：descriptor 合法、field type 合法、unknown key 不存在。
2. **生成层**：final `runtime_snapshot.json` 与 generated key 类型一致。
3. **行为层**：玩家能进入 `PlayerInputStrategy`，敌人能进入 `AIControlledStrategy`，ability record 能生成 `AbilityEntity` 并进入手动技能列表。

只有前两层通过，仍可能出现“数据都合法，但组件启动用不到”的问题。

## 4. 为什么旧兼容删除后才集中爆发

旧系统里很多错误被隐式兜底掩盖：

- Entity 子类直接 `Data.Set(...)`。
- `LoadFromConfig` 或旧 Config DTO 提供默认值。
- `DataMeta.DefaultValue` 与业务代码兜底混在一起。
- 裸字符串 key 和宽松转换允许错误延后暴露。

no-compat hard cutover 删除这些兜底后，系统从“多入口最终凑齐数据”变成“snapshot 必须一次给齐”。这是正确方向，但它要求补齐新的契约：

```text
旧兜底删除
    不等于
业务默认值不需要表达

兼容入口删除
    不等于
组件初始化时序自动正确

类型严格校验
    不等于
generator 投影不需要测试
```

这次 Bug 说明 Data 重构完成标准不能只看“旧 API 删除、测试通过、snapshot 可加载”，还必须看“关键业务 record 能驱动真实组件行为”。

## 5. 解决方案

### 5.1 P0：把组件注册期必需字段前移到 snapshot record

修复方向：

- 在 DataOS authoring 中为玩家和敌人明确表达默认移动模式。
- generator 必须为 `unit.player` 输出 `DefaultMoveMode = PlayerInput`。
- generator 必须为 `unit.enemy` 输出 `DefaultMoveMode = AIControlled`。
- `EntityMovementComponent` 不应依赖 `PlayerEntity` / `EnemyEntity` 在注册后补写默认模式。

推荐实现形态：

```text
unit_player.default_move_mode = 'PlayerInput'
unit_enemy.default_move_mode = 'AIControlled'

generator:
    unit.player -> DefaultMoveMode(enum)
    unit.enemy  -> DefaultMoveMode(enum)
```

如果短期不改表结构，也可以在 generator 中按 table 固定投影：

```sql
UNION ALL SELECT 'PlayerData', 'unit.player', id, name,
    'DefaultMoveMode', 'enum', 'PlayerInput', ...

UNION ALL SELECT 'EnemyData', 'unit.enemy', id, name,
    'DefaultMoveMode', 'enum', 'AIControlled', ...
```

长期更推荐加业务列，因为默认移动模式是 unit authoring 内容，不应埋在 generator 常量里。

### 5.2 P0：删除或降级 Entity / Pool 对注册期 Data 的补写职责

修复方向：

- `EnemyEntity.OnPoolAcquire()` 不再负责写 `DefaultMoveMode`，或者只作为断言 / 观测，不作为唯一来源。
- `PlayerEntity` 不恢复注释掉的 `OnPoolAcquire()` 兜底。
- `OnPoolAcquire()` 只处理对象池复用状态：可见性、碰撞恢复、事件复订阅、运行时缓存清理。
- 对组件注册期字段，若缺失，应在 validation 或 spawn apply 阶段失败，而不是在组件里静默使用 `None`。

可以考虑在 `EntityMovementComponent.OnComponentRegistered()` 增加日志断言：

```text
如果 entity 是 IUnit 且拥有 Movement 组件，但 DefaultMoveMode == None：
    输出明确错误：unit record 缺少 DefaultMoveMode
```

注意这不是 fallback，只是把缺数据变成可定位错误。

### 5.3 P0：为 unit / ability record 增加 completeness validation

新增 validator 规则：

```text
unit.player:
    required: Name, Team, EntityType, DeathType, MoveSpeed, DefaultMoveMode
    expected DefaultMoveMode: PlayerInput

unit.enemy:
    required: Name, Team, EntityType, DeathType, MoveSpeed, DefaultMoveMode, DetectionRange
    expected DefaultMoveMode: AIControlled

ability:
    required: Name, AbilityType, AbilityTriggerMode, AbilityFeatureGroup, FeatureHandlerId
    if AbilityType != Passive and AbilityTriggerMode contains Manual:
        record must be discoverable by manual ability query
```

这类规则不能只检查 descriptor 是否存在，必须检查 final `runtime_snapshot.json.records`。

### 5.4 P1：消除 generator 投影中的重复 value_type

当前 `field_rows` 手写了 `value_type`，但 `active_fields` 又 JOIN descriptor 并用 descriptor type 覆盖：

`SlimeAI/Data/DataOS/Tools/generate-runtime-snapshot.sh:180-188`：

```sql
f.field_key,
d.descriptor_type AS value_type,
f.value_text,
```

这能减少 final snapshot 类型漂移，但仍让 `field_rows` 维护了一个容易过期的类型副本。推荐改成：

```text
field_rows 只输出:
    table_id, record_id, field_key, value_text, source_table, source_row_id, source_column

value_type 只来自 data_key_descriptor
```

如果为了 `dataos_runtime_field_stream` 保留 value_type，也必须让 stream 的 value_type 同样来自 descriptor，而不是业务投影手写值。

### 5.5 P1：把 generated handle 校验纳入固定门禁

`validate-dataos.sh` 已有 generated handle 类型检查逻辑：

- `string_array -> string[]`
- `enum + runtime_type_id -> enum type`
- `object_ref -> ResourceRef` 或 `Godot.Node2D`

这类检查必须成为 DataOS 变更后的固定门禁。否则 `GeneratedDataKey<T>` 与 descriptor 漂移时，编译期看似通过，运行时 `Data.Get<T>()` 才爆。

### 5.6 P1：为 ApplyRecord 增加代表性 record 测试

最低测试矩阵：

```text
Apply unit.player/player.deluyi:
    Data.Get<MoveMode>(DefaultMoveMode) == PlayerInput
    Data.Get<float>(MoveSpeed) > 0

Apply representative unit.enemy:
    Data.Get<MoveMode>(DefaultMoveMode) == AIControlled
    Data.Get<float>(DetectionRange) > 0

Apply ability.dash:
    Data.Get<AbilityType>(AbilityType) == Active
    Data.Get<AbilityTriggerMode>(AbilityTriggerMode) contains Manual or expected trigger mode
    Data.Get<ResourceRef>(AbilityIcon) does not throw
```

这些测试不需要启动完整 Godot 主场景，先用纯 C# runtime snapshot loader 覆盖 Data 链路，再用 Godot scene smoke 覆盖组件注册和交互行为。

### 5.7 P2：建立“组件注册期字段”清单

需要把所有在 `OnComponentRegistered()` 或 `_Ready()` 首次读取并决定长期策略的 DataKey 列出来。初步包括：

- Movement：`DefaultMoveMode`
- Animation：`AvailableAnimations`，由视觉节点扫描写入，后续攻击组件读取
- Ability：`AbilityType`、`AbilityTriggerMode`、`AbilityTargetSelection`、`FeatureHandlerId`
- Status：`StatusCanMoveInput`、`StatusCanAttack`、`StatusCanCast`
- AI：`DetectionRange`、`LoseTargetRange`、`PatrolRadius`

每个字段都要标注：

```text
来源：snapshot / visual scan / system runtime / object pool runtime
读取时机：spawn before register / component registered / process loop / event trigger
缺失策略：fail-fast / descriptor default / runtime optional
```

没有这张表，后续还会出现“字段有定义但时机不对”的同类 Bug。

## 6. 修复顺序

推荐按以下顺序推进，避免重新引入兼容层：

1. **补 DataOS authoring / generator**
   - 给 `unit.player`、`unit.enemy` 输出 `DefaultMoveMode`。
   - 修正 `AbilityIcon` 等投影中手写 value_type 与 descriptor 不一致的问题，或删除手写 value_type。

2. **补 final snapshot validator**
   - 检查 unit / ability required fields。
   - 检查 `DefaultMoveMode` 的 table-specific expected value。
   - 检查 generated handle 与 descriptor 一致。

3. **补 runtime tests**
   - 直接加载 `runtime_snapshot.json`。
   - Apply `unit.player`、`unit.enemy`、`ability.dash`。
   - 用 generated key 读取关键字段。

4. **补 Godot behavior validation**
   - 玩家场景验证 `DefaultMoveMode == PlayerInput` 且 movement strategy 非空。
   - 敌人 spawn 验证 `DefaultMoveMode == AIControlled` 且 movement strategy 非空。
   - ability 添加验证 `Spawn<AbilityEntity>()` 成功，手动技能列表包含可触发技能。

5. **最后清理 Entity 局部兜底**
   - 删除或保留为断言日志，不作为数据来源。
   - 明确 `OnPoolAcquire()` 不写注册期配置。

## 7. 验证建议

### 7.1 DataOS 层

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
bash Data/DataOS/Tools/generate-runtime-snapshot.sh \
    Data/DataOS/Authoring/slimeainew.authoring.db \
    Data/DataOS/Snapshots/runtime_snapshot.json
bash Data/DataOS/Tools/validate-dataos.sh \
    Data/DataOS/Authoring/slimeainew.authoring.db
```

必须确认：

- final `runtime_snapshot.json` 中 `unit.player/*` 有 `DefaultMoveMode: PlayerInput`。
- final `runtime_snapshot.json` 中 `unit.enemy/*` 有 `DefaultMoveMode: AIControlled`。
- `ability/*` 的 `AbilityIcon` 为 `object_ref`，generated key 为 `DataKey<ResourceRef>`。
- `AvailableAnimations` 为 `string_array`，generated key 为 `DataKey<string[]>`。

### 7.2 Runtime Data 层

建议新增测试断言：

```text
DataRuntimeBootstrap.Default.FindRecord("unit.player", "player.deluyi")
ApplyToData(...)
Get<MoveMode>(GeneratedDataKey.DefaultMoveMode) == MoveMode.PlayerInput

FindRecord("ability", "ability.dash")
ApplyToData(...)
Get<AbilityType>(GeneratedDataKey.AbilityType) 不抛异常
Get<AbilityTriggerMode>(GeneratedDataKey.AbilityTriggerMode) 不抛异常
Get<ResourceRef>(GeneratedDataKey.AbilityIcon) 不抛异常
```

### 7.3 Godot 行为层

建议独立场景验证，不只依赖主场景 smoke：

```text
MovementDataBootstrapValidation.tscn:
    spawn player
    assert DefaultMoveMode == PlayerInput
    assert movement component has active/default strategy

EnemyDataBootstrapValidation.tscn:
    spawn enemy from unit.enemy record
    assert DefaultMoveMode == AIControlled before OnPoolAcquire
    assert movement component has active/default strategy

AbilityDataBootstrapValidation.tscn:
    create owner
    add ability.dash
    assert ability entity exists
    assert GetManualAbilities(owner) contains dash when expected
```

## 8. 深度思考：Data 系统完成标准要从“结构正确”升级到“行为可启动”

Data no-compat 重构的价值是减少入口、删除旧兜底、让错误早暴露。但这会把过去分散在 Entity、Config、DataMeta、Resource、对象池里的默认值全部推回 DataOS。这个变化需要配套三类契约。

第一类是 **record completeness contract**。Descriptor 只说明“字段存在且怎么解释”，不说明“某类业务记录必须有哪些字段”。玩家、敌人、技能、系统配置都需要自己的 required field list。没有这层，validator 只能证明“写出来的字段没错”，不能证明“没写出来的字段不重要”。

第二类是 **initialization timing contract**。Data 字段不是都一样。有些字段是运行时状态，可以晚写；有些字段是组件注册期配置，必须在 `RegisterComponents()` 之前存在。`DefaultMoveMode` 就是后者。把它放到 `OnPoolAcquire()`，在旧兼容环境里可能“看起来能跑”，但在 no-compat 严格链路下就是时序 Bug。

第三类是 **type single-source contract**。`value_type` 只能有一个事实源。既然 PRJ-0002 已裁决 descriptor-first，那么 generator 的业务投影不应再手写类型。业务投影只提供值和来源，类型由 descriptor 决定。否则 `AbilityIcon` 这种 `string` / `object_ref` 漂移会反复出现。

这次问题也说明，不能把“恢复 Entity 兜底”当成修复。恢复兜底会让游戏暂时能动，但会重新制造双入口：

```text
snapshot 说 DefaultMoveMode = None
Entity 子类说 DefaultMoveMode = PlayerInput
```

这会破坏 DataOS 作为事实源的目标。正确修复是让 snapshot 明确表达玩家和敌人的默认移动模式，并让 validator 阻止缺字段进入运行时。

## 9. 最终裁决

本 Bug 应按 Data 链路完整性修复，而不是按单个组件补丁修复：

- 不恢复旧 `PlayerEntity` / `EnemyEntity` 默认值兜底作为长期方案。
- 不放宽 `RuntimeDataSnapshotLoader` 类型校验。
- 不让 generator 和 descriptor 继续各自表达字段类型。
- 必须把 `DefaultMoveMode` 等注册期必需字段前移到 DataOS authoring / runtime snapshot。
- 必须新增 record completeness 和行为启动验证。

完成后，Data no-compat hard cutover 的判断标准应更新为：

```text
旧入口删除
    + descriptor / snapshot / generated key 类型一致
    + required records 字段完整
    + ApplyRecord 能读取代表性 unit / ability
    + Godot 行为场景证明玩家能移动、敌人能移动、技能能创建和触发
```

只有同时满足这些条件，Data 系统才算真正完成无兼容重构后的运行闭环。

## 10. 落地文档

如果要把本 Bug 进一步拆成代码和文档修改任务，继续看：

- [`05-Data残余问题代码修复分解.md`](./05-Data残余问题代码修复分解.md)
- [`06-Data文档更新与门禁清单.md`](./06-Data文档更新与门禁清单.md)

`05` 负责逐文件怎么改，`06` 负责哪些文档要同步更新、哪些门禁必须补上。
