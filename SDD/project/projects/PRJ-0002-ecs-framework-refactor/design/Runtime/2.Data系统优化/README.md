# Data 系统优化设计

> 更新：2026-05-30
> 状态：完整重构主链路已按 SDD-0012~SDD-0021 收口；当前仍成立的残余问题分析见 `2.Data无兼容完全重构/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md`、`04-BUG:Data无兼容重构后移动与施法失败根因说明.md`、`05-Data残余问题代码修复分解.md`、`06-Data文档更新与门禁清单.md`。
> 决策会话：2026-05-28 AI-first Data 系统重评。
> 涉及范围：旧 Godot C# ECS 主线的 `SlimeAI/Src/ECS/Base/Data/`、`SlimeAI/Data/DataKey/`、`SlimeAI/Data/Data/`、`SlimeAI/Data/DataNew/`、`SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/`、DataOS SQLite authoring、`runtime_snapshot.json` 消费层。
> 详细实现说明：[`01-代码实现说明.md`](./01-代码实现说明.md)。
> 属性与 Feature 边界审计：[`02-DataMeta属性审计与Feature计算边界.md`](./02-DataMeta属性审计与Feature计算边界.md)。
> 完全重构范围与 TDD 计划：[`03-完全重构范围与TDD测试计划.md`](./03-完全重构范围与TDD测试计划.md)。
> 现状复查与兼任问题：[`04-Data系统现状复查与兼任问题.md`](./04-Data系统现状复查与兼任问题.md)。
> 运行报错根因分析：[`05-Data重构运行报错根因分析.md`](./05-Data重构运行报错根因分析.md)。
> 当前残余问题总览：[`03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md`](./2.Data无兼容完全重构/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md)。
> 具体运行 Bug 复盘：[`04-BUG:Data无兼容重构后移动与施法失败根因说明.md`](./2.Data无兼容完全重构/04-BUG:Data无兼容重构后移动与施法失败根因说明.md)。
> 代码修复分解：[`05-Data残余问题代码修复分解.md`](./2.Data无兼容完全重构/05-Data残余问题代码修复分解.md)。
> 文档与门禁清单：[`06-Data文档更新与门禁清单.md`](./2.Data无兼容完全重构/06-Data文档更新与门禁清单.md)。

## 1. 重新评估结论

之前的阶段性结论是：

```text
C# DataMeta = 字段定义事实源
DataOS SQLite / runtime_snapshot.json = 数据值填充源
snapshot descriptors = C# 定义的校验副本
```

这个结论对人类 C# 开发者比较友好，但对 AI-first 框架不是最优。当前重新裁决为：

```text
DataOS SQLite authoring
    -> runtime_snapshot.json
         ├── descriptors   Data 字段定义事实源
         └── records       Data 字段值事实源
    -> DataDefinitionCatalog
    -> Data.Get / Data.Set / Modifier / Computed
```

核心判断：

- **Data 系统应优先服务 AI**：AI 应能通过结构化数据表理解、增删、校验字段定义，而不是阅读大量 C# `DataMeta` 静态初始化代码。
- **人类编辑需求降级为简单可读可改**：人能在 SQLite / 表格中看懂字段、默认值、范围、描述和归属即可，不要求字段定义必须写成 C#。
- **`DataMeta` 不再作为长期事实源**：它只允许作为一次性迁移审计输入；执行完成后不保留兼容层、不保留 adapter。
- **旧配置路径必须清理**：`SlimeAI/Data/Data/` 与 `SlimeAI/Data/DataNew/` 不再作为新 Data 系统输入，执行型 SDD 应删除或迁出其 Data 职责。
- **旧测试场景必须重建**：`SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/` 不做修补，按新 Data 系统 TDD 矩阵重写。
- **运行时仍然不直接查询 SQLite**：SQLite 是 authoring 源，运行时只消费 generator 产出的 snapshot。
- **行为逻辑仍留在 C#**：数据库保存字段契约与约束，不保存任意 C# lambda；复杂计算通过 `ComputeId + C# resolver` 绑定。

## 2. 当前 ECS Data 系统核心问题

### 2.1 字段定义过度依赖 C# 静态代码

当前字段定义主要散落在 `Data/DataKey/*.cs`，每个字段通过 `DataRegistry.Register(new DataMeta { ... })` 注册。典型字段包含：

- **身份信息**：`Key`、`DisplayName`、`Description`。
- **类型信息**：`Type = typeof(float)`。
- **约束信息**：`DefaultValue`、`MinValue`、`MaxValue`、`Options`。
- **分类信息**：`Category = DataCategory_Attribute.Computed`。
- **运行时行为**：`Compute = data => ...`、`Dependencies = [...]`。

这让一个字段定义同时混合 authoring metadata、runtime contract 和 behavior hook。AI 要修改字段时，需要同时理解 C# 初始化语法、`nameof()`、enum、`typeof()`、lambda、依赖数组和注册顺序。

### 2.2 `DataMeta` 的隐式字符串转换削弱类型安全

`DataMeta` 支持隐式转为 string：

```csharp
public static implicit operator string(DataMeta meta) => meta.Key;
```

这会让 `Data.Get<T>(string key)`、`Data.Get<T>(DataKey.Xxx)` 和裸字符串调用混在一起。编译器无法阻止：

```csharp
data.Get<float>("BaseHp");
data.Get<float>("BaseHP");
data.Get<float>(DataKey.BaseHp);
```

对 AI 来说，这种 API 会鼓励继续写裸字符串，拼写错误只能运行时暴露。

### 2.3 字段定义和数据值分裂

旧系统中，字段定义在 C#，实体模板数值又可能散落在 C# Config、DataNew 资源或未来 DataOS seed 中。AI 想新增一个敌人或属性时，常常需要跨越：

- `Src/ECS/Base/Data/`
- `Data/DataKey/`
- `Data/Data/`
- `Data/Config/`
- `Data/DataNew/`
- DataOS seed / snapshot

这不符合 AI-first 的“少猜、少扫、入口清晰、可验证”原则。

### 2.4 旧 Data 配置路径制造双新系统

`SlimeAI/Data/Data/` 与 `SlimeAI/Data/DataNew/` 都不应继续保留为新 Data 系统输入：

- **`Data/Data/`**：依赖 Resource / Config 到 `Data.LoadFromConfig` 的反射映射，会继续要求 AI 维护 C# 配置类。
- **`Data/DataNew/`**：依赖 DTO 默认值和 `DataKey.DefaultValue`，会让 C# DataKey 继续成为事实源。
- **两者共同问题**：会和 DataOS descriptor / records 并存，形成三套输入路径。

裁决：后续执行型 SDD 应删除这两条 Data 输入路径；如果其中有非 Data 专属资源，必须迁出并重新归属，不允许继续作为 Data 字段定义或记录值来源。

### 2.5 `DataMeta` 对人友好，对 AI 不友好

当初保留大量 C# `DataMeta` 的原因，是方便人类开发者在 IDE 里写字段、跳转和调试。但当前目标已经变化：框架应优先方便 AI 使用，不能为了兼容旧写法继续保留 C# 字段定义事实源。

对 AI 来说，表格化 descriptor 明显优于大量 C# 初始化代码：

```text
stable_key | value_type | default | min | max | owner_capability | owner_skill | description
```

这类结构可搜索、可 diff、可验证、可批量生成，也更适合作为 prompt / skill / validator 的事实源。

## 3. SlimeAI-AiFirst Data 系统分析

### 3.1 值得继续采用的部分

`SlimeAI-AiFirst/DataOS` 的核心方向是正确的：

- **SQLite 作为 authoring 真相源**：业务数据写入清晰业务表，再投影到 runtime snapshot。
- **业务表优先，不退化为万能 EAV**：Unit / Ability / Feature / System / Spawn 等内容优先使用明确业务表。
- **`data_key_descriptor` 显式保存字段 metadata**：包括 stable key、owner capability、owner skill、value type、默认值、范围、选项、modifier/computed 标记等。
- **generator 生成 runtime snapshot**：运行时不查询 SQLite。
- **validator 输出结构化报告**：检查外键、空 key、类型漂移、默认值漂移、disabled capability trimming、资源路径等问题。
- **capability manifest 支持裁剪**：不启用的能力不进入 active snapshot。

这些设计非常适合 AI-first：AI 可以直接操作表，validator 可以直接告诉 AI 哪一行、哪一列、哪个字段错了。

### 3.2 需要进一步纠偏的部分

AiFirst 运行时示例仍保留了 C# `DataKey<T>` 作为 active catalog 的核心 contract，再用 snapshot descriptors 做 drift check。这个方向比旧 ECS 更类型安全，但对当前目标仍有不足：

- **字段定义仍需要 C# 手写**：AI 仍要维护 C# `DataKey<T>` 文件。
- **descriptor 被降级为 mirror**：DB 里有字段定义，却不能成为真正事实源。
- **双源同步成本仍存在**：C# DataKey 与 DB descriptor 需要保持一致。

因此，旧 ECS 的 Data 优化不应停在“C# typed DataKey + descriptor 校验副本”，而应进一步走向 descriptor-first。

## 4. 数据库是否能做字段定义

结论：**数据库可以承载大部分字段定义和 authoring 约束，但不能承载任意运行时行为。**

### 4.1 适合放进数据库的内容

| 内容 | 推荐承载方式 |
| ---- | ---- |
| 字段 key | `stable_key` |
| 归属模块 | `owner_capability` |
| AI 路由 | `owner_skill` |
| 类型 | `value_type` + validator |
| 默认值 | `default_value_text` + 类型转换校验 |
| 存储策略 | `storage_policy` |
| 写入策略 | `write_policy` |
| 范围策略 | `range_policy` + `min_value` / `max_value` |
| 展示单位 | `unit` / `format` |
| 选项约束 | `allowed_values_json` |
| modifier 策略 | `modifier_policy` |
| computed 绑定 | `compute_id` |
| computed 依赖与参数 | `dependencies_json` / `compute_params_json` |
| 迁移策略 | `migration_policy` |
| 显示与说明 | `display_name` / `description` / `ui_group` / `reset_group` / `icon_path` |

SQLite 可通过 `CHECK`、外键、唯一约束、view、JSON 函数和 validator 脚本表达这些规则。SQLite 文档也确认：SQLite 支持 `CHECK`、foreign key、JSON 查询、STRICT table 思路，适合做轻量 authoring 数据约束。

### 4.2 不适合放进数据库的内容

| 内容 | 原因 | 推荐做法 |
| ---- | ---- | ---- |
| 任意 C# lambda | 数据库不能保存可调试的 C# 函数 | 用 `compute_id` 绑定 C# resolver |
| Godot Node / Resource 实例 | 运行时对象不能稳定序列化 | 只存 resource path / resource key |
| 高频运行时状态 | SQLite 不应进入热路径 | 存在 `Data` 容器内 |
| 复杂玩法流程 | 容易把数据层变成脚本语言 | 保留在 System / Component / Service |

所以推荐方向不是“纯 DB-first”，而是：

```text
DB / descriptor 定义字段契约和 authoring 约束
C# resolver / Data 容器负责运行时行为
Generator / Validator 保证数据可生成、可验证、可追踪
```

## 5. 外部参考结论

### 5.1 SQLite authoring 约束

SQLite 适合作为 DataOS authoring 层，因为它能提供：

- **结构化表**：比 JSON 大文件更适合 AI 定位行列。
- **`CHECK` 约束**：表达值域、枚举、布尔规范。
- **外键与唯一约束**：表达 owner、record、field 的关系。
- **JSON 函数**：支持 options / dependencies 等半结构字段。
- **生成与验证脚本**：把 authoring DB 投影成 runtime snapshot。

但 SQLite 类型系统仍不能替代 C# 编译期类型检查，因此运行时 loader 必须再次校验 descriptor 和 record field 的类型。

### 5.2 Unity Entities baking 参考

Unity Entities 的 authoring / baking workflow 将编辑器中的 authoring data 转换为 runtime entity data。它说明一个成熟 ECS 系统可以把“人类/工具编辑友好的数据”和“运行时高效消费的数据”分离。

SlimeAI 可以采用同类分层：

```text
SQLite authoring tables   = 编辑与 AI 操作友好
runtime_snapshot.json     = 运行时加载友好
DataDefinitionCatalog     = ECS Data 查询友好
Data                      = 局内状态读写友好
```

区别是：SlimeAI 的 authoring 操作者主要是 AI，所以表结构、validator 和错误报告要比传统人类编辑器更重要。

## 6. 方案对比

### 6.1 方案 A：继续 C# `DataMeta` first

```text
Data/DataKey/*.cs -> DataRegistry -> Data
DataOS snapshot records -> Data.Set
```

优点：

- **改动最小**：现有运行时逻辑复用最多。
- **C# 调试方便**：lambda、enum、`typeof()` 都在 IDE 内。

缺点：

- **AI 仍要维护大量 C# 字段定义**。
- **DB descriptor 只能当副本**。
- **字段定义和数据值继续分裂**。
- **无法解决当前最核心的 AI-first 阻碍**。

裁决：**不推荐作为长期方向**。

### 6.2 方案 B：C# `DataKey<T>` + descriptor mirror

```text
C# DataKey<T> / DataMeta -> DataCatalog
DataOS descriptors -> drift check
DataOS records -> Data.Set
```

优点：

- **类型安全强于旧 `DataMeta`**。
- **比裸字符串更适合 C# 调用**。
- **可借鉴 AiFirst 现有实现**。

缺点：

- **字段定义仍在 C#**。
- **AI 仍需改 DataKey 文件**。
- **descriptor 明明能表达定义，却只能做 mirror**。

裁决：**只作为反例和历史参考，不作为执行路径**。本轮完整重构不再规划长期 mirror 或 drift-check 双事实源。

### 6.3 方案 C：DataOS descriptor-first

```text
DataOS data_key_descriptor
    -> runtime_snapshot.json.descriptors
    -> DataDefinitionCatalog
    -> Data
```

优点：

- **最符合 AI-first**：AI 可在 SQLite 表中增删字段定义。
- **字段定义和值在同一 authoring 管线内**。
- **人类也能用 DB 工具查看和编辑**。
- **validator 可给出结构化错误**。
- **运行时仍保持 snapshot-first，不查 SQLite**。

缺点：

- **需要新增 `DataDefinitionCatalog` 和 loader**。
- **computed 字段需要 resolver 机制**。
- **旧 `DataKey_*.cs` 只能作为一次性迁移审计输入**。
- **旧 `Data/Data`、`DataNew` 和 Data 测试场景需要删除或重建**。

裁决：**推荐方案，并按完整重构执行**。

## 7. 推荐实现方向

推荐按 descriptor-first 方案推进，核心组件如下：

| 组件 | 作用 | 新旧关系 |
| ---- | ---- | ---- |
| `DataDefinition` | descriptor 的运行时形态 | 长期替代 `DataMeta` 的定义责任 |
| `DataDefinitionCatalog` | active profile 字段定义索引 | 长期替代 `DataRegistry` 的查询责任 |
| `RuntimeDataSnapshotLoader` | 读取 snapshot，构建 catalog，应用 records | 替代旧 `Data/RuntimeSnapshot/RuntimeDataSnapshot.cs` |
| `DataComputeRegistry` | 注册 `compute_id -> resolver` | 替代 `DataMeta.Compute` 直接写 lambda 的长期事实源角色 |
| `DataKey<T>` | 可选 typed handle | 只保存 stable key，不保存字段定义 |
| `LegacyDataAuditTool` | 读取旧 `DataMeta` / `DataKey` 生成迁移报告 | 一次性审计工具，执行完成后删除 |
| `NewDataTddSuite` | 新 Data 小测试和 Godot smoke | 替代旧 `SingleTest/ECS/Data` 场景 |

详细接口草案和伪代码见 [`01-代码实现说明.md`](./01-代码实现说明.md)。`DataMeta` 属性分层裁决、Feature 与 computed 的边界见 [`02-DataMeta属性审计与Feature计算边界.md`](./02-DataMeta属性审计与Feature计算边界.md)。完整删除范围和 TDD 矩阵见 [`03-完全重构范围与TDD测试计划.md`](./03-完全重构范围与TDD测试计划.md)。

## 8. 修改核心方向

### 8.1 `DataMeta` 降级为一次性审计输入

不再设计长期 `DataMeta` 兼容层。执行型 SDD 中，旧 `DataMeta` 只允许用于：

- **迁移清单生成**：把旧 C# 字段定义转成待迁移 descriptor 草案。
- **缺口审计**：确认新 descriptor 未遗漏旧字段能力。
- **删除前验证**：删除旧 `DataKey_*.cs` 前确认新测试和调用点已覆盖。

执行完成后的目标是：源码不再依赖 `DataRegistry.Register(new DataMeta)`，也不保留 `DataMetaAdapter` 作为运行时 fallback。

### 8.2 `runtime_snapshot.json.descriptors` 成为字段定义输入

运行时启动时：

```text
读取 runtime_snapshot.json
  -> 遍历 descriptors
  -> 构建 DataDefinitionCatalog
  -> 校验 computed resolver / dependencies / default / range
  -> 再应用 records 到 Data
```

如果 record 中出现 descriptor 不存在的 key，必须报错，不能自动创建。

### 8.3 Compute 改为 `ComputeId + resolver`

数据库不保存 C# lambda。descriptor 保存：

```json
{
  "stableKey": "Attribute.FinalHp",
  "isComputed": true,
  "computeId": "AttributeBonus",
  "dependencies": ["Attribute.BaseHp", "Attribute.HpBonus"]
}
```

C# 注册：

```text
AttributeBonus -> AttributeBonusComputeResolver
```

这样 AI 可以通过 descriptor 新增同类计算字段；只有新增一种计算语义时才需要写 C# resolver。

### 8.4 `DataKey<T>` 只做调用 handle

长期推荐：

```csharp
public readonly record struct DataKey<T>(string StableKey);
```

它不再承载默认值、范围、分类和 computed 信息。这些定义来自 catalog。这样既保留 C# 业务调用的类型安全，也避免 C# DataKey 成为第二个字段定义事实源。

### 8.5 裸字符串 API 收口

`Data.Get<T>(string key)` / `Data.Set<T>(string key, T value)` 不作为公共业务 API。执行型 SDD 中只允许内部 loader、测试 fixture 或 generated handle 包装使用，业务代码必须收口到：

```csharp
data.Get(DataKey.BaseHp);
data.Set(DataKey.BaseHp, 100f);
```

后续必须用 grep gate / analyzer 检查新增裸字符串访问。

### 8.6 Feature 不替代 computed

本方案不是废弃 `FeatureSystem`。Feature 继续负责生命周期、效果执行、`FeatureModifierEntry` 授予和按 source 回滚；computed 字段仍由 Data 层的 `ComputeId + IDataComputeResolver` 负责。

边界如下：

| 场景 | 归属 |
| ---- | ---- |
| 装备、Buff、天赋给属性加成 | `FeatureSystem` + `DataModifier` |
| 技能激活、事件触发、复杂效果 | `FeatureSystem` / `AbilitySystem` / 具体系统 |
| `FinalHp`、`HpPercent`、`DPS` 等纯派生值 | `DataComputeRegistry` / `IDataComputeResolver` |

核心原则是：

```text
Feature 改变 Data 输入。
Data computed 读取输入并计算派生输出。
```

因此 `FeatureModifiers` 需要被正式纳入 descriptor，但它的 `storage_policy` 应是 `authoring_blob` 或等价策略，而不是普通数值字段。

## 9. DataOS 需要补齐的分层表达力

旧 ECS 迁移到 descriptor-first 时，不应把 `DataMeta` 原样平铺进数据库，而应按职责拆成 core、runtime policy、compute policy、migration policy 和 presentation。

| 分层 | 字段 | 目的 |
| ---- | ---- | ---- |
| Core | `stable_key` | 稳定字段 key |
| Core | `owner_domain` / `owner_capability` / `owner_skill` | 字段归属与 AI 路由 |
| Core | `value_type` / `runtime_type_id` | Data 基础类型与可选 C# / Godot 类型 |
| Core | `default_value_text` | 玩法默认值 |
| Core | `storage_policy` | `persisted` / `runtime_state` / `runtime_only` / `computed` / `authoring_blob` |
| Core | `write_policy` | `read_write` / `loader_only` / `system_only` / `computed_readonly` / `debug_only` |
| Runtime policy | `range_policy` / `min_value` / `max_value` | 校验、拒绝或运行时 clamp |
| Runtime policy | `modifier_policy` | 是否允许 modifier，以及 modifier 类型范围 |
| Runtime policy | `allowed_values_json` | 枚举或选项约束 |
| Compute policy | `compute_id` / `dependencies_json` / `compute_params_json` | computed resolver 绑定、依赖和参数 |
| Migration policy | `migration_policy` | Entity 迁移复制策略 |
| Presentation | `display_name` / `description` / `ui_group` / `reset_group` / `unit` / `format` / `icon_path` | AI、编辑器、调试 UI 和文档生成 |

其中 `DisplayName`、`Description`、`ui_group`、`unit` 等不是 Data 热路径必需字段，但仍应保留在 authoring descriptor 中，因为 AI 需要通过这些信息理解字段语义。`IsPercentage` 不应作为计算语义，应降级为 `unit=percent` 或 `format` 这类展示语义；真正的 0-100 限制由 range policy 表达。

Validator 必须检查：

- **类型合法**：`value_type` 必须在允许集合内。
- **默认值可转换**：`default_value_text` 必须能转成 `value_type`。
- **范围合法**：`min_value <= max_value`。
- **策略合法**：`storage_policy`、`write_policy`、`modifier_policy` 和 `migration_policy` 必须在允许集合内。
- **依赖存在**：`dependencies_json` 中每个 key 必须有 descriptor。
- **computed 绑定存在**：`storage_policy=computed` 或 `compute_id` 非空时，运行时 resolver manifest 必须能找到。
- **Feature modifier 目标合法**：`Feature.Modifiers` 中每个目标 key 必须存在、是数值字段，且允许 modifier。
- **record 与 descriptor 一致**：record field 的 key 和 type 必须匹配 descriptor。

## 10. 完全重构路线

### 10.1 第一阶段：TDD 红灯测试与一次性旧定义审计

先建立新 Data 测试骨架，再写红灯测试。旧 C# 定义只能用于生成审计报告：

```text
旧 C# DataMeta / DataRegistry / DataKey
    -> LegacyDataAuditReport
    -> 待迁移 descriptor 草案
```

输出：

- **`missing_in_snapshot`**：C# 有，descriptor 没有。
- **`missing_in_csharp`**：descriptor 有，C# 没有。
- **`type_mismatch`**：类型不同。
- **`default_mismatch`**：默认值不同。
- **`range_mismatch`**：范围不同。
- **`computed_mismatch`**：computed 标记、依赖或 resolver 不一致。
- **`old_path_reference`**：仍引用 `SlimeAI/Data/Data`、`DataNew`、`LoadFromConfig`、旧 Data 测试入口。

这一步不是为了保留兼容层，而是为了确认删除旧路径前没有遗漏能力。

### 10.2 第二阶段：实现 descriptor-first catalog

只做纯 C# 最小闭环：

- **`DataValueType`**
- **`DataDefinition`**
- **`DataDefinitionCatalog`**
- **`RuntimeDataDescriptorDto`**
- **`RuntimeDataSnapshotLoader.BuildCatalog`**
- **`DataComputeRegistry`**
- **TDD 小测试**

暂不接入全量 Entity spawn。

### 10.3 第三阶段：records 写入 `Data`

实现：

```text
record.fields[key]
  -> catalog.GetRequired(key)
  -> type convert
  -> range policy / allowed values validate
  -> Data.SetUntyped(definition, value)
```

### 10.4 第四阶段：分模块重建字段定义

建议顺序：

1. **Base / Unit**
2. **Attribute**
3. **Movement**
4. **Ability**
5. **Feature**
6. **AI / Test**

每迁移一组，都运行差异扫描和 Data 行为测试。

### 10.5 第五阶段：删除旧路径和重建 Data 测试场景

当 descriptor 覆盖完整、调用点切换完成、TDD 测试通过后：

- **禁止新增 `DataRegistry.Register(new DataMeta)`**。
- **新增字段必须先写 DataOS descriptor**。
- **删除 `SlimeAI/Data/Data/` 的 Data 输入职责**。
- **删除 `SlimeAI/Data/DataNew/`**。
- **重写 `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data/`**。
- **删除 `DataMetaAdapter` / legacy fallback**。
- **`DataKey<T>` 可由 descriptor codegen 生成薄 handle**。

## 11. 验证要求

第一批执行型 SDD 至少需要：

- **Catalog 测试**：重复 key、未知类型、默认值转换、依赖索引、resolver 缺失。
- **Data 行为测试**：默认值、range policy、write policy、类型拒绝、computed cache、依赖标脏、事件通知。
- **Snapshot apply 测试**：unknown key、type mismatch、value conversion、批量错误报告。
- **删除旧路径测试**：`DataNew`、`Data/Data/`、`LoadFromConfig`、旧 `SingleTest/ECS/Data` 行为不再参与新 Data 系统。
- **Feature bridge 测试**：`Feature.Modifiers` 作为 `authoring_blob` 接入 modifier policy，不接管 computed。
- **TDD 证据**：每个新行为至少有 RED / GREEN / REFACTOR 记录。
- **文档验证**：目标设计文档、`SlimeAI/Src/ECS/**` 旁文档、DocsNew 和相关 skill/rule 同步；`SlimeAI/DocsAI/` 已删除，不再作为同步目标。

代码实现阶段再按影响范围运行：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
Tools/run-build.sh
Tools/run-tests.sh
```

如触及 DataOS schema / generator / snapshot，则追加 DataOS 验证。

## 12. 不做什么

- **不把旧 ECS 替换成纯 GameOS**：DocsNew 已确认当前方向是 AI-first ECS 游戏框架。
- **不让运行时热路径查询 SQLite**：运行时只读 snapshot。
- **不把业务数据退回万能 key-value 主入口**：Unit / Ability / Feature 等仍优先清晰业务表。
- **不把任意 C# lambda 存入 DB**：用 `ComputeId + resolver`。
- **不把 `DataMeta` 继续当长期事实源**：否则无法解决 AI 维护成本问题。
- **不保留旧 `Data/Data` / `DataNew` 作为新 Data 输入路径**：否则会形成多事实源。
- **不修补旧 Data 测试场景**：必须按新 Data TDD 矩阵重建。

## 13. 推荐第一个执行型 SDD

第一个 SDD 应命名为 Data System Full Rewrite 的第一切片，建议目标缩小为：

```text
TDD 红灯测试 -> snapshot descriptors -> DataDefinitionCatalog -> 纯 C# 测试通过
```

包含：

- **新增 descriptor-first runtime model**：`DataValueType`、`DataDefinition`、`DataDefinitionCatalog`。
- **新增 snapshot descriptor loader**：只构建 catalog，不写入 Entity。
- **新增 compute registry 骨架**：能校验 resolver 是否存在。
- **新增旧 `DataMeta` 一次性审计工具**。
- **补新 Data 最小 TDD 单元测试**。

不包含：

- **全量业务字段迁移**。
- **游戏 seed 大规模改动**。
- **立即删除旧路径**。

后续切片必须显式包含删除 `SlimeAI/Data/Data`、`SlimeAI/Data/DataNew` 和重建 `SlimeAI/Src/ECS/Test/SingleTest/ECS/Data`。
