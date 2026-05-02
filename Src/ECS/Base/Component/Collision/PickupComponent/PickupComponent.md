# PickupComponent (拾取组件)

> 当前代码状态：`PickupComponent.cs` 是禁用物理监控的占位实现，只用于保证旧场景引用可实例化。以下内容仅保留历史设计语义；新增拾取功能前必须恢复/重写组件逻辑，并按当前 `Entity.Events`、`DataKey static readonly DataMeta`、`EntityManager.Destroy / ObjectPool` 规则校准。

历史目标：实现物品（如经验值、道具、货币）的拾取检测、磁吸效果以及与采集者的交互逻辑。

## 核心功能

- **范围检测**: 历史设计继承自 `Area2D`，用于检测玩家或其他实体的进入。
- **磁吸效果**: 支持物品向采集者平滑移动的功能，速度由 `MagnetSpeed` 控制。
- **拾取回调**: 当物品与采集者发生实际接触时，触发 `PickedUp` 事件。
- **状态管理**: 记录当前的采集者 (`Collector`) 和磁吸状态。

## 属性 (Data 容器)

该组件从父节点的 `Data` 容器中读取/写入以下键值：

| 键名 | 类型 | 读写 | 默认值 | 说明 |
| :--- | :--- | :--- | :--- | :--- |
| `MagnetSpeed` | `float` | 读 | `300.0` | 磁吸模式下的移动速度 |
| `MagnetEnabled` | `bool` | 读写 | `false` | 当前是否启用了磁吸移动 |

## 公开接口

### 属性

- `Collector`: `Node2D?` (只读) - 当前正在磁吸该物品的目标节点。
- `MagnetEnabled`: `bool` (只读) - 磁吸状态开关。

### 事件 (Action)

- `PickedUp(Node2D collector)`: 物品被成功拾取时触发。

### 方法

- `EnableMagnet(Node2D collector)`: 开始向指定的采集者磁吸。
- `DisableMagnet()`: 停止磁吸移动。

## 使用说明

当前不要按本文直接接入玩法。若要重新启用：

1. 先把 `PickupComponent.cs` 从占位实现恢复为实际拾取逻辑。
2. 事件订阅放入 `OnComponentRegistered` / `OnComponentUnregistered`，避免在 `_Ready` 中绑定核心逻辑。
3. 磁吸共享状态进入 `Entity.Data`，临时采集者引用可保留为组件私有字段。
4. 拾取完成后通过 `EntityManager.Destroy` 或对象池归还路径处理，不要直接 `QueueFree`。
5. 同步更新 `DocsAI/Modules/Component.md`、`DocsAI/Modules/Tools.md` 和相关测试。
