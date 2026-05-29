# DataKey 生成规范

`Data/DataKey/` 现在只保存 descriptor 生成的 typed handle、业务枚举和历史分类枚举。字段定义事实源是 `Data/DataOS/Authoring/DataKeyDescriptors.seed.sql` 与 `runtime_snapshot.json.descriptors`，不是手写 `DataMeta`。

## 当前结构

```text
Data/DataKey/
├── Generated/DataKey_Generated.cs  # 由 runtime_snapshot.json 生成
├── Ability/                        # 技能枚举
├── Base/                           # 基础枚举 / 碰撞层
├── Feature/FeatureId/              # Feature 稳定 id
└── */DataCategory_*.cs             # 迁移期 UI 分类枚举
```

`GeneratedDataKey.Xxx` 和兼容别名 `DataKey.Xxx` 都是 `DataKey<T>` thin handle，只包含 stable key。默认值、范围、modifier、computed、allowed values 和展示信息全部来自 descriptor catalog。

## 新增或修改字段

1. 修改 DataOS descriptor authoring。
2. 重新生成 `Data/DataOS/Snapshots/runtime_snapshot.json`。
3. 运行 `Data/DataOS/Tools/generate-data-key-handles.py` 生成 handle。
4. 更新 RuntimeTables / Component / System 调用点。
5. 运行 `Src/ECS/Test/SingleTest/ECS/DataOS/` 下的 DataOS 场景测试。

## 禁止

- 不手写 `DataRegistry.Register` + `new DataMeta` 字段定义。
- 不在 DataKey handle 上放默认值、范围或计算函数。
- 不新增 `const string` 作为普通业务字段。
- 不把旧 `.tres` 或旧 Resource config 作为字段定义来源。

## 示例

```csharp
var hp = entity.Data.Get<float>(GeneratedDataKey.FinalHp);
entity.Data.Set(GeneratedDataKey.BaseHp, 100f);
```

兼容旧调用点时可以继续使用：

```csharp
entity.Data.Set(DataKey.BaseHp, 100f);
```

但新代码优先写 `GeneratedDataKey`，让调用点明确来自 descriptor 生成物。
