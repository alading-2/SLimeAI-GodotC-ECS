# 确认点与后续 SDD 建议

## 已确认裁决

2026-06-15 用户已经明确授权重大重构，不再要求兼容旧 Data 方案。后续实现应把这几个裁决当成默认前提：

- 功能解耦才是 SlimeAI 的核心目标；数据形态统一不是目标本身。
- 当前 Data 系统过度复杂，允许整体推倒重来。
- 不把现有 `DataDefinition` / `DataRuntimeStorage` / policy 管线当成必须保留的架构资产。
- 传统 ECS、QFramework、普通 Godot C# 强类型字段都可以是可选参考；AI-first 不绑定统一 Data 形式。
- 第一轮仍建议先瘦身当前 Data runtime，而不是同时重建完整 world/query/chunk。

## 推荐方案

推荐采用“三阶段硬收口”，不要继续围绕当前 Data 小修，也不要第一步就把全 Runtime 改成完整传统 ECS。

### 阶段一：Data Runtime Simplification

目标：

- `RuntimeDataDescriptorDto` / `DataDefinition` 只保留 runtime 必要字段。
- 删除 snapshot 中旧 mirror 字段投影：`category`、`options`、`supportsModifiers`、`isComputed`、`isPercentage`。
- `ownerCapability/ownerSkill/displayName/description/uiGroup/unit/format/iconPath` 移到 presentation / authoring sidecar，不进入 runtime `DataDefinition`。
- `RuntimeTypeId` 改成只服务非标量的 `clr_type_hint` 语义。
- `write_policy` 这类权限约束降级为 authoring/report/DocsAI 规则，不作为 runtime hot path 的核心 enforcement。
- default 在 catalog build 阶段转成 typed default，并确保 `DataSlot<T>` 不重复从 object/text 转换。
- catalog build 生成 report，fatal 前写 structured Log，再 throw。
- 建立 Data 进入条件：只有跨功能共享、需要 AI/validator/diagnostic 追溯的 runtime state 才进入 Data。

### 阶段二：Data Type Contract

目标：

- 引入 `DataRuntimeDefinition` 或 `DataRuntimeDefinition<T>`。
- slot 类型由 catalog 决定，不再由 boundary value 决定。
- 将 range/allowed/modifier policy 拆成 typed policy。
- 对 numeric modifier lane 做专用收口。
- 让 `DataComputeRegistry` 使用 catalog 已绑定的 `ClrType`，不再各自 switch `DataValueType`。

### 阶段三：Generated RuntimeId Storage

目标：

- `DataKey<T>` 携带 `stableKey + runtimeId`。
- runtime storage 优先通过 `runtimeId -> IDataSlot?[]` 查 slot。
- stable key 保留给 DataOS、日志、debug、AI 和 snapshot。
- profiler 证明热点后，再考虑 numeric lane。

## Must Confirm

### 思路问题

1. 是否接受“完整传统 ECS 分支只作为 SDD-A/B/C 后的备选，而不是第一步”？
   为什么问：这是保留解耦和解决类型问题的核心取舍。
   默认值：接受。影响：先把当前 Data runtime 砍小并类型化；只有仍证明 Data 是瓶颈，才重建 world/query/chunk。

2. 是否接受 Data 进入条件：只把跨功能共享 runtime state 放进 Data，Capability 内部缓存/索引/临时状态不强制进 Data？
   为什么问：这是纠正“数据解耦伪需求”的核心。
   默认值：接受。影响：后续实现会允许 owner service / component / system cache 保存内部数据，只要求有 invalidation、diagnostics 或验证入口。

3. 是否接受 `OwnerCapability / OwnerSkill / DisplayName / Description` 移出 runtime `DataDefinition`，但继续保留在 DataOS authoring / presentation sidecar？
   为什么问：这些字段不是没用，只是不应在 runtime hot object。
   默认值：接受。影响：后续 generator/snapshot shape 会变。

4. 是否接受 `RuntimeTypeId` 不完全删除，而改成仅非标量使用的 `clr_type_hint`？
   为什么问：enum、Godot Node、modifier list 仍需要 CLR 类型补充。
   默认值：接受。影响：普通数值字段会更干净，非标量仍可生成正确 `DataKey<T>`。

5. 是否接受 policy 分级：`write_policy` 权限约束降级，`data_kind/range/allowed/modifier/computed` 这类数据形态契约保留？
   为什么问：这决定 Data runtime 是不是继续承担“谁能写”的权限裁判。
   默认值：接受。影响：后续会减少 runtime 权限判断，强化 DocsAI、validator 和测试约束。

### 信息缺口

1. `MigrationPolicy` 当前是否有真实运行时消费者？
   为什么问：若没有，应先留在 authoring；若有，runtime definition 仍需保留。
   默认值：按“暂无真实消费者”处理，先不进入最小 runtime 字段集。

2. TestSystem / debug UI 是否需要继续从 runtime catalog 读取 `DisplayName/Description/UiGroup`？
   为什么问：如果需要，需要同步建立 presentation descriptor 查询入口。
   默认值：需要，但从 sidecar / manifest 读取，不从 `DataRuntimeStorage` 读取。

3. 是否已有必须支持的自定义游戏侧 Data 类型？
   为什么问：如果有，`DataValueType` / `clr_type_hint` / generator 的扩展方式必须先设计。
   默认值：当前只支持现有集合：string/string_array/int/float/double/bool/vector2/enum/modifier_list/object_ref。

### 决策未定

1. 第一轮 SDD 是否只做“runtime simplification + report/log + default typed cache”，不做 generated runtime id？
   为什么问：runtime id 会影响 `DataKey<T>` 结构和大量调用点，风险较高。
   默认值：第一轮不做 runtime id。

2. 第二轮是否把 `DataKey<T>` 升级为 `DataKey<T>(stableKey, runtimeId)`？
   为什么问：这是保留 AI-first stable key 同时降低热路径字符串字典成本的中期关键。
   默认值：第二轮做，不和第一轮字段瘦身混在一起。

3. 是否把 `RuntimeDataDescriptorDto` 拆成 `RuntimeDataDescriptorDto` + `RuntimeDataPresentationDto`？
   为什么问：这是最清晰的 schema 收口，但会影响 snapshot generator、loader、tests、DocsAI。
   默认值：拆。若担心范围，可先让 loader 忽略 presentation 字段并停止写入 `DataDefinition`。

4. `DataValueType` 名称是否保留？
   为什么问：用户对它不信任，但它作为 authoring value type 仍有价值。
   默认值：短期保留名称，限制作用域；后续如重命名为 `DataAuthoringValueType` 再单独执行。

5. 是否新增统一 `DataTypeContract` / `DataValueCodec`？
   为什么问：这是消除 loader/storage/compute/generator 多处类型转换重复的关键。
   默认值：新增 C# runtime contract；Python validator 先保留映射并加一致性测试，后续再统一到 JSON manifest。

## Should Confirm

- 是否在 DataOS SQLite schema 中直接删除旧 mirror 字段，还是先停止投影到 snapshot。
  默认值：先停止投影，确认验证通过后再 schema migration 删除。
- 是否需要为 presentation descriptor 增加独立 JSON 文件。
  默认值：暂时放在 runtime snapshot 的 `presentationDescriptors` 节点，避免新增文件路径。
- 是否需要对高频 numeric 字段先做性能基线。
  默认值：第一轮先不做 profiler，等类型边界收口后再按证据优化。

## 后续 SDD 拆分建议

### SDD-A：Data Runtime Simplification Hard Cutover

范围：

- `Data/DataOS/Schema/core.sql`
- `Data/DataOS/Tools/generate-runtime-snapshot.sh`
- `Data/DataOS/Tools/validate-dataos.sh`
- `Src/ECS/Runtime/Data/RuntimeSnapshot/RuntimeDataDescriptorDto.cs`
- `Src/ECS/Runtime/Data/DataDefinition.cs`
- `Src/ECS/Runtime/Data/RuntimeSnapshot/RuntimeDataSnapshotLoader.cs`
- DataOS/Data runtime tests
- DocsAI / owner skill

验收：

- runtime descriptor 不再包含旧 mirror 字段。
- `DataDefinition` 不再包含 owner/presentation 字段。
- `write_policy` 不再作为新 runtime 架构核心 enforcement。
- Data 进入条件写入 DocsAI / owner skill：跨功能共享 runtime state 才进 Data。
- `DataOS validate` 检查旧字段不再进入 runtime snapshot。
- TestSystem/debug 如需展示字段说明，走 presentation sidecar。

### SDD-B：Data Type Contract Hard Cutover

范围：

- `DataRuntimeStorage`
- `DataSlot<T>`
- `DataValueConverter`
- `DataTypeContract` / `DataValueCodec`
- `DataComputeRegistry` / catalog build report，可合并或取代 SDD-0044 的局部小修

验收：

- default 只转换一次并缓存 typed default。
- boundary set 不重复转换。
- object_ref slot 类型固定。
- modifier 只进入 numeric lane。
- typed policy 覆盖 write/range/allowed value。
- `DataComputeRegistry` 不再自己推断 descriptor value type；computed 输出类型由 catalog build 统一验证。

### SDD-C：Generated RuntimeId Storage

范围：

- `DataKey<T>`
- `generate-data-key-handles.py`
- catalog build
- runtime storage lookup
- 调用点迁移

验收：

- `GeneratedDataKey` 包含 stable key + runtime id。
- runtime 可选走 id lookup。
- diagnostics 仍输出 stable key。

这个切片风险最大，建议排在字段瘦身和 typed boundary 之后。

### SDD-D：传统 ECS 分支评估

触发条件：

- SDD-A/B/C 后仍证明 Data 是主要复杂度或性能瓶颈。
- profiler 证明 Data.Get/Set、modifier 或 computed 是主热点。
- owner cache / index 无法在不破坏事实源的前提下解决问题。

验收：

- 单独评估 Entity/Component/System/Query/GodotBridge/DataOS/DocsAI 路由成本。
- 不默认复制 Unity/Bevy/Flecs API 形态。
- 必须说明哪些 DataOS / generated key / validator 能保留，哪些会删除。

## 默认假设

- 不恢复旧 `DataMeta` / `DataRegistry`。
- 不放弃 DataOS descriptor-first。
- 不强制所有数据进 Data；Capability 内部数据可以留 owner 内部。
- 第一轮只处理 runtime definition 瘦身与类型转换边界，不引入 chunk/archetype storage。
- Data fatal 错误继续 throw，但必须先生成 report/log。
- 第一轮不引入完整 ECS storage；中期只预留 generated runtime id / slot array。
- 字段顺序调整优先服务人和 AI 决策：`stable_key/display_name/description` 放前面，再看 owner、type、default、policy。
- `write_policy` 不作为长期 runtime enforcement；先保留 authoring 输入或 report 提示，等实现设计冻结后决定是否 schema 删除。
- `SDD-0044` 不应孤立执行；其 registry/catelog 验证收敛内容并入 SDD-B 或作为 SDD-B 前置子任务。
