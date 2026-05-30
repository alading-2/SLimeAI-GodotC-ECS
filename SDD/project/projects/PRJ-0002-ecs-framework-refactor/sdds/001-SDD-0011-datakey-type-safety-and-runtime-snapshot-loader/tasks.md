# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

### Group 1 — 类型安全层（`Src/ECS/Base/Data/`）

- [x] T1.1 新增 `DataKey.cs`：定义 `IDataKey` 接口 + `DataKey<T>` readonly record struct
  - 目标文件：`SlimeAI/Src/ECS/Base/Data/DataKey.cs`（新文件）
  - `IDataKey`：`Key`、`ValueType`、`UntypedDefaultValue`
  - `DataKey<T>`：实现 `IDataKey`，包装 `DataMeta`
  - **Validation**: `dotnet build SlimeAI/SlimeAI.csproj` DataKey 定义无编译错误

- [x] T1.2 扩展 `DataRegistry`：新增 `Register<T>(DataMeta)` 返回 `DataKey<T>`
  - 目标文件：`SlimeAI/Src/ECS/Base/Data/DataRegistry.cs`
  - 复用现有 `Register(DataMeta)` 逻辑，在此基础上 wrap 泛型返回
  - **Validation**: `DataRegistry.Register<float>(meta)` 能编译且返回 `DataKey<float>`

- [x] T1.3 扩展 `Data`：新增 `Get(DataKey<T>)` / `Set(DataKey<T>, T)` 类型安全重载
  - 目标文件：`SlimeAI/Src/ECS/Base/Data/Data.cs`
  - 内部转发到 `Get<T>(key.Key)` / `Set(key.Key, value)`，不改核心逻辑
  - **Validation**: `data.Set(DataKey.BaseHp, "hello")` 编译错误；`data.Set(DataKey.BaseHp, 100f)` 通过

- [x] T1.4 修改 `DataMeta`：删除 `implicit operator string`
  - 目标文件：`SlimeAI/Src/ECS/Base/Data/DataMeta.cs`
  - 删除一行：`public static implicit operator string(DataMeta meta) => meta.Key;`
  - 修复因删除隐式转换引发的编译错误（预计在 DataKey 使用处）
  - **Validation**: `dotnet build` 后 `Data/DataKey/*.cs` 全部编译通过，0 error

### Group 2 — 数据加载层（`Src/ECS/Base/Data/`）

- [x] T2.1 确认 `runtime_snapshot.json` 格式并记录到 notes
  - 已有：`schemaVersion`、`manifest`、`descriptors[]`、`records[]`、`resources[]`
  - 需要加载的字段：`records[].table`、`records[].id`、`records[].fields[]`（key/value/type）
  - **Validation**: 在 notes.md 记录 JSON 结构摘要

- [x] T2.2 新增 `SnapshotLoader.cs`（`Src/ECS/Base/Data/`）
  - 职责：读取 JSON → 遍历 `records` → 对每个 field 调用 `Data.Set(key, value)`
  - 校验：key 是否在 DataRegistry 注册；类型是否与 DataMeta 一致
  - 错误策略：记录警告并跳过不认识的 key，不抛出异常
  - 参考：旧 `Data/RuntimeSnapshot/RuntimeDataSnapshot.cs` 的 field 遍历和 `ConvertValueBoxed` 逻辑
  - **Validation**: 能从 `runtime_snapshot.json` 读取至少一个 record 并通过 `Data.Get` 验证写入

### Group 3 — 清理

- [x] T3.1 删除 `Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`
  - 旧文件引用了不存在的 `IDataKey`、`DataApplyReport`、`data.Catalog`
  - 删除后确认无其他文件 import 该类
  - **Validation**: `dotnet build` 无因该文件删除引发的引用错误

- [x] T3.2 全量构建验证
  - `dotnet build Brotato_my.csproj` — DataKey 相关编译错误全部修复（196→0 Data 错误）
  - 剩余 120 个错误为预存 GameEventType CS0119 问题，与本 SDD 无关，不在修复范围
  - `python3 Workspace/SDD/sdd.py validate SDD-0011` — 0 error / 0 warning
