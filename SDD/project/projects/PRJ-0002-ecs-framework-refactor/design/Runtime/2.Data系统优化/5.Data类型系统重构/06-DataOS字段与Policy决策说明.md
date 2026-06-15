# DataOS 字段与 Policy 决策说明

> 本轮原始问题：见 [`0.Prompt.md`](./0.Prompt.md)。用户认为 `data_key_descriptor` 字段太多，尤其 policy、owner、display、description、旧 category/options 等混在一起；并进一步裁决 `write_policy` 这类硬性读写约束冗余，AI-first 解耦更应该依赖规则、文档和自觉。  
> 本文目标：用通俗方式说明当前字段为什么存在、哪些留下、哪些删除或降级、字段顺序怎么改。

## 先给结论

`data_key_descriptor` 不应该再是一份 runtime `DataDefinition` 镜像。它应该被理解成 authoring 表，也就是“给人、AI、validator、generator 看”的字段定义表。

运行时只应该拿最小投影：

```text
DataOS AuthoringDescriptor
  字段较完整，服务人、AI、validator、generator、报告和展示。

RuntimeDescriptor / DataRuntimeDefinition
  字段最少，只服务 DataSlot 创建、默认值、computed、modifier 和必要防御校验。

PresentationDescriptor
  给 TestSystem、debug UI、DocsAI、AI 解释字段，不进 DataRuntimeStorage 热路径。
```

这轮对 policy 的新裁决是：

```text
权限类 policy 降级或删除 runtime enforcement。
数据形态类 contract 保留，但尽量前移到 DB validator / generator。
```

也就是说，`write_policy=system_only/loader_only/debug_only` 这种“谁可以写”不适合作为 SlimeAI runtime 的核心规则；它更适合变成 owner 文档、lint/gate、validator/report 提示和 code review 约束。AI-first 的解耦要靠清晰入口和规则，而不是靠 Data runtime 在每次写入时替所有系统做组织纪律判断。

## Policy 重新分类

过去文档把所有 policy 都当成“有用的 runtime 规则”，这是不准确的。需要分四类。

| 类别 | 字段 | 新裁决 |
| --- | --- | --- |
| 字段形态 | `storage_policy` | 保留语义，但建议改名或收窄为 `data_kind`：`value/runtime_only/computed/blob`。它决定字段是否存基础值，不是权限。 |
| 写入权限 | `write_policy` | 降级。第一轮不再作为 runtime hot path 设计核心；后续可删除或只保留 authoring/report 提示。 |
| 数据质量 | `range_policy`、`min_value`、`max_value`、`allowed_values_json` | 保留，但主要在 validator/generator 检查；runtime 只做防御。 |
| 数值能力 | `modifier_policy` | 保留，因为它决定字段能不能进入 modifier pipeline，是数据结构契约。 |
| 迁移/重置 | `migration_policy`、`reset_group` | 移出 runtime descriptor；如果未来真有迁移系统，放独立 manifest。 |

更简单的目标模型是：

```text
不要让每个字段都背一堆默认 policy。
普通字段只需要：stable_key + type + typed default。
只有特殊字段才携带：range / allowed values / modifier / computed / object ref hint。
```

## 当前字段为什么显得冗余

当前 `Data/DataOS/Schema/core.sql` 的 `data_key_descriptor` 同时有：

```text
owner_domain / owner_capability / owner_skill
value_type / runtime_type_id / default_value_text
storage_policy / write_policy / range_policy / modifier_policy / migration_policy
compute_id / dependencies_json / compute_params_json
allowed_values_json
display_name / description / ui_group / reset_group / unit / format / icon_path
category / options_json / is_percentage / supports_modifiers / is_computed
```

当前 `RuntimeDataDescriptorDto` 和 `DataDefinition` 又把其中大部分继续搬到 runtime。结果是：

- authoring、presentation、runtime、legacy 字段都在同一个对象里。
- 很多字段对 90% 普通 Data 没意义，但每条 descriptor 都带默认值。
- `category/options/is_percentage/supports_modifiers/is_computed` 与新字段形成双事实源。
- `write_policy` 让 Data runtime 变成权限裁判，代码复杂度上升，但 AI-first 的真正约束仍然要靠文档和测试。

这就是用户看到“比之前更冗余”的根因。

## 推荐字段顺序

给人和 AI 看时，应先看这个字段是什么，再看它属于谁、是什么类型、默认值是什么。

建议 seed / authoring view 顺序：

```text
1. 身份与说明
stable_key
display_name
description

2. 归属与路由
owner_capability
owner_skill

3. 类型与默认值
value_type
clr_type_hint
default_value_text

4. 数据形态
data_kind              -- 替代或收窄 storage_policy

5. 数据质量
min_value
max_value
allowed_values_json
unit
format

6. Modifier
modifier_policy

7. Computed
compute_id
dependencies_json
compute_params_json

8. 展示
ui_group
icon_path

9. 迁移/重置/历史
migration_policy
reset_group
legacy_key
legacy_status
```

`display_name`、`description` 放在 `stable_key` 后面是对的。这样人和 AI 不会先被 owner/policy 淹没。

## 字段删留裁决

### 留在 DB authoring

| 字段 | 原因 |
| --- | --- |
| `stable_key` | 长期协议 key，DataKey、snapshot、日志、AI 路由都依赖。 |
| `display_name` | 人读、TestSystem、debug UI 有用。 |
| `description` | AI 理解字段含义需要，尤其区分相似字段。 |
| `owner_capability` | AI 路由、报告聚合、DataOS 裁剪仍有价值。 |
| `owner_skill` | AI 修改字段时知道读哪个 skill。 |
| `value_type` | DB/CSV/JSON 不适合直接保存 CLR Type，逻辑类型必须有。 |
| `runtime_type_id` / `clr_type_hint` | 仅 enum/object_ref/modifier_list 需要。建议改名为 `clr_type_hint`。 |
| `default_value_text` | authoring 可用 text；生成期必须恢复 typed value。 |
| `storage_policy` / `data_kind` | 保留字段形态语义：普通值、computed、runtime_only、authoring_blob。 |
| `min_value` / `max_value` | 数值质量校验。 |
| `allowed_values_json` | enum/string 白名单，也可给 UI 选项。 |
| `modifier_policy` | 决定是否允许 modifier，是 runtime 数据结构契约。 |
| `compute_id` / `dependencies_json` / `compute_params_json` | computed resolver 绑定与依赖图需要。 |
| `ui_group` / `unit` / `format` / `icon_path` | presentation sidecar，不进 runtime hot object。 |

### 降级或迁出

| 字段 | 新归属 |
| --- | --- |
| `write_policy` | 优先改成文档/validator/report 提示；不作为 runtime hot path 核心。computed 字段可由 `data_kind=computed` 自然禁止写基础值。 |
| `migration_policy` | migration manifest；没有真实消费者前不进 runtime。 |
| `reset_group` | reset/profile manifest；不要塞进 DataRuntimeStorage。 |
| `owner_capability` / `owner_skill` | 保留在 DB 和 presentation/manifest，不进入 runtime definition。 |
| `display_name` / `description` | 保留在 presentation descriptor，不进入 runtime definition。 |

### 删除或停止投影

| 字段 | 裁决 | 替代 |
| --- | --- | --- |
| `owner_domain` | 删除或历史化 | `owner_capability` + stable key 命名足够。 |
| `category` | 删除 | `owner_capability` 或 `ui_group`。 |
| `options_json` | 删除 | `allowed_values_json`。 |
| `is_percentage` | 删除 | `unit=percent` / `format=percent`。 |
| `supports_modifiers` | 删除 | `modifier_policy`。 |
| `is_computed` | 删除 | `data_kind=computed` 或 `compute_id` 非空。 |

短期第一步不是强行 schema migration，而是停止投影到 runtime snapshot 和 `DataDefinition`。确认验证通过后再清 schema。

## Runtime snapshot 应输出什么

建议 `runtime_snapshot.json` 拆成：

```json
{
  "descriptors": [],
  "presentationDescriptors": [],
  "records": [],
  "resources": [],
  "manifest": {}
}
```

`descriptors` 只输出 runtime 必需字段：

```text
stableKey
valueType
clrTypeHint      条件字段：enum/object_ref/modifier_list
defaultValue     typed JSON
dataKind         普通值 / computed / runtime_only / authoring_blob
minValue         条件字段
maxValue         条件字段
allowedValues    条件字段
modifierPolicy   条件字段
computeId        条件字段
dependencies     条件字段
computeParams    条件字段
```

`presentationDescriptors` 输出：

```text
stableKey
displayName
description
ownerCapability
ownerSkill
uiGroup
unit
format
iconPath
```

这样 TestSystem / debug UI 仍能展示说明，但 `DataRuntimeStorage` 不再背 owner 和展示字段。

## 默认值为什么可以在 DB 里存 text

SQLite / CSV / 知识库里用 text 保存默认值是合理的，因为那是 authoring 表达。

正确链路是：

```text
default_value_text + value_type + clr_type_hint
  -> DataOS validator 检查能否解析
  -> generator 输出 typed JSON
  -> catalog build 构造 typed default
  -> DataSlot<T> 保存 T default
```

错误链路是：

```text
text/object 进入 runtime
每次 Get/Set 再根据 DataValueType 转一次
```

所以“DB 只能记录 text”不是问题；问题是 text 没在进入 runtime 前被严格验证和类型化。

## enum 应该怎么检查

`enum` 不能只靠 `runtime_type_id` 字符串暗示。建议：

```text
value_type = enum 时：
  clr_type_hint 必填
  default_value_text 必须是该 enum 合法值
  allowed_values_json 如果为空，generator 从 enum manifest 补齐
  record value_text 必须是合法 enum 值
  GeneratedDataKey<T> 的 T 必须等于 clr_type_hint 对应 C# enum
```

短期可用 generated handle gate 和手写 manifest 校验，不急着扩表。

## Validator 应前移的检查

DataOS validator / generator 应检查：

```text
value_type 是否合法
default_value_text 是否能按 value_type 解析
enum default / record value 是否合法
min/max 是否能转数字且 min <= max
range 是否只用于 numeric
modifier_policy=numeric 是否只用于 numeric
computed 是否有 compute_id 和合法 dependencies
runtime_only object_ref 是否有 clr_type_hint
GeneratedDataKey<T> 是否与 descriptor type 一致
```

如果这些检查通过，runtime 只做防御性检查。运行时 Data 代码会明显简单。

## 建议的数据库重构步骤

第一步：不改 schema，只改投影。

```text
停止把 legacy mirror 字段写进 runtime snapshot descriptors
新增 presentationDescriptors 或 sidecar
runtime loader 不再把 owner/display/ui 写入 DataDefinition
write_policy 暂不作为新 runtime 架构核心扩展
```

第二步：重排 seed / authoring view。

```text
把 INSERT 顺序调整为 stable_key, display_name, description, owner, type, default...
新增 authoring view，方便人和 AI 看字段
```

第三步：重命名和删除。

```text
runtime_type_id -> clr_type_hint
storage_policy -> data_kind 或收窄语义
删除 category/options/is_percentage/supports_modifiers/is_computed
评估 owner_domain/write_policy/migration_policy/reset_group 是否迁移到独立表或报告
```

第四步：增强 validator。

```text
enum manifest / generated handle type gate
record value typed check
policy/contract combination check
snapshot typed JSON check
```

## 最小 runtime 字段模型

```text
DataRuntimeDefinition
  stableKey
  runtimeId             中期引入
  valueType
  clrType
  typedDefault
  dataKind
  range?                条件字段
  allowedValues?        条件字段
  modifierContract?     条件字段
  computeBinding?       条件字段
```

不再放：

```text
ownerDomain
ownerCapability
ownerSkill
displayName
description
uiGroup
unit
format
iconPath
writePolicy 作为热路径权限规则
legacy mirror fields
```

这才符合用户要求的“Data 系统代码简单直接，代码清晰”。
