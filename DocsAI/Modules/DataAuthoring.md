# DataAuthoring 模块契约

本文是 AI 修改 `Data/` 目录下数据配置、DataOS runtime table 运行时外壳、typed DataKey、EventType 和资源映射时必须阅读的执行契约。运行时 Data 容器规则见 `DocsAI/Modules/Data.md`。迁移后的 DataOS 目标见 `DocsAI/Protocols/AI原生数据层协议.md`。

## 职责边界

DataAuthoring 负责“数据协议和配置写在哪里、怎么映射”，不负责运行时 Data 容器内部实现。

DataAuthoring 负责：

- `Data/DataOS/RuntimeTables/` snapshot-backed DTO 外壳和兼容查询 API。
- `SlimeAINew/DataOS/` SQLite schema、seed、生成器和快照。
- `Data/DataKey/Generated/` 的 typed `DataKey<T>` 句柄；默认值和计算规则来自 `data_key_descriptor` / runtime snapshot。
- `Data/EventType/` 的事件名与事件载荷。
- `Data/Config/` 的系统级配置结构。
- `Data/ResourceManagement/` 的资源路径和资源目录。

DataAuthoring 不负责：

- `Src/ECS/Base/Data/Data.cs` 的容器读写实现。
- Component / System 的业务逻辑。
- 用旧 `.tres` 重新作为运行时主数据源。

## 核心入口

- `Data/README.md`
- `Data/DataOS/RuntimeTables/README.md`
- `DocsAI/Protocols/AI原生数据层协议.md`
- `Data/DataKey/README.md`
- `Data/Config/`
- `Data/DataOS/RuntimeTables/`
- `Data/DataKey/`
- `Data/EventType/`
- `Data/ResourceManagement/README.md`

## 数据目录分工

- `Data/DataOS/RuntimeTables/`：运行时 DTO 外壳，不再是 authoring 主数据源。
- `Data/DataOS/`：AI-first 数据真相源，SQLite + schema + generator + runtime snapshot。
- `Data/DataKey/Generated/`：descriptor 生成的运行时 DataKey handle；`DataMeta/DataRegistry` 不再作为字段定义事实源。
- `Data/EventType/`：Entity 局部事件和全局事件协议，事件数据优先 `readonly record struct`。
- `Data/Config/`：系统级配置，不直接等于 Entity.Data 字段。
- `Data/ResourceManagement/`：资源路径、资源目录和加载入口，不存业务运行时状态。

## 必须遵守

- 运行时配置优先写 `DataOS/`，`Data/DataOS runtime table/` 只保留运行时兼容外壳。
- 新迁移任务不把 `DataOS/RuntimeTables` 当最终目标；目标是 DataOS 数据库真相源和生成快照。
- `Name` 是 DataOS runtime table 默认查询键，同表内必须唯一。
- DataOS runtime table 场景、贴图、特效、投射物引用保存 `res://` 字符串路径。
- DataOS 数据只存标量、enum 文本、资源路径、稳定 id 和关系表，不存 Godot/C# 运行时对象。
- 属性名和目标 DataKey 不一致时使用 `[DataKey(nameof(DataKey.Xxx))]`。
- 新普通 DataKey 写入 `data_key_descriptor`，再由生成器产出 `DataKey<T>` handle；特殊运行时引用键也应优先 descriptor 化。
- 数值“不限制”统一使用 `-1`。
- 概率统一使用 `0-100`，计算时再 `/100`。
- 系统配置运行时入口是 `Data/DataOS runtime table/System/SystemData.cs` 与 `SystemPresetData.cs`，但它们的数据来源是 DataOS snapshot。

## 禁止事项

- 禁止把运行时业务逻辑写进 `Data/`。
- 禁止新增运行时字段时只改旧 `DataOS removed legacy Data/*.cs` 或 `.tres`。
- 禁止在 DataKey 中新增同名旧常量或 `DataMeta` 注册。
- 禁止用裸字符串替代 `[DataKey(nameof(DataKey.Xxx))]`。
- 禁止把 `PackedScene`、Node 或运行时对象引用作为 DataOS runtime table 主配置值。
- 禁止在 DataOS 里把稳定结构塞进未约束 JSON 字符串。
- 禁止运行时热路径绕过生成快照直接查询数据库。
- 禁止把资源路径索引当作 Data 容器状态。

## 修改流程

1. 判断字段属于运行时 Entity.Data、系统级配置、事件协议还是资源路径。
2. 运行时 Entity.Data 字段先在 `Data/DataOS/Authoring/DataKeyDescriptors.seed.sql` / schema 中定义 descriptor。
3. 需要配置输入时先在 `Data/DataOS/` 补齐 schema 和 seed，再同步 `Data/DataOS/RuntimeTables/` 对应运行时外壳属性，必要时加 `[DataKey]`。
4. 系统级规则优先放 `Data/DataOS/`，再生成 `Data/DataOS/RuntimeTables/System/` 运行时外壳，不要强行定义 DataKey。
5. 事件协议放 `Data/EventType/` 对应分域，并检查监听生命周期。
6. 资源路径更新后检查 `ResourceManagement` / `ResourcePaths` / `ResourceCatalog` 入口。
7. 更新 `Data/README.md`、`DocsAI/Modules/DataAuthoring.md` 或相关 Skill。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/DataOS/DataFeatureBridgeTestScene.tscn --build`
- 系统配置修改时运行 SystemCore 测试。

## 人工审查重点

- 字段是否放在正确目录。
- descriptor 的类型、默认值、分类和约束是否正确。
- RuntimeTables 属性是否能通过 snapshot record 或显式 record id 正确注入。
- 事件协议是否有稳定载荷和生命周期。
- 是否误把旧 `.tres` 路线重新作为主流程。
