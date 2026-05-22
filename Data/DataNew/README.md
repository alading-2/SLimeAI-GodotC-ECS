# DataNew DTO 外壳说明

`Data/DataNew/` 不再是 authoring 真相源。它只保留运行时需要的 DTO、查询外壳和命名快捷属性，实际数据来源是 `DataOS/Snapshots/runtime_snapshot.json`。

## 规则

- `All` 从 runtime snapshot 读取。
- `Get(name)` 只做按名查询，不负责 authoring。
- 命名属性（如 `PlayerData.Deluyi`、`AbilityData.Slam`）只是 `GetRequiredByName(...)` 的便捷入口。
- 不再保留 `public static readonly XxxData = new()` 这类手写 authoring 实例。
- 场景引用只保存 `res://` 字符串路径。
- 旧 `.tres`、CSV、反射写回都不是主路径。

## 使用方式

```csharp
var enemy = EnemyData.Get("鱼人");
var slam = AbilityData.Slam;
var chain = ChainAbilityData.ChainLightning;
```

Entity 生成时仍然可以把 DTO 传给 `EntitySpawnConfig.Config`，`Data.LoadFromConfig(object config)` 会把公开属性注入到运行时 `Data` 容器。

## authoring 位置

唯一可维护 authoring 放在：

- `DataOS/Schema/core.sql`
- `DataOS/Authoring/SlimeAINew.seed.sql`
- `DataOS/Tools/build-authoring-db.sh`
- `DataOS/Tools/validate-dataos.sh`
- `DataOS/Tools/generate-runtime-snapshot.sh`

## 常见判断

- 需要新数据：改 `DataOS`。
- 需要运行时读取：改 DTO 和 snapshot loader。
- 需要旧调用不炸：保留 `Get` / `All` / 命名属性。
