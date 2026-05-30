# DataOS RuntimeModels

`Data/DataOS/RuntimeModels/` 只保存 runtime snapshot projection 需要的轻量 DTO。它不是 authoring 事实源，不提供按名字查询的表 API，也不参与 `Entity.Data` 的字段推断。

## 规则

- 数据来源必须是 DataOS schema / seed 生成的 `runtime_snapshot.json`。
- DTO 只表达 projection 输出形状，不保留静态行、查询 facade 或配置表事实源。
- 字段默认值、类型、范围、写入策略和 modifier policy 只来自 descriptor catalog。
- Entity 初始化必须显式传入 `RuntimeDataRecord` 或 `RuntimeDataRecordTable/RuntimeDataRecordId`。

## Spawn 接入

`EntityManager.Spawn` 根据显式 `RuntimeDataRecord` 或 `RuntimeDataRecordTable/RuntimeDataRecordId` 应用 snapshot record，并通过 `DataRuntimeBootstrap.ApplyToData` 绑定 descriptor catalog 与 record。

```csharp
var enemy = EntityManager.Spawn<EnemyEntity>(new EntitySpawnConfig
{
    Config = record,
    RuntimeDataRecord = record,
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
