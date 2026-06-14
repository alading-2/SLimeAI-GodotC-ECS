# 确认点与后续 SDD 建议

## 推荐方案

推荐采用“两阶段硬收口”，不要一次性把 Data 改成完全生成式 storage。

### 阶段一：瘦身和边界收敛

目标：

- `RuntimeDataDescriptorDto` / `DataDefinition` 只保留 runtime 必要字段。
- 删除 snapshot 中旧 mirror 字段投影：`category`、`options`、`supportsModifiers`、`isComputed`、`isPercentage`。
- `ownerCapability/ownerSkill/displayName/description/uiGroup/unit/format/iconPath` 移到 presentation / authoring sidecar，不进入 runtime `DataDefinition`。
- `RuntimeTypeId` 改成只服务非标量的 `clr_type_hint` 语义。
- default 在 catalog build 阶段转成 typed default，并确保 `DataSlot<T>` 不重复从 object/text 转换。
- catalog build 生成 report，fatal 前写 structured Log，再 throw。

### 阶段二：类型系统 runtime 化

目标：

- 引入 `DataRuntimeDefinition` 或 `DataRuntimeDefinition<T>`。
- slot 类型由 catalog 决定，不再由 boundary value 决定。
- 将 range/allowed/modifier policy 拆成 typed policy。
- 对 numeric modifier lane 做专用收口。
- 可选引入 generated runtime id / typed accessor。

## Must Confirm

### 思路问题

1. 是否接受“统一 Data 容器继续保留，但字典只做内部索引，不承担类型系统”？  
   为什么问：这是保留解耦和解决类型问题的核心取舍。  
   默认值：接受。影响：后续不会改成传统 ECS 组件字段存储，也不会回退旧 `DataMeta`。

2. 是否接受 `OwnerCapability / OwnerSkill / DisplayName / Description` 移出 runtime `DataDefinition`，但继续保留在 DataOS authoring / presentation sidecar？  
   为什么问：这些字段不是没用，只是不应在 runtime hot object。  
   默认值：接受。影响：后续 generator/snapshot shape 会变。

3. 是否接受 `RuntimeTypeId` 不完全删除，而改成仅非标量使用的 `clr_type_hint`？  
   为什么问：enum、Godot Node、modifier list 仍需要 CLR 类型补充。  
   默认值：接受。影响：普通数值字段会更干净，非标量仍可生成正确 `DataKey<T>`。

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

1. 第一轮 SDD 是否只做“字段瘦身 + report/log + default typed cache”，不做 generated runtime id？  
   为什么问：runtime id 会影响 `DataKey<T>` 结构和大量调用点，风险较高。  
   默认值：第一轮不做 runtime id。

2. 是否把 `RuntimeDataDescriptorDto` 拆成 `RuntimeDataDescriptorDto` + `RuntimeDataPresentationDto`？  
   为什么问：这是最清晰的 schema 收口，但会影响 snapshot generator、loader、tests、DocsAI。  
   默认值：拆。若担心范围，可先让 loader 忽略 presentation 字段并停止写入 `DataDefinition`。

3. `DataValueType` 名称是否保留？  
   为什么问：用户对它不信任，但它作为 authoring value type 仍有价值。  
   默认值：短期保留名称，限制作用域；后续如重命名为 `DataAuthoringValueType` 再单独执行。

## Should Confirm

- 是否在 DataOS SQLite schema 中直接删除旧 mirror 字段，还是先停止投影到 snapshot。  
  默认值：先停止投影，确认验证通过后再 schema migration 删除。
- 是否需要为 presentation descriptor 增加独立 JSON 文件。  
  默认值：暂时放在 runtime snapshot 的 `presentationDescriptors` 节点，避免新增文件路径。
- 是否需要对高频 numeric 字段先做性能基线。  
  默认值：第一轮先不做 profiler，等类型边界收口后再按证据优化。

## 后续 SDD 拆分建议

### SDD-A：Data Definition Runtime Slimming

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
- `DataOS validate` 检查旧字段不再进入 runtime snapshot。
- TestSystem/debug 如需展示字段说明，走 presentation sidecar。

### SDD-B：Data Typed Runtime Boundary Completion

范围：

- `DataRuntimeStorage`
- `DataSlot<T>`
- `DataValueConverter`
- `DataComputeRegistry` / catalog build report 可与 SDD-0044 合并或依赖其完成

验收：

- default 只转换一次并缓存 typed default。
- boundary set 不重复转换。
- object_ref slot 类型固定。
- modifier 只进入 numeric lane。
- typed policy 覆盖 write/range/allowed value。

### SDD-C：Generated Data Runtime Id / Accessor

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

## 默认假设

- 不恢复旧 `DataMeta` / `DataRegistry`。
- 不放弃 DataOS descriptor-first。
- 不把 Data 拆回各 Capability 私有字段。
- 第一轮只处理 runtime definition 瘦身与类型转换边界，不引入 chunk/archetype storage。
- Data fatal 错误继续 throw，但必须先生成 report/log。
