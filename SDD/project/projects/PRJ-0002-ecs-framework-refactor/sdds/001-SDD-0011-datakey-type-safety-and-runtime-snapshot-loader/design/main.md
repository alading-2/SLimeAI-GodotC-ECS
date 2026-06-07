# DataKey Type Safety and Runtime Snapshot Loader

## Goal

1. 补齐 `DataKey<T>` 泛型薄包装和 `IDataKey` 接口，让 `Data/DataKey/*.cs` 编译通过并获得编译期类型安全。
2. 在 `Src/ECS/Base/Data/` 实现 runtime snapshot JSON 加载器，支持从 `SlimeAI/Data/Data/Snapshots/runtime_snapshot.json` 读取 entity 模板数据并写入 `Data` 容器。
3. 删除过时的 `Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`（引用不存在类型，位置错误）。

## Context

- 共享设计文档：`../../design/Runtime/2.Data系统优化/README.md`（§4 采纳方向、§6 需要做什么）
- 当前运行时核心：`SlimeAI/Src/ECS/Base/Data/`（DataMeta.cs / DataRegistry.cs / Data.cs / DataModifier.cs）
- DataKey 定义：`SlimeAI/Data/DataKey/*.cs`（已使用 `DataKey<T>` 格式但底层类型不存在）
- DataOS 数据库：`SlimeAI/Data/Data/`（Authoring DB + Schema + Snapshots + Tools）
- 旧数据库参考：`SlimeAI/Data/DataOld/`（历史实现，仅供参考）
- 旧 snapshot loader：`SlimeAI/Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`（待删除）

### 约束

- 运行时 JSON 逻辑放在 `SlimeAI/Src/ECS/Base/Data/`（与 DataMeta/DataRegistry/Data 同层）
- DataOS 数据文件（SQLite/JSON/schema/tools）保持在 `SlimeAI/Data/Data/`
- 不改 DataMeta 字段结构、DataRegistry 查询 API、Data 核心（Get/Set/Modifier/Compute）
- DataMeta 只删除 `implicit operator string`
- 保留 `Data.Get<T>(string key)` 作为内部/兼容路径

## Design

### 6.1 类型安全层（`Src/ECS/Base/Data/`）

```csharp
// 新文件：DataKey.cs（~30 行）
public interface IDataKey
{
    string Key { get; }
    Type ValueType { get; }
    object UntypedDefaultValue { get; }
}

public readonly record struct DataKey<T>(DataMeta Meta) : IDataKey
{
    public string Key => Meta.Key;
    public Type ValueType => typeof(T);
    public object UntypedDefaultValue => Meta.GetDefaultValue();
}
```

```csharp
// DataRegistry 扩展（~10 行）
public static DataKey<T> Register<T>(DataMeta meta)
{
    Register(meta);  // 复用现有注册逻辑
    return new DataKey<T>(meta);
}
```

```csharp
// Data 扩展（~10 行）
public T Get<T>(DataKey<T> key) => Get<T>(key.Key);
public bool Set<T>(DataKey<T> key, T value) => Set(key.Key, value);
```

```csharp
// DataMeta 修改：删除 implicit operator string
// - public static implicit operator string(DataMeta meta) => meta.Key;
```

### 6.2 数据加载层（`Src/ECS/Base/Data/`）

```csharp
// 新文件：SnapshotLoader.cs
// 职责：读取 runtime_snapshot.json → 遍历 records → Data.Set(key, value)
// 校验：key 是否在 DataRegistry 中注册、类型是否匹配
// 参考：旧 RuntimeDataSnapshot.ApplyRecordToData() 的 field 遍历和类型转换
```

JSON 格式对接 `SlimeAI/Data/Data/Snapshots/runtime_snapshot.json` 已有结构。

### 6.3 清理

- 删除 `SlimeAI/Data/RuntimeSnapshot/RuntimeDataSnapshot.cs`
- 确认 `Data/DataKey/*.cs` 全部编译通过

## Verification

1. `dotnet build SlimeAI/SlimeAI.csproj` — 0 error（当前 196 error 全部修复）
2. 编写使用 `DataKey<T>` 的类型安全调用，确认编译期拒绝类型不匹配
3. `SnapshotLoader` 能从 `runtime_snapshot.json` 加载数据到 `Data` 容器
4. `python3 Workspace/SDD/sdd.py validate SDD-0011` — 0 error
