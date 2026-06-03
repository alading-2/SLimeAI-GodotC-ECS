# Data 系统架构设计

> 类型：概念文档
> 范围：`Src/ECS/Runtime/Data/`、`Data/DataOS/`
> 事实源：`SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/2.Data系统优化/`

## 1. 一句话定位

Data 是运行时数据容器。字段定义事实源来自 DataOS descriptor；运行时消费 snapshot 生成结果。

```
DataOS SQLite authoring
  -> runtime_snapshot.json (descriptors + records)
     -> DataDefinitionCatalog
        -> Data.Get / Data.Set / Modifier / Computed
```

## 2. 为什么 descriptor-first

旧模式：C# `DataMeta` 静态初始化 -> AI 需维护大量 C# 字段定义代码。
新模式：DataOS 表格化 descriptor -> AI 直接操作表。

| 维度 | 旧 C# DataMeta | DataOS descriptor |
|------|---------------|-------------------|
| AI 维护成本 | 高（需理解 C# 初始化、lambda、enum） | 低（表格化，可搜索/验证） |
| 字段定义位置 | 散落在 C# 文件 | 集中在 SQLite 表 |
| 默认值/范围 | C# 代码 | 表字段 |
| 双源风险 | C# DataKey 与 DB 需同步 | DB 是唯一事实源 |

## 3. 核心概念

### 3.1 Descriptor
字段定义运行时 DTO：`stableKey`、`valueType`、`defaultValue`、`writePolicy`、`rangePolicy`、`modifierPolicy`、`computeId`、`dependencies`。

### 3.2 Catalog
`DataDefinitionCatalog`：运行时索引，启动时注册全部 `DataDefinition`，校验后冻结。

Fail fast：
- stable key 空或重复
- valueType / policy 非法
- computed 依赖不存在或成环

### 3.3 DataKey<T>
C# 调用侧 typed handle：
```csharp
public readonly record struct DataKey<T>(string StableKey);
```
只保存 stable key 和类型，不承载默认值/范围/computed。

### 3.4 Modifier 管线
base value -> Additive -> Multiplicative -> FinalAdditive -> Override -> Cap

Feature 通过 modifier 改输入；computed 读取输入计算输出。

### 3.5 Computed
DB 不存 C# lambda。descriptor 存 `computeId` + `dependencies`；运行时 `DataComputeRegistry` 绑定 resolver。

```csharp
// descriptor
{ "stableKey": "FinalAttack", "computeId": "AttributeBonus", "dependencies": ["BaseAttack", "AttackBonus"] }

// C# 注册
DataComputeRegistry.Register("AttributeBonus", new AttributeBonusResolver());
```

## 4. 数据分层

| 目录 | 角色 |
|------|------|
| `Data/DataOS/` | SQLite authoring、schema、seed、snapshot 生成 |
| `Data/DataKey/Generated/` | descriptor 生成的 typed handle |
| `Src/ECS/Runtime/Data/` | 运行时容器（DataRuntimeStorage、slot、modifier、computed） |

## 5. 禁止

- 新增 `const string` DataKey
- 手写 C# 元数据作为新字段定义
- 在 `DataKey<T>` 上放默认值/范围
- 运行时热路径查 SQLite
- `DataMeta` / `DataRegistry` 作为长期事实源
- 裸字符串 `Data.Get<T>("key")` 作为业务 API

## 6. 历史判断

从 "C# DataMeta first" -> "C# DataKey<T> + descriptor mirror" -> "descriptor-first"。
核心判断：字段定义应优先服务 AI（表格化操作），人类编辑降级为简单可读。
