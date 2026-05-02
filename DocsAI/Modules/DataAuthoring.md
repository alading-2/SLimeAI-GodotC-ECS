# DataAuthoring 模块契约

本文是 AI 修改 `Data/` 目录下数据配置、DataNew 表、DataKey、EventType 和资源映射时必须阅读的执行契约。运行时 Data 容器规则见 `DocsAI/Modules/Data.md`。

## 职责边界

DataAuthoring 负责“数据协议和配置写在哪里、怎么映射”，不负责运行时 Data 容器内部实现。

DataAuthoring 负责：

- `Data/DataNew/` 纯 C# 运行时表数据。
- `Data/DataKey/` 的 `DataMeta` 键定义、分类、默认值和计算规则。
- `Data/EventType/` 的事件名与事件载荷。
- `Data/Config/` 的系统级配置结构。
- `Data/ResourceManagement/` 的资源路径和资源目录。

DataAuthoring 不负责：

- `Src/ECS/Base/Data/Data.cs` 的容器读写实现。
- Component / System 的业务逻辑。
- 用旧 `.tres` 重新作为运行时主数据源。

## 核心入口

- `Data/README.md`
- `Data/DataNew/README.md`
- `Data/DataKey/README.md`
- `Data/Data/README.md`
- `Data/Config/`
- `Data/DataNew/`
- `Data/DataKey/`
- `Data/EventType/`
- `Data/ResourceManagement/README.md`

## 数据目录分工

- `Data/DataNew/`：当前运行时主数据源，纯 C# POCO，一张表一个 `XxxData`，一行一个 `public static readonly XxxData`。
- `Data/DataKey/`：运行时 Data 容器可用键，主流写法是 `static readonly DataMeta` + `DataRegistry.Register`。
- `Data/EventType/`：Entity 局部事件和全局事件协议，事件数据优先 `readonly record struct`。
- `Data/Config/`：系统级配置，不直接等于 Entity.Data 字段。
- `Data/Data/`：旧 Resource / `.tres` 配置归档和对照迁移，不作为新增运行时主入口。
- `Data/ResourceManagement/`：资源路径、资源目录和加载入口，不存业务运行时状态。

## 必须遵守

- 运行时配置优先写 `Data/DataNew/`。
- `Name` 是 DataNew 默认查询键，同表内必须唯一。
- DataNew 场景、贴图、特效、投射物引用保存 `res://` 字符串路径。
- 属性名和目标 DataKey 不一致时使用 `[DataKey(nameof(DataKey.Xxx))]`。
- 新普通 DataKey 使用 `static readonly DataMeta`，特殊运行时引用键才评估 `const string`。
- 数值“不限制”统一使用 `-1`。
- 概率统一使用 `0-100`，计算时再 `/100`。
- 系统配置运行时主线是 `Data/DataNew/System/SystemData.cs` 与 `SystemPresetData.cs`。

## 禁止事项

- 禁止把运行时业务逻辑写进 `Data/`。
- 禁止新增运行时字段时只改旧 `Data/Data/*.cs` 或 `.tres`。
- 禁止在 DataKey 中新增同名旧常量和 `DataMeta` 并存。
- 禁止用裸字符串替代 `[DataKey(nameof(DataKey.Xxx))]`。
- 禁止把 `PackedScene`、Node 或运行时对象引用作为 DataNew 主配置值。
- 禁止把资源路径索引当作 Data 容器状态。

## 修改流程

1. 判断字段属于运行时 Entity.Data、系统级配置、事件协议还是资源路径。
2. 运行时 Entity.Data 字段先在 `Data/DataKey/` 定义 `DataMeta`。
3. 需要配置输入时在 `Data/DataNew/` 对应表加属性，必要时加 `[DataKey]`。
4. 系统级规则优先放 `Data/DataNew/System/` 或 `Data/Config/`，不要强行定义 DataKey。
5. 事件协议放 `Data/EventType/` 对应分域，并检查监听生命周期。
6. 资源路径更新后检查 `ResourceManagement` / `ResourcePaths` / `ResourceCatalog` 入口。
7. 更新 `Data/README.md`、`DocsAI/Modules/DataAuthoring.md` 或相关 Skill。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/DataTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/Data/TestDataKeyMapping.tscn --build`
- 系统配置修改时运行 SystemCore 测试。

## 人工审查重点

- 字段是否放在正确目录。
- DataKey 的类型、默认值、分类和约束是否正确。
- DataNew 是否能被 `Data.LoadFromConfig` 正确映射。
- 事件协议是否有稳定载荷和生命周期。
- 是否误把旧 `.tres` 路线重新作为主流程。
