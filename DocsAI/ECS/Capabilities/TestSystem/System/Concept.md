# TestSystem 概念

> status: current
> sourcePaths: Src/ECS/Capabilities/TestSystem/System/
> relatedDocs: DocsAI/ECS/Capabilities/TestSystem/System/Usage.md
> lastReviewed: 2026-06-01

## 1. 一句话定位

TestSystem 是运行时调试系统，模块化架构，集成 SystemRegistry，支持运行时查看 Entity 状态、Data、Ability、Feature 等信息。

## 2. 核心概念

### 模块化架构

```
TestSystem
  ├─ Attribute（属性查看）
  ├─ Ability（技能调试）
  ├─ System Monitor（系统监控）
  ├─ Info（信息展示）
  ├─ ResourceCatalog（资源目录）
  └─ Spawn（生成调试）
```

### 与 SystemRegistry 集成

TestSystem 通过 SystemRegistry 注册，可按需启用/禁用各调试模块。

### 鼠标选择集成

选中 Entity 后显示其详细调试信息。

## 3. 职责边界

| TestSystem 做 | TestSystem 不做 |
| ---- | ---- |
| 运行时调试信息展示 | 修改游戏状态 |
| Entity 状态查看 | 自动化测试 |
| Data/Ability/Feature 调试 | 性能分析（归专用工具） |
