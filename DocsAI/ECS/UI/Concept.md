# UI 概念

> status: current
> sourcePaths: Src/ECS/UI/
> relatedDocs: DocsAI/ECS/UI/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

UI 不应该再只被描述成“独立 View 层”。更准确的定位是：**UI = 前端 Controller / Adapter 层**。

它仍然使用 Godot `Control` 节点树承载表现，但在框架角色上，它负责：

- 接收玩家输入
- 绑定 Object / Data / Event
- 把前端意图转发为 Command / owner API / request
- 根据 Data 和事件更新显示

所以 UI 不是 Entity / Object，也不是普通 gameplay Component；它是连接 Godot 前端节点树与 SlimeAIFramework Runtime 的一层前端逻辑壳。

## 2. 核心概念

### Binding Pattern

```
Object / Entity（运行时对象）
  ├─ Data：业务数据 / 共享状态
  └─ Events：事件总线
       ↕ 绑定
UI（Controller / View Adapter 层）
  ├─ UIBase：UI 基类
  └─ 监听对象事件、读取 Data、转发前端意图
```

### 核心原则

- **UI 不是 Entity Component**：UI 是独立的 Godot Control 节点树
- **UI 也不是框架外特例**：它在框架角色上属于 Controller / Adapter 层
- **UI 持有 Object / Entity 引用**：通过绑定对象访问 Data 和 Events
- **事件驱动更新**：UI 监听 Object / Entity 事件响应式更新
- **前端意图只转发，不沉淀玩法状态**：按钮点击、面板操作应转成 Command / owner API / request
- **UIBase 子类模式**：所有 UI 继承 UIBase

### 快速开始

1. 创建 UI 场景（Godot Control 节点）
2. 创建 UI 脚本继承 UIBase
3. 在 UIBase 中绑定 Entity
4. 监听 Entity.Events 更新 UI

## 3. 职责边界

| UI 做 | UI 不做 |
| ---- | ---- |
| 显示 Entity 数据 | 存储业务状态 |
| 监听事件更新显示 | 修改 Entity.Data |
| 响应用户输入 | 包含游戏逻辑 |
