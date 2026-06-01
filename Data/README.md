# Data 目录说明

本目录存放所有**数据侧定义**与**数据配置资产**，与 `Src/` 中的运行时逻辑分离。

## 设计分层

- **`Src/`**：负责运行时逻辑与系统实现。
  - 例如：`Data.cs` 如何读写、`Entity.Events` 如何派发、伤害和技能如何执行。
- **`Data/`**：负责数据目录结构、配置、typed 键定义与事件协议。
  - 例如：有哪些配置字段、有哪些 `DataKey<T>`、有哪些事件类型、某个配置默认值是多少。

> 当前方向：`DataOS/` 是唯一 authoring 真相源，运行时只读 `DataOS/Snapshots/runtime_snapshot.json`，再通过 snapshot query/projection 和 catalog-bound `Data` 消费。

## 当前目录职责

### 1. `Config/`

**系统级配置路径**。

- **用途**：存放不属于某个具体 Entity 配置的系统参数、全局规则、生成规则等。
- **典型内容**：波次配置、Spawn 全局规则、全局开关、系统阈值。
- **特点**：更偏“系统配置”，不是 Entity 运行时状态，不直接等同于 `Data` 容器内容。

### 2. `DataOS/`

**AI-first authoring 与 runtime snapshot 路径**。

- **用途**：保存 SQLite authoring、schema、generator、validator 和 generated snapshot。
- **典型内容**：`Authoring/`、`Schema/`、`Tools/`、`Snapshots/runtime_snapshot.json`。
- **核心职责**：生成 descriptors、records 和 resources，供运行时只读消费。

### 3. `DataKey/`

**DataKey 定义路径**。

- **用途**：集中保存生成后的 typed handle 和相关枚举。
- **当前架构**：DataKey 由 DataOS descriptor / runtime snapshot 生成；`DataMeta` 不作为当前字段定义事实源。
- **核心职责**：保存 generated typed handle；默认值、分类、约束、modifier、computed 等元数据来自 descriptor。
- **详细说明**：见 [`Data/DataKey/README.md`](DataKey/README.md)

### 4. `EventType/`

**事件协议路径**。

- **用途**：定义 `Entity.Events` / `GlobalEventBus` 使用的事件类型与事件数据结构。
- **核心职责**：作为模块间通信契约，统一事件名与事件载荷。
- **典型内容**：`GameEventType_Data.cs`、Ability/Unit/Base 等分域事件定义。

### 5. `ResourceManagement/`

**资源注册与加载路径**。

- **用途**：集中管理场景、资源、预制体等资产引用。
- **说明**：这是资源索引系统，不等同于 `Data` 容器数据。
- **详细文档**：见 [`Data/ResourceManagement/README.md`](ResourceManagement/README.md)

## 与 `Src/ECS/Runtime/Data` 的关系

- **`Data/` 目录**：定义“配置写什么、键叫什么、事件怎么约定”。
- **`Src/ECS/Runtime/Data/`**：定义“运行时怎么加载、怎么存、怎么约束、怎么计算”。

可理解为：

- `Data/` 决定**数据内容与协议**。
- `Src/ECS/Runtime/Data/` 决定**数据容器与运行时行为**。

## 常见工作流

### 新增一个可配置数据字段

1. 先在 `DataOS/Schema/`、`DataOS/Authoring/` 和 descriptor seed 中补齐字段定义与 authoring 列。
2. 运行 generator / validator 更新 `DataOS/Snapshots/runtime_snapshot.json`。
3. 运行 generated handle 生成器更新 `Data/DataKey/Generated/DataKey_Generated.cs`。
4. 运行时通过 explicit record 注入 Entity 的 `Data` 容器。

### 新增一个系统级规则

1. 优先写入 `DataOS/` 业务表，并由 generator 投影为 runtime snapshot record。
2. 如果它不是 Entity 运行时状态，通常**不需要**定义 `DataKey`。

### 新增一个事件协议

1. 在 `Data/EventType/` 对应域中定义事件类型与事件数据。
2. 在系统或组件中通过 `Entity.Events` / `GlobalEventBus` 使用。

## 维护规则

- **不要**把运行时业务逻辑写进 `Data/`。
- **不要**在旧 Resource/Config 类里重复定义 `DataKey`。
- **不要**新增 `const string` DataKey。
- **不要**使用旧 `DataKey` 别名；它和 `DataKey<T> -> string` 隐式转换已经删除。
- **系统配置**由 `DataOS/` 驱动并通过 `system.config` / `system.preset` snapshot records 投影，**可注入 Data 的配置**同样先写 `DataOS/`，**键定义**来自 descriptor，**事件契约**放 `EventType/`。
