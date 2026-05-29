# DataOS RuntimeTables

`Data/DataOS/RuntimeTables/` 是 DataOS snapshot 之外的轻量 C# 表外壳，用于保留按名字查询的运行时便利 API。它不是 authoring 事实源，也不再通过反射把字段直接灌入 `Entity.Data`。

## 规则

- 表数据最终应来自 DataOS schema / seed / snapshot。
- RuntimeTables 只保留 C# 调用便利和少量迁移期静态行。
- 属性默认值使用字面量，不能读取 `DataKey` 默认值。
- 字段映射只用 `[DataKey(nameof(DataKey.Xxx))]` 标明目标 stable key。
- Entity 初始化优先使用 `DataRuntimeBootstrap` 查找并应用 snapshot record。

## 查询示例

```csharp
var enemy = EnemyData.Get("鱼人") ?? EnemyData.Yuren;
```

## Spawn 接入

`EntityManager.Spawn` 会根据显式 `RuntimeDataRecordTable/RuntimeDataRecordId`，或根据已知 RuntimeTables 类型和 `Name` 推断 snapshot record，并通过 `DataRuntimeBootstrap.ApplyToData` 绑定 descriptor catalog 与 record。

```csharp
var enemyConfig = EnemyData.Get("鱼人") ?? EnemyData.Yuren;
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = enemyConfig,
    UsingObjectPool = true,
    PoolName = ObjectPoolNames.EnemyPool,
    Position = spawnPosition
});
```

## 验证

- `res://Src/ECS/Test/SingleTest/ECS/DataOS/DataCatalogTestScene.tscn`
- `res://Src/ECS/Test/SingleTest/ECS/DataOS/DataRuntimeTestScene.tscn`
- `res://Src/ECS/Test/SingleTest/ECS/DataOS/DataSnapshotApplyTestScene.tscn`
- `res://Src/ECS/Test/SingleTest/ECS/System/SystemCore/SystemCoreRuntimeTest.tscn`
