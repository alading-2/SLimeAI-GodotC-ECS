# Data 使用说明

> status: current
> sourcePaths: Src/ECS/Runtime/Data/, Data/DataOS/, Data/DataKey/
> relatedDocs: ./Data系统说明.md

## 1. 源码入口

- `Data/DataOS/` — SQLite 编辑端（descriptor + snapshot 生成）
- `Data/DataKey/Generated/` — 生成的 typed DataKey handle
- `Src/ECS/Runtime/Data/` — 运行时存储（DataRuntimeStorage、DataSlot、Modifier、Computed）
- `Src/ECS/Runtime/Data/Events/` — 数据变更事件定义
- `Data/Config/` — 系统配置（DataDefinitionCatalog）

相关迁移全文：

- [DataInitComponent.md](../Component/Unit/Common/DataInitComponent/DataInitComponent.md) — 实体生成时的运行时数据初始化

## 2. 常见调用流程

### 读写数据

```csharp
// 读取
float hp = _data.Get<float>(GeneratedDataKey.CurrentHp);

// 写入
_data.Set(GeneratedDataKey.CurrentHp, 80f);

// 累加
_data.Add(GeneratedDataKey.Score, 10);
```

### Modifier（修改器）

```csharp
// Feature 通过 Modifier 修改数据
_data.AddModifier(GeneratedDataKey.MoveSpeed, new Modifier(0.5f, ModifierType.Multiply));
_data.RemoveModifier(GeneratedDataKey.MoveSpeed, modifier);
```

### Computed（计算属性）

在 DataOS descriptor 中声明 `computeId` + `dependencies`，运行时通过 `DataComputeRegistry` 绑定 resolver。

### 新增 DataKey

1. 在 DataOS descriptor 新增字段
2. 生成 runtime snapshot
3. 运行 handle 生成器
4. 更新调用方使用 `GeneratedDataKey.*`

## 3. 数据和事件

数据变更通过 `Entity.Events` 发出事件，不使用 `Data.On()` 监听。

```csharp
// ❌ 旧方式
_data.On(DataKey.CurrentHp, (old, newVal) => { ... });

// ✅ 新方式：通过 Entity.Events
_entity.Events.On<GameEventType.Data.PropertyChangedEventData>(
    GameEventType.Data.PropertyChanged, evt => { ... });
```

## 4. 修改边界

- **新增 DataKey**：必须先写 DataOS descriptor，再生成 typed handle
- **禁止字符串字面量**：`_data.Get<float>("CurrentHp")` → 使用 `GeneratedDataKey.CurrentHp`
- **禁止手写 const string DataKey**：先写 descriptor
- **Data 不监听变化**：用 `Entity.Events`
- **raw string API 不是公开业务 API**

## 5. Debug 入口

- TestSystem Data 面板可查看运行时数据
- `DataDefinitionCatalog` 验证 descriptor 完整性
- `Src/ECS/Runtime/Data/Tests/DataOS/` 四个测试场景覆盖 catalog、runtime、snapshot、feature bridge
