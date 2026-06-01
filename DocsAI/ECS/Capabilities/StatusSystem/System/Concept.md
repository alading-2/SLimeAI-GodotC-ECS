# Status 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/StatusSystem/
> relatedDocs: DocsAI/ECS/Capabilities/StatusSystem/Concepts/实体状态效果系统设计.md
> lastReviewed: 2026-06-01

## 1. 一句话定位

实体状态效果系统，使用 StatusDefinition + StatusInstance + StatusCollection + StatusControllerComponent 架构，支持多来源状态叠加（如多源无敌独立过期）。

## 2. 核心概念

### 架构

```
StatusDefinition（状态定义）
  ├─ 持续时间
  ├─ 效果（修改器、事件触发）
  └─ 叠加规则

StatusInstance（状态实例）
  ├─ 剩余时间
  ├─ 来源 Entity
  └─ 修改器列表

StatusCollection（状态集合）
  └─ 管理 Entity 上所有活跃的状态实例

StatusControllerComponent（状态控制器组件）
  └─ 挂在 Entity 上，驱动状态生命周期
```

### 不使用行为树或单一状态机

状态效果是独立的、可叠加的、有独立过期时间的。多源无敌（来自不同技能的无敌效果）各自独立计时。

## 3. 职责边界

| Status 做 | Status 不做 |
| ---- | ---- |
| 状态效果生命周期管理 | 具体战斗逻辑 |
| 状态叠加与优先级 | 伤害计算 |
| 修改器应用与回滚 | UI 显示 |

## 4. 依赖关系

- **Data**：状态配置和运行时状态
- **Entity.Events**：状态变更事件
- **Modifier**：数据修改器
- **TimerManager**：状态持续时间
