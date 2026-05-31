# UI 概念

> status: current
> sourcePaths: Src/ECS/UI/
> relatedDocs: DocsAI/ECS/UI/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

UI 采用 Binding Pattern：UI 是独立的 View 层（Godot Control 节点树），通过持有 Entity 引用并监听 Entity.Events 驱动显示，不是 Entity Component。

## 2. 核心概念

### Binding Pattern

```
Entity（Model 层）
  ├─ Data：业务数据
  └─ Events：事件总线
       ↕ 绑定
UI（View 层）
  ├─ UIBase：UI 基类
  └─ 监听 Entity.Events 更新显示
```

### 核心原则

- **UI 不是 Entity Component**：UI 是独立的 Godot Control 节点树
- **UI 持有 Entity 引用**：通过 Entity 引用访问 Data 和 Events
- **事件驱动更新**：UI 监听 Entity.Events 响应式更新
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
