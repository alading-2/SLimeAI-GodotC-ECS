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

`GeneratedDataKey.<field>` 是 descriptor 生成的 `DataKey<T>` thin handle，只包含 stable key。默认值、范围、modifier、computed、allowed values 和展示信息全部来自 descriptor catalog。

旧 `DataKey` 兼容别名和 `DataKey<T> -> string` 隐式转换已经删除；新代码和新测试只使用 generated typed handle。

## 新增或修改字段

1. 修改 DataOS descriptor authoring。
2. 重新生成 `Data/DataOS/Snapshots/runtime_snapshot.json`。
3. 运行 `Data/DataOS/Tools/generate-data-key-handles.py` 生成 handle。
4. 更新 Component / System 调用点，直接使用 typed handle 或 snapshot projection。
5. 运行 `Src/ECS/Test/SingleTest/ECS/DataOS/` 下的 DataOS 场景测试。

## 禁止

- 不手写 C# metadata 字段定义。
- 不在 DataKey handle 上放默认值、范围或计算函数。
- 不新增 `const string` 作为普通业务字段。
- 不把旧 `.tres` 或旧 Resource config 作为字段定义来源。

## 示例

```csharp
var hp = entity.Data.Get<float>(GeneratedDataKey.FinalHp);
entity.Data.Set(GeneratedDataKey.BaseHp, 100f);
```

不要再写 `DataKey.BaseHp` 这类旧别名。错误别名会直接编译失败，而不是通过 `DataKey<T> -> string` 进入旧 string API。
